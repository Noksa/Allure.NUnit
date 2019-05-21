namespace Allure.Commons
{
    public sealed class AllureConstants
    {
        #region Public

        public const string TestContainerUuid = "TestContainerUuidProp";
        public const string TestUuid = "TestUuidProp";

        #endregion

        #region Internal

        public const string ConfigFilename = "allureConfig.json";
        internal const string DefaultResultsFolder = "allure-results";
        internal const string CategoriesFileName = "categories.json";
        internal const string TestResultFileSuffix = "-result.json";
        internal const string TestResultContainerFileSuffix = "-container.json";
        internal const string AttachmentFileSuffix = "-attachment";
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
        internal const string AllureConfigFileCustomDirMemberName = "AllureConfigDir";

        #endregion
    }
}