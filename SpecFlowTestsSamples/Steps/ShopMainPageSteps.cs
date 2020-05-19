using System;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SpecFlowTestsSamples.PageObjects;
using TechTalk.SpecFlow;

namespace SpecFlowTestsSamples.Steps
{
    [Binding]
    public class ShopMainPageSteps
    {
        private IWebDriver _driver;
        private ShopMainPage shopMainPage;

        public ShopMainPageSteps(IWebDriver driver)
        {
            _driver = driver;
            shopMainPage = new ShopMainPage(driver);
        }

        [Given(@"I am navigated to Shop application main page")]
        public void GivenIAmNavigatedToShopApplication()
        {
            _driver.Navigate().GoToUrl("http://automationpractice.com/index.php");
            //throw new Exception("fail");
        }

        [Then(@"I am redirected to Shop application main page")]
        public void ThenIAmRedirectedToShopApplicationMainPage()
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromMinutes(1));
            wait.Until(driver => shopMainPage.MainPageSlider.Displayed);
        }

        [Then(@"I see that page title equals to ""(.*)""")]
        public void ThenISeeThatPageTitleEqualsTo(string pageTitleExpected)
        {
            var titleActual = _driver.Title;

            titleActual.Should().BeEquivalentTo(pageTitleExpected);
        }

        [Then(@"I see that shop phone number is ""(.*)""")]
        public void ThenISeeThatShopPhoneNumberIs(string phoneNumberExpected)
        {
            var phoneNumberActual = shopMainPage.ShopPhoneNumberLabel.Text;

            phoneNumberActual.Should().BeEquivalentTo(phoneNumberExpected);
        }


    }
}