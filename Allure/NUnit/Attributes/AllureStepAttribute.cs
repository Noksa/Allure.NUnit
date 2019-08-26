using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Allure.Commons;
using Allure.Commons.Helpers;
using Noksa.Allure.StepInjector.Abstract;

namespace Allure.NUnit.Attributes
{
    public class AllureStepAttribute : AbstractAllureStepAttribute
    {
        #region Private fields

        private readonly StepHelper _stepHelper;

        #endregion


        public AllureStepAttribute() : this(null)
        {
        }
        
        public AllureStepAttribute(string stepText) : base(stepText)
        {
            _stepHelper = new StepHelper(stepText);
        }

        public override void OnEnter(MethodBase method, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(StepText)) StepText = method.Name;
            else ConvertParameters(parameters);
            AllureLifecycle.Instance.StartStep(StepText, StepUuid);
            if (parameters != null && parameters.Count > 0) ReportHelper.AddStepParameters(parameters.Select(o => o.Value).ToArray(), StepUuid);
            
        }

        public override void OnExit()
        {
            var stepStatus = _stepHelper.GetStepStatus();
            AllureLifecycle.Instance.UpdateStep(StepUuid, step => step.status = stepStatus);
            AllureLifecycle.Instance.StopStep(StepUuid);
        }

        public override void OnException(Exception e)
        {
            var (stepStatus, _, _) = _stepHelper.ProceedException(e);
            AllureLifecycle.Instance.UpdateStep(StepUuid, step => step.status = stepStatus);
            AllureLifecycle.Instance.StopStep(StepUuid);
        }

        protected override string ConvertParameters(Dictionary<string, object> parameters)
        {
            foreach (var valuePair in parameters)
            {
                var match = Regex.Match(StepText, $"&{valuePair.Key}&");
                if (!match.Success)
                {
                    var matches = Regex.Matches(StepText, $"&{valuePair.Key}\\.(.*?)&");
                    foreach (Match match1 in matches)
                    {
                        if (!match1.Success || match1.Groups.Count < 2) continue;
                        var obj = valuePair.Value;
                        var call = match1.Groups[1].Value;
                        var firstIndex = call.LastIndexOf('(');
                        if (firstIndex != -1)
                        {
                            var lastIndex = call.LastIndexOf(')');
                            if (lastIndex - firstIndex > 1) throw new ArgumentException("Sorry, you cannot use methods with parameters in AllureStep at the moment.\nThis will be added in later releases.");
                            call = call.Remove(firstIndex, lastIndex - firstIndex + 1);
                        }
                        var member = ReflectionHelper.GetMember(obj.GetType(), call);
                        if (member != null)
                        {
                            var memberValue = ReflectionHelper.GetMemberValue(member, obj);
                            StepText = StepText.Replace(match1.Value, memberValue?.ToString());
                        }
                    }
                }
                else
                {
                    var val = match.Value;
                    StepText = StepText.Replace(val, valuePair.Value?.ToString());
                }
            }

            return StepText;
        }

        

       

    }
}
