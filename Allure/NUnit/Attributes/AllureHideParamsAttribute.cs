using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureHideParamsAttribute : NUnitAttribute
    {
        public AllureHideParamsAttribute(params int[] paramNumbers)
        {
            ParamNumbers = paramNumbers;
        }

        internal int[] ParamNumbers { get; }
    }
}