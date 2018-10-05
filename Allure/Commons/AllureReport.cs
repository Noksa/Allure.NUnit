using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        private TestFixture _currentSuite;
        private IList<ITest> _currentSuiteTests;

        [SetUp]
        protected void StartAllureLogging()
        {
            // For started tests renew info in props
            var testUuids = TestExecutionContext.CurrentContext.CurrentTest.GetCurrentTestRunInfo();
            var currentTest = TestExecutionContext.CurrentContext.CurrentTest;
            currentTest.SetProp(AllureConstants.TestContainerUuid,
                    testUuids.ContainerUuid)
                .SetProp(AllureConstants.TestUuid,
                    testUuids.TestUuid);
            TestExecutionContext.CurrentContext.CurrentResult.SetResult(ResultState.Success);
            AllureStorage.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            AllureLifecycle.CurrentTestActionsInException = null;
            currentTest.SetCurrentTestSetupFixture(new FixtureResult {suiteUuid = "null"});
            currentTest.SetCurrentTestTearDownFixture(new FixtureResult {suiteUuid = "null"});
            AllureLifecycle.Instance.Storage.ClearStepContext();
            AllureLifecycle.Instance.Storage.CurrentThreadStepContext.AddLast(testUuids.TestUuid);
            AllureStorage.TempContext =
                new LinkedList<string>(AllureLifecycle.Instance.Storage.CurrentThreadStepContext.ToList());

            AllureLifecycle.Instance.UpdateTestContainer(
                testUuids.ContainerUuid,
                x => x.start = AllureLifecycle.ToUnixTimestamp());
            AllureLifecycle.Instance.UpdateTestCase(testUuids.TestUuid,
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
                    AllureLifecycle.Instance.StopFixture(q =>
                        q.status = ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status));
                    AllureLifecycle.Instance.Storage.CurrentThreadStepContext =
                        new LinkedList<string>(AllureStorage.TempContext);
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
                    currentTest.GetPropAsString(AllureConstants.TestContainerUuid),
                    currentTest.GetPropAsString(AllureConstants.FixtureUuid)));
            }
        }

        [OneTimeSetUp]
        public void AllureOneTimeSetUp()
        {
            AllureStorage.MainThreadId = Thread.CurrentThread.ManagedThreadId;
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
                AllureLifecycle.Instance.Storage.ClearStepContext();
                AllureLifecycle.Instance.Storage.CurrentThreadStepContext.AddLast(testTupleInfo.TestUuid);
                ReportHelper.StopAllureLogging(testTupleInfo.testResult,
                    testTupleInfo.TestUuid, testTupleInfo.TestContainerUuid, _currentSuite, false, null);
            }

            if (string.IsNullOrEmpty(TestContext.CurrentContext.Test.MethodName) &&
                TestContext.CurrentContext.Result.Outcome.Site == FailureSite.SetUp &&
                AllureLifecycle.Instance.Config.Allure.AllowEmptySuites &&
                TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
                lock (ReportHelper.Locker)
                {
                    AllureStorage.MainThreadId = Thread.CurrentThread.ManagedThreadId;
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
                        AllureLifecycle.Instance.Storage.ClearStepContext();
                        AllureLifecycle.Instance.Storage.CurrentThreadStepContext.AddLast(
                            test.GetPropAsString(AllureConstants.TestUuid));
                        AllureLifecycle.Instance.StartStepAndStopIt(null, "The test was not started",
                            Status.failed);
                        AllureLifecycle.Instance.UpdateTestCase(
                            test.GetPropAsString(AllureConstants.TestUuid),
                            x =>
                            {
                                x.labels.RemoveAll(q => q.name == "thread");
                                x.labels.Add(Label.Thread());
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