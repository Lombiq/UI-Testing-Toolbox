using Atata;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

/// <summary>
/// Provides a set of extension methods for basic Orchard features testing.
/// </summary>
public static class BasicOrchardFeaturesTestingUITestContextExtensions
{
    /// <summary>
    /// <para>
    /// Tests all the basic Orchard features. At first sets up Orchard with the recipe with the specified <paramref
    /// name="setupRecipeId"/>.
    /// </para>
    /// <para>The test method assumes that the site is not set up.</para>
    /// </summary>
    /// <param name="setupRecipeId">The ID of the recipe to be used to set up the site.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestBasicOrchardFeaturesAsync(this UITestContext context, string setupRecipeId) =>
        context.TestBasicOrchardFeaturesAsync(new OrchardCoreSetupParameters(context)
        {
            RecipeId = setupRecipeId,
        });

    /// <summary>
    /// <para>
    /// Tests all the basic Orchard features. At first sets up Orchard with optionally specified <paramref
    /// name="setupParameters"/>. By default uses new <see cref="OrchardCoreSetupParameters"/> instance with
    /// <c>"SaaS"</c><see cref="OrchardCoreSetupParameters.RecipeId"/> value.
    /// </para>
    /// <para>The test method assumes that the site is not set up.</para>
    /// </summary>
    /// <param name="setupParameters">The setup parameters.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static async Task TestBasicOrchardFeaturesAsync(
        this UITestContext context,
        OrchardCoreSetupParameters setupParameters = null)
    {
        await context.TestSetupWithInvalidAndValidDataAsync(setupParameters);
        await context.TestBasicOrchardFeaturesExceptSetupAsync();
    }

    /// <summary>
    /// <para>
    /// Tests all the basic Orchard features except for registration. At first sets up Orchard with the recipe with the
    /// specified <paramref name="setupRecipeId"/>.
    /// </para>
    /// <para>The test method assumes that the site is not set up.</para>
    /// <para>
    /// When running the headless version of Orchard Core, frontend operations shall be excluded. Utilize <paramref
    /// name="dontCheckFrontend"></paramref>> for this purpose.
    /// </para>
    /// </summary>
    /// <param name="setupRecipeId">The ID of the recipe to be used to set up the site.</param>
    /// <param name="dontCheckFrontend">Boolean to decide whether to check content on frontend.</param>>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestBasicOrchardFeaturesExceptRegistrationAsync(
        this UITestContext context,
        string setupRecipeId,
        bool dontCheckFrontend = false) =>
        context.TestBasicOrchardFeaturesExceptRegistrationAsync(
            dontCheckFrontend,
            new OrchardCoreSetupParameters(context) { RecipeId = setupRecipeId, });

    /// <summary>
    /// <para>
    /// Tests all the basic Orchard features except for registration. At first sets up Orchard with optionally specified
    /// <paramref name="setupParameters"/>. By default uses new <see cref="OrchardCoreSetupParameters"/> instance with
    /// <c>"SaaS"</c><see cref="OrchardCoreSetupParameters.RecipeId"/> value.
    /// </para>
    /// <para>The test method assumes that the site is not set up.</para>
    /// <para>
    /// When running the headless version of Orchard Core, frontend operations shall be excluded. Utilize <paramref
    /// name="dontCheckFrontend"></paramref>> for this purpose.
    /// </para>
    /// </summary>
    /// <param name="setupParameters">The setup parameters.</param>
    /// <param name="dontCheckFrontend">Boolean to decide whether to check content on frontend.</param>>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static async Task TestBasicOrchardFeaturesExceptRegistrationAsync(
        this UITestContext context,
        bool dontCheckFrontend = false,
        OrchardCoreSetupParameters setupParameters = null)
    {
        await context.TestSetupWithInvalidAndValidDataAsync(setupParameters);
        await context.TestBasicOrchardFeaturesExceptSetupAndRegistrationAsync(dontCheckFrontend);
    }

    /// <summary>
    /// <para>Tests all the basic Orchard features except for setup.</para>
    /// <para>The test method assumes that the site is set up.</para>
    /// </summary>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static async Task TestBasicOrchardFeaturesExceptSetupAsync(this UITestContext context)
    {
        await context.TestRegistrationWithInvalidDataAsync();
        await context.TestRegistrationAsync();
        await context.TestRegistrationWithAlreadyRegisteredEmailAsync();
        await context.TestLoginWithInvalidDataAsync();
        await context.TestLoginAsync();
        await context.TestContentOperationsAsync();
        await context.TestTurningFeatureOnAndOffAsync();
        await context.TestMediaOperationsAsync();
        await context.TestLogoutAsync();
    }

    /// <summary>
    /// <para>Tests all the basic Orchard features except for setup and registration.</para>
    /// <para>The test method assumes that the site is set up.</para>
    /// <para>When running headless version of Orchard Core, ContentOperations shall be excluded.</para>
    /// <para>
    /// When running the headless version of Orchard Core, frontend operations shall be excluded. Utilize <paramref
    /// name="dontCheckFrontend"></paramref>> for this purpose.
    /// </para>
    /// </summary>
    /// <param name="dontCheckFrontend">Boolean to decide whether to check content on frontend.</param>>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static async Task TestBasicOrchardFeaturesExceptSetupAndRegistrationAsync(
        this UITestContext context,
        bool dontCheckFrontend)
    {
        await context.TestLoginWithInvalidDataAsync();
        await context.TestLoginAsync();
        await context.TestContentOperationsAsync(dontCheckFrontend);
        await context.TestTurningFeatureOnAndOffAsync();
        await context.TestLogoutAsync();
    }

    /// <summary>
    /// <para>Tests the site setup with optionally set <paramref name="setupParameters"/>. By default uses new <see
    /// cref="OrchardCoreSetupParameters"/> instance with <c>"SaaS"</c><see cref="OrchardCoreSetupParameters.RecipeId"/>
    /// value, and tests the site setup negatively. Negative test uses new <see cref="OrchardCoreSetupParameters"/>
    /// instance with empty values of properties: <see cref="OrchardCoreSetupParameters.SiteName"/>, <see
    /// cref="OrchardCoreSetupParameters.UserName"/>, <see cref="OrchardCoreSetupParameters.Email"/> and <see
    /// cref="OrchardCoreSetupParameters.Password"/>.</para>
    /// <para>The test method assumes that the site is not set up.</para>
    /// </summary>
    /// <param name="setupParameters">The setup parameters.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static async Task TestSetupWithInvalidAndValidDataAsync(
        this UITestContext context,
        OrchardCoreSetupParameters setupParameters = null)
    {
        await context.TestSetupWithInvalidDataAsync();
        await context.TestSetupAsync(setupParameters);
    }

    /// <summary>
    /// <para>Tests the site setup with the recipe with the specified <paramref name="setupRecipeId"/> and tests the
    /// site setup negatively. Negative test uses new <see cref="OrchardCoreSetupParameters"/> instance with empty
    /// values of properties: <see cref="OrchardCoreSetupParameters.SiteName"/>, <see
    /// cref="OrchardCoreSetupParameters.UserName"/>, <see cref="OrchardCoreSetupParameters.Email"/> and <see
    /// cref="OrchardCoreSetupParameters.Password"/>.</para>
    /// <para>The test method assumes that the site is not set up.</para>
    /// </summary>
    /// <param name="setupRecipeId">The ID of the recipe to be used to set up the site.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static async Task TestSetupWithInvalidAndValidDataAsync(this UITestContext context, string setupRecipeId)
    {
        await context.TestSetupWithInvalidDataAsync();
        await context.TestSetupAsync(setupRecipeId);
    }

    /// <summary>
    /// <para>Tests the site setup with the recipe with the specified <paramref name="setupRecipeId"/>.</para>
    /// <para>The test method assumes that the site is not set up.</para>
    /// </summary>
    /// <param name="setupRecipeId">The ID of the recipe to be used to set up the site.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestSetupAsync(this UITestContext context, string setupRecipeId) =>
        context.TestSetupAsync(new OrchardCoreSetupParameters(context)
        {
            RecipeId = setupRecipeId,
        });

    /// <summary>
    /// <para>
    /// Tests the site setup with optionally set <paramref name="setupParameters"/>. By default uses new <see
    /// cref="OrchardCoreSetupParameters"/> instance with <c>"SaaS"</c><see cref="OrchardCoreSetupParameters.RecipeId"/>
    /// value.
    /// </para>
    /// <para>The test method assumes that the site is not set up.</para>
    /// </summary>
    /// <param name="setupParameters">The setup parameters.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestSetupAsync(this UITestContext context, OrchardCoreSetupParameters setupParameters = null)
    {
        setupParameters ??= new OrchardCoreSetupParameters(context);

        return context.ExecuteTestAsync(
            "Test setup",
            async () =>
            {
                var setupPage = await context.GoToSetupPageAsync();
                (await setupPage.SetupOrchardCoreAsync(context, setupParameters)).ShouldLeaveSetupPage();
            });
    }

    /// <summary>
    /// <para>
    /// Tests the site setup negatively with optionally set <paramref name="setupParameters"/>. By default uses new <see
    /// cref="OrchardCoreSetupParameters"/> instance with empty values of properties: <see
    /// cref="OrchardCoreSetupParameters.SiteName"/>, <see cref="OrchardCoreSetupParameters.UserName"/>, <see
    /// cref="OrchardCoreSetupParameters.Email"/> and <see cref="OrchardCoreSetupParameters.Password"/>.
    /// </para>
    /// <para>The test method assumes that the site is not set up.</para>
    /// </summary>
    /// <param name="setupParameters">The setup parameters.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestSetupWithInvalidDataAsync(
        this UITestContext context,
        OrchardCoreSetupParameters setupParameters = null)
    {
        setupParameters ??= new OrchardCoreSetupParameters(context)
        {
            SiteName = string.Empty,
            UserName = string.Empty,
            Email = string.Empty,
            Password = string.Empty,
        };

        return context.ExecuteTestAsync(
            "Test setup with invalid data",
            async () =>
            {
                var setupPage = await context.GoToSetupPageAsync();
                (await setupPage.SetupOrchardCoreAsync(context, setupParameters)).ShouldStayOnSetupPage();
            });
    }

    /// <summary>
    /// <para>
    /// Tests the login with the specified <paramref name="userName"/> and <paramref name="password"/> values.
    /// </para>
    /// <para>The test method assumes that there is a registered user with the given credentials.</para>
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestLoginAsync(
        this UITestContext context,
        string userName = DefaultUser.UserName,
        string password = DefaultUser.Password) =>
        context.ExecuteTestAsync(
            "Test login",
            async () =>
            {
                var loginPage = await context.GoToLoginPageAsync();
                (await loginPage.LogInWithAsync(context, userName, password)).ShouldLeaveLoginPage();

                (await context.GetCurrentUserNameAsync()).ShouldBe(userName);
            });

    /// <summary>
    /// <para>
    /// Tests the login negatively with the specified <paramref name="userName"/> and <paramref name="password"/>
    /// values.
    /// </para>
    /// <para>The test method assumes that there is no registered user with the given credentials.</para>
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestLoginWithInvalidDataAsync(
        this UITestContext context,
        string userName = DefaultUser.UserName,
        string password = "WrongPass!") =>
        context.ExecuteTestAsync(
            "Test login with invalid data",
            async () =>
            {
                await context.SignOutDirectlyAsync();

                var loginPage = await context.GoToLoginPageAsync();
                (await loginPage.LogInWithAsync(context, userName, password))
                    .ShouldStayOnLoginPage()
                    .ValidationSummaryErrors.Should.Not.BeEmpty();

                (await context.GetCurrentUserNameAsync()).ShouldBeEmpty();
            });

    /// <summary>
    /// <para>Tests the logout.</para>
    /// <para>The test method assumes that there is currently a logged in admin user session.</para>
    /// </summary>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestLogoutAsync(this UITestContext context) =>
        context.ExecuteTestAsync(
            "Test logout",
            async () =>
            {
                var dashboard = await context.GoToDashboardAsync();

                context.RefreshCurrentAtataContext();

                dashboard
                    .TopNavbar.Account.LogOff.Click()
                    .ShouldLeaveAdminPage();

                await context.TriggerAfterPageChangeEventAsync();

                (await context.GetCurrentUserNameAsync()).ShouldBeNullOrEmpty();
            });

    /// <summary>
    /// <para>
    /// Tests the user registration with optionally specified <paramref name="parameters"/>. After the user is
    /// registered, the test performs login with the user credentials, then logout.
    /// </para>
    /// <para>
    /// The test method assumes that the "Users Registration" Orchard feature is enabled and there is no registered user
    /// with the given values of <see cref="UserRegistrationParameters.Email"/> or <see
    /// cref="UserRegistrationParameters.UserName"/>.
    /// </para>
    /// </summary>
    /// <param name="parameters">The user registration parameters.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestRegistrationAsync(this UITestContext context, UserRegistrationParameters parameters = null)
    {
        parameters ??= UserRegistrationParameters.CreateDefault();

        return context.ExecuteTestAsync(
            "Test registration",
            async () =>
            {
                var loginPage = await context.GoToLoginPageAsync();
                context.RefreshCurrentAtataContext();
                var registrationPage = await loginPage
                    .RegisterAsNewUser.Should.BeVisible()
                    .RegisterAsNewUser.ClickAndGo()
                    .RegisterWithAsync(context, parameters);
                registrationPage.ShouldLeaveRegistrationPage();

                (await context.GetCurrentUserNameAsync()).ShouldBe(parameters.UserName);
                await context.SignOutDirectlyAsync();

                loginPage = await context.GoToLoginPageAsync();
                await loginPage.LogInWithAsync(context, parameters.UserName, parameters.Password);
                await context.TriggerAfterPageChangeEventAsync();
                (await context.GetCurrentUserNameAsync()).ShouldBe(parameters.UserName);
                await context.SignOutDirectlyAsync();
            });
    }

    /// <summary>
    /// <para>
    /// Tests the user registration negatively with optionally specified invalid <paramref name="parameters"/>. Fills
    /// user registration fields with <paramref name="parameters"/> on registration page, clicks "Register" button and
    /// verifies that there are validation messages on the page.
    /// </para>
    /// <para>The test method assumes that the "Users Registration" Orchard feature is enabled.</para>
    /// </summary>
    /// <param name="parameters">The user registration parameters.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestRegistrationWithInvalidDataAsync(
        this UITestContext context, UserRegistrationParameters parameters = null)
    {
        parameters ??= new()
        {
            UserName = "InvalidUser",
            Email = Randomizer.GetString("{0}@example.org", 25),
            Password = "short",
            ConfirmPassword = "short",
        };

        return context.ExecuteTestAsync(
            "Test registration with invalid data",
            async () =>
            {
                var registrationPage = await context.GoToRegistrationPageAsync();
                registrationPage = await registrationPage.RegisterWithAsync(context, parameters);
                registrationPage.ShouldStayOnRegistrationPage().ValidationMessages.Should.Not.BeEmpty();
            });
    }

    /// <summary>
    /// <para>
    /// Tests the user registration negatively with optionally specified <paramref name="parameters"/> that uses email
    /// of the already registered user. Fills user registration fields with <paramref name="parameters"/> on
    /// registration page, clicks "Register" button and verifies that there is a validation message near "Email" field
    /// on the page.
    /// </para>
    /// <para>
    /// The test method assumes that the "Users Registration" Orchard feature is enabled and there is an already
    /// registered user with the given <see cref="UserRegistrationParameters.Email"/> value.
    /// </para>
    /// </summary>
    /// <param name="parameters">The user registration parameters.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestRegistrationWithAlreadyRegisteredEmailAsync(
        this UITestContext context,
        UserRegistrationParameters parameters = null)
    {
        parameters ??= UserRegistrationParameters.CreateDefault();

        return context.ExecuteTestAsync(
            "Test registration with already registered email",
            async () =>
            {
                var registrationPage = await context.GoToRegistrationPageAsync();
                registrationPage = await registrationPage.RegisterWithAsync(context, parameters);
                context.RefreshCurrentAtataContext();
                registrationPage
                    .ShouldStayOnRegistrationPage()
                    .ValidationMessages[page => page.Email].Should.BeVisible();
            });
    }

    /// <summary>
    /// <para>Tests content operations. The test executes the following steps:</para>
    /// <list type="number">
    /// <item><description>Navigate to the "Content / Content Items" page.</description></item>
    /// <item><description>Create the page with the given <paramref name="pageTitle"/>.</description></item>
    /// <item><description>Publish the page.</description></item>
    /// <item><description>Verify that the page is created.</description></item>
    ///  <item><description>Check whether frontend operations shall be executed.</description></item>
    /// <item><description>Navigate to view the published page.</description></item>
    /// <item><description>Verify the page title and header.</description></item>
    /// </list>
    /// <para>The test method assumes that there is currently a logged in admin user session.</para>
    /// <para>
    /// When running the headless version of Orchard Core, frontend operations shall be excluded. Utilize <paramref
    /// name="dontCheckFrontend"></paramref>> for this purpose.
    /// </para>
    /// </summary>
    /// <param name="dontCheckFrontend">Boolean to decide whether to check content on frontend.</param>>
    /// <param name="pageTitle">The page title to enter.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestContentOperationsAsync(
        this UITestContext context,
        bool dontCheckFrontend = false,
        string pageTitle = "Test page") =>
        context.ExecuteTestAsync(
            "Test content operations",
            async () =>
            {
                var contentItemsPage = await context.GoToContentItemsPageAsync();
                context.RefreshCurrentAtataContext();
                contentItemsPage
                    .CreateNewPage()
                        .Title.Set(pageTitle)
                        .Publish.ClickAndGo()
                    .AlertMessages.Should.Contain(message => message.IsSuccess);

                await context.TriggerAfterPageChangeEventAsync();

                if (dontCheckFrontend) return;

                contentItemsPage.Items[item => item.Title == pageTitle].View.Click();

                await context.TriggerAfterPageChangeEventAsync();

                context.Scope.AtataContext.Go.ToNextWindow(new OrdinaryPage(pageTitle))
                    .AggregateAssert(page => page
                        .PageTitle.Should.Contain(pageTitle)
                        .Find<H1<OrdinaryPage>>().Should.Equal(pageTitle))
                    .CloseWindow();
            });

    /// <summary>
    /// <para>Tests turning feature on and off. The test executes the following steps:</para>
    /// <list type="number">
    /// <item><description>Navigate to the "Configuration / Features" page.</description></item>
    /// <item><description>Search the feature with the given <paramref name="featureName"/>.</description></item>
    /// <item><description>Read current feature enabled/disabled state.</description></item>
    /// <item><description>Toggle the feature state.</description></item>
    /// <item><description>Verify that the feature state is changed.</description></item>
    /// <item><description>Toggle the feature state again.</description></item>
    /// <item><description>Verify that the feature state is changed to the original.</description></item>
    /// </list>
    /// <para>The test method assumes that there is currently a logged in admin user session.</para>
    /// </summary>
    /// <param name="featureName">The name of the feature to use.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task TestTurningFeatureOnAndOffAsync(
        this UITestContext context, string featureName = "Background Tasks") =>
        context.ExecuteTestAsync(
            "Test turning feature on and off",
            async () =>
            {
                var featuresPage = await context.GoToFeaturesPageAsync();

                context.RefreshCurrentAtataContext();

                featuresPage
                    .SearchForFeature(featureName).IsEnabled.Get(out bool originalEnabledState)
                    .Features[featureName].CheckBox.Check()
                    .BulkActions.Toggle.Click()
                    .AggregateAssert(page => page
                        .ShouldContainSuccessAlertMessage(TermMatch.Contains, featureName)
                        .AdminMenu.FindMenuItem(featureName).IsPresent.Should.Equal(!originalEnabledState)
                        .SearchForFeature(featureName).IsEnabled.Should.Equal(!originalEnabledState))
                    .Features[featureName].CheckBox.Check()
                    .BulkActions.Toggle.Click()
                    .AggregateAssert(page => page
                        .ShouldContainSuccessAlertMessage(TermMatch.Contains, featureName)
                        .AdminMenu.FindMenuItem(featureName).IsPresent.Should.Equal(originalEnabledState)
                        .SearchForFeature(featureName).IsEnabled.Should.Equal(originalEnabledState));
            });

    public static Task TestMediaOperationsAsync(this UITestContext context) =>
        context.ExecuteTestAsync(
            "Test media operations",
            async () =>
            {
                context.GoToRelativeUrlAsync("Admin/Media");

                context.UploadSamplePngByIdOfAnyVisibility("fileupload");
                context.UploadSamplePdfByIdOfAnyVisibility("fileupload");

                // Workaround for pending uploads.
                context.WaitForPageLoad();
                await context.ClickReliablyOnAsync(By.CssSelector("body"));

                context
                    .Get(By.CssSelector("#mediaContainerMain tbody tr:nth-child(4) .break-word"))
                    .Text
                    .ShouldBeAsString("Image.png");
                context
                    .Get(By.CssSelector("#mediaContainerMain tbody tr:nth-child(2) .break-word"))
                    .Text
                    .ShouldBeAsString("Document.pdf");

                await context.Get(By.CssSelector("#folder-tree .treeroot .folder-actions")).ClickReliablyAsync(context);

                var folderNameInput = context.Get(By.Id("create-folder-name"));
                folderNameInput.SendKeys("Example Folder");

                await context.Get(By.Id("modalFooterOk")).ClickReliablyAsync(context);

                context.UploadSamplePngByIdOfAnyVisibility("fileupload");
                context.UploadSamplePdfByIdOfAnyVisibility("fileupload");
                context.WaitForPageLoad();
                var image = context
                    .Get(By.CssSelector("#mediaContainerMain tbody tr:nth-child(2) .break-word"));
                image.Text.ShouldBeAsString("Image.png");
                var pdf = context
                    .Get(By.CssSelector("#mediaContainerMain tbody tr:nth-child(1) .break-word"));
                pdf.Text.ShouldBeAsString("Document.pdf");

                await image.ClickReliablyAsync(context);
                await context
                    .Get(By.CssSelector(
                        "#mediaContainerMain div.media-container-middle.p-3 tr:nth-child(2) a.btn.btn-link.btn-sm.delete-button"))
                    .ClickReliablyAsync(context);
                await context.ClickModalOkAsync();

                var deleteFolderButton =
                    context.Get(By.CssSelector("#folder-tree > li > ol > li.selected > div > a > div.btn-group.folder-actions > a:nth-child(2)"));
                await deleteFolderButton.ClickReliablyAsync(context);
                await context.ClickModalOkAsync();
            });

    /// <summary>
    /// Executes the <paramref name="testFunctionAsync"/> with the specified <paramref name="testName"/>.
    /// </summary>
    /// <param name="testName">The test name.</param>
    /// <param name="testFunctionAsync">The test action.</param>
    /// <returns>The same <see cref="UITestContext"/> instance.</returns>
    public static Task ExecuteTestAsync(
        this UITestContext context, string testName, Func<Task> testFunctionAsync)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (testName is null) throw new ArgumentNullException(nameof(testName));
        if (testFunctionAsync is null) throw new ArgumentNullException(nameof(testFunctionAsync));

        return ExecuteTestInnerAsync(context, testName, testFunctionAsync);
    }

    private static Task ExecuteTestInnerAsync(
        UITestContext context, string testName, Func<Task> testFunctionAsync) =>
        context.ExecuteLoggedAsync(testName, testFunctionAsync);
}
