using APP.Core.Config;

namespace APP.Core.Services
{
    public enum FlipOrientation
    {
        Unknown,
        FaceDown,
        FaceUp
    }

    public sealed class FlipDetector
    {
        private readonly double _faceDownThreshold;
        private readonly double _faceUpThreshold;
        private readonly double _liftedFromDownThreshold;
        private readonly TimeSpan _debounce;//防抖
        private readonly TimeSpan _cooldown;//冷却，避免连续翻转

        private FlipOrientation _confirmedOrientation = FlipOrientation.Unknown;
        private FlipOrientation _candidateOrientation = FlipOrientation.Unknown;
        private DateTimeOffset _candidateStart;
        private DateTimeOffset _lastEventTime = DateTimeOffset.MinValue;

        public event Action? FlipDownDetected;
        public event Action? FlipUpDetected;

        public FlipOrientation CurrentOrientation => _confirmedOrientation;

        public FlipDetector()
            : this(
                  Constants .FaceDownThreshold,
                  Constants.FaceUpThreshold,
                  Constants.LiftedFromDownThreshold,
                  Constants.FlipDebounce,
                  Constants.FlipDebounce) // FlipCooldown 与 FlipDebounce 相同
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

        public void OnAccelerometerReading(double zNormalized, DateTimeOffset timestamp)
        {
            // perform a rough classification of the raw Z-values
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
                // 先记成候选态，不急着翻转；手机挪一下、桌子震一下都可能抖出假信号。
                // Hold it as a candidate first instead of flipping immediately; a small nudge or desk vibration can fake the signal.
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

            // 手机从桌面拿起来时，Z 轴常常会先停在这个区间，不一定马上冲到完整的 face-up。
            // When the phone is lifted from the desk, the Z axis often settles here before it reaches a full face-up reading.
            if ((_confirmedOrientation == FlipOrientation.FaceDown
                 || _confirmedOrientation == FlipOrientation.Unknown)
                && z > _liftedFromDownThreshold)
                return FlipOrientation.FaceUp;
            // easier to recognise the action of ‘picking up a mobile phone’ 
            
            if (z >= _faceUpThreshold)
                return FlipOrientation.FaceUp;

            return FlipOrientation.Unknown;
        }
    }
}
