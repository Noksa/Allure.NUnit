using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons.Helpers;
using Allure.Commons.Model;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Logger = Allure.Commons.Helpers.Logger;

namespace Allure.Commons.Storage
{
    internal class AllureStorage
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
                if (_currentThreadStepContext.Value != null) return _currentThreadStepContext.Value;
                _currentThreadStepContext.Value = new LinkedList<string>();
                return _currentThreadStepContext.Value;
            }

            set => _currentThreadStepContext.Value = value;
        }

        public T Get<T>(string uuid)
        {
            try
            {
                return (T) _storage[uuid];
            }
            catch (KeyNotFoundException e)
            {
                var msg = $"{e.Message} \nTried to find the key: {uuid}";
                var newEx = new KeyNotFoundException(msg);
                throw newEx;
            }
        }

        public T Put<T>(string uuid, T item)
        {
            return (T) _storage.GetOrAdd(uuid, item);
        }

        public T Remove<T>(string uuid)
        {
            _storage.TryRemove(uuid, out var value);
            return (T) value;
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

        public string GetRootStep()
        {
            return CurrentThreadStepContext.Last?.Value;
        }

        public string GetCurrentStep()
        {
            if (!ReportHelper.IsSpecFlow)
            {
                var methodInfo = BeforeAfterFixturesHelper.GetTypeOfCurrentMethodInTest();
                var currentTestOrSuite = TestExecutionContext.CurrentContext.CurrentTest;
                if (methodInfo.Keys.First() == BeforeAfterFixturesHelper.MethodType.OneTimeSetup)
                {
                    var oneTimeSetUpFixture =
                        currentTestOrSuite.GetCurrentOneTimeSetupFixture();
                    if (oneTimeSetUpFixture == null)
                    {
                        oneTimeSetUpFixture = new FixtureResult {name = methodInfo.Values.First()};
                        foreach (var tuple in currentTestOrSuite.GetAllTestsInFixture())
                            AllureLifecycle.Instance.StartBeforeFixture(tuple.TestContainerUuid,
                                $"{tuple.FixtureUuid}-onetimesetup",
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
                            TempContext = new LinkedList<string>(CurrentThreadStepContext.ToList());
                            AllureLifecycle.Instance.StopFixture(otsf.suiteUuid + "-onetimesetup",
                                q => q.status =
                                    ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status));
                            CurrentThreadStepContext = new LinkedList<string>(TempContext);
                            currentTestOrSuite.SetCurrentOneTimeSetupFixture(new FixtureResult {suiteUuid = "null"});
                        }
                    }
                }

                if (methodInfo.Keys.First() == BeforeAfterFixturesHelper.MethodType.Setup)
                    if (currentTestOrSuite.GetCurrentTestSetupFixture() == null)
                    {
                        var fixture = new FixtureResult {name = methodInfo.Values.First()};

                        AllureLifecycle.Instance.StartBeforeFixture(
                            currentTestOrSuite.GetPropAsString(AllureConstants.TestContainerUuid),
                            $"{currentTestOrSuite.GetPropAsString(AllureConstants.TestUuid)}-before",
                            fixture);
                        currentTestOrSuite.SetCurrentTestSetupFixture(fixture);
                    }

                if (methodInfo.Keys.First() == BeforeAfterFixturesHelper.MethodType.TestBody)
                    if (currentTestOrSuite.GetCurrentTestSetupFixture() != null)
                    {
                        AllureLifecycle.Instance.StopFixture(q =>
                        {
                            var status = ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status);
                            q.status = status;
                        });
                        currentTestOrSuite.SetCurrentTestSetupFixture(new FixtureResult {suiteUuid = "null"});
                        ClearStepContext();
                        CurrentThreadStepContext = TempContext;
                    }

                if (methodInfo.Keys.First() == BeforeAfterFixturesHelper.MethodType.Teardown)
                    if (currentTestOrSuite.GetCurrentTestTearDownFixture() == null)
                    {
                        if (currentTestOrSuite.GetCurrentTestSetupFixture() != null)
                        {
                            AllureLifecycle.Instance.StopFixture(q =>
                            {
                                var status =
                                    ReportHelper.GetNUnitStatus(TestContext.CurrentContext.Result.Outcome.Status);
                                q.status = status;
                                q.name = "TearDown";
                            });
                            currentTestOrSuite.SetCurrentTestSetupFixture(new FixtureResult {suiteUuid = "null"});
                            ClearStepContext();
                            CurrentThreadStepContext = TempContext;
                        }

                        TempContext = new LinkedList<string>(CurrentThreadStepContext.ToList());

                        var fixture = new FixtureResult {name = methodInfo.Values.First()};
                        AllureLifecycle.Instance.StartAfterFixture(
                            TestExecutionContext.CurrentContext.CurrentTest.Properties
                                .Get(AllureConstants.TestContainerUuid)
                                .ToString(),
                            $"{TestExecutionContext.CurrentContext.CurrentTest.Properties.Get(AllureConstants.TestUuid)}-after",
                            fixture);
                        currentTestOrSuite.SetCurrentTestTearDownFixture(fixture);
                    }

                if (methodInfo.Keys.First() == BeforeAfterFixturesHelper.MethodType.OneTimeTearDown)
                    if (currentTestOrSuite.GetCurrentOneTimeTearDownFixture() == null)
                    {
                        TempContext = new LinkedList<string>(CurrentThreadStepContext);
                        var oneTimeTearDownFixture = new FixtureResult {name = methodInfo.Values.First()};
                        foreach (var tuple in TestExecutionContext.CurrentContext.CurrentTest.GetAllTestsInFixture())
                            AllureLifecycle.Instance.StartAfterFixture(tuple.TestContainerUuid,
                                $"{tuple.FixtureUuid}-onetimeteardown",
                                oneTimeTearDownFixture);
                        oneTimeTearDownFixture.suiteUuid = TestContext.CurrentContext.Test.Properties
                            .Get(AllureConstants.FixtureUuid)
                            .ToString();
                        currentTestOrSuite.SetCurrentOneTimeTearDownFixture(oneTimeTearDownFixture);
                    }
            }

            var stepId = CurrentThreadStepContext.First?.Value;
            return stepId;
        }

        public void AddStep(string parentUuid, string uuid, StepResult stepResult)
        {
            Put(uuid, stepResult);
            Get<ExecutableItem>(parentUuid).steps.Add(stepResult);
        }
    }
}