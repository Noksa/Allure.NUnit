using NUnit.Framework;

namespace Allure.Commons.Helpers
{
    internal static class Logger
    {
        private static readonly object LogLocker = new object();

        internal static void LogInProgress(string content)
        {
            if (!AllureLifecycle.Instance.Config.Allure.DebugMode) return;
            lock (LogLocker)
            {
                TestContext.Progress.WriteLine("\n===");
                TestContext.Progress.WriteLine(content);
                TestContext.Progress.WriteLine("\n===");
            }
        }
    }
}