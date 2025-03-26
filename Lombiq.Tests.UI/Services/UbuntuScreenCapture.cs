using System;
using System.Diagnostics;

namespace Lombiq.Tests.UI.Services;

public static class UbuntuScreenCapture
{
    public static void CaptureScreen(string filePath)
    {
        if (!IsImageMagickInstalled())
        {
            Console.WriteLine("ImageMagick is not installed. Installing now...");
            if (!InstallImageMagick())
            {
                Console.WriteLine("Failed to install ImageMagick. Cannot take screenshot.");
                return;
            }
        }

        string command = $"import -window root {filePath}";
        int exitCode = ExecuteBashCommand(command);

        if (exitCode == 0)
            Console.WriteLine($"Screenshot saved: {filePath}");
        else
            Console.WriteLine("Failed to take screenshot on Ubuntu.");
    }

    private static bool IsImageMagickInstalled()
    {
        return ExecuteBashCommand("command -v import") == 0;
    }

    private static bool InstallImageMagick()
    {
        return ExecuteBashCommand("sudo apt update && sudo apt install -y imagemagick") == 0;
    }

    private static int ExecuteBashCommand(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }
}
