using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Allure.Commons.Helpers
{
    internal static class BeforeAfterFixturesHelper
    {
        internal static MethodType GetTypeOfCurrentTestMethod()
        {
            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();
            var frame = frames?.FirstOrDefault(w =>
                w.GetMethod().DeclaringType != typeof(AllureReport) && w.GetMethod().GetCustomAttributes().Any(attr =>
                    attr is SetUpAttribute || attr is OneTimeSetUpAttribute || attr is TearDownAttribute ||
                    attr is OneTimeTearDownAttribute));
            if (frame == null) return MethodType.TestBody;
            var method = frame.GetMethod();
            var attrs = method.GetCustomAttributes();
            foreach (var attribute in attrs)
                switch (attribute)
                {
                    case SetUpAttribute setup:
                        return MethodType.Setup;
                    case OneTimeSetUpAttribute oneTimeSetup:
                        return MethodType.OneTimeSetup;
                    case TearDownAttribute tearDown:
                        return MethodType.Teardown;
                    case OneTimeTearDownAttribute oneTimeTearDown:
                        return MethodType.OneTimeTearDown;
                }

            return MethodType.TestBody;
        }

        internal enum MethodType
        {
            Setup,
            Teardown,
            OneTimeSetup,
            OneTimeTearDown,
            TestBody
        }
    }
}