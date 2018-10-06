using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons.Model;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Allure.Commons.Helpers
{
    internal class StepsWorker
    {
        private static readonly object OneTimeSetupLocker = new object();
        [ThreadStatic] internal static LinkedList<string> TempContext;

        private readonly ThreadLocal<LinkedList<string>> _currentThreadStepContext =
            new ThreadLocal<LinkedList<string>>(true);

        private readonly ConcurrentDictionary<string, object> _storage = new ConcurrentDictionary<string, object>();

        [field: ThreadStatic] internal static int MainThreadId { get; set; }

        internal static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

        internal LinkedList<string> CurrentThreadStepContext
        {
            get
            {
                if (_currentThreadStepContext.Value == null)
                {
                    _currentThreadStepContext.Value = new LinkedList<string>();
                }

                return _currentThreadStepContext.Value;
            }

            set => _currentThreadStepContext.Value = value;
        }

        public void ClearStepContext()
        {
            CurrentThreadStepContext.Clear();
        }

        public void StartStep(string uuid)
        {
            CurrentThreadStepContext.AddFirst(uuid);
        }

        public void StopStep()
        {
            CurrentThreadStepContext.RemoveFirst();
        }

        public string RootStep
        {
            get
            {
                var stageInfo = AllureStageHelper.GetCurrentAllureStageInfo();
                return CurrentThreadStepContext.Last?.Value;
            }
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
                    oneTimeSetUpFixture = new FixtureResult { name = stageInfo.MethodName };
                    foreach (var (_, TestContainerUuid, FixtureUuid) in currentTestOrSuite.GetAllTestsInFixture())
                        AllureLifecycle.Instance.StartBeforeFixture(TestContainerUuid,
                            $"{FixtureUuid}-onetimesetup",
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
                        TempContext = new LinkedList<string>(Enumerable.ToList<string>(CurrentThreadStepContext));
                        AllureLifecycle.Instance.StopFixture(otsf.suiteUuid + "-onetimesetup",
                            q => q.status =
                                ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status));
                        CurrentThreadStepContext = new LinkedList<string>(TempContext);
                        currentTestOrSuite.SetCurrentOneTimeSetupFixture(new FixtureResult { suiteUuid = "null" });
                    }
                }
            }

            if (stageInfo.Stage == AllureStageHelper.MethodType.Setup)
                if (currentTestOrSuite.GetCurrentTestSetupFixture() == null)
                {
                    var fixture = new FixtureResult { name = stageInfo.MethodName };

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
                    currentTestOrSuite.SetCurrentTestSetupFixture(new FixtureResult { suiteUuid = "null" });
                    ClearStepContext();
                    CurrentThreadStepContext = TempContext;
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
                        currentTestOrSuite.SetCurrentTestSetupFixture(new FixtureResult { suiteUuid = "null" });
                        ClearStepContext();
                        CurrentThreadStepContext = TempContext;
                    }

                    TempContext = new LinkedList<string>(Enumerable.ToList<string>(CurrentThreadStepContext));

                    var fixture = new FixtureResult { name = stageInfo.MethodName };
                    AllureLifecycle.Instance.StartAfterFixture(
                        currentTestOrSuite.GetPropAsString(AllureConstants.TestContainerUuid),
                        $"{currentTestOrSuite.GetProp(AllureConstants.TestUuid)}-after",
                        fixture);
                    currentTestOrSuite.SetCurrentTestTearDownFixture(fixture);
                }

            if (stageInfo.Stage == AllureStageHelper.MethodType.OneTimeTearDown)
                if (currentTestOrSuite.GetCurrentOneTimeTearDownFixture() == null)
                {
                    TempContext = new LinkedList<string>(CurrentThreadStepContext);
                    var oneTimeTearDownFixture = new FixtureResult { name = stageInfo.MethodName };
                    foreach (var (_, TestContainerUuid, FixtureUuid) in currentTestOrSuite.GetAllTestsInFixture())
                        AllureLifecycle.Instance.StartAfterFixture(TestContainerUuid,
                            $"{FixtureUuid}-onetimeteardown",
                            oneTimeTearDownFixture);
                    oneTimeTearDownFixture.suiteUuid = currentTestOrSuite.GetPropAsString(AllureConstants.FixtureUuid);
                    currentTestOrSuite.SetCurrentOneTimeTearDownFixture(oneTimeTearDownFixture);
                }

            var stepId = CurrentThreadStepContext.First?.Value;
            return stepId;
        }

        public void AddSubStep(string parentUuid, string uuid, StepResult stepResult)
        {
            TestExecutionContext.CurrentContext.CurrentTest.Storage().Put(uuid, stepResult);
            TestExecutionContext.CurrentContext.CurrentTest.Storage().Get<ExecutableItem>(parentUuid).steps.Add(stepResult);
        }
    }
}