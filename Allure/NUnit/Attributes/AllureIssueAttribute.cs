using System;
using Allure.Commons.Model;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class AllureIssueAttribute : NUnitAttribute
    {
        private const string IssueType = "issue";

        public AllureIssueAttribute(string name, string url = null)
        {
            IssueLink = new Link {name = name, type = IssueType, url = url};
        }

        public AllureIssueAttribute(string url)
        {
            IssueLink = new Link {name = url, type = IssueType, url = url};
        }

        internal Link IssueLink { get; }
    }
}