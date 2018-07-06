using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AllureStoryAttribute : NUnitAttribute
    {
        public AllureStoryAttribute(params string[] story)
        {
            Stories = story;
        }

        internal string[] Stories { get; }
    }
}