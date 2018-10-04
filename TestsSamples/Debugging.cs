using Allure.Commons;
using Allure.NUnit.Attributes;
using NUnit.Framework;

namespace TestsSamples
{
    [Parallelizable(ParallelScope.All)]
    [AllureSuite("This is debug suite")]
    [TestFixture("Arg1")]
    [TestFixture("Arg1", "Arg2")]
    [TestFixture(1, 2.4, "FAFAFA")]
    public class Debugging : AllureReport
    {
        [SetUp]
        public void Set()
        {
            AllureLifecycle.Instance.RunStep($"This is setup step in test {TestContext.CurrentContext.Test.FullName}",
                () => { });
        }

        [TearDown]
        public void Tea()
        {
            AllureLifecycle.Instance.RunStep(
                $"This is teardown step in test {TestContext.CurrentContext.Test.FullName}", () => { });
        }

        public Debugging()
        {
        }

        public Debugging(object arg1)
        {
            fixtureArg = arg1;
        }

        public Debugging(object arg1, object arg2)
        {
            fixtureArg = $"{arg1} {arg2}";
        }

        public Debugging(object arg1, object arg2, object arg3)
        {
            fixtureArg = $"{arg1} {arg2} {arg3}";
        }

        private object fixtureArg;

        [OneTimeSetUp]
        public void OTS()
        {
            AllureLifecycle.Instance.RunStep(
                $"This is onetimesetup step of fixture {TestContext.CurrentContext.Test.FullName}", () => { });
        }

        [OneTimeTearDown]
        public void OTTD()
        {
            AllureLifecycle.Instance.RunStep(
                $"This is onetimeteardown step of fixture {TestContext.CurrentContext.Test.FullName}", () => { });
        }

        [TestCase(TestName = "Debug testing")]
        [Repeat(5)]
        public void Debug()
        {
            AllureLifecycle.Instance.RunStep("This is step in debugging test", () => { });
        }

        [TestCase(TestName = "Ignore testing")]
        [Ignore("Ignored test")]
        public void IgnoredTest()
        {
            AllureLifecycle.Instance.RunStep("This is step in ignored test", () => { });
        }

        [TestCase(TestName = "Retry testing")]
        [Retry(5)]
        public void RetryingTest()
        {
            AllureLifecycle.Instance.RunStep("This is step in retry test", () => { });
            Assert.Fail($"This is retry fail {TestContext.CurrentContext.Random.Next(1, 5000)}");
        }
    }
}