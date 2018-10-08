using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons.Model;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Allure.Commons.Helpers
{
    internal class StepsWorker
    {
        private static readonly object OneTimeSetupLocker = new object();
        [ThreadStatic] internal static LinkedList<string> TempContext;


        [field: ThreadStatic] internal static int MainThreadId { get; set; }

        internal static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

        internal LinkedList<string> GetCurrentStepContext(ITest test)
        {
            var (stageType, _) = AllureStageHelper.GetCurrentAllureStageInfo();
            return test.GetStepContext(stageType);
        }

        public void ClearStepContext(ITest test)
        {
            GetCurrentStepContext(test).Clear();
        }

        public void StartStep(ITest test, string uuid)
        {
            GetCurrentStepContext(test).AddFirst(uuid);
        }

        public void StopStep(ITest test)
        {
            GetCurrentStepContext(test).RemoveFirst();
        }

        public string GetRootStep(ITest test)
        {
             return GetCurrentStepContext(test).Last?.Value;
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
                lock (OneTimeSetupLocker)
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
                    var fixture = new FixtureResult {name = stageInfo.MethodName};

                    AllureLifecycle.Instance.StartBeforeFixture(
                        currentTestOrSuite.GetPropAsString(AllureConstants.TestContainerUuid),
                        $"{currentTestOrSuite.GetPropAsString(AllureConstants.TestUuid)}-before",
                        fixture);
                    currentTestOrSuite.SetCurrentTestSetupFixture(fixture);
                }

            if (stageInfo.Stage == AllureStageHelper.MethodType.TestBody)
                if (currentTestOrSuite.GetCurrentTestSetupFixture() != null)
                {
                    AllureLifecycle.Instance.StopFixture(q =>
                    {
                        var status = ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status);
                        q.status = status;
                    });
                    currentTestOrSuite.SetCurrentTestSetupFixture(new FixtureResult {suiteUuid = "null"});
                    //ClearStepContext();
                }

            if (stageInfo.Stage == AllureStageHelper.MethodType.Teardown)
                if (currentTestOrSuite.GetCurrentTestTearDownFixture() == null)
                {
                    if (currentTestOrSuite.GetCurrentTestSetupFixture() != null)
                    {
                        AllureLifecycle.Instance.StopFixture(q =>
                        {
                            var status = ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status);
                            q.status = status;
                            q.name = "TearDown";
                        });
                        currentTestOrSuite.SetCurrentTestSetupFixture(new FixtureResult {suiteUuid = "null"});
                        //ClearStepContext();
                    }


                    var fixture = new FixtureResult {name = stageInfo.MethodName};
                    AllureLifecycle.Instance.StartAfterFixture(
                        currentTestOrSuite.GetPropAsString(AllureConstants.TestContainerUuid),
                        $"{currentTestOrSuite.GetProp(AllureConstants.TestUuid)}-after",
                        fixture);
                    currentTestOrSuite.SetCurrentTestTearDownFixture(fixture);
                }

            if (stageInfo.Stage == AllureStageHelper.MethodType.OneTimeTearDown)
                if (currentTestOrSuite.GetCurrentOneTimeTearDownFixture() == null)
                {
                    var oneTimeTearDownFixture = new FixtureResult {name = stageInfo.MethodName};
                    foreach (var (_, testContainerUuid, fixtureUuid) in currentTestOrSuite.GetAllTestsInFixture())
                        AllureLifecycle.Instance.StartAfterFixture(testContainerUuid,
                            $"{fixtureUuid}-onetimeteardown",
                            oneTimeTearDownFixture);
                    oneTimeTearDownFixture.suiteUuid = currentTestOrSuite.GetPropAsString(AllureConstants.FixtureUuid);
                    currentTestOrSuite.SetCurrentOneTimeTearDownFixture(oneTimeTearDownFixture);
                }

            var stepId = GetCurrentStepContext(currentTestOrSuite).First?.Value;
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