using System;
using Allure.Commons.Model;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureSeverityAttribute : NUnitAttribute
    {
        public AllureSeverityAttribute(SeverityLevel severity = SeverityLevel.Normal)
        {
            Severity = severity;
        }

        internal SeverityLevel Severity { get; }
    }
}