using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Lombiq.Tests.UI.Services;

public static class WindowsScreenCapture
{
    public static void CaptureScreen(string filePath)
    {
        int screenWidth = GetSystemMetrics(0);
        int screenHeight = GetSystemMetrics(1);

        IntPtr hdcScreen = GetDC(IntPtr.Zero);
        IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, screenWidth, screenHeight);
        IntPtr oldObject = SelectObject(hdcMem, hBitmap);

        BitBlt(hdcMem, 0, 0, screenWidth, screenHeight, hdcScreen, 0, 0, CopyPixelOperation.SourceCopy);
        SelectObject(hdcMem, oldObject);

        SaveBitmap(hBitmap, filePath);

        DeleteObject(hBitmap);
        DeleteDC(hdcMem);
        ReleaseDC(IntPtr.Zero, hdcScreen);

        Console.WriteLine($"Screenshot saved: {filePath}");
    }

    private static void SaveBitmap(IntPtr hBitmap, string filePath)
    {
        using var bitmap = Image.FromHbitmap(hBitmap);
        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
    }

    // 📌 Windows API functions
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int w, int h, IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
}
