using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Allure.Commons.Helpers;
using Allure.Commons.Json;
using Allure.Commons.Model;
using Allure.Commons.Utils;
using Allure.Commons.Writer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using TestResult = Allure.Commons.Model.TestResult;

namespace Allure.Commons
{
    public sealed class AllureLifecycle
    {
        private static readonly object Locker = new object();
        private static AllureLifecycle _instance;
        internal static List<Action> GlobalActionsInException;
        [ThreadStatic] internal static List<Action> CurrentTestActionsInException;
        internal readonly Configuration Config;
        internal readonly StepsWorker StepsWorker;
        private IAllureResultsWriter _writer;

        private AllureLifecycle()
        {
            Verify = new Verify();
            using (var r = new StreamReader(ConfigFinder.AllureConfigFilePath))
            {
                var deserializeSettings = new JsonSerializerSettings {Formatting = Formatting.Indented};
                var json = r.ReadToEnd();
                Config = JsonConvert.DeserializeObject<Configuration>(json,
                    deserializeSettings);
                AllureEnvironment = JObject.Parse(json).GetValue("environment");
            }

            StepsWorker = new StepsWorker();
        }

        private static string AllureVersion { get; } =
            Assembly.GetAssembly(typeof(AllureLifecycle)).GetName().Version.ToString();

        internal static JToken AllureEnvironment { get; set; }


        public Verify Verify { get; }

        internal string WorkspaceDir => TestContext.CurrentContext.WorkDirectory;

        public string ResultsDirectory => _writer.ToString();

        public static bool IsMainThread => StepsWorker.IsMainThread;

        public static AllureLifecycle Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (Locker)
                {
                    _instance = new AllureLifecycle();
                    LocalHistoryTrendBuilder.MakeLocalHistoryTrend();
                    _instance.SetDefaultResultsWriter(_instance.Config.Allure.Directory);
                    var categories = _instance.Config.Categories;
                    MakeCategoriesFile(categories, _instance._writer.Dir);
                }

                return _instance;
            }
        }

        public static long ToUnixTimestamp(DateTimeOffset value = default)
        {
            if (value == default) value = DateTimeOffset.Now;
#if NET45 || NET451 || NET452
            return (long) value.UtcDateTime.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
#else
            return value.ToUnixTimeMilliseconds();
#endif
        }

        #region TestContainer

        public AllureLifecycle StartTestContainer(TestResultContainer container)
        {
            container.start = ToUnixTimestamp();
            TestExecutionContext.CurrentContext.CurrentTest.Storage().Put(container.uuid, container);
            return this;
        }

        public AllureLifecycle StartTestContainer(string parentUuid, TestResultContainer container)
        {
            UpdateTestContainer(parentUuid, c => c.children.Add(container.uuid));
            StartTestContainer(container);
            return this;
        }

        public AllureLifecycle UpdateTestContainer(string uuid, Action<TestResultContainer> update)
        {
            update.Invoke(TestExecutionContext.CurrentContext.CurrentTest.Storage().Get<TestResultContainer>(uuid));
            return this;
        }

        public AllureLifecycle StopTestContainer(string uuid, bool updateStopTime)
        {
            if (updateStopTime) UpdateTestContainer(uuid, c => c.stop = ToUnixTimestamp());
            return this;
        }

        public AllureLifecycle WriteTestContainer(string uuid)
        {
            _writer.Write(TestExecutionContext.CurrentContext.CurrentTest.Storage().Remove<TestResultContainer>(uuid));
            return this;
        }

        #endregion

        #region Fixture

        public AllureLifecycle StartBeforeFixture(string parentUuid, string uuid, FixtureResult result)
        {
            UpdateTestContainer(parentUuid, container => container.befores.Add(result));
            StartFixture(uuid, result);
            return this;
        }

        public AllureLifecycle StartAfterFixture(string parentUuid, string uuid, FixtureResult result)
        {
            UpdateTestContainer(parentUuid, container => container.afters.Add(result));
            StartFixture(uuid, result);
            return this;
        }

        public AllureLifecycle UpdateFixture(Action<FixtureResult> update)
        {
            UpdateFixture(StepsWorker.GetRootStep(), update);
            return this;
        }

        public AllureLifecycle UpdateFixture(string uuid, Action<FixtureResult> update)
        {
            update.Invoke(TestExecutionContext.CurrentContext.CurrentTest.Storage().Get<FixtureResult>(uuid));
            return this;
        }


        public AllureLifecycle StopFixture(Action<FixtureResult> beforeStop)
        {
            UpdateFixture(beforeStop);
            return StopFixture(StepsWorker.GetRootStep());
        }

        public AllureLifecycle StopFixture(string uuid)
        {
            var fixture =
                TestExecutionContext.CurrentContext.CurrentTest.Storage().Remove<FixtureResult>(uuid);
            //StepsWorker.ClearStepContext();
            fixture.stage = Stage.finished;
            fixture.stop = ToUnixTimestamp();
            return this;
        }

        public AllureLifecycle StopFixture(string uuid, Action<FixtureResult> beforeStop)
        {
            UpdateFixture(uuid, beforeStop);
            var fixture =
                TestExecutionContext.CurrentContext.CurrentTest.Storage().Remove<FixtureResult>(uuid);
            //StepsWorker.ClearStepContext();
            fixture.stage = Stage.finished;
            fixture.stop = ToUnixTimestamp();
            return this;
        }

        #endregion

        #region TestCase

        public AllureLifecycle StartTestCase(string containerUuid, TestResult testResult)
        {
            UpdateTestContainer(containerUuid, c => c.children.Add(testResult.uuid));
            return StartTestCase(testResult);
        }

        public AllureLifecycle StartTestCase(TestResult testResult)
        {
            testResult.stage = Stage.running;
            testResult.start = ToUnixTimestamp();
            TestExecutionContext.CurrentContext.CurrentTest.Storage().Put(testResult.uuid, testResult);
            //StepsWorker.StartStep(testResult.uuid);
            return this;
        }

        public AllureLifecycle UpdateTestCase(string uuid, Action<TestResult> update)
        {
            update.Invoke(TestExecutionContext.CurrentContext.CurrentTest.Storage().Get<TestResult>(uuid));
            return this;
        }

        public AllureLifecycle UpdateTestCase(Action<TestResult> update)
        {
            var uuid = TestExecutionContext.CurrentContext.CurrentTest.GetPropAsString(AllureConstants.TestUuid);
            return UpdateTestCase(uuid, update);
        }

        public AllureLifecycle StopTestCase(Action<TestResult> beforeStop)
        {
            var uuid = TestExecutionContext.CurrentContext.CurrentTest.GetPropAsString(AllureConstants.TestUuid);
            UpdateTestCase(uuid, beforeStop);
            return StopTestCase(uuid);
        }

        public AllureLifecycle StopTestCase(string uuid, bool updateStopTime = true)
        {
            var testResult = TestExecutionContext.CurrentContext.CurrentTest.Storage().Get<TestResult>(uuid);
            testResult.stage = Stage.finished;
            if (updateStopTime) testResult.stop = ToUnixTimestamp();
            //StepsWorker.ClearStepContext();
            return this;
        }

        public AllureLifecycle WriteTestCase(string uuid)
        {
            _writer.Write(TestExecutionContext.CurrentContext.CurrentTest.Storage().Remove<TestResult>(uuid));
            return this;
        }

        #endregion

        #region Step

        /// <summary>
        ///     Use this function carefully.
        ///     <para></para>
        ///     If you use static fields or just fields with already assigned values and in the future they will not change, and
        ///     tests are run in parallel, the behavior may be unpredictable.
        /// </summary>
        /// <param name="action">Action in exception</param>
        /// <returns></returns>
        public AllureLifecycle SetGlobalActionInException(Action action)
        {
            if (GlobalActionsInException == null) GlobalActionsInException = new List<Action>();
            GlobalActionsInException.Add(action);
            return this;
        }

        /// <summary>
        ///     The function specifies additional behavior of the system with exceptions in methods Instance.RunStep and
        ///     Verify.That
        /// </summary>
        /// <param name="action">Action in Exception</param>
        /// <returns></returns>
        public AllureLifecycle SetCurrentTestActionInException(Action action)
        {
            if (CurrentTestActionsInException == null) CurrentTestActionsInException = new List<Action>();
            CurrentTestActionsInException.Add(action);
            return this;
        }

        public void RunStep(Action stepBody, params object[] stepParams)
        {
            var stepName = StepRunner.GetCallerMethodName();
            StepRunner.Run<object>(stepName, stepBody, true, Status.failed, stepParams);
        }

        public TResult RunStep<TResult>(Func<TResult> stepBody, params object[] stepParams)
        {
            var stepName = StepRunner.GetCallerMethodName();
            return StepRunner.Run<TResult>(stepName, stepBody, true, Status.failed, stepParams);
        }

        public void RunStep(string stepName, Action stepBody, params object[] stepParams)
        {
            StepRunner.Run<object>(stepName, stepBody, true, Status.failed, stepParams);
        }

        public TResult RunStep<TResult>(string stepName, Func<TResult> stepBody, params object[] stepParams)
        {
            return StepRunner.Run<TResult>(stepName, stepBody, true, Status.failed, stepParams);
        }

        public AllureLifecycle StartStep(string uuid, StepResult result)
        {
            StartStep(StepsWorker.GetCurrentStep(), uuid, result);
            return this;
        }

        public AllureLifecycle MakeStepWithExMessage(int assertsBeforeCount, string stepName, Exception ex,
            Status stepStatus)
        {
            var exMsg = ex.Message;
            if (assertsBeforeCount != TestContext.CurrentContext.Result.Assertions.Count() && ex.InnerException == null)
            {
                exMsg = MakeExMessageWithoutStepName(stepName, TestContext.CurrentContext.Result.Assertions.Last()
                    .Message
                    .Trim());
                if (string.IsNullOrEmpty(exMsg)) return this;
                StartStepAndStopIt(ex, exMsg, stepStatus);
            }
            else if (string.IsNullOrEmpty(exMsg))
            {
                return this;
            }
            else if (ex.InnerException == null)
            {
                exMsg = MakeExMessageWithoutStepName(stepName, exMsg);
                StartStepAndStopIt(ex, exMsg, Status.failed);
            }
            else
            {
                if (ex.GetType() != typeof(TargetInvocationException))
                {
                    var exStepMsg = MakeExMessageWithoutStepName(stepName, exMsg);
                    StartStepAndStopIt(ex, exStepMsg, Status.failed);
                }

                StepRunner.GetInnerExceptions(ex).ToList().ForEach(inEx =>
                {
                    var msg = MakeExMessageWithoutStepName(stepName, inEx.Message);
                    StartStepAndStopIt(inEx, msg, Status.failed);
                });
            }

            return this;
        }

        private string MakeExMessageWithoutStepName(string stepName, string exMsg)
        {
            exMsg = exMsg.Trim();
            var firstStepIndex = exMsg.IndexOf(stepName, StringComparison.Ordinal);
            if (firstStepIndex != -1) exMsg = exMsg.Substring(stepName.Length);
            var indexOfTire = exMsg.IndexOf('-');
            if (indexOfTire != -1) exMsg = exMsg.Substring(0, indexOfTire).Trim();
            return exMsg;
        }

        internal AllureLifecycle StartStepAndStopIt(Exception ex, string stepName, Status stepStatus)
        {
            var exName = "";
            if (ex == null)
            {
            }
            else
            {
                exName = ex is ThreadAbortException ? "" : $"{ex.GetType().Name}: ";
            }

            var uuid = $"{Guid.NewGuid():N}";
            Instance.StartStep($"{exName}{stepName}", uuid);
            Instance.UpdateStep(uuid, _ => _.status = stepStatus);
            Instance.StopStep(uuid);
            return this;
        }

        public void StartStep(string stepName, string uuid)
        {
            var stepResult = new StepResult
            {
                name = stepName,
                start = ToUnixTimestamp()
            };
            Instance.StartStep(uuid, stepResult);
        }


        public AllureLifecycle StartStep(string parentUuid, string uuid, StepResult stepResult)
        {
            stepResult.stage = Stage.running;
            stepResult.start = ToUnixTimestamp();
            StepsWorker.StartStep(uuid);
            StepsWorker.AddSubStep(parentUuid, uuid, stepResult);
            return this;
        }

        public AllureLifecycle UpdateStep(Action<StepResult> update)
        {
            update.Invoke(TestExecutionContext.CurrentContext.CurrentTest.Storage()
                .Get<StepResult>(StepsWorker.GetCurrentStep()));
            return this;
        }

        public AllureLifecycle UpdateStep(string uuid, Action<StepResult> update)
        {
            update.Invoke(TestExecutionContext.CurrentContext.CurrentTest.Storage().Get<StepResult>(uuid));
            return this;
        }

        public AllureLifecycle StopStep(Action<StepResult> beforeStop)
        {
            UpdateStep(beforeStop);
            return StopStep(StepsWorker.GetCurrentStep());
        }

        public AllureLifecycle StopStep(string uuid)
        {
            var step = TestExecutionContext.CurrentContext.CurrentTest.Storage().Remove<StepResult>(uuid);
            step.stage = Stage.finished;
            step.stop = ToUnixTimestamp();
            StepsWorker.StopStep();
            return this;
        }

        #endregion

        #region Attachment

        public enum AttachFormat
        {
            Xml,
            Json,
            ImagePng,
            Txt,
            Video,
            Html
        }

        public AllureLifecycle AddAttachment(string name, AttachFormat type, byte[] bytesArray,
            string fileExtension = "")
        {
            var asString = Encoding.Default.GetString(bytesArray);
            switch (type)
            {
                case AttachFormat.Xml:
                    return AddAttachment(name, type, asString);
                case AttachFormat.Json:
                    return AddAttachment(name, type, asString);
                case AttachFormat.ImagePng:
                    return AddAttachment(name, "image/png", bytesArray, ".png");
                case AttachFormat.Txt:
                    return AddAttachment(name, type, asString);
                case AttachFormat.Html:
                    return AddAttachment(name, type, asString);
                case AttachFormat.Video:
                    throw new ArgumentException(
                        $"You cant use \"{type}\" argument at this method. Try use method with FileInfo parameter.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public AllureLifecycle AddAttachment(string name, AttachFormat type, FileInfo file)
        {
            string inside;
            using (var u = new StreamReader(file.OpenRead()))
            {
                inside = u.ReadToEnd();
            }

            switch (type)
            {
                case AttachFormat.Xml:
                    return AddAttachment(name, AttachFormat.Xml, inside);
                case AttachFormat.Json:
                    return AddAttachment(name, AttachFormat.Json, inside);
                case AttachFormat.ImagePng:
                    var bytes = Encoding.Default.GetBytes(inside);
                    return AddAttachment(name, "image/png", bytes, ".png");
                case AttachFormat.Txt:
                    return AddAttachment(name, AttachFormat.Txt, inside);
                case AttachFormat.Video:
                    return AddAttachment(name, "video/mp4", file.FullName);
                case AttachFormat.Html:
                    return AddAttachment(name, AttachFormat.Html, inside);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public AllureLifecycle AddAttachment(string name, AttachFormat type, string content, string fileExtension = "")
        {
            switch (type)
            {
                case AttachFormat.ImagePng:
                case AttachFormat.Video:
                    throw new ArgumentException(
                        $"You cant use \"{type}\" argument at this method. Try use method with FileInfo or with bytes array parameter.");
                case AttachFormat.Xml:
                    content = XDocument.Parse(content).ToString();
                    if (string.IsNullOrEmpty(fileExtension)) fileExtension = ".xml";
                    return AddAttachment(name, "text/xml", Encoding.UTF8.GetBytes(content), fileExtension);
                case AttachFormat.Json:
                    var obj = JsonConvert.DeserializeObject(content);
                    content = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    if (string.IsNullOrEmpty(fileExtension)) fileExtension = ".json";
                    return AddAttachment(name, "application/json", Encoding.UTF8.GetBytes(content), fileExtension);
                case AttachFormat.Txt:
                    if (string.IsNullOrEmpty(fileExtension)) fileExtension = ".txt";
                    return AddAttachment(name, "text/txt", Encoding.UTF8.GetBytes(content), fileExtension);
                case AttachFormat.Html:
                    content = WebUtility.HtmlEncode(content);
                    if (string.IsNullOrEmpty(fileExtension)) fileExtension = ".html";
                    return AddAttachment(name, "text/html", Encoding.UTF8.GetBytes(content), fileExtension);
                default:
                    throw new ArgumentException($"You cant use \"{type}\" argument at this method.");
            }
        }

        public AllureLifecycle AddAttachment(string name, string type, string path)
        {
            var file = new FileInfo(path);
            if (!file.Exists)
                throw new ArgumentException(
                    $"Cant find file at path \"{path}\".\nMake sure you are using the correct path to the file.");
            var fileExtension = file.Extension;
            return AddAttachment(name, type, File.ReadAllBytes(path), fileExtension);
        }

        public AllureLifecycle AddAttachment(string name, string type, byte[] content, string fileExtension = "")
        {
            var source = $"{Guid.NewGuid():N}{AllureConstants.AttachmentFileSuffix}{fileExtension}";
            var attachment = new Attachment
            {
                name = name,
                type = type,
                source = source
            };
            _writer.Write(source, content);
            TestExecutionContext.CurrentContext.CurrentTest.Storage().Get<ExecutableItem>(StepsWorker.GetCurrentStep())
                .attachments.Add(attachment);
            return this;
        }

        public AllureLifecycle AddAttachment(string path, string name = null)
        {
            name = name ?? Path.GetFileName(path);
            var type = MimeTypesMap.GetMimeType(path);
            return AddAttachment(name, type, path);
        }

        #endregion

        #region Extensions

        public void CleanupResultDirectory()
        {
            _writer.CleanUp(false);
        }

        public AllureLifecycle AddScreenDiff(string testCaseUuid, string expectedPng, string actualPng, string diffPng)
        {
            AddAttachment(expectedPng, "expected")
                .AddAttachment(actualPng, "actual")
                .AddAttachment(diffPng, "diff")
                .UpdateTestCase(testCaseUuid, x => x.labels.Add(Label.TestType("screenshotDiff")));

            return this;
        }

        #endregion


        #region Privates

        private static void MakeCategoriesFile(List<Configuration.Category> categoriesList, FileSystemInfo dir)
        {
            if (categoriesList == null || categoriesList.Count == 0 ||
                string.IsNullOrEmpty(categoriesList.First().name)) return;
            var fileName = AllureConstants.CategoriesFileName;
            var jsonContent = JsonConvert.SerializeObject(categoriesList,
                new JsonSerializerSettings
                    {NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented});
            var dirPath = dir.FullName;
            var filePath = Path.Combine(dirPath, fileName);
            var file = new FileInfo(filePath);
            if (file.Exists) file.Delete();

            using (var sw = new StreamWriter(filePath))
            {
                sw.WriteLine(jsonContent);
            }
        }

        private void StartFixture(string uuid, FixtureResult fixtureResult)
        {
            fixtureResult.statusDetails = new StatusDetails();
            TestExecutionContext.CurrentContext.CurrentTest.Storage().Put(uuid, fixtureResult);
            fixtureResult.stage = Stage.running;
            fixtureResult.start = ToUnixTimestamp();
            //StepsWorker.ClearStepContext();
            StepsWorker.StartStep(uuid);
        }

        internal string GetDirectoryWithResults(string outDir)
        {
            if (outDir == AllureConstants.DefaultResultsFolder)
                outDir = Path.Combine(WorkspaceDir, AllureConstants.DefaultResultsFolder);
            if (outDir == "temp") outDir = Path.Combine(Path.GetTempPath(), AllureConstants.DefaultResultsFolder);
            return outDir;
        }

        internal void SetDefaultResultsWriter(string outDir)
        {
            var dir = GetDirectoryWithResults(outDir);
            _writer = new FileSystemResultsWriter(dir);
        }

        #endregion
    }
}