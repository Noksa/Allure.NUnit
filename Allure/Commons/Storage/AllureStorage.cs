using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons.Helpers;
using Allure.Commons.Model;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Allure.Commons.Storage
{
    internal class AllureStorage
    {
        [ThreadStatic] private static int _mainThreadId;
        [ThreadStatic] internal static LinkedList<string> TempContext;
        [ThreadStatic] internal static FixtureResult CurrentTestTearDownFixture;
        [ThreadStatic] internal static FixtureResult CurrentTestSetUpFixture;

        private readonly ThreadLocal<LinkedList<string>> _currentThreadStepContext =
            new ThreadLocal<LinkedList<string>>(true);

        private readonly ConcurrentDictionary<string, object> _storage = new ConcurrentDictionary<string, object>();

        internal static int MainThreadId
        {
            get => _mainThreadId;
            set => _mainThreadId = value;
        }

        internal static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;


        internal LinkedList<string> CurrentThreadStepContext
        {
            get
            {
                if (IsMainThread)
                {
                    if (_currentThreadStepContext.Value != null) return _currentThreadStepContext.Value;
                    _currentThreadStepContext.Value = new LinkedList<string>();
                    return _currentThreadStepContext.Value;
                }

                var sc = _currentThreadStepContext.Values.FirstOrDefault(
                    _ => _.Any(d => d == TestContext.CurrentContext.Test.ID));

                var rootStep = sc.Last.Value;
                if (_currentThreadStepContext.Value == null || _currentThreadStepContext.Value.Count == 0)
                {
                    _currentThreadStepContext.Value = new LinkedList<string>();
                    _currentThreadStepContext.Value.AddLast(rootStep);
                }

                if (_currentThreadStepContext.Value.Last.Value !=
                    TestContext.CurrentContext.Test.ID)
                {
                    _currentThreadStepContext.Value.Clear();
                    _currentThreadStepContext.Value.AddLast(rootStep);
                }

                return _currentThreadStepContext.Value;
            }

            set => _currentThreadStepContext.Value = value;
        }

        public T Get<T>(string uuid)
        {
            return (T) _storage[uuid];
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
            CurrentThreadStepContext?.Clear();
            var scs = _currentThreadStepContext?.Values.Where(_ =>
                _.Count != 0 && _.Last.Value == TestContext.CurrentContext.Test.ID);
            if (scs == null) return;
            var list = scs.ToList();
            if (!list.Any()) return;
            foreach (var nestedList in list)
                try
                {
                    nestedList.Clear();
                }
                catch (Exception)
                {
                    //nothing
                }
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
            var methodType = BeforeAfterFixturesHelper.GetTypeOfCurrentTestMethod();

            if (methodType == BeforeAfterFixturesHelper.MethodType.Setup)
            {
                if (CurrentTestSetUpFixture == null)
                {
                    CurrentTestSetUpFixture = new FixtureResult(); // dummy
                    AllureLifecycle.Instance.StartBeforeFixture(
                        TestExecutionContext.CurrentContext.CurrentTest.Properties
                            .Get(AllureConstants.TestContainerUuid)
                            .ToString(),
                        $"{TestExecutionContext.CurrentContext.CurrentTest.Properties.Get(AllureConstants.TestUuid)}-before",
                        CurrentTestSetUpFixture);
                }
            }

            else if (methodType == BeforeAfterFixturesHelper.MethodType.TestBody)
            {
                if (CurrentTestSetUpFixture != null)
                {
                    AllureLifecycle.Instance.StopFixture(q =>
                    {
                        var status = ReportHelper.GetNunitStatus(TestContext.CurrentContext.Result.Outcome.Status);
                        q.status = status;
                    });
                    CurrentTestSetUpFixture = null;
                    CurrentThreadStepContext.Clear();
                    CurrentThreadStepContext = TempContext;
                }
            }

            else if (methodType == BeforeAfterFixturesHelper.MethodType.Teardown)
            {
                if (CurrentTestTearDownFixture == null)
                {
                    if (CurrentTestSetUpFixture != null)
                    {
                        AllureLifecycle.Instance.StopFixture(q =>
                        {
                            var status = ReportHelper.GetNunitStatus(TestContext.CurrentContext.Result.Outcome.Status);
                            q.status = status;
                        });
                        CurrentTestSetUpFixture = null;
                        CurrentThreadStepContext.Clear();
                        CurrentThreadStepContext = TempContext;
                    }

                    TempContext = new LinkedList<string>(CurrentThreadStepContext.ToList());

                    CurrentTestTearDownFixture = new FixtureResult(); // dummy
                    AllureLifecycle.Instance.StartAfterFixture(
                        TestExecutionContext.CurrentContext.CurrentTest.Properties
                            .Get(AllureConstants.TestContainerUuid)
                            .ToString(),
                        $"{TestExecutionContext.CurrentContext.CurrentTest.Properties.Get(AllureConstants.TestUuid)}-after",
                        CurrentTestTearDownFixture);
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