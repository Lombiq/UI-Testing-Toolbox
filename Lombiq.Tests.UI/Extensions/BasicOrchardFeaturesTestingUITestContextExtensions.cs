using Atata;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using Shouldly;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    /// <summary>
    /// Provides a set of extension methods for basic Orchard features testing.
    /// </summary>
    public static class BasicOrchardFeaturesTestingUITestContextExtensions
    {
        /// <summary>
        /// <para>
        /// Tests all the basic Orchard features. At first sets up Orchard with optionally specified
        /// <paramref name="setupParameters"/>. By default uses new <see cref="OrchardCoreSetupParameters"/> instance
        /// with <c>"SaaS"</c> <see cref="OrchardCoreSetupParameters.RecipeId"/> value.
        /// </para>
        /// <para>
        /// The test method assumes that the site is not set up.
        /// </para>
        /// </summary>
        /// <param name="setupParameters">The setup parameters.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestBasicOrchardFeatures(this UITestContext context, OrchardCoreSetupParameters setupParameters = null) =>
            context
                .TestSetupWithInvalidData()
                .TestSetup(setupParameters)
                .TestRegistrationWithInvalidData()
                .TestRegistration()
                .TestRegistrationWithAlreadyRegisteredEmail()
                .TestLoginWithInvalidData()
                .TestLogin()
                .TestContentOperations()
                .TestTurningFeatureOnAndOff()
                .TestLogout();

        /// <summary>
        /// <para>
        /// Tests the site setup with optionally set <paramref name="parameters"/>.
        /// By default uses new <see cref="OrchardCoreSetupParameters"/> instance
        /// with <c>"SaaS"</c> <see cref="OrchardCoreSetupParameters.RecipeId"/> value.
        /// </para>
        /// <para>
        /// The test method assumes that the site is not set up.
        /// </para>
        /// </summary>
        /// <param name="parameters">The setup parameters.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestSetup(this UITestContext context, OrchardCoreSetupParameters parameters = null)
        {
            parameters ??= new OrchardCoreSetupParameters(context);

            return context.ExecuteTest(
                "Test setup",
                () => context
                    .GoToSetupPage()
                    .SetupOrchardCore(parameters)
                    .ShouldLeaveSetupPage());
        }

        /// <summary>
        /// <para>
        /// Tests the site setup negatively with optionally set <paramref name="parameters"/>.
        /// By default uses new <see cref="OrchardCoreSetupParameters"/> instance
        /// with empty values of properties: <see cref="OrchardCoreSetupParameters.SiteName"/>,
        /// <see cref="OrchardCoreSetupParameters.UserName"/>, <see cref="OrchardCoreSetupParameters.Email"/>
        /// and <see cref="OrchardCoreSetupParameters.Password"/>.
        /// </para>
        /// <para>
        /// The test method assumes that the site is not set up.
        /// </para>
        /// </summary>
        /// <param name="parameters">The setup parameters.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestSetupWithInvalidData(this UITestContext context, OrchardCoreSetupParameters parameters = null)
        {
            parameters ??= new OrchardCoreSetupParameters(context)
            {
                SiteName = string.Empty,
                UserName = string.Empty,
                Email = string.Empty,
                Password = string.Empty,
            };

            return context.ExecuteTest(
                "Test setup with invalid data",
                () => context
                    .GoToSetupPage()
                    .SetupOrchardCore(parameters)
                    .ShouldStayOnSetupPage());
        }

        /// <summary>
        /// <para>
        /// Tests the login with the specified <paramref name="userName"/> and <paramref name="password"/> values.
        /// </para>
        /// <para>
        /// The test method assumes that there is a registered user with the given credentials.
        /// </para>
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestLogin(
            this UITestContext context,
            string userName = DefaultUser.UserName,
            string password = DefaultUser.Password) =>
            context.ExecuteTest(
                "Test login",
                () =>
                {
                    context.GoToLoginPage()
                        .LogInWith(userName, password)
                        .ShouldLeaveLoginPage();

                    context.GetCurrentUserName().ShouldBe(userName);
                });

        /// <summary>
        /// <para>
        /// Tests the login negatively with the specified <paramref name="userName"/> and <paramref name="password"/> values.
        /// </para>
        /// <para>
        /// The test method assumes that there is no registered user with the given credentials.
        /// </para>
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestLoginWithInvalidData(
            this UITestContext context,
            string userName = DefaultUser.UserName,
            string password = "WrongPass!") =>
            context.ExecuteTest(
                "Test login with invalid data",
                () =>
                {
                    context.SignOutDirectly();

                    context.GoToLoginPage()
                        .LogInWith(userName, password)
                        .ShouldStayOnLoginPage()
                        .ValidationSummaryErrors.Should.Not.BeEmpty();

                    context.GetCurrentUserName().ShouldBeEmpty();
                });

        /// <summary>
        /// <para>
        /// Tests the logout.
        /// </para>
        /// <para>
        /// The test method assumes that there is currently a logged in admin user session.
        /// </para>
        /// </summary>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestLogout(this UITestContext context) =>
            context.ExecuteTest(
                "Test logout",
                () =>
                {
                    context.GoToDashboard()
                        .TopNavbar.Account.LogOff.Click()
                        .ShouldLeaveAdminPage();

                    context.GetCurrentUserName().ShouldBeNullOrEmpty();
                });

        /// <summary>
        /// <para>
        /// Tests the user registration with optionally specified <paramref name="parameters"/>. After the user is
        /// registered, the test performs login with the user credentials, then logout.
        /// </para>
        /// <para>
        /// The test method assumes that the "Users Registration" Orchard feature is enabled and there is no registered
        /// user with the given values of <see cref="UserRegistrationParameters.Email"/> or <see
        /// cref="UserRegistrationParameters.UserName"/>.
        /// </para>
        /// </summary>
        /// <param name="parameters">The user registration parameters.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestRegistration(this UITestContext context, UserRegistrationParameters parameters = null)
        {
            parameters ??= UserRegistrationParameters.CreateDefault();

            return context.ExecuteTest(
                "Test registration",
                () =>
                {
                    context.GoToLoginPage()
                        .RegisterAsNewUser.Should.BeVisible()
                        .RegisterAsNewUser.ClickAndGo()
                            .RegisterWith(parameters)
                            .ShouldLeaveRegistrationPage();

                    context.GetCurrentUserName().ShouldBe(parameters.UserName);
                    context.SignOutDirectly();

                    context.GoToLoginPage()
                        .LogInWith(parameters.UserName, parameters.Password);
                    context.GetCurrentUserName().ShouldBe(parameters.UserName);
                    context.SignOutDirectly();
                });
        }

        /// <summary>
        /// <para>
        /// Tests the user registration negatively with optionally specified invalid <paramref name="parameters"/>.
        /// Fills user registration fields with <paramref name="parameters"/> on registration page, clicks "Register"
        /// button and verifies that there are validation messages on the page.
        /// </para>
        /// <para>
        /// The test method assumes that the "Users Registration" Orchard feature is enabled.
        /// </para>
        /// </summary>
        /// <param name="parameters">The user registration parameters.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestRegistrationWithInvalidData(this UITestContext context, UserRegistrationParameters parameters = null)
        {
            parameters ??= new()
            {
                UserName = "InvalidUser",
                Email = Randomizer.GetString("{0}@example.org", 25),
                Password = "short",
                ConfirmPassword = "short",
            };

            return context.ExecuteTest(
                "Test registration with invalid data",
                () => context
                    .GoToRegistrationPage()
                    .RegisterWith(parameters)
                    .ShouldStayOnRegistrationPage()
                    .ValidationMessages.Should.Not.BeEmpty());
        }

        /// <summary>
        /// <para>
        /// Tests the user registration negatively with optionally specified <paramref name="parameters"/> that uses
        /// email of the already registered user. Fills user registration fields with <paramref name="parameters"/> on
        /// registration page, clicks "Register" button and verifies that there is a validation message near "Email"
        /// field on the page.
        /// </para>
        /// <para>
        /// The test method assumes that the "Users Registration" Orchard feature is enabled and there is an already
        /// registered user with the given <see cref="UserRegistrationParameters.Email"/> value.
        /// </para>
        /// </summary>
        /// <param name="parameters">The user registration parameters.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestRegistrationWithAlreadyRegisteredEmail(
            this UITestContext context,
            UserRegistrationParameters parameters = null)
        {
            parameters ??= UserRegistrationParameters.CreateDefault();

            return context.ExecuteTest(
                "Test registration with already registered email",
                () => context
                    .GoToRegistrationPage()
                    .RegisterWith(parameters)
                    .ShouldStayOnRegistrationPage()
                    .ValidationMessages[page => page.Email].Should.BeVisible());
        }

        /// <summary>
        /// <para>
        /// Tests content operations. The test executes the following steps:
        /// </para>
        /// <list type="number">
        /// <item><description>Navigate to the "Content / Content Items" page.</description></item>
        /// <item><description>Create the page with the given <paramref name="pageTitle"/>.</description></item>
        /// <item><description>Publish the page.</description></item>
        /// <item><description>Verify that the page is created.</description></item>
        /// <item><description>Navigate to view the published page.</description></item>
        /// <item><description>Verify the page title and header.</description></item>
        /// </list>
        /// <para>
        /// The test method assumes that there is currently a logged in admin user session.
        /// </para>
        /// </summary>
        /// <param name="pageTitle">The page title to enter.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestContentOperations(this UITestContext context, string pageTitle = "Test page") =>
            context.ExecuteTest(
                "Test content operations",
                () =>
                {
                    context.GoToContentItemsPage()
                        .CreateNewPage()
                            .Title.Set(pageTitle)
                            .Publish.ClickAndGo()
                        .AlertMessages.Should.Contain(message => message.IsSuccess)
                        .Items[item => item.Title == pageTitle].View.Click();

                    context.Scope.AtataContext.Go.ToNextWindow(new OrdinaryPage(pageTitle))
                        .AggregateAssert(page => page
                            .PageTitle.Should.Contain(pageTitle)
                            .Find<H1<OrdinaryPage>>().Should.Equal(pageTitle))
                        .CloseWindow();
                });

        /// <summary>
        /// <para>
        /// Tests turning feature on and off. The test executes the following steps:
        /// </para>
        /// <list type="number">
        /// <item><description>Navigate to the "Configuration / Features" page.</description></item>
        /// <item><description>Search the feature with the given <paramref name="featureName"/>.</description></item>
        /// <item><description>Read current feature enabled/disabled state.</description></item>
        /// <item><description>Toggle the feature state.</description></item>
        /// <item><description>Verify that the feature state is changed.</description></item>
        /// <item><description>Toggle the feature state again.</description></item>
        /// <item><description>Verify that the feature state is changed to the original.</description></item>
        /// </list>
        /// <para>
        /// The test method assumes that there is currently a logged in admin user session.
        /// </para>
        /// </summary>
        /// <param name="featureName">The name of the feature to use.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestTurningFeatureOnAndOff(this UITestContext context, string featureName = "Background Tasks") =>
            context.ExecuteTest(
                "Test turning feature on and off",
                () => context
                    .GoToFeaturesPage()
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
                        .SearchForFeature(featureName).IsEnabled.Should.Equal(originalEnabledState)));

        /// <summary>
        /// Executes the <paramref name="testAction"/> with the specified <paramref name="testName"/>.
        /// </summary>
        /// <param name="testName">The test name.</param>
        /// <param name="testAction">The test action.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext ExecuteTest(this UITestContext context, string testName, Action testAction)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (testName is null) throw new ArgumentNullException(nameof(testName));
            if (testAction is null) throw new ArgumentNullException(nameof(testAction));

            context.Scope.AtataContext.Log.ExecuteSection(new LogSection(testName), testAction);

            return context;
        }
    }
}
