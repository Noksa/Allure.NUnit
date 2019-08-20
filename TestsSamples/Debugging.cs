using System;
using System.Collections.Generic;
using System.Reflection;
using Allure.Commons;
using Allure.NUnit.Attributes;
using Noksa.Allure.StepInjector.Abstract;
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
        public void CurrentTestSetup()
        {
            AllureLifecycle.Instance.RunStep($"This is setup step in test {TestContext.CurrentContext.Test.FullName}",
                () => { });
        }

        [TearDown]
        public void CurrentTestTearDown()
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
        public void OneTimeSetup()
        {
            AllureLifecycle.Instance.RunStep(
                $"This is onetimesetup step of fixture {TestContext.CurrentContext.Test.FullName}", () => { });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AllureLifecycle.Instance.RunStep(
                $"This is onetimeteardown step of fixture {TestContext.CurrentContext.Test.FullName}", () => { });
        }

        [TestCase(TestName = "Debug testing")]
        [Repeat(5)]
        [AllureLink("http://mysite.com/ID-123", false)] // will not replaced by pattern
        [AllureLink("ID-124")]
        public void Debug()
        {
            Test(new MegaClass());
            Test2(new MegaClass());
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

        [TestCase("login1", "password1")]
        public void LoginToApp(string login, string password)
        {
            AllureLifecycle.Instance.Verify.That("This is parametrized step", 5, Is.GreaterThan(2), login, password);
        }

        [TestCase("5th Avenue", 10, null)]
        public void TestWithNullParameter(string street, int houseNUmber, int? apartment)
        {
            AllureLifecycle.Instance.Verify.That("This is parametrized step with null parameter", !apartment.HasValue);
        }

        [AllureStep("Test &myClass.GetString()& &myClass.Int& &myClass._fieldName&")]
        private void Test(MegaClass myClass)
        {

        }

        [AllureStep("This is your class? &myClass&")]
        private void Test2(MegaClass myClass)
        {

        }


    }

    public class MegaClass
    {
        public override string ToString()
        {
            return "This is my class";
        }

        private string GetString()
        {
            return "Mega";
        }

        private static double _fieldName = 4005;
        public int Int => 10;
    }
}