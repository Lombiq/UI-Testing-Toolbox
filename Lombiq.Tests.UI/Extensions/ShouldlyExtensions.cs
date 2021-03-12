namespace Shouldly
{
    public static class ShouldlyExtensions
    {
        /// <summary>
        /// Calls <see cref="object.ToString()"/> on both <paramref name="actual"/> and <paramref name="expected"/>
        /// (unless they are <see langword="null"/>) and
        /// </summary>
        public static void ShouldBeAsString(this object actual, object expected, string customMessage = null)
        {
            // We need this variable because the null-forgiving operator is shortcutting. This way the ShouldBe is
            // called even if actual is null.
            var actualText = actual?.ToString();

            actualText.ShouldBe(expected?.ToString(), customMessage);
        }
    }
}
