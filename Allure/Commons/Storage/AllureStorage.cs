using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons.Model;
using NUnit.Framework;

namespace Allure.Commons.Storage
{
    internal class AllureStorage
    {
        [ThreadStatic] private static int _mainThreadId;

        private readonly ThreadLocal<LinkedList<string>> _currentThreadStepContext =
            new ThreadLocal<LinkedList<string>>(true);

        private readonly ConcurrentDictionary<string, object> _storage = new ConcurrentDictionary<string, object>();

        internal static int MainThreadId
        {
            get => _mainThreadId;
            set => _mainThreadId = value;
        }

        internal static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;


        private LinkedList<string> CurrentThreadStepContext
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
                if (sc == null)
                {
                    _currentThreadStepContext.Value = new LinkedList<string>();
                    _currentThreadStepContext.Value.AddFirst("Fake");
                    sc = _currentThreadStepContext.Value;
                    _storage.GetOrAdd("Fake", new StepResult());
                }
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
            CurrentThreadStepContext.Clear();
            var scs = _currentThreadStepContext.Values.Where(_ =>
                _.Count != 0 && _.Last.Value == TestContext.CurrentContext.Test.ID);
            foreach (var list in scs) list.Clear();
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
            var stepId = CurrentThreadStepContext.First?.Value;
            if (!string.IsNullOrEmpty(stepId)) return stepId;
            CurrentThreadStepContext.AddFirst("Fake");
            _storage.GetOrAdd("Fake", new StepResult());
            return CurrentThreadStepContext.First.Value;
        }

        public void AddStep(string parentUuid, string uuid, StepResult stepResult)
        {
            Put(uuid, stepResult);
            Get<ExecutableItem>(parentUuid).steps.Add(stepResult);
        }
    }
}