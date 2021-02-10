using Atata;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Helpers
{
    /// <summary>
    /// Provides helper functions for generating <see cref="By"/> selectors.
    /// </summary>
    public static class ByHelper
    {
        /// <summary>
        /// Returns an XPath selector for an email in the list with the given subject.
        /// </summary>
        public static By SmtpInboxRow(string subject) =>
            By
                .XPath($"//tr[contains(@class,'el-table__row')]//div[contains(@class,'cell')][contains(text(), {JsonConvert.SerializeObject(subject)})]")
                .Within(TimeSpan.FromMinutes(2));
    }
}
