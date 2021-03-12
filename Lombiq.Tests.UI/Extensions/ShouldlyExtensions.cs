using System.Diagnostics.CodeAnalysis;

namespace Shouldly
{
    public static class ShouldlyExtensions
    {
        /// <summary>
        /// Calls <see cref="object.ToString()"/> on both <paramref name="actual"/> and <paramref name="expected"/>
        /// (unless they are <see langword="null"/>) and checks if they are the same.
        /// </summary>
        [SuppressMessage("Code Smell", "S4225:Extension methods should not extend \"object\"", Justification = "This is what Shouldly does.")]
        public static void ShouldBeAsString(this object actual, object expected, string customMessage = null)
        {
            // We need this variable because the null-forgiving operator is shortcutting. This way the ShouldBe is
            // called even if actual is null.
            var actualText = actual?.ToString();

            actualText.ShouldBe(expected?.ToString(), customMessage);
        }
    }
}
