using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace Allure.Commons.Helpers
{
    internal static class BeforeAfterFixturesHelper
    {
        internal static Dictionary<MethodType, string> GetTypeOfCurrentMethodInTest()
        {
            var dict = new Dictionary<MethodType, string>();
            var stackTrace = new StackTrace(1);
            var frames = stackTrace.GetFrames();
            //if (AllureLifecycle.Instance.Config.Allure.DebugMode)
            //{
            //    if (frames != null)
            //    {
            //        var sb = new StringBuilder();
            //        foreach (var stackFrame in frames)
            //        {
            //            sb.AppendLine($"{stackFrame}");
            //        }
            //        Logger.LogInProgress($"Current Allure step frames:\n {sb}");
            //        sb.Clear();
            //    }
            //}
            var frame = frames?.FirstOrDefault(w =>
            {
                try
                {
                    var temp = w.GetMethod().CustomAttributes;
                }
                catch (Exception)
                {
                    return false;
                }

                var result = w.GetMethod().DeclaringType != typeof(AllureReport) &&
                             w.GetMethod().GetCustomAttributes().Any(
                                 attr =>
                                     attr is SetUpAttribute || attr is OneTimeSetUpAttribute ||
                                     attr is TearDownAttribute ||
                                     attr is OneTimeTearDownAttribute);
                return result;
            });
            if (frame == null)
            {
                dict.Add(MethodType.TestBody, "");
                return dict;
            }

            var method = frame.GetMethod();
            var methodName = method.Name;
            var attrs = method.GetCustomAttributes();
            var methodType = MethodType.TestBody;
            foreach (var attribute in attrs)
                switch (attribute)
                {
                    case SetUpAttribute _:
                        methodType = MethodType.Setup;
                        break;
                    case OneTimeSetUpAttribute _:
                        methodType = MethodType.OneTimeSetup;
                        break;
                    case TearDownAttribute _:
                        methodType = MethodType.Teardown;
                        break;
                    case OneTimeTearDownAttribute _:
                        methodType = MethodType.OneTimeTearDown;
                        break;
                }
            dict.Add(methodType, methodName);
            return dict;
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