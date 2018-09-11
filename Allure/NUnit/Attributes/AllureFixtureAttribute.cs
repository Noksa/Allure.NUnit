using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons;
using Allure.Commons.Helpers;
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
            var tests = ReportHelper.GetAllTestsInSuite(test);
            foreach (var nestedTest in tests)
            {
                var uuid = $"{nestedTest.Id}_{Guid.NewGuid():N}";
                nestedTest.Properties.Set(AllureConstants.TestContainerUuid, $"{uuid}-{test.Id}-container");
                nestedTest.Properties.Set(AllureConstants.TestUuid, $"{uuid}-{test.Id}-test");
                nestedTest.Properties.Set(AllureConstants.FixtureUuid, $"{test.Id}-fixture");
            }
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

            FailIgnoredTests(_ignoredTests);
        }

        #region Privates

        private static void FailIgnoredTests(Dictionary<ITest, string> dict)
        {
            foreach (var pair in dict)
            {
                var ourFixture = new TestResultContainer
                {
                    uuid = pair.Key.Properties.Get(AllureConstants.TestContainerUuid).ToString(),
                    name = pair.Key.ClassName
                };
                AllureLifecycle.Instance.StartTestContainer(ourFixture);

                var realUuid = pair.Key.Properties.Get(AllureConstants.TestUuid).ToString();
                var testResult = new TestResult
                {
                    uuid = realUuid,
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
                AllureLifecycle.Instance.StartTestContainer(ourFixture);
                AllureLifecycle.Instance.UpdateTestContainer(pair.Key.Properties.Get(AllureConstants.TestContainerUuid)
                    .ToString(), q => q.children.Add(pair.Key.Properties.Get(AllureConstants.TestUuid)
                    .ToString()));
                AllureLifecycle.Instance.StartTestCase(testResult);
                ReportHelper.AddInfoInTestCase(pair.Key);
                AllureLifecycle.Instance.UpdateTestCase(x =>
                {
                    var subSuites = x.labels.Where(lbl => lbl.name.ToLower().Equals("subsuite")).ToList();
                    subSuites.ForEach(lbl => x.labels.Remove(lbl));
                    x.labels.Add(Label.SubSuite("Ignored tests/test-cases"));
                });
                AllureLifecycle.Instance.StopTestCase(_ =>
                {
                    _.status = Status.skipped;
                    _.statusDetails = new StatusDetails
                    {
                        message = $"Test was ignored by reason: {pair.Value}"
                    };
                });
                AllureLifecycle.Instance.WriteTestCase(testResult.uuid);
                AllureLifecycle.Instance.StopTestContainer(pair.Key.Properties.Get(AllureConstants.TestContainerUuid)
                    .ToString());
                AllureLifecycle.Instance.WriteTestContainer(pair.Key.Properties.Get(AllureConstants.TestContainerUuid)
                    .ToString());
            }
        }

        #endregion
    }
}