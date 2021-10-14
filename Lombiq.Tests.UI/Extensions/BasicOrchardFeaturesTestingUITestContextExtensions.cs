using Atata;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using Shouldly;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class BasicOrchardFeaturesTestingUITestContextExtensions
    {
        public static UITestContext TestBasicOrchardFeatures(this UITestContext context) =>
            context
                .TestSetupWithInvalidData()
                .TestSetup()
                .TestRegistrationWithInvalidData()
                .TestRegistration()
                .TestRegistrationWithAlreadyRegisteredEmail()
                .TestLoginWithInvalidData()
                .TestLogin()
                .TestContentOperations()
                .TestTurningFeatureOnAndOff()
                .TestLogout();

        public static UITestContext TestSetup(this UITestContext context, OrchardCoreSetupParameters parameters = null)
        {
            parameters ??= new OrchardCoreSetupParameters
            {
                RecipeId = "Lombiq.OSOCE.BasicOrchardFeaturesTests",
            };

            return context.ExecuteTest(
                "Test setup",
                () => context
                    .GoToSetupPage()
                    .SetupOrchardCore(parameters)
                    .ShouldLeaveSetupPage());
        }

        public static UITestContext TestSetupWithInvalidData(this UITestContext context, OrchardCoreSetupParameters parameters = null)
        {
            parameters ??= new OrchardCoreSetupParameters
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

        public static UITestContext TestLogin(
            this UITestContext context,
            string userName = DefaultUser.UserName,
            string password = DefaultUser.Password)
            =>
            context.ExecuteTest(
                "Test login",
                () =>
                {
                    context.GoToLoginPage()
                        .LogInWith(userName, password)
                        .ShouldLeaveLoginPage();

                    context.GetCurrentUserName().ShouldBe(userName);
                });

        public static UITestContext TestLoginWithInvalidData(
            this UITestContext context,
            string userName = DefaultUser.UserName,
            string password = "WrongPass!")
            =>
            context.ExecuteTest(
                "Test login with invalid data",
                () =>
                {
                    context.GoToLoginPage()
                        .LogInWith(userName, password)
                        .ShouldStayOnLoginPage()
                        .ValidationSummaryErrors.Should.Not.BeEmpty();

                    context.GetCurrentUserName().ShouldNotBe(userName);
                });

        public static UITestContext TestLogout(this UITestContext context)
            =>
            context.ExecuteTest(
                "Test logout",
                () =>
                {
                    context.GoToDashboard()
                        .TopNavbar.Account.LogOff.Click()
                        .ShouldLeaveAdminPage();

                    context.GetCurrentUserName().ShouldBeNullOrEmpty();
                });

        public static UITestContext TestRegistration(this UITestContext context, UserRegistrationParameters parameters = null)
        {
            parameters ??= UserRegistrationParameters.CreateDefault();

            return context.ExecuteTest(
                "Test registration",
                () =>
                {
                    context.GoToLoginPage()
                        .RegisterAsNewUser.Should.BeVisible();

                    context.GoToRegistrationPage()
                        .RegisterWith(parameters)
                        .ShouldLeaveRegistrationPage();

                    context.GetCurrentUserName().ShouldBe(parameters.UserName);
                });
        }

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

        public static UITestContext TestContentOperations(this UITestContext context, string pageTitle = "Test page")
            =>
            context.ExecuteTest(
                "Test content operations",
                () =>
                {
                    context.GoToContentItemsPage()
                        .New.Page.ClickAndGo()
                            .Title.Set(pageTitle)
                            .Publish.ClickAndGo()
                        .AlertMessages.Should.Contain(message => message.IsSuccess)
                        .Items[item => item.Title == pageTitle].View.Click();

                    context.Scope.AtataContext.Go.ToNextWindow(new OrdinaryPage(pageTitle))
                        .AggregateAssert(page => page
                            .PageTitle.Should.Contain(pageTitle)
                            .Controls.Create<H1<OrdinaryPage>>("Main").Should.Equal(pageTitle))
                        .CloseWindow();
                });

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
