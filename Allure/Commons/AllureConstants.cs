namespace Allure.Commons
{
    public sealed class AllureConstants
    {
        public const string ConfigFilename = "allureConfig.json";
        public const string DefaultResultsFolder = "allure-results";
        public const string CategoriesFileName = "categories.json";
        public const string TestResultFileSuffix = "-result.json";
        public const string TestResultContainerFileSuffix = "-container.json";
        public const string AttachmentFileSuffix = "-attachment";
        internal const string TestContainerUuid = "TestContainerUuidProp";
        public const string TestUuid = "TestUuidProp";
        internal const string FixtureUuid = "FixtureUuidProp";
        internal const string TestAsserts = "TestAssertsProp";
        internal const string TestResult = "TestResultProp";
        internal const string TestWasIgnored = "TestWasIgnoredProp";
        internal const string TestIgnoreReason = "TestIgnoreReasonProp";
        internal const string EnvironmentFileName = "environment.properties";
        internal const string HistoryTrendFileName = "history-trend.json";
        internal const string HistoryDirName = "history";
        internal const string CompletedTestsInFixture = "CompletedTestsInFixtureProp";
        internal const string AllTestsInFixture = "AllTestsInFixtureProp";
        internal const string RunsCountTests = "RunsCountTestsProp";
        internal const string OneTimeTearDownFixture = "OneTimeTearDownFixtureProp";
        internal const string OneTimeSetupFixture = "OneTimeSetupFixtureProp";
        internal const string CurrentTestSetupFixture = "CurrentTestSetupFixtureProp";
        internal const string CurrentTestTearDownFixture = "CurrentTestTearDownFixtureProp";
        internal const string IgnoredTests = "IgnoredTestsProp";
    }
}