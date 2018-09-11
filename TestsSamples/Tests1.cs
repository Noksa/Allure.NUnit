using System;
using Allure.Commons;
using Allure.Commons.Model;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;

namespace TestsSamples
{
    [TestFixture]
    public class Tests1 : AllureReport
    {
        [ThreadStatic] private static IWebDriver _driver;

        [OneTimeSetUp]
        public void OneTimeSetUp1()
        {
            AllureLifecycle.Instance.SetGlobalActionInException(() =>
            {
                AllureLifecycle.Instance.AddAttachment("Step Screenshot #1", AllureLifecycle.AttachFormat.ImagePng,
                    _driver.TakeScreenshot().AsByteArray);
            });
        }
        [SetUp]
        public void Setup()
        {
            _driver = new ChromeDriver();
            AllureLifecycle.Instance.SetCurrentTestActionInException(() =>
            {
                AllureLifecycle.Instance.AddAttachment("Step Screenshot #2", AllureLifecycle.AttachFormat.ImagePng,
                    _driver.TakeScreenshot().AsByteArray);
            });
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Dispose();
        }

        [TestCase(TestName = "Verify method negative scenario")]
        public void VerifyTestNeg()
        {
            AllureLifecycle.Instance.Verify.That("5 is greater than 10", 5, Is.GreaterThan(10));
            AllureLifecycle.Instance.Verify.That("5 is greater than 20", 5, Is.GreaterThan(20));
        }

        [TestCase(TestName = "Verify method positive scenario")]
        public void VerifyTestPos()
        {
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
                    AllureLifecycle.Instance.Verify.That("Check driver", _driver, Is.Not.Null);
                });
        }

        [TestCase(TestName = "RunStep negative scenario")]
        public void RunStepNeg()
        {
            AllureLifecycle.Instance.RunStep("Open google.com page",
                () =>
                {
                    Assert.Fail("This is fail!");
                });
        }

        [TestCase(TestName = "RunStep negative scenario #2")]
        public void RunStepNeg2()
        {
            AllureLifecycle.Instance.UpdateStep(q =>
            {
                var param = new Parameter {name = "Param #1", value = "value"};
                q.parameters.Add(param);
            });

            AllureLifecycle.Instance.RunStep("Open google.com page",
                () =>
                {
                    _driver = null;
                    var s = _driver.Url;
                });
        }


    }
}
