using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons;
using Allure.Commons.Helpers;
using Allure.Commons.Storage;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using Logger = Allure.Commons.Helpers.Logger;

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
                AllureStorage.MainThreadId = Thread.CurrentThread.ManagedThreadId;
                var suite = (TestFixture) context.CurrentTest;
                var tests = ReportHelper.GetAllTestsInSuite(suite);
                context.CurrentTest.Properties.Set(AllureConstants.FixtureUuid, $"{suite.FullName}-fixture");
                context.CurrentTest.Properties.Set(AllureConstants.AllTestsInFixture,
                    new ConcurrentBag<(string, string, string)>());
                context.CurrentTest.Properties.Set(AllureConstants.CompletedTestsInFixture,
                    new ConcurrentBag<(TestContext.ResultAdapter, string, string, string)>());
                context.CurrentTest.Properties.Set(AllureConstants.RunsCountTests,
                    new List<(string, string, string)>());
                foreach (var nestedTest in tests)
                {
                    var countRun = 1;
                    StartTestAndAddPropertiesInside(nestedTest, suite, countRun);
                    countRun++;
                    nestedTest.Method.GetCustomAttributes<RepeatAttribute>(true).FirstOrDefault()
                        ?.ApplyToTest((Test) nestedTest);
                    nestedTest.Method.GetCustomAttributes<RetryAttribute>(true).FirstOrDefault()
                        ?.ApplyToTest((Test) nestedTest);

                    if (nestedTest.Properties.ContainsKey(PropertyNames.RepeatCount))
                    {
                        var repeatCount = (int) nestedTest.Properties.Get(PropertyNames.RepeatCount);

                        for (var i = 1; i < repeatCount; i++)
                        {
                            StartTestAndAddPropertiesInside(nestedTest, suite, countRun);
                            countRun++;
                        }
                    }

                    if (nestedTest.Properties.ContainsKey("Retry"))
                    {
                        var retryCount = (int) nestedTest.Properties.Get("Retry");

                        for (var i = 1; i < retryCount; i++)
                        {
                            StartTestAndAddPropertiesInside(nestedTest, suite, countRun);
                            countRun++;
                        }
                    }
                }
            }
        }


        internal static void StartTestAndAddPropertiesInside(ITest test, TestFixture suite, int countRun)
        {
            var uuid = $"{test.Id}_{Guid.NewGuid():N}";
            var testUuid = $"{uuid}-{suite.Id}-test-run{countRun}";
            var containerUuid = $"{uuid}-{suite.Id}-run{countRun}-container";
            var fixtureUuid = suite.GetPropAsString(AllureConstants.FixtureUuid);
            test.SetProp(AllureConstants.TestContainerUuid, containerUuid);
            test.SetProp(AllureConstants.TestUuid, testUuid);
            test.SetProp(AllureConstants.FixtureUuid, fixtureUuid);
            test.SetProp(AllureConstants.TestAsserts, new List<Exception>());
            ReportHelper.StartAllureLogging(test, testUuid, containerUuid, suite);
            Logger.LogInProgress(
                $"Started allure logging for \"{test.FullName}\", run #{countRun}\n\"{testUuid}\"\n\"{containerUuid}\"\n\"{fixtureUuid}\"");

            suite.GetAllTestsInFixture()
                .Add((testUuid, containerUuid, fixtureUuid));
            suite.GetCountTestInFixture()
                .Add((testUuid, containerUuid, fixtureUuid));
        }
    }
}