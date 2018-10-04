using System;
using Allure.Commons.Model;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class AllureTmsAttribute : NUnitAttribute
    {
        private const string TmsType = "tms";

        public AllureTmsAttribute(string name, string url = null)
        {
            TmsLink = new Link {name = name, type = TmsType, url = url};
        }

        public AllureTmsAttribute(string url)
        {
            TmsLink = new Link {name = url, type = TmsType, url = url};
        }

        internal Link TmsLink { get; }
    }
}