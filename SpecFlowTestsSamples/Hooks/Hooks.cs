using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Allure.Commons;
using Allure.Commons.Model;
using BoDi;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TechTalk.SpecFlow;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace SpecFlowTestsSamples.Hooks
{
    [Binding]
    public class Hooks
    {
        private IWebDriver _driver;
        private readonly IObjectContainer _objectContainer;
        private readonly ScenarioContext _scenarioContext;
        private AllureLifecycle _allureLifecycle;

        public Hooks(IObjectContainer objectContainer, ScenarioContext scenarioContext)
        {
            _objectContainer = objectContainer;
            _scenarioContext = scenarioContext;
            _allureLifecycle = AllureLifecycle.Instance;
        }

        [OneTimeSetUp]
        public void SetupForAllure()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
        }

        [BeforeScenario]
        public void Setup()
        {
            new DriverManager().SetUpDriver(new ChromeConfig());
            _driver = new ChromeDriver();
            _objectContainer.RegisterInstanceAs(_driver);
        }

        [AfterScenario]
        public void TearDown()
        {
            if (_scenarioContext.TestError != null)
            {
                var path = MakeScreenshot(_driver);
                _allureLifecycle.AddAttachment(path);
            }

            _driver.Close();

            AllureHackForScenarioOutlineTests();
        }

        private void AllureHackForScenarioOutlineTests()
        {
            _scenarioContext.TryGetValue(out TestResult testresult);
            _allureLifecycle.UpdateTestCase(testresult.uuid, tc =>
            {
                tc.name = _scenarioContext.ScenarioInfo.Title;
                tc.historyId = Guid.NewGuid().ToString();
            });
        }

        [AfterTestRun]
        public static void AfterTests()
        {
            CloseChromeDriverProcesses();
        }

        private static void CloseChromeDriverProcesses()
        {
            var chromeDriverProcesses = Process.GetProcesses().
                Where(pr => pr.ProcessName == "chromedriver");

            if (chromeDriverProcesses.Count() == 0)
            {
                return;
            }

            foreach (var process in chromeDriverProcesses)
            {
                process.Kill();
            }
        }

        public static string MakeScreenshot(IWebDriver driver, string testName = "screen")
        {
            string projectPath = Path.GetDirectoryName(GetTestAssemblyFolder());
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            string fileLocation = $"{projectPath}/{testName}.png";
            ss.SaveAsFile(fileLocation, ScreenshotImageFormat.Png);
            return fileLocation;
        }

        private static string GetTestAssemblyFolder()
        {
            return Assembly.GetExecutingAssembly().Location;
        }
    }

}