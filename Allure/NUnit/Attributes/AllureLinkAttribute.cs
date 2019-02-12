using System;
using Allure.Commons.Model;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class AllureLinkAttribute : NUnitAttribute
    {
        private const string LinkType = "link";

        public AllureLinkAttribute(string name, string url, bool replaceWithPattern = true)
        {
            Link = new Link {name = name, type = LinkType, url = url};
            ReplaceWithPattern = replaceWithPattern;
        }

        public AllureLinkAttribute(string url, bool replaceWithPattern = true)
        {
            Link = new Link {name = url, type = LinkType, url = url};
            ReplaceWithPattern = replaceWithPattern;
        }

        internal Link Link { get; }
        internal bool ReplaceWithPattern { get; }
    }
}