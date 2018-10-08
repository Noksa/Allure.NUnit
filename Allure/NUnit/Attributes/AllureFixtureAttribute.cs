using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons;
using Allure.Commons.Helpers;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AllureFixtureAttribute : NUnitAttribute, IApplyToContext
    {
        private static readonly object Locker = new object();

        public AllureFixtureAttribute(string description = "")
        {
            Description = description;
        }

        private string Description { get; }

        public void ApplyToContext(TestExecutionContext context)
        {
            lock (Locker)
            {
                StepsWorker.MainThreadId = Thread.CurrentThread.ManagedThreadId;
                var suite = (TestFixture) context.CurrentTest;
                var tests = ReportHelper.GetAllTestsInSuite(suite);
                context.CurrentTest.SetProp(AllureConstants.FixtureUuid, $"{suite.FullName}-fixture")
                    .SetProp(AllureConstants.AllTestsInFixture,
                        new ConcurrentBag<(string, string, string)>())
                    .SetProp(AllureConstants.CompletedTestsInFixture,
                        new ConcurrentBag<(TestContext.ResultAdapter, string, string, string, ITest)>())
                    .SetProp(AllureConstants.RunsCountTests,
                        new List<(string, string)>())
                    .SetProp(AllureConstants.FixtureStorage, new ConcurrentDictionary<string, object>());
                foreach (var nestedTest in tests)
                {
                    var countRun = 1;
                    StartTestAndAddPropertiesInside(nestedTest, suite, countRun);
                    countRun++;
                    nestedTest.Method.GetCustomAttributes<RepeatAttribute>(true).FirstOrDefault()
                        ?.ApplyToTest((Test) nestedTest);
                    nestedTest.Method.GetCustomAttributes<RetryAttribute>(true).FirstOrDefault()
                        ?.ApplyToTest((Test) nestedTest);

                    if (nestedTest.ContainsProp(PropertyNames.RepeatCount))
                    {
                        var repeatCount = (int) nestedTest.GetProp(PropertyNames.RepeatCount);

                        for (var i = 1; i < repeatCount; i++)
                        {
                            StartTestAndAddPropertiesInside(nestedTest, suite, countRun);
                            countRun++;
                        }
                    }

                    if (nestedTest.ContainsProp("Retry"))
                    {
                        var retryCount = (int) nestedTest.GetProp("Retry");

                        for (var i = 1; i < retryCount; i++)
                        {
                            StartTestAndAddPropertiesInside(nestedTest, suite, countRun);
                            countRun++;
                        }
                    }
                }
            }
        }


        private static void StartTestAndAddPropertiesInside(ITest test, TestFixture suite, int countRun)
        {
            var uuid = $"{test.Id}_{Guid.NewGuid():N}";
            var testUuid = $"{uuid}-{suite.Id}-test-run{countRun}";
            var containerUuid = $"{uuid}-{suite.Id}-run{countRun}-container";
            var fixtureUuid = suite.GetPropAsString(AllureConstants.FixtureUuid);

            test.SetProp(AllureConstants.TestContainerUuid, containerUuid)
                .SetProp(AllureConstants.TestUuid, testUuid)
                .SetProp(AllureConstants.FixtureUuid, fixtureUuid)
                .SetProp(AllureConstants.TestAsserts, new List<Exception>())
                .SetProp(AllureConstants.TestBodyContext, new LinkedList<string>());
            ReportHelper.StartAllureLogging(test, testUuid, containerUuid, suite);
            OutLogger.LogInProgress(
                $"Started allure logging for \"{test.FullName}\", run #{countRun}\n\"{testUuid}\"\n\"{containerUuid}\"\n\"{fixtureUuid}\"");

            suite.GetAllTestsInFixture()
                .Add((testUuid, containerUuid, fixtureUuid));
            suite.GetCountTestInFixture()
                .Add((testUuid, containerUuid));
        }
    }
}