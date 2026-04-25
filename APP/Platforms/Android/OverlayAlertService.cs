using APP.Core.Config;
using APP.Core.Models;
using APP.Core.StateMachine;
using Android.App;
using Android.Content;
using AContext = Android.Content.Context;
using Application = Android.App.Application;
using Build = Android.OS.Build;
using BuildVersionCodes = Android.OS.BuildVersionCodes;
using VibrationEffect = Android.OS.VibrationEffect;
using Vibrator = Android.OS.Vibrator;
using VibratorManager = Android.OS.VibratorManager;

namespace APP.Platforms.Android
{
    public sealed class OverlayAlertService : IDisposable
    {
        private const string OverlayAlertsChannelId = "overlay_alerts";
        private const string OverlayAlertsChannelName = "Overlay Alerts";
        private const int OverlayAlertNotificationId = 2001;
        private static readonly long[] NotificationVibrationPattern = [0, 160, 120, 160, 160];
        private static readonly TimeSpan StrongPulse = TimeSpan.FromMilliseconds(450);
        private static readonly TimeSpan StrongPause = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan WeakPulse = TimeSpan.FromMilliseconds(120);
        private static readonly TimeSpan WeakPause = TimeSpan.FromMilliseconds(900);

        private readonly PomodoroStateMachine _coordinator;
        private readonly object _lock = new();
        private readonly NotificationManager? _notificationManager;

        private CancellationTokenSource? _vibrationCts;

        public OverlayAlertService(PomodoroStateMachine coordinator)
        {
            _coordinator = coordinator;
            _coordinator.ConfigChanged += OnConfigChanged;
            _coordinator.OverlayChanged += OnOverlayChanged;
            MainActivity.ForegroundChanged += OnForegroundChanged;
            _notificationManager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
            EnsureNotificationChannel();
        }

        private void OnConfigChanged(RuntimeConfig config)
        {
            if (!config.VibrationEnabled)
            {
                StopVibration();
                return;
            }

            if (MainActivity.IsAppInForeground)
                PlayVibrationForOverlay(_coordinator.CurrentOverlay);
        }

        private void OnForegroundChanged(bool isForeground)
        {
            if (isForeground)
            {
                CancelBackgroundNotification();
                PlayVibrationForOverlay(_coordinator.CurrentOverlay);
                return;
            }

            StopVibration();
        }

        private void OnOverlayChanged(OverlayState overlay)
        {
            if (overlay == OverlayState.None)
            {
                StopVibration();
                if (MainActivity.IsAppInForeground)
                    CancelBackgroundNotification();
                return;
            }

            if (MainActivity.IsAppInForeground)
            {
                CancelBackgroundNotification();
                PlayVibrationForOverlay(overlay);
                return;
            }

            StopVibration();

            if (overlay == OverlayState.HaveABreak
                || overlay == OverlayState.BackToFocus
                || overlay == OverlayState.YouDidIt)
            {
                ShowBackgroundNotification(overlay);
            }
        }

        private void PlayVibrationForOverlay(OverlayState overlay)
        {
            StopVibration();

            if (!_coordinator.Config.VibrationEnabled)
                return;

            if (overlay == OverlayState.PutMeDown)
            {
                StartLoop(StrongPulse, StrongPause);
                return;
            }

            if (overlay == OverlayState.HaveABreak
                || overlay == OverlayState.BackToFocus
                || overlay == OverlayState.YouDidIt)
            {
                StartLoop(WeakPulse, WeakPause);
            }
        }

        private void EnsureNotificationChannel()
        {
            if (_notificationManager == null || Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            var channel = new NotificationChannel(
                OverlayAlertsChannelId,
                OverlayAlertsChannelName,
                NotificationImportance.High)
            {
                Description = "Pomodoro overlay alerts while the app is in the background."
            };
            channel.EnableVibration(true);
            channel.SetVibrationPattern(NotificationVibrationPattern);

            _notificationManager.CreateNotificationChannel(channel);
        }

        private void ShowBackgroundNotification(OverlayState overlay)
        {
            var notificationManager = _notificationManager;
            if (notificationManager == null || MainActivity.IsAppInForeground || !CanPostNotifications())
                return;

            var (title, body) = overlay switch
            {
                OverlayState.HaveABreak => ("Have a break", "Focus finished. Break started."),
                OverlayState.BackToFocus => ("Back to focus", "Break finished. Focus has started."),
                OverlayState.YouDidIt => ("You did it", "Pomodoro session complete."),
                _ => (string.Empty, string.Empty)
            };

            if (string.IsNullOrEmpty(title))
                return;

            var launchIntent = new Intent(Application.Context, typeof(MainActivity));
            launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop | ActivityFlags.NewTask);

            var pendingIntent = PendingIntent.GetActivity(
                Application.Context,
                0,
                launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            Notification.Builder builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? new Notification.Builder(Application.Context, OverlayAlertsChannelId)
                : new Notification.Builder(Application.Context);

            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                builder.SetVibrate(NotificationVibrationPattern);

            var notification = builder
                .SetContentTitle(title)
                .SetContentText(body)
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true)
                .SetShowWhen(true)
                .SetPriority((int)NotificationPriority.High)
                .Build();

            notificationManager.Notify(OverlayAlertNotificationId, notification);
        }

        private void CancelBackgroundNotification()
        {
            _notificationManager?.Cancel(OverlayAlertNotificationId);
        }

        private static bool CanPostNotifications()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                return true;

            return Application.Context.CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) == global::Android.Content.PM.Permission.Granted;
        }

        private void StartLoop(TimeSpan pulse, TimeSpan pause)
        {
            var cts = new CancellationTokenSource();
            CancellationTokenSource? previous;

            lock (_lock)
            {
                previous = _vibrationCts;
                _vibrationCts = cts;
            }

            CancelAndDispose(previous);
            _ = RunLoopAsync(pulse, pause, cts.Token);
        }

        private async Task RunLoopAsync(TimeSpan pulse, TimeSpan pause, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        VibrateForeground(pulse);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[OverlayAlertService] Vibrate failed: {ex}");
                    }

                    await Task.Delay(pulse + pause, token);
                }
            }
            catch (System.OperationCanceledException)
            {
            }
        }

        private void StopVibration()
        {
            CancellationTokenSource? cts;

            lock (_lock)
            {
                cts = _vibrationCts;
                _vibrationCts = null;
            }

            CancelAndDispose(cts);

            try
            {
                CancelForegroundVibration();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OverlayAlertService] Cancel failed: {ex}");
            }
        }

        private static void CancelAndDispose(CancellationTokenSource? cts)
        {
            if (cts == null) return;

            try
            {
                cts.Cancel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OverlayAlertService] Cancel token failed: {ex}");
            }

            cts.Dispose();
        }

        private static Vibrator? GetVibrator()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var manager = Application.Context.GetSystemService(AContext.VibratorManagerService) as VibratorManager;
                return manager?.DefaultVibrator;
            }

            return Application.Context.GetSystemService(AContext.VibratorService) as Vibrator;
        }

        private static void VibrateForeground(TimeSpan duration)
        {
            var vibrator = GetVibrator();
            if (vibrator == null || !vibrator.HasVibrator)
                return;

            var milliseconds = Math.Max(1, (long)duration.TotalMilliseconds);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                vibrator.Vibrate(VibrationEffect.CreateOneShot(milliseconds, VibrationEffect.DefaultAmplitude));
                return;
            }

            vibrator.Vibrate(milliseconds);
        }

        private static void CancelForegroundVibration()
        {
            GetVibrator()?.Cancel();
        }

        public void Dispose()
        {
            _coordinator.OverlayChanged -= OnOverlayChanged;
            _coordinator.ConfigChanged -= OnConfigChanged;
            MainActivity.ForegroundChanged -= OnForegroundChanged;
            StopVibration();
        }
    }
}
