# Tips on troubleshooting failing tests



## General tips

- When a test fails it'll create a dump in the test execution's folder, in a new *FailureDumps* folder. This should help you pinpoint where the issue is even if the test was run in the CI environment, and you can't reproduce it locally. The dump contains the following:
    - The Orchard application's folder, including settings files, the SQLite DB if used, logs, etc. that you can utilize to see log entries and to run the app from that state.
    - Browser logs, i.e. the developer console output
    - Screenshots of each page in order the test visiting them, as well as when the test failed (Windows Photo Viewer won't be able to open it though, use something else like the Windows 10 Photos app)
    - The HTML output on the page the test failed.
    - Any direct output of the test (like the exception thrown) as well as a log of the operations it completed.
    - If accessibility was checked and asserting it failed then an accessibility report will be included too. 
    - If HTML validation was done and asserting it failed then an HTML validation report will be included too. 
- Run tests with the debugger attached to stop where the test fails. This way you can take a good look at what's in the driven browser window so you can examine the web page. Alternatively, if you want to debug the web app then you can run the test without debugging and attach the debugger to the *dotnet.exe* process running the app.
- An aborted test can leave processes open (a failed test should clean up after itself nevertheless). Look for *dotnet.exe* and *chromedriver.exe* processes (and also for *geckodriver.exe*, *IEDriverServer.exe* and *msedgedriver.exe* if you use other browsers) with [Process Explorer](https://docs.microsoft.com/en-us/sysinternals/downloads/process-explorer) and kill them. You can also use the included *KillLeftoverProcesses.bat* script to kill these (optionally install the [Command Task Runner](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.CommandTaskRunner) Visual Studio Extension so you can run this script from Task Runner Explorer directly). If you see build warnings or errors with files being locked then most possibly a *dotnet.exe* process is locking them (or an IIS Express one if you have run the app from VS too). If you get an `IOException` with the message along the lines of "Starting the Orchard Core application via dotnet.exe failed with the following output: Unhandled exception. System.IO.IOException: Failed to bind to address https://127.0.0.1:5122: address already in use." then that's also because there's a leftover *dotnet.exe* using that port.
- All tests failing is a telltale sign of the setup failing (if your tests run with a fresh setup), check out the failure dumps. Note that app and browser logs are asserted on only at the end of test execution so a test will fail at the very end even if some swallowed exception was logged on the first page view.
- If tests fail to start the Orchard Core app with an "Unable to configure HTTPS endpoint. No server certificate was specified, and the default developer certificate could not be found or is out of date." error then you have to install a new development certificate. Do the following under the user executing the tests:
    1. Run the `dotnet dev-certs https --check --verbose` command. It should display "No valid certificates found." If it displays "A valid certificate was found." then go to step 3.
    2. Run `dotnet dev-certs https --clean`.
    3. Run `dotnet dev-certs https --trust` and accept the security warning that pops up. (This is why we can't automate all of this.)
    4. Now `dotnet dev-certs https --check --verbose` should display "A valid certificate was found."
- If you have retries configured for the tests and they've been running for a long time without any visible progress then most possibly all of them are failing. This can happen e.g. if the setup or initialization steps are failing. However, by default the `OrchardCoreUITestExecutorConfiguration.FastFailSetup` configuration will quickly fail all affected tests if a common setup operation is failing, without retrying it for all tests.


## Issues with driving browsers

- If text form fields aren't always actually filled during the test then the recommended standard is to do the `Click()` - `Clear()` - `SendKeys()` pattern instead of just `SendKeys()`. Our `ClickAndFillInWith()` will do all of this.
- UI tests are not exact and a lot of things can randomly break. A typical issue is Selenium not finding elements when they're supposedly there. The foremost reason of this is that it attempted to access it before the given element was ready (like after a page load).
  - Atata's `Get()` (and `GetAll()`, `Exists()`, `Missing()`) includes retry logic specifically for this. If some slow operation is still failing then just increase its timeout: It's better to have randomly slower tests and hackish solutions than to have randomly failing (flaky) tests (which should be avoided at all costs because it'll make the whole test suite unreliable).
  - Be aware that if a page contains e.g. an element with the ID "Email", then you navigate away and the next page contains an element with the ID "Email" as well then a command will match it on the first page too, without waiting for the navigation to finish, even if you have a command to navigate away before that. So in such cases be sure to somehow execute a match that will indicate the navigation being done (like an element unique to the first page `Missing()` or `Get()`-ting a new one).
  - While tempting, don't use `Thread.Sleep()` to overcome a randomly failing condition! Use the above detailed mechanisms to do actions based on UI elements' presence.
  - When you experience a test being flaky but can't figure out what the issue might be then you can try to make the operation after which the test fails artificially slow (like putting a `Thread.Sleep()` into the app being tested). It'll show you if the test doesn't properly await the operation but continues, causing the flakyness.
- If you can't get a link or button click working for some random reason but it's one that initiates a page leave then use `ClickSafelyUntilPageLeave()`.
- When you're running a lot of tests in parallel then you may see random browser driver startup errors of the following sorts: 
  > OpenQA.Selenium.WebDriverException: Creating the web driver failed with the message "Cannot start the driver service on http://localhost:50526/". Note that this can mean that there is a leftover web driver process that you have to kill manually.
  
    This, unfortunately, is not something we can do much about. However, the automatic test retries will prevent tests failing due to random errors like this.


## Headless mode

- A difference in the results of a normal and headless execution is almost always because of different window sizes since headless mode uses small browser windows by default (if this is the case, you should see the issue right away from the failure dump's screenshot). To overcome this, always set the browser window's size explicitly with `SetBrowserSize()` (which is a good practice anyway, because otherwise in normal mode it won't be reliable either). Note that if you switch windows/tabs during the test you may need to set the browser size again. 
- To further figure out why headless execution is different try the following:
  - Save screenshots (with e.g. `context.Driver.GetScreenshot().SaveAsFile()`) in between steps in the test to see what actually happens. Similarly, you can save the HTML source of the page on each step (with e.g. `File.WriteAllText("Source.html", context.Scope.Driver.PageSource)`).
  - Just as during normal execution attach the debugger to see the failing step in action. At that point, you can access the app by its URL (it's in `context.Driver.Url`) from a normal browser and look around to see what the problem may be. Alternatively, for Chrome you can configure a remote debugging port via `BrowserConfiguration.BrowserOptionsConfigurator` with e.g. `((ChromeOptions)options).AddArgument("--remote-debugging-port=9222");`. Then if you open http://localhost:9222/ you'll be able to instruct the headless browser instance. During test execution you'll also be able to watch the test running. See the [Headless Chromium docs](https://chromium.googlesource.com/chromium/src/+/lkgr/headless/README.md) for details.


## SQL Server

- When using SQL Server put the UI tests' executable (or the solution you're working on) under a short path that doesn't contain any accented characters. Those can break SQL Server's backup and restore operations and you'll see exceptions of the sort of "BACKUP DATABASE is terminating abnormally."
- If you canceled a lot of tests mid-flight then test databases will remain. You can clean them up quickly with the following SQL script:
    ```sql
    DECLARE @sql NVARCHAR(MAX) = ''
    SELECT @sql = @sql 
      + 'ALTER DATABASE [' + [name] + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; '
      + 'DROP DATABASE [' + [name] + ']; '
    FROM sys.databases 
    WHERE [name] like 'LombiqUITestingToolbox_%'

    EXEC sp_executesql @sql 
    ```


## Monkey testing

- Errors uncovered by monkey testing functionality can be reproduced locally by executing the same test with the same random seed by setting `MonkeyTestingOptions.BaseRandomSeed` with the value the test failed. If `BaseRandomSeed` is generated then you can see it in the log; if you specified it then nothing else to do.
- The last monkey testing interaction before a failure is logged. You can correlate with the coordinates of it with the last page screenshot.
- If you want to test the failed page granularly, you can write a test that navigates to that page and executes `context.TestCurrentPageAsMonkey(_monkeyTestingOptions, 12345);`, where `12345` is the random seed number that can be found in a failed test log.
- It is also possible to set a larger time value to the `MonkeyTestingOptions.GremlinsAttackDelay` property in order to make gremlin interaction slower, thus allowing you to watch what's happening.
