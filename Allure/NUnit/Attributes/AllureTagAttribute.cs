using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AllureTagAttribute : NUnitAttribute
    {
        public AllureTagAttribute(params string[] tags)
        {
            Tags = tags;
        }

        internal string[] Tags { get; }
    }
}