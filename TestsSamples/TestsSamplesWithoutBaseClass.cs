using System;
using Allure.Commons;
using Allure.NUnit.Attributes;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;

namespace TestsSamples
{
    [AllureSuite("Suite without base class")]
    [AllureEpic("Epic story")]
    [Parallelizable(ParallelScope.All)]
    public class TestsSamplesWithoutBaseClass : AllureReport
    {
        [SetUp]
        public void SetupOfTest()
        {
            _driver = new ChromeDriver();
            AllureLifecycle.Instance.SetCurrentTestActionInException(() =>
            {
                AllureLifecycle.Instance.AddAttachment("Step Screenshot", AllureLifecycle.AttachFormat.ImagePng,
                    _driver.TakeScreenshot().AsByteArray);
            });
        }

        [TearDown]
        public void TearDownTest()
        {
            AllureLifecycle.Instance.RunStep("Closing driver", () => {
            {
                _driver?.Close();
                _driver?.Dispose();
            } });
            }
        
        

        [ThreadStatic] private static IWebDriver _driver;

        [OneTimeSetUp]
        public void OneTimeSetUpForAllTests()
        {
            AllureLifecycle.Instance.RunStep("Test OneTimeSetup 1", () => { });
            AllureLifecycle.Instance.RunStep("Test OneTimeSetup 2", () => { });
        }

        [OneTimeTearDown]
        public void OneTimeTearDownYeah()
        {
            AllureLifecycle.Instance.Verify.Pass($"Suite {TestContext.CurrentContext.Test.FullName}: testing done.");
        }

        [TestCase(TestName = "Verify method negative scenario")]
        public void VerifyTestNeg()
        {
            _driver.Navigate().GoToUrl("http://google.com");
            AllureLifecycle.Instance.Verify.That("5 is greater than 10", 5, Is.GreaterThan(10));
            AllureLifecycle.Instance.RunStep("Open google.com page",
                () =>
                {
                    _driver.Navigate().GoToUrl("http://google.com");
                });
        }

        [TestCase(TestName = "Verify method positive scenario")]
        public void VerifyTestPos()
        {
            _driver.Navigate().GoToUrl("http://ya.ru");
            AllureLifecycle.Instance.Verify.That("5 is greater than 1", 5, Is.GreaterThan(1));
            AllureLifecycle.Instance.Verify.That("5 is greater than 2", 5, Is.GreaterThan(2));
        }

        [TestCase(TestName = "RunStep positive scenario")]
        public void RunStepPos()
        {
            AllureLifecycle.Instance.RunStep("Open google.com page",
                () =>
                {
                    _driver.Navigate().GoToUrl("http://google.com");
                    AllureLifecycle.Instance.Verify.That("Check driver Title is not null", _driver.Title, Is.Not.Null);
                });
        }

        [TestCase(TestName = "RunStep negative scenario")]
        public void RunStepNeg()
        {
            AllureLifecycle.Instance.RunStep("Open google.com page",
                () =>
                {
                    _driver.Navigate().GoToUrl("http://ya.ru");
                    AllureLifecycle.Instance.Verify.That("Check driver url", _driver.Url,
                        Is.EqualTo("http://google.com"));
                });
        }

        [Ignore("Dont need this test")]
        [TestCase(TestName = "Ignored test")]
        public void RunStepNeg2()
        {
            AllureLifecycle.Instance.RunStep("Open google.com page",
                () =>
                {
                    // nothing
                });
        }
    }
}

