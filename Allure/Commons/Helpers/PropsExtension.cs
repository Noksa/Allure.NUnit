using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Allure.Commons.Model;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Allure.Commons.Helpers
{
    internal static class PropsExtension
    {
        private static readonly object GetTestLocker = new object();

        internal static (string TestUuid, string ContainerUuid, string FixtureUuid) GetCurrentTestRunInfo(
            this ITest iTest)
        {
            if (iTest.IsSuite) throw new ArgumentException("You cant use this method with suite");

            lock (GetTestLocker)
            {
                var allCurrentTestRuns = GetCountTestInFixture(iTest);
                var testInfo = allCurrentTestRuns
                    .FirstOrDefault(q => q.TestUuid.StartsWith($"{TestContext.CurrentContext.Test.ID}_"));
                if (testInfo != default) allCurrentTestRuns.Remove(testInfo);

                return testInfo;
            }
        }

        internal static object GetProp(this ITest iTest, string propName)
        {
            return !iTest.Properties.ContainsKey(propName) ? null : iTest.Properties.Get(propName);
        }

        internal static void AddTestToCompletedInFixture(this ITest iTest,
            (TestContext.ResultAdapter testResult, string TestUuid, string ContainerUuid, string FixtureUuid)
                testTupleInfo)
        {
            lock (GetTestLocker)
            {
                GetCompletedTestsInFixture(iTest).Add(testTupleInfo);
            }
        }

        internal static string GetPropAsString(this ITest iTest, string propName)
        {
            return !iTest.Properties.ContainsKey(propName) ? null : iTest.Properties.Get(propName).ToString();
        }

        internal static bool ContainsProp(this ITest iTest, string propName)
        {
            return iTest.Properties.ContainsKey(propName);
        }

        internal static ITest SetProp(this ITest iTest, string propName, object propValue)
        {
            iTest.Properties.Set(propName, propValue);
            return iTest;
        }

        internal static ConcurrentBag<(string TestUuid, string TestContainerUuid, string FixtureUuid)>
            GetAllTestsInFixture(this ITest iTest)
        {
            ConcurrentBag<(string TestUuid, string TestContainerUuid, string FixtureUuid)> bag;
            if (iTest.IsSuite)
                bag = iTest.GetProp(AllureConstants.AllTestsInFixture) as
                    ConcurrentBag<(string TestUuid, string TestContainerUuid, string FixtureUuid)>;
            else
                bag = ((TestFixture) iTest.Fixture).GetProp(AllureConstants.AllTestsInFixture) as
                    ConcurrentBag<(string TestUuid, string TestContainerUuid, string FixtureUuid)>;

            return bag;
        }

        internal static
            ConcurrentBag<(TestContext.ResultAdapter testResult, string TestUuid, string TestContainerUuid, string
                FixtureUuid)> GetCompletedTestsInFixture(this ITest iTest)
        {
            ConcurrentBag<(TestContext.ResultAdapter testResult, string TestUuid, string TestContainerUuid, string
                FixtureUuid)> bag;
            if (iTest.IsSuite)
            {
                bag = iTest.GetProp(AllureConstants.CompletedTestsInFixture) as
                    ConcurrentBag<(TestContext.ResultAdapter testResult, string TestUuid, string TestContainerUuid,
                        string FixtureUuid)>;
            }
            else
            {
                var fixture = GetTestFixture(iTest);
                bag = fixture.GetProp(AllureConstants.CompletedTestsInFixture) as
                    ConcurrentBag<(TestContext.ResultAdapter testResult, string TestUuid, string TestContainerUuid,
                        string FixtureUuid)>;
            }

            return bag;
        }

        internal static List<(string TestUuid, string TestContainerUuid, string FixtureUuid)> GetCountTestInFixture(
            this ITest iTest)
        {
            if (iTest.IsSuite)
                return iTest.GetProp(AllureConstants.RunsCountTests) as
                    List<(string TestUuid, string TestContainerUuid, string FixtureUuid)>;
            var fixture = GetTestFixture(iTest);
            return fixture.GetProp(AllureConstants.RunsCountTests) as
                List<(string TestUuid, string TestContainerUuid, string FixtureUuid)>;
        }

        internal static TestFixture GetTestFixture(this ITest test)
        {
            if (test.IsSuite) return (TestFixture) test;
            var parents = GetAllTestParents(test);
            var fixture = parents.FirstOrDefault(q =>
                q.GetPropAsString(AllureConstants.FixtureUuid) == test.GetPropAsString(AllureConstants.FixtureUuid));
            return (TestFixture) fixture;
        }

        private static IEnumerable<ITest> GetAllTestParents(ITest test)
        {
            var parent = test.Parent;
            do
            {
                yield return parent;
                parent = parent.Parent;
            } while (parent != null);
        }

        internal static FixtureResult GetCurrentOneTimeSetupFixture(this ITest iTest)
        {
            var fixture = GetTestFixture(iTest);

            var cotsf = fixture.GetProp(AllureConstants.OneTimeSetupFixture) as FixtureResult;
            if (cotsf == null) return null;
            if (cotsf.suiteUuid == "null") return null;
            return cotsf;
        }

        internal static ITest SetCurrentOneTimeSetupFixture(this ITest iTest, FixtureResult ft)
        {
            var fixture = GetTestFixture(iTest);
            fixture.SetProp(AllureConstants.OneTimeSetupFixture, ft);
            return iTest;
        }

        internal static FixtureResult GetCurrentOneTimeTearDownFixture(this ITest iTest)
        {
            var fixture = GetTestFixture(iTest);

            var cottdf = fixture.GetProp(AllureConstants.OneTimeTearDownFixture) as FixtureResult;
            if (cottdf == null) return null;
            if (cottdf.suiteUuid == "null") return null;
            return cottdf;
        }

        internal static ITest SetCurrentOneTimeTearDownFixture(this ITest iTest, FixtureResult ft)
        {
            var fixture = GetTestFixture(iTest);
            fixture.SetProp(AllureConstants.OneTimeTearDownFixture, ft);
            return iTest;
        }

        internal static FixtureResult GetCurrentTestSetupFixture(this ITest iTest)
        {
            if (iTest.IsSuite)
                if (iTest.GetProp(AllureConstants.IgnoredTests) != null)
                    return null;
            var fixture = iTest.GetProp(AllureConstants.CurrentTestSetupFixture) as FixtureResult;
            if (fixture == null) return null;
            if (fixture.suiteUuid == "null") return null;
            return fixture;
        }

        internal static ITest SetCurrentTestSetupFixture(this ITest iTest, FixtureResult ft)
        {
            if (iTest.IsSuite) throw new ArgumentException("You cant use this method with suite");
            iTest.SetProp(AllureConstants.CurrentTestSetupFixture, ft);
            return iTest;
        }

        internal static FixtureResult GetCurrentTestTearDownFixture(this ITest iTest)
        {
            if (iTest.IsSuite) throw new ArgumentException("You cant use this method with suite");
            var fixture = iTest.GetProp(AllureConstants.CurrentTestTearDownFixture) as FixtureResult;
            if (fixture == null) return null;
            if (fixture.suiteUuid == "null") return null;
            return fixture;
        }

        internal static ITest SetCurrentTestTearDownFixture(this ITest iTest, FixtureResult ft)
        {
            if (iTest.IsSuite) throw new ArgumentException("You cant use this method with suite");
            iTest.SetProp(AllureConstants.CurrentTestTearDownFixture, ft);
            return iTest;
        }
    }
}