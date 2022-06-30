using System.Drawing;

using ImageSharpRectangle = SixLabors.ImageSharp.Rectangle;

namespace Lombiq.Tests.UI.Extensions;

public static class RectangleExtensions
{
    /// <summary>
    /// Converts <see cref="Rectangle"/> to <see cref="ImageSharpRectangle"/>.
    /// </summary>
    /// <param name="rectangle">The source <see cref="Rectangle"/>.</param>
    /// <returns><see cref="ImageSharpRectangle"/>.</returns>
    public static ImageSharpRectangle ToImageSharpRectangle(this Rectangle rectangle) =>
        new(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
}
