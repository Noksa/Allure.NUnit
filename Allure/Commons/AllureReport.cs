using System;
using Allure.Commons.Helpers;
using Allure.Commons.Model;
using Allure.NUnit.Attributes;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Allure.Commons
{
    [AllureFixture]
    public abstract class AllureReport
    {
        [ThreadStatic] internal static string TestUuid;

        [SetUp]
        protected void StartAllureLogging()
        {
            ReportHelper.StartAllureLogging(TestContext.CurrentContext.Test.Name, TestContext.CurrentContext.Test.ID,
                TestContext.CurrentContext.Test.FullName, TestContext.CurrentContext.Test.ClassName,
                TestContext.CurrentContext.Test.MethodName);
        }

        [TearDown]
        protected void StopAllureLogging()
        {
            ReportHelper.StopAllureLogging(TestExecutionContext.CurrentContext.CurrentTest.Method.MethodInfo,
                TestContext.CurrentContext.Result.Message, TestContext.CurrentContext.Result.StackTrace,
                TestContext.CurrentContext.Result.Outcome.Status);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (string.IsNullOrEmpty(TestContext.CurrentContext.Test.MethodName) && TestContext.CurrentContext.Result.Outcome.Site == FailureSite.SetUp && AllureLifecycle.Instance.Config.Allure.AllowEmptySuites && TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                var fixture = new TestResultContainer
                {
                    uuid = TestContext.CurrentContext.Test.ID,
                    name = TestContext.CurrentContext.Test.ClassName
                };
                AllureLifecycle.Instance.StartTestContainer(fixture);
                foreach (var test in AllureLifecycle.Instance._currentSuiteTests)
                {
                    ReportHelper.StartAllureLogging(test.Name, test.Id, test.FullName, test.ClassName, test.MethodName);
                    var uuid = $"{Guid.NewGuid():N}";
                    AllureLifecycle.Instance.StartStep("The test was not started", uuid);
                    AllureLifecycle.Instance.UpdateStep(q =>
                    {
                        q.status = Status.failed;
                        q.stop = AllureLifecycle.ToUnixTimestamp();
                    });
                    ReportHelper.StopAllureLogging(test.Method.MethodInfo, TestContext.CurrentContext.Result.Message,
                        TestContext.CurrentContext.Result.StackTrace, TestContext.CurrentContext.Result.Outcome.Status);
                }

                AllureLifecycle.Instance.WriteTestContainer(TestContext.CurrentContext.Test.ID);
            }
        }

        protected string MakeGoodErrorMsg(string errorMsg)
        {
            return ReportHelper.MakeGoodErrorMsg(errorMsg);
        }
    }
}