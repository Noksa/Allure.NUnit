//using System;
//using Allure.Commons;
//using Allure.NUnit.Attributes;
//using NUnit.Framework;
//using OpenQA.Selenium;
//using OpenQA.Selenium.Chrome;
//using OpenQA.Selenium.Support.Extensions;

//namespace TestsSamples
//{
//    public class TestEnv
//    {
//        internal static string Test = "test";
//    }

//    [TestFixture(500345, "test arg", 3.4)]
//    [AllureSuite("It's my suite")]
//    [AllureEpic("Epic story")]
//    [Parallelizable(ParallelScope.All)]
//    public class Tests1 : AllureReport
//    {
//        [SetUp]
//        public void SetupOfTest()
//        {
//            _driver = new ChromeDriver();
//            AllureLifecycle.Instance.RunStep(() => _driver.Close());
//            AllureLifecycle.Instance.SetCurrentTestActionInException(() =>
//            {
//                AllureLifecycle.Instance.AddAttachment("Step Screenshot #2", AllureLifecycle.AttachFormat.ImagePng,
//                    _driver.TakeScreenshot().AsByteArray);
//            });
//            AllureLifecycle.Instance.RunStep("Test Setup 2", () => { });
//        }

//        [TearDown]
//        public void TearDownTest()
//        {
//            TestEnv.Test = "MyTest is so bad";
//            AllureLifecycle.Instance.RunStep("Test TearDown 1", () => { });
//            _driver?.Dispose();
//            AllureLifecycle.Instance.RunStep("Test TearDown 2", () => { });
//        }

//        private string _param1;

//        public Tests1(string param1)
//        {
//            _param1 = param1;
//        }

//        public Tests1(int param1, string param2, double param3)
//        {
//        }

//        [ThreadStatic] private static IWebDriver _driver;

//        [OneTimeSetUp]
//        public void OneTimeSetUpForAllTests()
//        {
//            AllureLifecycle.Instance.RunStep("Test OneTimeSetup 1", () => { });
//            AllureLifecycle.Instance.SetGlobalActionInException(() =>
//            {
//                AllureLifecycle.Instance.AddAttachment("Step Screenshot #1", AllureLifecycle.AttachFormat.ImagePng,
//                    _driver.TakeScreenshot().AsByteArray);
//            });
//            AllureLifecycle.Instance.RunStep("Test OneTimeSetup 2", () => { });
//            //throw new Exception("This is ex");
//        }

//        [OneTimeTearDown]
//        public void OneTimeTearDownYeah()
//        {
//            AllureLifecycle.Instance.RunStep("OneTimeTearDown step", () => { });
//            //throw new Exception("This is exception");
//        }

//        [TestCase(TestName = "Verify method negative scenario")]
//        public void VerifyTestNeg()
//        {
//            AllureLifecycle.Instance.Verify.That("5 is greater than 10", 5, Is.GreaterThan(10));
//            AllureLifecycle.Instance.Verify.That("5 is greater than 20", 5, Is.GreaterThan(20));
//        }

//        [TestCase(TestName = "Verify method positive scenario")]
//        public void VerifyTestPos()
//        {
//            AllureLifecycle.Instance.Verify.That("5 is greater than 1", 5, Is.GreaterThan(1));
//            AllureLifecycle.Instance.Verify.That("5 is greater than 2", 5, Is.GreaterThan(2));
//        }

//        [TestCase(TestName = "RunStep positive scenario")]
//        public void RunStepPos()
//        {
//            AllureLifecycle.Instance.RunStep("Open google.com page",
//                () =>
//                {
//                    _driver.Navigate().GoToUrl("http://google.com");
//                    AllureLifecycle.Instance.Verify.That("Check driver", _driver, Is.Not.Null);
//                });
//        }

//        [TestCase(TestName = "RunStep negative scenario")]
//        public void RunStepNeg()
//        {
//            AllureLifecycle.Instance.RunStep("Open google.com page",
//                () => { Assert.Fail("This is fail!"); });
//        }

//        [Ignore("Reason")]
//        [TestCase(TestName = "RunStep negative scenario #2")]
//        public void RunStepNeg2()
//        {
//            AllureLifecycle.Instance.RunStep("Open google.com page",
//                () =>
//                {
//                    _driver = null;
//                    var s = _driver.Url;
//                });
//        }

//        [AllureTms("Tms")]
//        [AllureStory("This is story")]
//        [AllureFeature("This is feature")]
//        [TestCase(TestName = "Debug testing")]
//        public void Debug()
//        {
//            AllureLifecycle.Instance.RunStep("This is step", () => { });
//        }
//    }
//}

