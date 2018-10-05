# C# Allure with many new features
[![nuget](http://flauschig.ch/nubadge.php?id=Noksa.NUnit.Allure)](https://www.nuget.org/packages/Noksa.NUnit.Allure/)

## How to use

Just inherit your base class or test class from `[AllureReport]`<br/>
Everything else is optional and is not required to enable reporting.

### Some new features:
1) Steps in `[Setup]` `[TearDown]` and `[OneTimeSetup]` `[OneTimeTearDown]` will be displayed in report:

![alt text](https://github.com/Noksa/Allure.NUnit/blob/master/TestsSamples/ScreenshotsReadme/StepsExample.png)

2) Using `allureConfig.json` you can add your own categories to the report.

3) Using `allureConfig.json`, you can add environment variables to the report.
You can add both constants and system/CI server variables in this way:
* Constants:<br/> ![alt text](https://github.com/Noksa/Allure.NUnit/blob/master/TestsSamples/ScreenshotsReadme/ConstantsEnvExample.png)<br/>
* Runtime variables in `environment.runtime` block, syntax: `Namespace.ClassName.MemberName`:<br/> ![alt text](https://github.com/Noksa/Allure.NUnit/blob/master/TestsSamples/ScreenshotsReadme/RuntimeVariablesExample.png)<br/>
<br/>__Works with public/internal/private static fields, static properties and with constants.__<br/>
* System or CI server variables in `environment.runtime` block, syntax `System.Environment.NameOfVariable`:<br/>
![alt text](https://github.com/Noksa/Allure.NUnit/blob/master/TestsSamples/ScreenshotsReadme/SystemVariablesExample.png)


4) New `AllureLifecycle.Instance.Verify` class to add steps without stopping the test, if an exception was thrown or the check failed.
In this case, the error information will be added as a substep in a running step with messages of all nested exceptions.

5) New `AllureLifecycle.Instance.RunStep` method for easy recording of steps.<br/> 
Same as methods in `AllureLifecycle.Instance.Verify` class, but stops the test if an error occurred in the step.

You can also use method `AllureLifecycle.Instance.RunStep` and `AllureLifecycle.Instance.Verify` methods inside each other as many times as you like.


`AllureLifecycle.Instance.RunStep` example, test stopped after fail:<br/><br/>
![alt text](https://github.com/Noksa/Allure.NUnit/blob/master/TestsSamples/ScreenshotsReadme/RunStepExample.PNG)
<br/><br/>

`AllureLifecycle.Instance.Verify` example, test not stopped at fail:<br/><br/>
![alt text](https://github.com/Noksa/Allure.NUnit/blob/master/TestsSamples/ScreenshotsReadme/VerifyStepExample.PNG)<br/>
<br/><br/>

`AllureLifecycle.Instance.Verify` multiple calls example, test collect all errors:<br/><br/>
![alt text](https://github.com/Noksa/Allure.NUnit/blob/master/TestsSamples/ScreenshotsReadme/MultiVerifyExample.PNG)
<br/><br/>
6) Two new tuning methods:
`AllureLifecycle.Instance.SetGlobalActionInException (Action action)` and `AllureLifecycle.Instance.SetCurrentTestActionInException (Action action)`<br/>
You can specify which actions you need to additionally perform if an error occurred in the `RunStep` method or in the checks in the `Verify` class.<br/>

For example, you can add a screenshot to attach to the report:
```
AllureLifecycle.Instance.SetCurrentTestActionInException(() =>
    {
        DriverManager.MakeScreenshotAtStep ();
    });
```
<br/>An example of the config can be found [here](https://github.com/Noksa/Allure.NUnit/blob/master/Allure/allureConfig.json).
<br/><br/>
Information about all new features in Russian can be found in this topic: https://automated-testing.info/t/csharp-allure-classic-nunit-with-improvements/20715


For more samples, see the [TestsSamples](https://github.com/Noksa/Allure.NUnit/tree/master/TestsSamples) project.

#### If you have questions contact me at telegram `@doomjke`
