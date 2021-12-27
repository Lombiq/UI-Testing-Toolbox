using Atata;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ControlExtensions
    {
        public static TOwner ClickAndAssertNoPageChanges<TOwner>(this Control<TOwner> control)
            where TOwner : PageObject<TOwner>
        {
            var pageObject = control.Owner;

            string savedUrl = pageObject.PageUrl;
            string savedHtml = pageObject.PageSource;

            control.Click();

            pageObject.PageUrl.Should.AtOnce.Equal(savedUrl);
            pageObject.PageSource.Should.AtOnce.Satisfy(value => value.EqualsOrdinal(savedHtml), "equal previous HTML");

            return pageObject;
        }
    }
}
