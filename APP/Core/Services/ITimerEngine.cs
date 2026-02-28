namespace APP.Core.Services
{
    public interface ITimerEngine
    {
        event Action<TimeSpan>? Tick;
        event Action? Finished;
        void Start(TimeSpan duration);
        void Pause();
        void Resume();
        void Stop();
        bool IsRunning { get; }
        TimeSpan Remaining { get; }
    }
}
