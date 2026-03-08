namespace APP.Core.Models
{
    // 这些状态都是流程上的提示层，不等同于页面路由本身。
    // These states describe flow overlays, not page routes.
    public enum OverlayState
    {
        None,
        PutMeDown,
        HaveABreak,
        YouDidIt,
        BackToFocus
    }
}
