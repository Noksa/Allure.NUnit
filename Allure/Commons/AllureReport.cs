using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        private TestFixture _currentSuite;
        private IList<ITest> _currentSuiteTests;

        [SetUp]
        protected void StartAllureLogging()
        {
            TestExecutionContext.CurrentContext.CurrentResult.SetResult(ResultState.Success);
            StepsWorker.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            AllureLifecycle.CurrentTestActionsInException = null;

            // For started tests renew info in props

            var (testUuid, containerUuid) = TestExecutionContext.CurrentContext.CurrentTest.GetCurrentTestRunInfo();
            var currentTest = TestExecutionContext.CurrentContext.CurrentTest;
            currentTest.SetProp(AllureConstants.TestContainerUuid,
                    containerUuid)
                .SetProp(AllureConstants.TestUuid,
                    testUuid)
                .SetCurrentTestSetupFixture(new FixtureResult {suiteUuid = "null"})
                .SetCurrentTestTearDownFixture(new FixtureResult {suiteUuid = "null"})
                .SetProp(AllureConstants.TestBodyContext, new ThreadLocal<LinkedList<string>>(true));
            var bodyContext = currentTest.GetProp(AllureConstants.TestBodyContext) as ThreadLocal<LinkedList<string>>;
            bodyContext.Value = new LinkedList<string>();
            currentTest.GetStepContext(AllureStageHelper.MethodType.TestBody).AddLast(testUuid);

            AllureLifecycle.Instance.UpdateTestContainer(
                containerUuid,
                x => x.start = AllureLifecycle.ToUnixTimestamp());
            AllureLifecycle.Instance.UpdateTestCase(testUuid,
                x =>
                {
                    x.start = AllureLifecycle.ToUnixTimestamp();
                    x.labels.RemoveAll(q => q.name == "thread");
                    x.labels.Add(Label.Thread());
                });
        }


        [TearDown]
        protected void StopAllureLogging()
        {
            var currentTest = TestExecutionContext.CurrentContext.CurrentTest;
            try
            {
                if (currentTest.GetCurrentTestTearDownFixture() != null)
                {
                    AllureLifecycle.Instance.StopFixture($"{currentTest.GetProp(AllureConstants.TestUuid)}-after", q =>
                        q.status = ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status));
                }

                var testresult = TestContext.CurrentContext.Result;
                currentTest.SetProp(AllureConstants.TestResult, testresult);
                AllureLifecycle.Instance.UpdateTestCase(currentTest.GetPropAsString(AllureConstants.TestUuid),
                    x => x.stop = AllureLifecycle.ToUnixTimestamp());
                AllureLifecycle.Instance.UpdateTestContainer(
                    currentTest.GetPropAsString(AllureConstants.TestContainerUuid),
                    x => x.stop = AllureLifecycle.ToUnixTimestamp());
            }
            finally
            {
                currentTest.AddTestToCompletedInFixture((
                    TestContext.CurrentContext.Result,
                    currentTest.GetPropAsString(AllureConstants.TestUuid),
                    currentTest.GetPropAsString(AllureConstants.TestContainerUuid), currentTest));
            }
        }

        [OneTimeSetUp]
        public void AllureOneTimeSetUp()
        {
            StepsWorker.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            _currentSuite = (TestFixture) TestExecutionContext.CurrentContext.CurrentTest;
            OutLogger.LogInProgress(
                $"Entering OneTimeSetup for \"{_currentSuite.GetProp(AllureConstants.FixtureUuid)}\"");
            var allTests = ReportHelper.GetAllTestsInSuite(_currentSuite);
            _currentSuiteTests = allTests;
            OutLogger.LogInProgress(
                $"Exiting OneTimeSetup for \"{_currentSuite.GetProp(AllureConstants.FixtureUuid)}\"");
        }

        [OneTimeTearDown]
        public void AllureOneTimeTearDown()
        {
            OutLogger.LogInProgress(
                $"Entering OneTimeTearDown for \"{_currentSuite.GetProp(AllureConstants.FixtureUuid)}\"");

            if (_currentSuite.GetCurrentOneTimeTearDownFixture() != null)
            {
                string testMsg = null;
                string testStackTrace = null;
                var status = ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status);

                if (TestContext.CurrentContext.Result.Outcome.Site == FailureSite.TearDown)
                {
                    testStackTrace = TestContext.CurrentContext.Result.StackTrace;
                    testMsg = TestContext.CurrentContext.Result.Message;
                }

                AllureLifecycle.Instance.StopFixture(
                    _currentSuite.GetCurrentOneTimeTearDownFixture().suiteUuid + "-onetimeteardown",
                    q =>
                    {
                        q.status = status;
                        q.statusDetails.message = testMsg;
                        q.statusDetails.trace = testStackTrace;
                    });
                TestExecutionContext.CurrentContext.CurrentTest.SetCurrentOneTimeTearDownFixture(new FixtureResult
                    {suiteUuid = "null"});
            }

            foreach (var testTupleInfo in _currentSuite.GetCompletedTestsInFixture())
            {
                ReportHelper.StopAllureLogging(testTupleInfo.testResult,
                    testTupleInfo.TestUuid, testTupleInfo.TestContainerUuid, _currentSuite, false, null);
            }

            if (string.IsNullOrEmpty(TestContext.CurrentContext.Test.MethodName) &&
                TestContext.CurrentContext.Result.Outcome.Site == FailureSite.SetUp &&
                AllureLifecycle.Instance.Config.Allure.AllowEmptySuites &&
                TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
                lock (ReportHelper.Locker)
                {
                    StepsWorker.MainThreadId = Thread.CurrentThread.ManagedThreadId;
                    foreach (var test in _currentSuiteTests)
                    {
                        test.SetProp(AllureConstants.TestResult, TestContext.CurrentContext.Result);
                        AllureLifecycle.Instance.UpdateTestContainer(
                            test.GetPropAsString(AllureConstants.TestContainerUuid)
                            ,
                            x => x.start = AllureLifecycle.ToUnixTimestamp());
                        AllureLifecycle.Instance.UpdateTestCase(test.GetPropAsString(AllureConstants.TestUuid),
                            x => { x.start = AllureLifecycle.ToUnixTimestamp(); });
                        Thread.Sleep(5);

                        AllureLifecycle.Instance.UpdateTestCase(
                            test.GetPropAsString(AllureConstants.TestUuid),
                            x =>
                            {
                                x.labels.RemoveAll(q => q.name == "thread");
                                x.labels.Add(Label.Thread());
                                x.descriptionHtml = "<font size=24, color=red>The test was not started</font>";
                            });
                        ReportHelper.StopAllureLogging(TestContext.CurrentContext.Result,
                            test.GetPropAsString(AllureConstants.TestUuid),
                            test.GetPropAsString(AllureConstants.TestContainerUuid), _currentSuite, true, null);
                    }
                }

            else ReportHelper.AddIgnoredTestsToReport(_currentSuite);

            lock (ReportHelper.Locker)
            {
                EnvironmentBuilder.BuildEnvFile(new DirectoryInfo(AllureLifecycle.Instance.ResultsDirectory));
            }

            OutLogger.LogInProgress(
                $"Exiting OneTimeTearDown for \"{_currentSuite.GetProp(AllureConstants.FixtureUuid)}\"");
        }

        protected string MakeGoodErrorMsg(string errorMsg)
        {
            return ReportHelper.MakeGoodErrorMsg(errorMsg);
        }
    }
}