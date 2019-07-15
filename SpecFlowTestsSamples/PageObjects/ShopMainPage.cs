using System.Collections.Generic;
using OpenQA.Selenium;
using SeleniumExtras.PageObjects;
using How = OpenQA.Selenium.Support.PageObjects.How;

namespace SpecFlowTestsSamples.PageObjects
{
    public class ShopMainPage
    {
        public ShopMainPage(IWebDriver driver)
        {
            PageFactory.InitElements(driver, this);
        }

        [OpenQA.Selenium.Support.PageObjects.FindsBy(How = How.CssSelector, Using = ".shop-phone strong")]
        public IWebElement ShopPhoneNumberLabel { get; set; }

        [OpenQA.Selenium.Support.PageObjects.FindsBy(How = How.Id, Using = "homepage-slider")]
        [OpenQA.Selenium.Support.PageObjects.CacheLookup]
        public IWebElement MainPageSlider { get; set; }

        [OpenQA.Selenium.Support.PageObjects.FindsBy(How = How.ClassName, Using = "login")]
        [OpenQA.Selenium.Support.PageObjects.CacheLookup]
        public IWebElement SignInLink { get; set; }

        [OpenQA.Selenium.Support.PageObjects.FindsBy(How = How.CssSelector, Using = "a[class='button ajax_add_to_cart_button btn btn-default']")]
        public IList<IWebElement> AddToCartBtn { get; set; }

        [OpenQA.Selenium.Support.PageObjects.FindsBy(How = How.CssSelector, Using = "a[class='button lnk_view btn btn-default']")]
        public IList<IWebElement> GoodMoreBtn { get; set; }
    }
}