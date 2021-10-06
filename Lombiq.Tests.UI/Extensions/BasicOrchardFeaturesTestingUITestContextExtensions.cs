using Atata;
using Lombiq.Tests.UI.Constants;
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
                .TestSetupNegatively()
                .TestSetup()
                .TestLoginNegatively()
                .TestLogin()
                .TestLogout();

        public static UITestContext TestSetup(this UITestContext context, OrchardCoreSetupParameters parameters = null)
        {
            parameters ??= new OrchardCoreSetupParameters();

            return context.ExecuteTest(
                "Test setup",
                () =>
                context.GoToSetupPage()
                    .SetupOrchardCore(parameters)
                    .ShouldLeaveSetupPage());
        }

        public static UITestContext TestSetupNegatively(this UITestContext context, OrchardCoreSetupParameters parameters = null)
        {
            parameters ??= new OrchardCoreSetupParameters
            {
                SiteName = string.Empty,
                UserName = string.Empty,
                Email = string.Empty,
                Password = string.Empty,
            };

            return context.ExecuteTest(
                "Test setup negatively",
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

        public static UITestContext TestLoginNegatively(
            this UITestContext context,
            string userName = DefaultUser.UserName,
            string password = "WrongPass!")
            =>
            context.ExecuteTest(
                "Test login negatively",
                () =>
                {
                    context.GoToLoginPage()
                        .LogInWith(userName, password)
                        .ShouldStayOnLoginPage();

                    context.GetCurrentUserName().ShouldNotBe(userName);
                });

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
