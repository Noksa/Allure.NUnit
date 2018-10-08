using System;
using System.Collections.Generic;
using System.Threading;
using Allure.Commons.Model;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Allure.Commons.Helpers
{
    internal class StepsWorker
    {
        private static readonly object FixtureCloseLocker = new object();

        [ThreadStatic] internal static int MainThreadId;

        internal static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

        internal LinkedList<string> GetCurrentStepContext()
        {
            var (stageType, _) = AllureStageHelper.GetCurrentAllureStageInfo();
            return TestExecutionContext.CurrentContext.CurrentTest.GetStepContext(stageType);
        }

        public void StartStep(string uuid)
        {
            GetCurrentStepContext().AddFirst(uuid);
        }

        public void StopStep()
        {
            GetCurrentStepContext().RemoveFirst();
        }

        public string GetRootStep()
        {
            return GetCurrentStepContext().Last?.Value;
        }

        public string GetCurrentStep()
        {
            var stageInfo = AllureStageHelper.GetCurrentAllureStageInfo();
            var currentTestOrSuite = TestExecutionContext.CurrentContext.CurrentTest;
            if (stageInfo.Stage == AllureStageHelper.MethodType.OneTimeSetup)
            {
                var oneTimeSetUpFixture =
                    currentTestOrSuite.GetCurrentOneTimeSetupFixture();
                if (oneTimeSetUpFixture == null)
                {
                    currentTestOrSuite.SetProp(AllureConstants.OneTimeSetupContext, new LinkedList<string>());
                    oneTimeSetUpFixture = new FixtureResult {name = stageInfo.MethodName};
                    foreach (var (_, testContainerUuid, fixtureUuid) in currentTestOrSuite.GetAllTestsInFixture())
                        AllureLifecycle.Instance.StartBeforeFixture(testContainerUuid,
                            $"{fixtureUuid}-onetimesetup",
                            oneTimeSetUpFixture);
                    oneTimeSetUpFixture.suiteUuid =
                        currentTestOrSuite.GetPropAsString(AllureConstants.FixtureUuid);
                    currentTestOrSuite.SetCurrentOneTimeSetupFixture(oneTimeSetUpFixture);
                }
            }
            else
            {
                lock (FixtureCloseLocker)
                {
                    var otsf = currentTestOrSuite.GetCurrentOneTimeSetupFixture();
                    if (otsf != null)
                    {
                        AllureLifecycle.Instance.StopFixture(otsf.suiteUuid + "-onetimesetup",
                            q => q.status =
                                ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status));
                        currentTestOrSuite.SetCurrentOneTimeSetupFixture(new FixtureResult {suiteUuid = "null"});
                    }
                }
            }

            if (stageInfo.Stage == AllureStageHelper.MethodType.Setup)
                if (currentTestOrSuite.GetCurrentTestSetupFixture() == null)
                {
                    lock (FixtureCloseLocker)
                    {
                        if (currentTestOrSuite.GetCurrentTestSetupFixture() == null)
                        {
                            currentTestOrSuite.SetProp(AllureConstants.TestSetupContext, new ThreadLocal<LinkedList<string>>(true));
                            var context =
                                currentTestOrSuite.GetProp(AllureConstants.TestSetupContext) as
                                    ThreadLocal<LinkedList<string>>;
                            context.Value = new LinkedList<string>();
                            var fixture = new FixtureResult {name = stageInfo.MethodName};

                            AllureLifecycle.Instance.StartBeforeFixture(
                                currentTestOrSuite.GetPropAsString(AllureConstants.TestContainerUuid),
                                $"{currentTestOrSuite.GetPropAsString(AllureConstants.TestUuid)}-before",
                                fixture);
                            currentTestOrSuite.SetCurrentTestSetupFixture(fixture);
                        }
                    }
                }

            if (stageInfo.Stage == AllureStageHelper.MethodType.TestBody)
                if (currentTestOrSuite.GetCurrentTestSetupFixture() != null)
                {
                    lock (FixtureCloseLocker)
                    {
                        if (currentTestOrSuite.GetCurrentTestSetupFixture() != null)
                        {
                            AllureLifecycle.Instance.StopFixture(
                                $"{currentTestOrSuite.GetPropAsString(AllureConstants.TestUuid)}-before", q =>
                                {
                                    var status =
                                        ReportHelper.GetNUnitStatus(
                                            TestContext.CurrentContext.Result.Outcome.Status);
                                    q.status = status;
                                });
                            currentTestOrSuite.SetCurrentTestSetupFixture(new FixtureResult {suiteUuid = "null"});
                        }
                    }
                }

            if (stageInfo.Stage == AllureStageHelper.MethodType.Teardown)
                if (currentTestOrSuite.GetCurrentTestTearDownFixture() == null)
                {
                    lock (FixtureCloseLocker)
                    {
                        if (currentTestOrSuite.GetCurrentTestTearDownFixture() == null)
                        {
                            currentTestOrSuite.SetProp(AllureConstants.TestTearDownContext,
                                new ThreadLocal<LinkedList<string>>(true));
                            var context =
                                currentTestOrSuite.GetProp(AllureConstants.TestTearDownContext) as
                                    ThreadLocal<LinkedList<string>>;
                            context.Value = new LinkedList<string>();
                            if (currentTestOrSuite.GetCurrentTestSetupFixture() != null)
                            {
                                AllureLifecycle.Instance.StopFixture(
                                    $"{currentTestOrSuite.GetPropAsString(AllureConstants.TestUuid)}-before", q =>
                                    {
                                        var status =
                                            ReportHelper.GetNUnitStatus(
                                                TestContext.CurrentContext.Result.Outcome.Status);
                                        q.status = status;
                                    });
                                currentTestOrSuite.SetCurrentTestSetupFixture(
                                    new FixtureResult {suiteUuid = "null"});
                            }


                            var fixture = new FixtureResult {name = stageInfo.MethodName};
                            AllureLifecycle.Instance.StartAfterFixture(
                                currentTestOrSuite.GetPropAsString(AllureConstants.TestContainerUuid),
                                $"{currentTestOrSuite.GetProp(AllureConstants.TestUuid)}-after",
                                fixture);
                            currentTestOrSuite.SetCurrentTestTearDownFixture(fixture);
                        }
                    }
                }

            if (stageInfo.Stage == AllureStageHelper.MethodType.OneTimeTearDown)
                if (currentTestOrSuite.GetCurrentOneTimeTearDownFixture() == null)
                {
                    lock (FixtureCloseLocker)
                    {
                        if (currentTestOrSuite.GetCurrentOneTimeTearDownFixture() == null)
                        {
                            currentTestOrSuite.SetProp(AllureConstants.OneTimeTearDownContext,
                                new LinkedList<string>());
                            var oneTimeTearDownFixture = new FixtureResult {name = stageInfo.MethodName};
                            foreach (var (_, testContainerUuid, fixtureUuid) in
                                currentTestOrSuite.GetAllTestsInFixture())
                                AllureLifecycle.Instance.StartAfterFixture(testContainerUuid,
                                    $"{fixtureUuid}-onetimeteardown",
                                    oneTimeTearDownFixture);
                            oneTimeTearDownFixture.suiteUuid =
                                currentTestOrSuite.GetPropAsString(AllureConstants.FixtureUuid);
                            currentTestOrSuite.SetCurrentOneTimeTearDownFixture(oneTimeTearDownFixture);
                        }
                    }
                }

            var stepId = GetCurrentStepContext().First?.Value;
            return stepId;
        }

        public void AddSubStep(string parentUuid, string uuid, StepResult stepResult)
        {
            TestExecutionContext.CurrentContext.CurrentTest.Storage().Put(uuid, stepResult);
            TestExecutionContext.CurrentContext.CurrentTest.Storage().Get<ExecutableItem>(parentUuid).steps
                .Add(stepResult);
        }
    }
}