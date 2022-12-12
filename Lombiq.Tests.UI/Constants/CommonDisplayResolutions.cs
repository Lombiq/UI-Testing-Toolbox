using SixLabors.ImageSharp;

namespace Lombiq.Tests.UI.Constants;

/// <summary>
/// Some common display resolutions to be used when setting browser window sizes with <see
/// cref="Extensions.ResponsivenessUITestContextExtensions.SetBrowserSize(Services.UITestContext, Size)"/>. Generally
/// it's better to test the given app's responsive breakpoints specifically though instead of using such standard
/// resolutions.
/// </summary>
/// <remarks>
/// <para>
/// Taken mostly from <see href="https://en.wikipedia.org/wiki/Display_resolution#Common_display_resolutions"/>, and
/// also from <see href="https://en.wikipedia.org/wiki/List_of_common_resolutions"/>.
/// </para>
/// </remarks>
public static class CommonDisplayResolutions
{
    public static readonly Size Qvga = new(320, 240);
    public static readonly Size Hvga = new(480, 320);
    public static readonly Size Nhd = new(640, 360);
    public static readonly Size NhdPortrait = new(Nhd.Height, Nhd.Width);
    public static readonly Size Vga = new(640, 480);
    public static readonly Size Svga = new(800, 600);
    public static readonly Size Qhd = new(960, 540);
    public static readonly Size Xga = new(1024, 768);
    public static readonly Size Hd = new(1280, 720);
    public static readonly Size Sxga = new(1280, 1024);
    public static readonly Size WxgaPlus = new(1440, 900);
    public static readonly Size HdPlus = new(1600, 900);
    public static readonly Size WsxgaPlus = new(1680, 1050);
    public static readonly Size Fhd = new(1920, 1080);
    public static readonly Size Wuxga = new(1920, 1200);
    public static readonly Size Dci2K = new(2048, 1080); // #spell-check-ignore-line
    public static readonly Size Qwxga = new(2048, 1152);
    public static readonly Size Wqhd = new(2560, 1440);
    public static readonly Size Uwqhd = new(3440, 1440);
    public static readonly Size FourKUhd = new(3840, 3160);
    public static readonly Size FiveK = new(5120, 2880);
    public static readonly Size EightKUhd = new(7680, 4320);

    public static readonly Size Standard = Fhd;
}
