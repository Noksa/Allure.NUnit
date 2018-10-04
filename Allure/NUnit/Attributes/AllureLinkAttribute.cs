using System;
using Allure.Commons.Model;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class AllureLinkAttribute : NUnitAttribute
    {
        private const string LinkType = "mylink";

        public AllureLinkAttribute(string name, string url = null)
        {
            Link = new Link {name = name, type = LinkType, url = url};
        }

        public AllureLinkAttribute(string url)
        {
            Link = new Link {name = url, type = LinkType, url = url};
        }

        internal Link Link { get; }
    }
}