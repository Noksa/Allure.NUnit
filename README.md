# C# Allure with many new features.

Some new features:
1) Steps in `[Setup]` `[TearDown]` and `[OneTimeSetup]` `[OneTimeTearDown]` will be displayed in report:

![alt text](https://snag.gy/sIhcX4.jpg)

2) Using `allureConfig.json` you can add your own categories to the report.

3) Using `allureConfig.json`, you can add environment variables to the report.
You can add both constants and system/CI server variables in this way:
* Constants:<br/> ![alt text](https://automated-testing.info/uploads/default/original/2X/1/1f114dad16bd8d71dbf17534c0573882a41cac06.png)<br/>
* Runtime variables in `environment.runtime` block, syntax: `Namespace.ClassName.MemberName`:<br/> ![alt text](https://automated-testing.info/uploads/default/optimized/2X/2/241b115c63437a39c63658cd7d5ab8fd1b0c9cbd_1_700x207.png)<br/>
<br/>Works with public/internal/private static fields, static properties and with constants.
* System or CI server variables in `environment.runtime` block, syntax `System.Environment.NameOfVariable`:<br/>
![alt text](https://automated-testing.info/uploads/default/optimized/2X/3/3cc9515b7bd134f15214b856a4ab5b6c8c74e6ac_1_700x224.png)


4) New `AllureLifecycle.Instance.Verify` class to add steps without stopping the test, if an exception was thrown or the check failed.
In this case, the error information will be added as a substep in a running step with messages of all nested exceptions.

5) New `AllureLifecycle.Instance.RunStep` class for easy recording of steps.<br/> 
Same as `AllureLifecycle.Instance.Verify`, but stops the test if an error occurred in the step.

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
Information about new features in Russian can be found in this topic: https://automated-testing.info/t/csharp-allure-classic-nunit-with-improvements/20715


For more information, see the TestSamples project.

# How to use

Just inherit your base class or test class from `[AllureReport]`<br/>
Everything else is optional and is not required to enable reporting.

# If you have questions
Contact me in telegram @doomjke
