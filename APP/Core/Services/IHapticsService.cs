namespace APP.Core.Services
{
    // 震动能力单独抽接口，方便后面按平台差异慢慢补细节。
    // Haptics sit behind an interface so platform-specific behavior can evolve without leaking into the core flow.
    public interface IHapticsService
    {
        void PlayShortBuzz();
        void StartContinuousVibration();
        void StopVibration();
    }
}
