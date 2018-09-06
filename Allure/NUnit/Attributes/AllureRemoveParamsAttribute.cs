using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureRemoveParamsAttribute : NUnitAttribute
    {
        public AllureRemoveParamsAttribute(params int[] paramNumbers)
        {
            ParamNumbers = paramNumbers;
        }

        internal int[] ParamNumbers { get; }
    }
}