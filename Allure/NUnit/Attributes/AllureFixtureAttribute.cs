using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons;
using Allure.Commons.Model;
using Allure.Commons.Storage;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using TestResult = Allure.Commons.Model.TestResult;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AllureFixtureAttribute : NUnitAttribute, ITestAction
    {
        private readonly Dictionary<ITest, string> _ignoredTests = new Dictionary<ITest, string>();

        public AllureFixtureAttribute(string description = "")
        {
            Description = description;
        }

        private string Description { get; }


        public ActionTargets Targets => ActionTargets.Suite;

        public void BeforeTest(ITest test)
        {
            var fixture = new TestResultContainer
            {
                uuid = test.Id,
                name = test.ClassName
            };
            AllureLifecycle.Instance.StartTestContainer(fixture);
        }

        public void AfterTest(ITest test)
        {
            AllureStorage.MainThreadId = Thread.CurrentThread.ManagedThreadId;

            bool IsIgnored(ITest oTest)
            {
                return oTest.RunState == RunState.Ignored || oTest.RunState == RunState.Skipped;
            }

            _ignoredTests.Clear();
            foreach (var testMethod in test.Tests)
            {
                if (!testMethod.HasChildren && IsIgnored(testMethod))
                    _ignoredTests.Add(testMethod, testMethod.Properties.Get(PropertyNames.SkipReason).ToString());

                if (testMethod.HasChildren && IsIgnored(testMethod))
                    testMethod.Tests.ToList().ForEach(_ =>
                        _ignoredTests.Add(_, testMethod.Properties.Get(PropertyNames.SkipReason).ToString()));
                if (testMethod.HasChildren && !IsIgnored(testMethod))
                    foreach (var localTest in testMethod.Tests)
                        if (IsIgnored(localTest))
                            _ignoredTests.Add(localTest, localTest.Properties.Get(PropertyNames.SkipReason).ToString());
            }

            FailTestIfIgnored(_ignoredTests);

            if (test.HasChildren)
                AllureLifecycle.Instance.UpdateTestContainer(test.Id,
                    t => t.children.AddRange(test.Tests.Select(s => s.Id)));
            if (!string.IsNullOrEmpty(Description))
                AllureLifecycle.Instance.UpdateTestContainer(test.Id, t => t.description = Description);
            AllureLifecycle.Instance.StopTestContainer(test.Id);
            AllureLifecycle.Instance.WriteTestContainer(test.Id);
        }

        private static void FailTestIfIgnored(Dictionary<ITest, string> dict)
        {
            foreach (var pair in dict)
            {
                var testResult = new TestResult
                {
                    uuid = pair.Key.Id,
                    name = pair.Key.Name,
                    fullName = pair.Key.FullName,
                    labels = new List<Label>
                    {
                        Label.Thread(),
                        Label.Host(),
                        Label.TestClass(pair.Key.ClassName),
                        Label.TestMethod(pair.Key.MethodName),
                        Label.Package(pair.Key.ClassName)
                    }
                };

                AllureLifecycle.Instance.StartTestCase(testResult);
                AllureReport.AddInfoInTestCase(pair.Key.Method.MethodInfo);
                var statusTest = pair.Key.RunState == RunState.Ignored ? Status.broken : Status.skipped;
                AllureLifecycle.Instance.UpdateTestCase(x => x.labels.Add(Label.SubSuite("Ignored tests/test-cases")));
                AllureLifecycle.Instance.StopTestCase(_ =>
                {
                    _.status = statusTest;
                    _.statusDetails = new StatusDetails
                    {
                        message = $"Test was ignored by reason: {pair.Value}"
                    };
                });
                AllureLifecycle.Instance.WriteTestCase(testResult.uuid);
            }
        }
    }
}