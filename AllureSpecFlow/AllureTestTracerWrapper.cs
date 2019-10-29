using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Allure.Commons;
using Allure.Commons.Helpers;
using Allure.Commons.Model;
using CsvHelper;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.BindingSkeletons;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Tracing;

namespace AllureSpecFlow
{
    public class AllureTestTracerWrapper : TestTracer, ITestTracer
    {
        private const string NoMatchingStepMessage = "No matching step definition found for the step";

        static AllureTestTracerWrapper()
        {
            _ = PluginHelper.SpecFlowCfg;
        }

        public AllureTestTracerWrapper(ITraceListener traceListener, IStepFormatter stepFormatter,
            IStepDefinitionSkeletonProvider stepDefinitionSkeletonProvider, SpecFlowConfiguration specFlowConfiguration)
            : base(traceListener, stepFormatter, stepDefinitionSkeletonProvider, specFlowConfiguration)
        {
        }

        void ITestTracer.TraceStep(StepInstance stepInstance, bool showAdditionalArguments)
        {
            TraceStep(stepInstance, showAdditionalArguments);
            StartStep(stepInstance);
        }

        void ITestTracer.TraceStepDone(BindingMatch match, object[] arguments, TimeSpan duration)
        {
            TraceStepDone(match, arguments, duration);
            AllureLifecycle.Instance.StopStep(x => x.status = Status.passed);
        }

        void ITestTracer.TraceError(Exception ex)
        {
            TraceError(ex);
            var stepText = "";
            AllureLifecycle.Instance.UpdateStep(x => { stepText = x.name; });
            var stepHelper = new StepHelper(stepText);
            stepHelper.ProceedException(ex);
            AllureLifecycle.Instance.StopStep(x => x.status = Status.failed);
            FailScenario(ex);
        }

        void ITestTracer.TraceStepSkipped()
        {
            TraceStepSkipped();
            AllureLifecycle.Instance.StopStep(x => x.status = Status.skipped);
        }

        void ITestTracer.TraceStepPending(BindingMatch match, object[] arguments)
        {
            TraceStepPending(match, arguments);
            AllureLifecycle.Instance.StopStep(x => x.status = Status.skipped);
        }

        void ITestTracer.TraceNoMatchingStepDefinition(StepInstance stepInstance, ProgrammingLanguage targetLanguage,
            CultureInfo bindingCulture, List<BindingMatch> matchesWithoutScopeCheck)
        {
            TraceNoMatchingStepDefinition(stepInstance, targetLanguage, bindingCulture, matchesWithoutScopeCheck);
            AllureLifecycle.Instance.StopStep(x => x.status = Status.broken);
            AllureLifecycle.Instance.UpdateTestCase(x =>
            {
                x.status = Status.broken;
                x.statusDetails = new StatusDetails {message = NoMatchingStepMessage};
            });
        }

        private void StartStep(StepInstance stepInstance)
        {
            var stepResult = new StepResult
            {
                name = $"{stepInstance.Keyword} {stepInstance.Text}"
            };


            // parse MultilineTextArgument
            if (stepInstance.MultilineTextArgument != null)
                AllureLifecycle.Instance.AddAttachment(
                    "multiline argument",
                    "text/plain",
                    Encoding.UTF8.GetBytes(stepInstance.MultilineTextArgument),
                    ".txt");

            var table = stepInstance.TableArgument;
            var isTableProcessed = table == null;

            // parse table as step params
            if (table != null)
            {
                var header = table.Header.ToArray();
                if (PluginHelper.SpecFlowCfg.stepArguments.convertToParameters)
                {
                    var parameters = new List<Parameter>();

                    // convert 2 column table into param-value
                    if (table.Header.Count == 2)
                    {
                        var paramNameMatch = Regex.IsMatch(header[0],
                            PluginHelper.SpecFlowCfg.stepArguments.paramNameRegex);
                        var paramValueMatch = Regex.IsMatch(header[1],
                            PluginHelper.SpecFlowCfg.stepArguments.paramValueRegex);
                        if (paramNameMatch && paramValueMatch)
                        {
                            for (var i = 0; i < table.RowCount; i++)
                                parameters.Add(new Parameter {name = table.Rows[i][0], value = table.Rows[i][1]});

                            isTableProcessed = true;
                        }
                    }
                    // add step params for 1 row table
                    else if (table.RowCount == 1)
                    {
                        for (var i = 0; i < table.Header.Count; i++)
                            parameters.Add(new Parameter {name = header[i], value = table.Rows[0][i]});
                        isTableProcessed = true;
                    }

                    stepResult.parameters = parameters;
                }
            }
            AllureLifecycle.Instance.StartStep(PluginHelper.NewId(), stepResult);

            // add csv table for multi-row table if was not processed as params already
            if (!isTableProcessed)
                using (var sw = new StringWriter())
                using (var csv = new CsvWriter(sw))
                {
                    foreach (var item in table.Header) csv.WriteField(item);
                    csv.NextRecord();
                    foreach (var row in table.Rows)
                    {
                        foreach (var item in row.Values) csv.WriteField(item);
                        csv.NextRecord();
                    }

                    AllureLifecycle.Instance.AddAttachment("table", "text/csv",
                        Encoding.UTF8.GetBytes(sw.ToString()), ".csv");
                }
        }

        private static void FailScenario(Exception ex)
        {
            AllureLifecycle.Instance.UpdateTestCase(
                x =>
                {
                    x.status = x.status != Status.none ? x.status : Status.failed;
                    x.statusDetails = PluginHelper.GetStatusDetails(ex);
                });
        }
    }
}