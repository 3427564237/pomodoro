using ObjCRuntime;
using UIKit;

namespace APP
{
    public class Program
    {
        static void Main(string[] args)
        {
            // MacCatalyst 也沿用 UIKit 的启动方式，先把进程入口交给 AppDelegate。
            // MacCatalyst uses the same UIKit-style startup path, so the process entry point forwards straight to AppDelegate.
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
