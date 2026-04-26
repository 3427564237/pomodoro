using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.OS;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AColor = Android.Graphics.Color;
using AEditText = Android.Widget.EditText;
using ASwitch = Android.Widget.Switch;
using ATextView = Android.Widget.TextView;
using AView = Android.Views.View;
using MauiButton = Microsoft.Maui.Controls.Button;
using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiImageButton = Microsoft.Maui.Controls.ImageButton;
using MauiSwitch = Microsoft.Maui.Controls.Switch;

namespace APP.Platforms.Android
{
    // AI-assisted code starts here.
    // This Android-specific bridge was drafted with AI help, then reviewed and adapted.
    // It handles MAUI handler mapping, native tinting, and ripple feedback for visual polish.
    public static class AndroidThemeBridge
    {
        private static readonly MauiColor StatusBarColor = MauiColor.FromArgb("#FFFFFF");
        private static MauiColor _activeColor = MauiColor.FromArgb("#2EC4B6");
        private static MauiColor _softColor = MauiColor.FromArgb("#CBF3F0");
        private static bool _handlersConfigured;

        public static void ConfigureHandlers()
        {
            if (_handlersConfigured)
                return;

            _handlersConfigured = true;

            ButtonHandler.Mapper.AppendToMapping("FlipThemeFeedback", (handler, _) =>
            {
                ApplyButtonFeedback(handler.VirtualView, handler.PlatformView);
            });

            ImageButtonHandler.Mapper.AppendToMapping("FlipThemeFeedback", (handler, _) =>
            {
                ApplyImageButtonFeedback(handler.VirtualView, handler.PlatformView);
            });

            EntryHandler.Mapper.AppendToMapping("FlipThemeFeedback", (handler, _) =>
            {
                ApplyEntryTint(handler.PlatformView);
            });

            SwitchHandler.Mapper.AppendToMapping("FlipThemeTint", (handler, _) =>
            {
                ApplySwitchTint(handler.VirtualView, handler.PlatformView);
            });
        }

        public static void Apply(MauiColor activeColor, MauiColor softColor)
        {
            _activeColor = activeColor;
            _softColor = softColor;

            RefreshSystemBars();
            RefreshCurrentPageFeedback();
        }

        public static void RefreshSystemBars()
        {
            MainActivity.ApplySystemBarColor(StatusBarColor);
        }

        private static void RefreshCurrentPageFeedback()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var root = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (root != null)
                    ApplyFeedback(root);

                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && root != null)
                {
                    dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () => ApplyFeedback(root));
                    dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () => ApplyFeedback(root));
                }
            });
        }

        private static void ApplyFeedback(Element element)
        {
            if (element is VisualElement visual && visual.Handler?.PlatformView is AView platformView)
            {
                if (element is MauiButton button)
                    ApplyButtonFeedback(button, platformView);
                else if (element is MauiImageButton imageButton)
                    ApplyImageButtonFeedback(imageButton, platformView);
                else if (element is MauiSwitch switchControl)
                    ApplySwitchTint(switchControl, platformView);
                else if (platformView is AEditText editText)
                    ApplyEntryTint(editText);
            }

            if (element is IElementController controller)
            {
                foreach (var child in controller.LogicalChildren)
                {
                    if (child is Element childElement)
                        ApplyFeedback(childElement);
                }
            }
        }

        private static void ApplyButtonFeedback(object? virtualView, AView platformView)
        {
            if (virtualView is not MauiButton button)
            {
                ApplyBoundedRipple(platformView, _activeColor);
                return;
            }

            var rippleColor = ResolveButtonFeedbackColor(button);

            if (button.BackgroundColor != null)
                platformView.BackgroundTintList = ColorStateList.ValueOf(button.BackgroundColor.ToPlatform());

            if (platformView is ATextView textView && button.TextColor != null)
                textView.SetTextColor(button.TextColor.ToPlatform());

            ApplyBoundedRipple(platformView, rippleColor);
        }

        private static void ApplyImageButtonFeedback(object? virtualView, AView platformView)
        {
            var rippleColor = _activeColor;

            if (virtualView is MauiImageButton imageButton)
            {
                if (imageButton.BackgroundColor != null && imageButton.BackgroundColor.Alpha > 0.05f)
                    rippleColor = imageButton.BackgroundColor;
            }

            ApplyBoundedRipple(platformView, rippleColor);
        }

        private static MauiColor ResolveButtonFeedbackColor(MauiButton button)
        {
            if (button.BackgroundColor != null && button.BackgroundColor.Alpha > 0.35f)
                return button.BackgroundColor;

            if (button.TextColor != null)
                return button.TextColor;

            return _activeColor;
        }

        private static void ApplyBoundedRipple(AView view, MauiColor color)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
                return;

            if (view.Width <= 0 || view.Height <= 0)
            {
                view.Post(() => ApplyBoundedRipple(view, color));
                return;
            }

            var mask = new GradientDrawable();
            mask.SetShape(ShapeType.Rectangle);
            mask.SetColor(AColor.White);
            mask.SetCornerRadius(Math.Min(view.Width, view.Height) / 2f);

            view.Foreground = new RippleDrawable(
                ColorStateList.ValueOf(WithAlpha(color, 0.14f).ToPlatform()),
                null,
                mask);
        }

        private static void ApplyEntryTint(AEditText editText)
        {
            editText.BackgroundTintList = ColorStateList.ValueOf(_activeColor.ToPlatform());
            editText.SetHighlightColor(WithAlpha(_activeColor, 0.24f).ToPlatform());
        }

        private static void ApplySwitchTint(object? virtualView, object platformView)
        {
            if (virtualView is MauiSwitch switchControl)
            {
                switchControl.OnColor = _softColor;
                switchControl.ThumbColor = _activeColor;
            }

            var checkedState = new[] { global::Android.Resource.Attribute.StateChecked };
            var uncheckedState = new[] { -global::Android.Resource.Attribute.StateChecked };
            var states = new[] { checkedState, uncheckedState };

            var thumbTint = new ColorStateList(
                states,
                [
                    _activeColor.ToPlatform(),
                    _activeColor.ToPlatform()
                ]);

            var trackTint = new ColorStateList(
                states,
                [
                    _softColor.ToPlatform(),
                    MauiColor.FromArgb("#E1E1E1").ToPlatform()
                ]);

            var type = platformView.GetType();
            type.GetProperty("ThumbTintList")?.SetValue(platformView, thumbTint);
            type.GetProperty("TrackTintList")?.SetValue(platformView, trackTint);
            type.GetProperty("ButtonTintList")?.SetValue(platformView, thumbTint);

            if (platformView is ASwitch nativeSwitch)
            {
                nativeSwitch.ThumbTintList = thumbTint;
                nativeSwitch.TrackTintList = trackTint;
                nativeSwitch.ThumbDrawable?.Mutate()?.SetTintList(thumbTint);
                nativeSwitch.TrackDrawable?.Mutate()?.SetTintList(trackTint);
                nativeSwitch.JumpDrawablesToCurrentState();
            }

            if (platformView is AView view)
            {
                view.RefreshDrawableState();
                view.Invalidate();
            }
        }

        private static MauiColor WithAlpha(MauiColor color, float alpha)
            => MauiColor.FromRgba(color.Red, color.Green, color.Blue, alpha);
    } // AI-assisted code ends here.
}
