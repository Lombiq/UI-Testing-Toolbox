using Atata;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.BasicOrchardFeaturesTesting;

/// <summary>
/// Provides a set of extension methods for basic Orchard features testing.
/// </summary>
public static class BasicFeaturesTestingUITestContextExtensions
{
    /// <summary>
    /// Tests all the basic Orchard features. At first sets up Orchard with the recipe with the specified <paramref
    /// name="setupRecipeId"/>.
    /// </summary>
    /// <param name="setupRecipeId">The ID of the recipe to be used to set up the site.</param>
    /// <param name="customPageHeaderCheckAsync">
    /// The custom page header check logic to locate and/or check the header's text. This ultimately gets passed to
    /// TestContentOperationsAsync().
    /// </param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
    public static Task TestBasicOrchardFeaturesAsync(
        this UITestContext context,
        string setupRecipeId,
        Func<UITestContext, Task> customPageHeaderCheckAsync = null) =>
        context.TestBasicOrchardFeaturesAsync(
            new OrchardCoreSetupParameters(context, setupRecipeId),
            customPageHeaderCheckAsync);

    /// <summary>
    /// Tests all the basic Orchard features. At first sets up Orchard with optionally specified <paramref
    /// name="setupParameters"/>. By default, uses new <see cref="OrchardCoreSetupParameters"/> instance with
    /// <c>"SaaS"</c> <see cref="OrchardCoreSetupParameters.RecipeId"/> value.
    /// </summary>
    /// <param name="customPageHeaderCheckAsync">
    /// The custom page header check logic to locate and/or check the header's text. This ultimately gets passed to
    /// TestContentOperationsAsync().
    /// </param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
    public static async Task TestBasicOrchardFeaturesAsync(
        this UITestContext context,
        OrchardCoreSetupParameters setupParameters = null,
        Func<UITestContext, Task> customPageHeaderCheckAsync = null)
    {
        setupParameters ??= new(context);

        if (!setupParameters.SkipSetup)
        {
            await context.TestSetupWithInvalidAndValidDataAsync(setupParameters);
        }

        if (!setupParameters.SkipRegistration)
        {
            await context.TestBasicOrchardRegistrationAsync(setupParameters.UserRegistrationParameters);
        }

        await context.TestBasicOrchardFeaturesExceptSetupAndRegistrationAsync(
            setupParameters,
            customPageHeaderCheckAsync);
    }

    /// <summary>
    /// Tests all the basic Orchard features except for registration. At first sets up Orchard with the recipe with the
    /// specified <paramref name="setupRecipeId"/>.
    /// </summary>
    /// <param name="setupRecipeId">The ID of the recipe to be used to set up the site.</param>
    /// <param name="dontCheckFrontend">Boolean to decide whether to check content on frontend.</param>>
    /// <param name="customPageHeaderCheckAsync">
    /// The custom page header check logic to locate and/or check the header's text. This ultimately gets passed to
    /// TestContentOperationsAsync().
    /// </param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks>
    /// <para>The test method assumes that the site is not set up.</para>
    /// <para>
    /// When running the headless version of Orchard Core, frontend operations shall be excluded. Utilize <paramref
    /// name="dontCheckFrontend"></paramref>> for this purpose.
    /// </para>
    /// </remarks>
    [Obsolete(
        $"This method will be removed to streamline the library. Use {nameof(TestBasicOrchardFeaturesAsync)} with " +
        $"{nameof(OrchardCoreSetupParameters)}, and set the {nameof(OrchardCoreSetupParameters.SkipRegistration)} or " +
        $"{nameof(OrchardCoreSetupParameters.SkipFrontend)} properties.")]
    public static Task TestBasicOrchardFeaturesExceptRegistrationAsync(
        this UITestContext context,
        string setupRecipeId,
        bool dontCheckFrontend = false,
        Func<UITestContext, Task> customPageHeaderCheckAsync = null) =>
        context.TestBasicOrchardFeaturesAsync(
            new OrchardCoreSetupParameters(context)
            {
                RecipeId = setupRecipeId,
                SkipFrontend = dontCheckFrontend,
                SkipRegistration = true,
            },
            customPageHeaderCheckAsync);

    /// <summary>
    /// Tests all the basic Orchard features except for registration. At first sets up Orchard with optionally specified
    /// <paramref name="setupParameters"/>. By default, uses new <see cref="OrchardCoreSetupParameters"/> instance with
    /// <c>"SaaS"</c><see cref="OrchardCoreSetupParameters.RecipeId"/> value.
    /// </summary>
    /// <param name="dontCheckFrontend">Boolean to decide whether to check content on frontend.</param>>
    /// <param name="customPageHeaderCheckAsync">
    /// The custom page header check logic to locate and/or check the header's text. This ultimately gets passed to
    /// TestContentOperationsAsync().
    /// </param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks>
    /// <para>The test method assumes that the site is not set up.</para>
    /// <para>
    /// When running the headless version of Orchard Core, frontend operations shall be excluded. Utilize <paramref
    /// name="dontCheckFrontend"></paramref>> for this purpose.
    /// </para>
    /// </remarks>
    [Obsolete(
        $"This method will be removed to streamline the library. Use {nameof(TestBasicOrchardFeaturesAsync)} with " +
        $"{nameof(OrchardCoreSetupParameters)}, and set the {nameof(OrchardCoreSetupParameters.SkipRegistration)} or " +
        $"{nameof(OrchardCoreSetupParameters.SkipFrontend)} properties.")]
    public static Task TestBasicOrchardFeaturesExceptRegistrationAsync(
        this UITestContext context,
        bool dontCheckFrontend = false,
        OrchardCoreSetupParameters setupParameters = null,
        Func<UITestContext, Task> customPageHeaderCheckAsync = null)
    {
        setupParameters ??= new(context);
        setupParameters.SkipRegistration = true;
        setupParameters.SkipFrontend = dontCheckFrontend;

        return context.TestBasicOrchardFeaturesAsync(setupParameters, customPageHeaderCheckAsync);
    }

    /// <summary>
    /// Tests the built-in registration feature in Orchard Core.
    /// </summary>
    public static async Task TestBasicOrchardRegistrationAsync(
        this UITestContext context,
        UserRegistrationParameters parameters = null)
    {
        await context.TestRegistrationWithInvalidDataAsync(parameters);
        await context.TestRegistrationAsync(parameters);
        await context.TestRegistrationWithAlreadyRegisteredEmailAsync(parameters);
    }

    /// <summary>
    /// Tests all the basic Orchard features except for setup.
    /// </summary>
    /// <param name="customPageHeaderCheckAsync">
    /// The custom page header check logic to locate and/or check the header's text. This ultimately gets passed to
    /// TestContentOperationsAsync().
    /// </param>
    /// <param name="dontCheckFrontend">Boolean to decide whether to check content on frontend.</param>>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is set up.</para></remarks>
    [Obsolete(
        $"This method will be removed to streamline the library. Use {nameof(TestBasicOrchardFeaturesAsync)} with " +
        $"{nameof(OrchardCoreSetupParameters)}, and set the {nameof(OrchardCoreSetupParameters.SkipSetup)} property.")]
    public static Task TestBasicOrchardFeaturesExceptSetupAsync(
        this UITestContext context,
        bool dontCheckFrontend,
        Func<UITestContext, Task> customPageHeaderCheckAsync = null)
    {
        var setupParameters = new OrchardCoreSetupParameters(context)
        {
            SkipSetup = true,
            SkipFrontend = dontCheckFrontend,
        };

        return context.TestBasicOrchardFeaturesAsync(setupParameters, customPageHeaderCheckAsync);
    }

    /// <summary>
    /// <para>Tests all the basic Orchard features except for setup.</para>
    /// </summary>
    /// <param name="customPageHeaderCheckAsync">
    /// The custom page header check logic to locate and/or check the header's text. This ultimately gets passed to
    /// TestContentOperationsAsync().
    /// </param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is set up.</para></remarks>
    [Obsolete(
        $"This method will be removed to streamline the library. Use {nameof(TestBasicOrchardFeaturesAsync)} with " +
        $"{nameof(OrchardCoreSetupParameters)}, and set the {nameof(OrchardCoreSetupParameters.SkipSetup)} property.")]
    public static Task TestBasicOrchardFeaturesExceptSetupAsync(
        this UITestContext context,
        Func<UITestContext, Task> customPageHeaderCheckAsync = null) =>
        context.TestBasicOrchardFeaturesAsync(new OrchardCoreSetupParameters(context) { SkipSetup = true }, customPageHeaderCheckAsync);

    /// <summary>
    /// Tests all the basic Orchard features except for setup and registration.
    /// </summary>
    /// <param name="dontCheckFrontend">Boolean to decide whether to check content on frontend.</param>>
    /// <param name="customPageHeaderCheckAsync">
    /// The custom page header check logic to locate and/or check the header's text. This ultimately gets passed to
    /// TestContentOperationsAsync().
    /// </param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks>
    /// <para>The test method assumes that the site is set up.</para>
    /// <para>When running headless version of Orchard Core, ContentOperations shall be excluded.</para>
    /// <para>
    /// When running the headless version of Orchard Core, frontend operations shall be excluded. Utilize <paramref
    /// name="dontCheckFrontend"></paramref>> for this purpose.
    /// </para>
    /// </remarks>
    [Obsolete(
        $"This method will be removed to streamline the library. Use the overload with with " +
        $"{nameof(OrchardCoreSetupParameters)} instead.")]
    public static Task TestBasicOrchardFeaturesExceptSetupAndRegistrationAsync(
        this UITestContext context,
        bool dontCheckFrontend,
        Func<UITestContext, Task> customPageHeaderCheckAsync = null) =>
        context.TestBasicOrchardFeaturesExceptSetupAndRegistrationAsync(new OrchardCoreSetupParameters(context)
        {
            SkipFrontend = dontCheckFrontend,
        });

    public static async Task TestBasicOrchardFeaturesExceptSetupAndRegistrationAsync(
        this UITestContext context,
        OrchardCoreSetupParameters setupParameters = null,
        Func<UITestContext, Task> customPageHeaderCheckAsync = null)
    {
        setupParameters ??= new(context);
        var login = setupParameters.UserLoginParameters;

        await context.TestLoginWithInvalidDataAsync(login.UserName, login.Password, login.LoginButtonText);
        await context.TestLoginAsync(login.UserName, login.Password, login.LoginButtonText);
        await context.SignInDirectlyAsync(setupParameters.UserName);
        await context.TestContentOperationsAsync(setupParameters.SkipFrontend, customPageHeaderCheckAsync: customPageHeaderCheckAsync);
        await context.TestTurningFeatureOnAndOffAsync();
        await context.TestMediaOperationsAsync();
        await context.TestAuditTrailAsync();
        await context.TestWorkflowsAsync();
        await context.TestLogoutAsync();
    }

    /// <summary>
    /// Tests the site setup with optionally set <paramref name="setupParameters"/>. By default, uses new <see
    /// cref="OrchardCoreSetupParameters"/> instance with <c>"SaaS"</c><see cref="OrchardCoreSetupParameters.RecipeId"/>
    /// value, and tests the site setup negatively. Negative test uses new <see cref="OrchardCoreSetupParameters"/>
    /// instance with empty values of properties: <see cref="OrchardCoreSetupParameters.SiteName"/>, <see
    /// cref="OrchardCoreSetupParameters.UserName"/>, <see cref="OrchardCoreSetupParameters.Email"/> and <see
    /// cref="OrchardCoreSetupParameters.Password"/>.
    /// </summary>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
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
    /// values of properties: </para>
    /// <list type="bullet">
    ///     <item><description><see cref="OrchardCoreSetupParameters.SiteName"/></description></item>
    ///     <item><description><see cref="OrchardCoreSetupParameters.UserName"/></description></item>
    ///     <item><description><see cref="OrchardCoreSetupParameters.Email"/></description></item>
    ///     <item><description><see cref="OrchardCoreSetupParameters.Password"/></description></item>
    /// </list>
    /// </summary>
    /// <param name="setupRecipeId">The ID of the recipe to be used to set up the site.</param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
    public static Task TestSetupWithInvalidAndValidDataAsync(this UITestContext context, string setupRecipeId) =>
        context.TestSetupWithInvalidAndValidDataAsync(new OrchardCoreSetupParameters(context, setupRecipeId));

    /// <summary>
    /// Tests the site setup with the recipe with the specified <paramref name="setupRecipeId"/>.
    /// </summary>
    /// <param name="setupRecipeId">The ID of the recipe to be used to set up the site.</param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
    public static Task TestSetupAsync(this UITestContext context, string setupRecipeId) =>
        context.TestSetupAsync(new OrchardCoreSetupParameters(context, setupRecipeId));

    /// <summary>
    /// Tests the site setup with optionally set <paramref name="setupParameters"/>. By default, uses new <see
    /// cref="OrchardCoreSetupParameters"/> instance with <c>"SaaS"</c><see cref="OrchardCoreSetupParameters.RecipeId"/>
    /// value.
    /// </summary>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
    public static Task TestSetupAsync(this UITestContext context, OrchardCoreSetupParameters setupParameters = null) =>
        context.TestSetupAsync(setupParameters, "Test setup", shouldBeSuccess: true);

    /// <summary>
    /// <para>
    /// Tests the site setup negatively with optionally set <paramref name="setupParameters"/>. By default, uses new
    /// <see cref="OrchardCoreSetupParameters"/> instance with empty values of properties:
    /// </para>
    /// <list type="bullet">
    ///     <item><description><see cref="OrchardCoreSetupParameters.SiteName"/></description></item>
    ///     <item><description><see cref="OrchardCoreSetupParameters.UserName"/></description></item>
    ///     <item><description><see cref="OrchardCoreSetupParameters.Email"/></description></item>
    ///     <item><description><see cref="OrchardCoreSetupParameters.Password"/></description></item>
    /// </list>
    /// </summary>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
    public static Task TestSetupWithInvalidDataAsync(
        this UITestContext context,
        OrchardCoreSetupParameters setupParameters = null)
    {
        setupParameters ??= new OrchardCoreSetupParameters(context);
        setupParameters.SiteName = string.Empty;
        setupParameters.UserName = string.Empty;
        setupParameters.Email = string.Empty;
        setupParameters.Password = string.Empty;

        return context.TestSetupAsync(setupParameters, "Test setup with invalid data", shouldBeSuccess: false);
    }

    private static Task TestSetupAsync(
        this UITestContext context,
        OrchardCoreSetupParameters setupParameters,
        string testName,
        bool shouldBeSuccess) =>
        context.ExecuteTestAsync(
            testName,
            async () =>
            {
                var setupPage = await context.GoToSetupPageAsync();
                (await setupPage.SetupOrchardCoreAsync(context, setupParameters)).ShouldLeaveSetupPage(shouldBeSuccess);
            });

    private static Task TestLoginAsync(
        this UITestContext context,
        string testName,
        string userName,
        string password,
        bool signOut,
        bool shouldBeSuccess,
        string loginButtonText) =>
        context.ExecuteTestAsync(
            testName,
            async () =>
            {
                if (signOut) await context.SignOutDirectlyAsync();

                var loginPage = await context.GoToLoginPageAsync();
                loginPage = await loginPage.LogInWithAsync(context, userName, password, loginButtonText);
                loginPage.ShouldLeaveLoginPage(shouldBeSuccess);

                var currentUser = await context.GetCurrentUserNameAsync();
                if (shouldBeSuccess)
                {
                    currentUser.ShouldBe(userName);
                }
                else
                {
                    currentUser.ShouldNotBe(userName);
                }
            });

    /// <summary>
    /// Tests the login with the specified <paramref name="userName"/> and <paramref name="password"/> values.
    /// </summary>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
    public static Task TestLoginAsync(
        this UITestContext context,
        string userName = DefaultUser.UserName,
        string password = DefaultUser.Password,
        string logInButtonText = UserRegistrationParameters.DefaultLoginButtonText,
        bool signOut = false) =>
        context.TestLoginAsync(
            "Test login",
            userName,
            password,
            signOut,
            shouldBeSuccess: true,
            logInButtonText);

    /// <summary>
    /// Tests the login negatively with the specified <paramref name="userName"/> and <paramref name="password"/>
    /// values.
    /// </summary>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
    public static Task TestLoginWithInvalidDataAsync(
        this UITestContext context,
        string userName = DefaultUser.UserName,
        string password = DefaultUser.Password,
        string logInButtonText = UserRegistrationParameters.DefaultLoginButtonText) =>
        context.TestLoginAsync(
            "Test login with invalid data",
            userName,
            password + "WrongPass!",
            signOut: true,
            shouldBeSuccess: false,
            logInButtonText);

    /// <summary>
    /// Tests the logout.
    /// </summary>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that the site is not set up.</para></remarks>
    public static Task TestLogoutAsync(this UITestContext context) =>
        context.ExecuteTestAsync(
            "Test logout",
            async () =>
            {
                await context.GoToAdminAsync();
                await context.SelectFromBootstrapDropdownReliablyAsync(
                    context.Get(By.Id("navbarDropdown")),
                    By.XPath("//button[contains(@class, 'dropdown-item') and contains(., 'Log off')]"));
                context.Driver.Url.ShouldNotContain(context.AdminUrlPrefix);

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
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    public static Task TestRegistrationAsync(this UITestContext context, UserRegistrationParameters parameters = null)
    {
        parameters ??= UserRegistrationParameters.CreateTest();

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

                await parameters.RegisterWithAsync(context, navigate: false);
                context.Driver.Url.ShouldNotBe(context.GetAbsoluteUri(UserRegistrationParameters.DefaultUrl).AbsoluteUri);

                (await context.GetCurrentUserNameAsync()).ShouldBe(parameters.UserName);
                await context.SignOutDirectlyAsync();

                loginPage = await context.GoToLoginPageAsync();
                await loginPage.LogInWithAsync(context, parameters);
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
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    public static Task TestRegistrationWithInvalidDataAsync(
        this UITestContext context, UserRegistrationParameters parameters = null)
    {
        parameters = (parameters ?? UserRegistrationParameters.CreateTest()) with
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
                await parameters.RegisterWithAsync(context);
                context.Exists(By.XPath("//div[contains(concat(' ', normalize-space(@class), ' '), ' validation-summary-errors ')]//li"));
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
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    public static Task TestRegistrationWithAlreadyRegisteredEmailAsync(
        this UITestContext context,
        UserRegistrationParameters parameters = null)
    {
        parameters ??= UserRegistrationParameters.CreateTest();

        return context.ExecuteTestAsync(
            "Test registration with already registered email",
            async () =>
            {
                await parameters.RegisterWithAsync(context);

                context
                    .Get(By.CssSelector(".text-danger.field-validation-error"))
                    .Text
                    .ShouldContain("A user with the same username already exists.");
            });
    }

    /// <summary>
    /// <para>Tests content operations. The test executes the following steps:</para>
    /// <list type="number">
    /// <item><description>Navigate to the "Content / Content Items" page.</description></item>
    /// <item><description>Create the page with the given <paramref name="pageTitle"/>.</description></item>
    /// <item><description>Publish the page.</description></item>
    /// <item><description>Verify that the page is created.</description></item>
    /// <item><description>Check whether frontend operations shall be executed.</description></item>
    /// <item><description>Navigate to view the published page.</description></item>
    /// <item><description>Verify the page title and header.</description></item>
    /// </list>
    /// </summary>
    /// <param name="dontCheckFrontend">Boolean to decide whether to check content on frontend.</param>
    /// <param name="pageTitle">The page title to enter.</param>
    /// <param name="customPageHeaderCheckAsync">
    /// The custom page header check logic to locate and/or check the header's text.
    /// </param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks>
    /// <para>The test method assumes that the site is not set up.</para>
    /// <para>
    /// When running the headless version of Orchard Core, frontend operations shall be excluded. Utilize <paramref
    /// name="dontCheckFrontend"></paramref> for this purpose.
    /// </para>
    /// </remarks>
    public static Task TestContentOperationsAsync(
        this UITestContext context,
        bool dontCheckFrontend = false,
        string pageTitle = "Test page",
        Func<UITestContext, Task> customPageHeaderCheckAsync = null) =>
        context.ExecuteTestAsync(
            "Test content operations",
            async () =>
            {
                await context.CreateNewPageContentItemAsync(pageTitle);

                if (dontCheckFrontend) return;

                await context.GoToContentItemListAsync("Page");
                await context.ClickReliablyOnAsync(By.CssSelector(".btn.view"));

                context.SwitchToLastWindow();
                context.Driver.Title.ShouldContain(pageTitle);

                customPageHeaderCheckAsync ??= context =>
                {
                    context.Get(By.TagName("h1")).GetTextTrimmed().ShouldBe(pageTitle);
                    return Task.CompletedTask;
                };

                await customPageHeaderCheckAsync(context);
                if (context.Driver.WindowHandles.Count > 1) context.Driver.Close();
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
    /// </summary>
    /// <param name="featureName">The name of the feature to use.</param>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    /// <remarks><para>The test method assumes that there is currently a logged in admin user session.</para></remarks>
    public static Task TestTurningFeatureOnAndOffAsync(
        this UITestContext context, string featureName = "Background Tasks") =>
        context.ExecuteTestAsync(
            "Test turning feature on and off",
            async () =>
            {
                async Task<IWebElement> SearchForFeatureAsync(UITestContext context) =>
                    (await context.GoToFeaturesAsync(featureName)).First();

                var feature = await SearchForFeatureAsync(context);
                var originalEnabledState = feature.Enabled;
                var targetState = originalEnabledState;

                for (var i = 0; i < 2; i++)
                {
                    feature = await SearchForFeatureAsync(context);
                    await feature.ClickReliablyAsync(context);
                    await context.BulkActionsToggleAsync();

                    targetState = !targetState;

                    context.ShouldBeSuccess();
                    feature = await SearchForFeatureAsync(context);
                    feature.Enabled.ShouldBe(originalEnabledState);
                }

                targetState.ShouldBe(originalEnabledState);
            });

    /// <summary>
    /// Executes the <paramref name="testFunctionAsync"/> with the specified <paramref name="testName"/>.
    /// </summary>
    /// <returns>The instance passed to <paramref name="context"/>.</returns>
    public static Task ExecuteTestAsync(
        this UITestContext context, string testName, Func<Task> testFunctionAsync)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(testName);
        ArgumentNullException.ThrowIfNull(testFunctionAsync);

        return context.ExecuteLoggedAsync(testName, testFunctionAsync);
    }
}
