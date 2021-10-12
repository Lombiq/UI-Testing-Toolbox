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
                () =>
                context.GoToSetupPage()
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
                () =>
                context.GoToSetupPage()
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

        public static UITestContext TestRegistration(this UITestContext context, UserRegistrationModel model = null)
        {
            model ??= UserRegistrationModel.CreateDefault();

            return context.ExecuteTest(
                "Test registration",
                () =>
                {
                    context.GoToLoginPage()
                        .RegisterAsNewUser.Should.BeVisible();

                    context.GoToRegistrationPage()
                        .RegisterWith(model)
                        .ShouldLeaveRegistrationPage();

                    context.GetCurrentUserName().ShouldBe(model.UserName);
                });
        }

        public static UITestContext TestRegistrationWithInvalidData(this UITestContext context, UserRegistrationModel model = null)
        {
            model ??= new()
            {
                UserName = "InvalidUser",
                Email = Randomizer.GetString("{0}@example.org", 25),
                Password = "short",
                ConfirmPassword = "short",
            };

            return context.ExecuteTest(
                "Test registration with invalid data",
                () =>
                context.GoToRegistrationPage()
                    .RegisterWith(model)
                    .ShouldStayOnRegistrationPage()
                    .ValidationMessages.Should.Not.BeEmpty());
        }

        public static UITestContext TestRegistrationWithAlreadyRegisteredEmail(this UITestContext context, UserRegistrationModel model = null)
        {
            model ??= UserRegistrationModel.CreateDefault();

            return context.ExecuteTest(
                "Test registration with already registered email",
                () =>
                context.GoToRegistrationPage()
                    .RegisterWith(model)
                    .ShouldStayOnRegistrationPage()
                    .ValidationMessages[x => x.Email].Should.BeVisible());
        }

        public static UITestContext TestTurningFeatureOnAndOff(this UITestContext context, string featureName = "Background Tasks")
            =>
            context.ExecuteTest(
                "Test turning feature on and off",
                () =>
                context.GoToFeaturesPage()
                    .SearchForFeature(featureName).IsEnabled.Get(out bool originalEnabledState)
                    .Features[featureName].CheckBox.Check()
                    .BulkActions.Toggle.Click()

                    .AggregateAssert(x => x
                        .ShouldContainSuccessAlertMessage(TermMatch.Contains, featureName)
                        .AdminMenu.FindMenuItem(featureName).IsPresent.Should.Equal(!originalEnabledState)
                        .SearchForFeature(featureName).IsEnabled.Should.Equal(!originalEnabledState))
                    .Features[featureName].CheckBox.Check()
                    .BulkActions.Toggle.Click()

                    .AggregateAssert(x => x
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
