using System;
using Allure.Commons.Model;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class AllureIssueAttribute : NUnitAttribute
    {
        private const string IssueType = "issue";

        public AllureIssueAttribute(string name, string url, bool replaceWithPattern = true)
        {
            IssueLink = new Link {name = name, type = IssueType, url = url};
            ReplaceWithPattern = replaceWithPattern;
        }

        public AllureIssueAttribute(string url, bool replaceWithPattern = true)
        {
            IssueLink = new Link {name = url, type = IssueType, url = url};
            ReplaceWithPattern = replaceWithPattern;
        }

        internal Link IssueLink { get; }
        internal bool ReplaceWithPattern { get; }
    }
}