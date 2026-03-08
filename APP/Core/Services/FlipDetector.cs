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
    /// 
    public sealed class FlipDetector
    {
        private readonly double _faceDownThreshold;
        private readonly double _faceUpThreshold;
        private readonly double _liftedFromDownThreshold;
        private readonly TimeSpan _debounce;
        private readonly TimeSpan _cooldown;

        private FlipOrientation _confirmedOrientation = FlipOrientation.Unknown;
        private FlipOrientation _candidateOrientation = FlipOrientation.Unknown;
        private DateTimeOffset _candidateStart;
        private DateTimeOffset _lastEventTime = DateTimeOffset.MinValue;

        public event Action? FlipDownDetected;
        public event Action? FlipUpDetected;

        /// <summary>
        /// Gets the current debounced orientation.
        /// 
        public FlipOrientation CurrentOrientation => _confirmedOrientation;

        public FlipDetector()
            : this(
                  InteractionTimings.FaceDownThreshold,
                  InteractionTimings.FaceUpThreshold,
                  InteractionTimings.LiftedFromDownThreshold,
                  InteractionTimings.FlipDebounce,
                  InteractionTimings.FlipCooldown)
        { }

        public FlipDetector(double faceDownThreshold, double faceUpThreshold,
                            double liftedFromDownThreshold,
                            TimeSpan debounce, TimeSpan cooldown)
        {
            _faceDownThreshold = faceDownThreshold;
            _faceUpThreshold = faceUpThreshold;
            _liftedFromDownThreshold = liftedFromDownThreshold;
            _debounce = debounce;
            _cooldown = cooldown;
        }

        /// <summary>
        /// Processes a new accelerometer reading.
        /// 
        public void OnAccelerometerReading(double zNormalized, DateTimeOffset timestamp)
        {
            var raw = ClassifyRaw(zNormalized);

            if (raw == FlipOrientation.Unknown)
            {
                _candidateOrientation = FlipOrientation.Unknown;
                return;
            }

            if (raw == _confirmedOrientation)
            {
                _candidateOrientation = FlipOrientation.Unknown;
                return;
            }

            if (raw != _candidateOrientation)
            {
                _candidateOrientation = raw;
                _candidateStart = timestamp;
                return;
            }

            if (timestamp - _candidateStart < _debounce)
                return;

            if (timestamp - _lastEventTime < _cooldown)
                return;

            _confirmedOrientation = raw;
            _candidateOrientation = FlipOrientation.Unknown;
            _lastEventTime = timestamp;

            if (raw == FlipOrientation.FaceDown)
                FlipDownDetected?.Invoke();
            else
                FlipUpDetected?.Invoke();
        }

        /// <summary>
        /// Resets the detector state.
        /// 
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

            // Treat upright readings as face-up when starting from unknown
            // or after a confirmed face-down state.
            if ((_confirmedOrientation == FlipOrientation.FaceDown
                 || _confirmedOrientation == FlipOrientation.Unknown)
                && z > _liftedFromDownThreshold)
                return FlipOrientation.FaceUp;

            if (z >= _faceUpThreshold)
                return FlipOrientation.FaceUp;

            return FlipOrientation.Unknown;
        }
    }
}
