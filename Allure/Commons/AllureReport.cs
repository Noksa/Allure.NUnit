using System;
using System.Collections.Generic;
using Allure.Commons.Helpers;
using Allure.Commons.Model;
using Allure.NUnit.Attributes;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

// ReSharper disable CollectionNeverUpdated.Local
#pragma warning disable 649

namespace Allure.Commons
{
    [AllureFixture]
    public abstract class AllureReport
    {
        private IList<ITest> _currentSuiteTests;

        [SetUp]
        protected void StartAllureLogging()
        {
            ReportHelper.StartAllureLogging(TestExecutionContext.CurrentContext.CurrentTest);
        }

        [TearDown]
        protected void StopAllureLogging()
        {
            ReportHelper.StopAllureLogging(TestExecutionContext.CurrentContext.CurrentTest);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var suite = TestExecutionContext.CurrentContext.CurrentTest;
            var allTests = ReportHelper.GetAllTestsInSuite(suite);
            _currentSuiteTests = allTests;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (string.IsNullOrEmpty(TestContext.CurrentContext.Test.MethodName) &&
                TestContext.CurrentContext.Result.Outcome.Site == FailureSite.SetUp &&
                AllureLifecycle.Instance.Config.Allure.AllowEmptySuites &&
                TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                var fixture = new TestResultContainer
                {
                    uuid = TestContext.CurrentContext.Test.ID,
                    name = TestContext.CurrentContext.Test.ClassName
                };
                AllureLifecycle.Instance.StartTestContainer(fixture);
                foreach (var test in _currentSuiteTests)
                {
                    ReportHelper.StartAllureLogging(test);
                    var uuid = $"{Guid.NewGuid():N}";
                    AllureLifecycle.Instance.StartStep("The test was not started", uuid);
                    AllureLifecycle.Instance.UpdateStep(q =>
                    {
                        q.status = Status.failed;
                        q.stop = AllureLifecycle.ToUnixTimestamp();
                    });
                    ReportHelper.StopAllureLogging(test);
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