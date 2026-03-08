using ObjCRuntime;
using UIKit;

namespace APP
{
    public class Program
    {
        static void Main(string[] args)
        {
            // iOS 的原生入口，职责很单一：把控制权交给 AppDelegate。
            // This is the native iOS entry point; its whole job is to hand control over to AppDelegate.
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
