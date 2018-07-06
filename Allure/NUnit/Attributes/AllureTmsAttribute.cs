using System;
using Allure.Commons.Model;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureTmsAttribute : NUnitAttribute
    {
        public AllureTmsAttribute(string name, string url = null)
        {
            TmsLink = new Link {name = name, type = "tms", url = url};
        }

        internal Link TmsLink { get; }
    }
}