using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Allure.Commons.Helpers;
using Allure.Commons.Model;
using Allure.Commons.Storage;
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
        private TestFixture _currentSuite;

        [SetUp]
        protected void StartAllureLogging()
        {
            ReportHelper.StartAllureLogging(TestExecutionContext.CurrentContext.CurrentTest, _currentSuite);
            AllureStorage.CurrentTestTearDownFixture = null;
            AllureStorage.CurrentTestSetUpFixture = null;
            AllureStorage.TempContext =
                new LinkedList<string>(AllureLifecycle.Instance.Storage.CurrentThreadStepContext);
        }

        [TearDown]
        protected void StopAllureLogging()
        {
            if (AllureStorage.CurrentTestTearDownFixture != null)
            {
                AllureLifecycle.Instance.StopFixture(q =>
                    q.status = ReportHelper.GetNunitStatus(TestContext.CurrentContext.Result.Outcome.Status));
                AllureLifecycle.Instance.Storage.CurrentThreadStepContext = AllureStorage.TempContext;
                ReportHelper.StopAllureLogging(TestExecutionContext.CurrentContext.CurrentTest);
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _currentSuite = (TestFixture) TestExecutionContext.CurrentContext.CurrentTest;
            var allTests = ReportHelper.GetAllTestsInSuite(_currentSuite);
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
                    ReportHelper.StartAllureLogging(test, _currentSuite);
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