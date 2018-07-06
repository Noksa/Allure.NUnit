using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable
namespace Allure.Commons.Model
{
    [DebuggerStepThrough()]
    public partial class TestRunResult
    { 
        public string uuid { get; set; }

        public string name { get; set; }
    }

    [DebuggerStepThrough()]
    public partial class TestResultContainer
    {
        public string uuid { get; set; }

        public string name { get; set; }

        public List<string> children { get; set; } = new List<string>();

        public string description { get; set; }

        public string descriptionHtml { get; set; }

        public List<FixtureResult> befores { get; set; } = new List<FixtureResult>();

        public List<FixtureResult> afters { get; set; } = new List<FixtureResult>();

        public List<Link> links { get; set; } = new List<Link>();

        public long start { get; set; }

        public long stop { get; set; }
    }

    [DebuggerStepThrough()]
    public partial class FixtureResult : ExecutableItem
    {
    }

    [DebuggerStepThrough()]
    public abstract partial class ExecutableItem
    {
        public string name { get; set; }

        public Status status { get; set; }

        public StatusDetails statusDetails { get; set; }

        public Stage stage { get; set; }

        public string description { get; set; }
      
        public string descriptionHtml { get; set; }

        public List<StepResult> steps { get; set; } = new List<StepResult>();

        public List<Attachment> attachments { get; set; } = new List<Attachment>();

        public List<Parameter> parameters { get; set; } = new List<Parameter>();

        public long start { get; set; }

        public long stop { get; set; }
    }

    public enum Status
    {
        none,
        failed,
        broken,
        passed,
        skipped,
    }

    [DebuggerStepThrough()]
    public partial class StatusDetails
    {

        public bool known { get; set; }

        public bool muted { get; set; }

        public bool flaky { get; set; }

        public string message { get; set; }

        public string trace { get; set; }
    }

    public enum Stage
    {
        scheduled,
        running,
        finished,
        pending,
        interrupted,
    }

    [DebuggerStepThrough()]
    public partial class StepResult : ExecutableItem
    {
    }

    [DebuggerStepThrough()]
    public partial class Attachment
    {
        public string name { get; set; }

        public string source { get; set; }

        public string type { get; set; }
    }

    [DebuggerStepThrough()]
    public partial class Parameter
    {
        public string name { get; set; }

        public string value { get; set; }
    }

    [DebuggerStepThrough()]
    public partial class TestResult : ExecutableItem
    {
        public string uuid { get; set; }

        public string historyId { get; set; }

        public string testCaseId { get; set; }
        public string rerunOf { get; set; }

        public string fullName { get; set; }

        public List<Label> labels { get; set; } = new List<Label>();

        public List<Link> links { get; set; } = new List<Link>();
    }

    [DebuggerStepThrough()]
    public partial class Label
    {
        public string name { get; set; }

        public string value { get; set; }
    }

    [DebuggerStepThrough()]
    public partial class Link
    {
        public string name { get; set; }

        public string url { get; set; }

        public string type { get; set; }
    }
}
#pragma warning restore