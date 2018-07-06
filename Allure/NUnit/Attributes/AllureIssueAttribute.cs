using System;
using Allure.Commons.Model;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AllureIssueAttribute : NUnitAttribute
    {
        public AllureIssueAttribute(string name, string url = null)
        {
            IssueLink = new Link {name = name, type = "issue", url = url};
        }

        internal Link IssueLink { get; }
    }
}