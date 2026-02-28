using APP.Core.Config;

namespace APP.Core.Services
{
    public enum FlipOrientation
    {
        Unknown,
        FaceDown,
        FaceUp
    }

    /// <summary>
    /// Detects face-up and face-down transitions from accelerometer readings.
    /// edge-triggered FlipDown / FlipUp events with debounce and cooldown.
    /// 
    /// 
    public sealed class FlipDetector
    {
        private readonly double _faceDownThreshold;
        private readonly double _faceUpThreshold;
        private readonly TimeSpan _debounce;
        private readonly TimeSpan _cooldown;

        private FlipOrientation _confirmedOrientation = FlipOrientation.Unknown;
        private FlipOrientation _candidateOrientation = FlipOrientation.Unknown;
        private DateTimeOffset _candidateStart;
        private DateTimeOffset _lastEventTime = DateTimeOffset.MinValue;

        public event Action? FlipDownDetected;
        public event Action? FlipUpDetected;

        /// <summary>Current confirmed orientation (after debounce).
        public FlipOrientation CurrentOrientation => _confirmedOrientation;

        public FlipDetector()
            : this(
                  InteractionTimings.FaceDownThreshold,
                  InteractionTimings.FaceUpThreshold,
                  InteractionTimings.FlipDebounce,
                  InteractionTimings.FlipCooldown)
        { }

        public FlipDetector(double faceDownThreshold, double faceUpThreshold,
                            TimeSpan debounce, TimeSpan cooldown)
        {
            _faceDownThreshold = faceDownThreshold;
            _faceUpThreshold = faceUpThreshold;
            _debounce = debounce;
            _cooldown = cooldown;
        }

        /// <summary>
        /// Feed a new accelerometer reading. <paramref name="zNormalized"/> is the
        /// Z-axis value in multiples of g (roughly –1 = face-down, +1 = face-up).
        /// <paramref name="timestamp"/> must be monotonically increasing.
        /// 
        public void OnAccelerometerReading(double zNormalized, DateTimeOffset timestamp)
        {
            var raw = ClassifyRaw(zNormalized);

            if (raw == FlipOrientation.Unknown)
            {
                // In the dead-zone — reset candidate but keep confirmed.
                _candidateOrientation = FlipOrientation.Unknown;
                return;
            }

            if (raw == _confirmedOrientation)
            {
                // Already in this state — no transition needed.
                _candidateOrientation = FlipOrientation.Unknown;
                return;
            }

            // We have a new candidate that differs from confirmed.
            if (raw != _candidateOrientation)
            {
                _candidateOrientation = raw;
                _candidateStart = timestamp;
                return;
            }

            // Same candidate persisted — check debounce window.
            if (timestamp - _candidateStart < _debounce)
                return;

            // Debounce satisfied — check cooldown from last event.
            if (timestamp - _lastEventTime < _cooldown)
                return;

            // Transition confirmed.
            _confirmedOrientation = raw;
            _candidateOrientation = FlipOrientation.Unknown;
            _lastEventTime = timestamp;

            if (raw == FlipOrientation.FaceDown)
                FlipDownDetected?.Invoke();
            else
                FlipUpDetected?.Invoke();
        }

        /// <summary>Reset internal state (e.g. when sensor is stopped/restarted).
        public void Reset()
        {
            _confirmedOrientation = FlipOrientation.Unknown;
            _candidateOrientation = FlipOrientation.Unknown;
            _lastEventTime = DateTimeOffset.MinValue;
        }

        private FlipOrientation ClassifyRaw(double z)
        {
            if (z <= _faceDownThreshold)
                return FlipOrientation.FaceDown;
            if (z >= _faceUpThreshold)
                return FlipOrientation.FaceUp;
            return FlipOrientation.Unknown;
        }
    }
}
