# Creating tests



## Creating a test project

Reference `Lombiq.Tests.UI` from your test project, and add a reference to the `Microsoft.NET.Test.Sdk` package. Set `<IsPackable>false</IsPackable>` in the project too unless you want NuGet packages to be generated (if the solution is packaged up).

For a sample test project see [`Lombiq.Tests.UI.Samples`](../../Lombiq.Tests.UI.Samples/Readme.md).

We also recommend always running some highly automated tests that need very little configuration:

- The suite of tests for checking that all the basic Orchard Core features work, like login, registration, and content management. Use `context.TestBasicOrchardFeatures()` to run all such tests but see the other, more granular tests too. This is also demonstrated in `Lombiq.Tests.UI.Samples` and in [this video](https://www.youtube.com/watch?v=jmhq63sRZrI).
- [Monkey tests](https://en.wikipedia.org/wiki/Monkey_testing) can also be useful. Use `context.TestCurrentPageAsMonkeyRecursively()` to run a monkey testing process, which walks through site pages and does random interactions with pages, like clicking, scrolling, form filling, etc. It's recommended to have at least 3 monkey tests that execute with different user states: As an admin, as a regular registered user and as an anonymous user. The admin test can start execution on admin dashboard page, while other tests can start on home page.

## Steps for creating a test class

Keep test classes relatively small, with just a couple of test cases in them, and only put tightly related tests into the same class. This not just makes working with tests easier but also allows for a higher degree of parallel test execution, making testing faster.

1. For complex and important Orchard-level pages that we re-use in multiple tests we create Atata Page classes, e.g. `OrchardSetupPage`, and instead of recording commands we code them directly, see e.g. `OrchardSetupPageExtensions.Setup()`. You can then do Atata testing as usual by starting with `context.GoToPage<TPage>();`. For simpler cases, however, we create recorded tests. The rest of this guide shows how to create such recorded tests. So create a class for this with the basics of a test method, but no commands yet. You can inherit your test class from `OrchardCoreUITestBase` to makes things simpler.
2. Launch the app and go to a page where the next click would lead to the first page of the tested feature. This is most possibly the homepage or the dashboard, both of which you can easily reach with helpers in the test.
3. For your first few tests we recommend you use the guidance of a step recording tool (but after that feel free to just write tests directly!). For this, open Selenium IDE, create a new project (it doesn't matter, we won't use it) and inside it create a new test case (again, doesn't matter).
4. Start recording. Now everything you do will be recorded as commands in Selenium IDE. Sometimes it messes up the order but don't worry, you can reorder commands freely.
5. Click through the app and use the feature you want to test as you'd use it normally (or try to break it for negative testing).
    - When you want to check whether something is on a site as it should then right click on the element → Selenium IDE → Assert or Verify and then select the appropriate condition. Assert will make the test fail if there is a mismatch, Verify won't (but you have to determine how to handle it, like write some message to the output; Selenium will just generate the same assertion expressions for them). Do make sure to use these appropriately, since most of the time it's not enough to just click through pages and only fail the test if there's an exception but you need to make sure the page looks like it should (e.g. is what you just saved actually loaded, are you logged in as you should?).
    - When the app opens new tabs or windows then don't close them. This way, the browser logs can be checked for all of them without any manual intervention. If you do want to prevent browser log checking for a tab (like when you open another app that you don't want to test) then you can close it. Additionally, `context.AssertBrowserLogAsync()` can be used to check the browser logs explicitly any time.
6. Once you're done stop the recording. While still in the command list:
      - Reorder commands if necessary.
      - Make sure that the selectors Selenium used as targets are appropriate (like they are indeed unique, as unfragile and future-proof if possible).
        - Use CSS selectors when the element can be better pinpointed from the HTML structure, and use XPATH selectors if the content of the element helps to match it (like the text of a link).
        - Don't make the selector depend on a user-facing string if possible (as these can change more frequently).
        - Aim for specific selectors that only match the element we want (there could be more similar ones in the future, like new form fields) but don't make it overly tied to the HTML structure. The ID selector is the most suitable for this, if the ID is indeed unique, as it should be. Usually links don't have IDs, link texts are the best most of the time, even though they're user-facing. Try to avoid using positional selectors (like `id('company-divisions')/tbody/tr[1]/td[8]` to find a cell in a table) as those are fragile - unless the positions specifically need to be tested too. Instead, if there isn't already a suitable ID or class on the element that you can address with CSS selectors, add a `data-testid` attribute with a unique ID and target that. For example, use `[data-testid="ID"]` as the CSS selector in the test.
7. Export the test case to C\# xUnit. You won't need Selenium to generate any comments.
8. Copy the commands to the previously prepared test class.
      - Replace what we do differently:
        - For the simple 1-1 replacements use this in the Notepad++ Replace dialog with "Regular expression" Search Mode: `(driver)|(FindElement\((.*)\)\.Click\()|(FindElement\((.*)\)\.SendKeys\()|(FindElements)|(FindElement)` for "Find what" and `(?1context)(?2ClickReliablyOn\($3)(?4ClickAndFillInWithRetries\($5, )(?6GetAll)(?7Get)` for "Replace with".
        - Note that the generated test does operations on an `IWebDriver` instance. While this is available in our tests you'll mostly use the ambient `UITestContext`. So change all `driver` references to `context` (extensions are available for this context to proxy usual driver calls to the driver contained in it and you can also access the driver directly).
        - Replace `FindElement()` calls with our `Get()`, `FindElements()` calls with `GetAll()` (unless it's an existence check on an item, then use `Exists()`; don't use `GetAll()` for existence check as it's much slower if no element exists. (These methods use Atata's similarly named ones behind the scenes. For more information on what can you do with them see [the Atata docs](https://github.com/atata-framework/atata-webdriverextras#usage).)
        - Replace `SendKeys()` calls with our `ClickAndFillInWithRetries()` (like there was `driver.FindElement(By.Id("my-id")).SendKeys("my text")` then replace it with `context.ClickAndFillInWithRetries(By.Id("my-id"), "my text")`. If there is a `Clear()` or `Click()` call before a `SendText()` call then remove it because `ClickAndFillInWithRetries()` already does these, together with retries if it doesn't succeed.
        - Replace `Click()` calls with `ClickReliablyOn()`, as in `driver.FindElement(By.Id("my-id")).Click()` should now be `context.ClickReliablyOn(By.Id("my-id")`. This won't fail randomly with certain clicks. however, be sure not to use them on `option` tags as that'll throw an exception.
        - Replace `Assert` calls with Shouldly ones. If any selector would make the command fragile (by e.g. making it depend on the number of elements in a container) then try to work around in C\# instead (like instead of selecting a specific element among multiple ones in a container and checking its text with `ShouldBe()`, check the text of the whole container with `ShouldContain()`).
      - Make use of our helpers that cover some common operations like `driver.LogIn()` (quickly run through the existing tests to see what's available).
      - If the code is interacting with checkboxes on the Orchard admin then be aware that the admin theme hides those to make them prettier. Thus selectors on them will fail. To overcome this you can make them visible again with `MakeAdminCheckboxesVisible()`.
      - Sanity check the commands, remove unneeded ones.
      - If there are a lot of commands then add line breaks between sections, like between groups of a form, different pages, and between the Arrange and Assert sections (though with UI tests every command is an assertion too).
      - Add documentation if something is hard to understand.
      - It's good practice to always explicitly set the size of the browser window so it doesn't depend on the machine executing the test. You can do this with the `UITestContext.SetBrowserSize()` shortcut.


## Notes on test execution

- By default any non-warning entry in the Orchard log and any warning or error in the browser log will fail a test.
- Individual driver operations are retried with a timeout, and failing tests are also retried. While you're developing a test this might not be what you want but rather for the tests to fail as quickly as possible. For this, lower the timeout values in and the try count, see [Configuration](Configuration.md).
- UI tests can be quite computationally demanding since your machine is hammering multiple concurrently running Orchard apps via separate browser processes while also serving them, and on top of this potentially also fetching resources from CDNs and other external services. To prevent tests failing locally simply because the machine is overwhelmed and they time out (or just taking more time than if the resources weren't completely saturated) the number of parallel tests is usually capped at a generally safe level in an xUnit config file in the root of the test project (*xunit.runner.json*). If your PC can handle it then feel free to increase the limit for the duration of local testing (but don't commit these changes).
- Accessibility checking is available with [axe](https://github.com/dequelabs/axe-core). [Check out](https://github.com/TroyWalshProf/SeleniumAxeDotnet) what you can do with the library we use. You can also use the [axe Chrome extension](https://chrome.google.com/webstore/detail/axe-web-accessibility-tes/lhdoppojpmngadmnindnejefpokejbdd) to aid you fixing issues.
- In case of testing Trumbowyg editors, make sure each editor is placed inside its own uniquely named container so that multiple editors on the same page can be identified separately.
- When asserting on dates be mindful about time zones. It's the easiest if both the app and the tests work with UTC.


## Tips on optimizing tests

- The biggest performance gain of test execution is parallelization. Note that xUnit [by design only executes test collections in parallel](https://github.com/xunit/xunit/issues/1227), not tests within them. So instead of large test collections (which mostly equals test classes) break them up into smaller ones.
- If you want different Orchard settings for tests then you can achieve this with recipes created just for tests, used when launching a test.
- Load static resources locally instead of from a CDN. While using CDNs, especially shared CDNs for framework files (like jQuery, Bootstrap) can provide a lot of performance benefits in production apps they seriously limit (parallel) UI test execution. This is because all tests are executed in a new and independent browser session thus they don't benefit from long-living caches. Instead, loading many external resources in parallel can saturate the given machine's internet connection, causing tests to be slower or even fail due to timeouts.
- The accompanying Lombiq.Tests.UI.Shortcuts module adds shortcuts for common operations so you can do something directly in the app instead of going through the UI (like logging in), making test execution much faster if you're not actually testing the given features. See `ShortcutsUITestContextExtensions`. In a similar fashion, you can make your tests a lot faster if you don't execute long operations repeatedly from the UI but just test them once and then use a shortcut for them. Just be sure to add the `[DevelopmentAndLocalhostOnly]` attribute from [Helpful Libraries](https://github.com/Lombiq/Helpful-Libraries/) to shortcut controllers so they can't be accessed in any other scenario.
