using System;
using System.Diagnostics;
using System.IO;

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

        // Ensure the file path is absolute
        string fullPath = Path.GetFullPath(filePath);

        // Run the screenshot command
        string command = $"import -window root \"{fullPath}\"";
        int exitCode = ExecuteBashCommand(command);

        if (exitCode == 0 && File.Exists(fullPath))
            Console.WriteLine($"Screenshot saved: {fullPath}");
        else
            Console.WriteLine($"Failed to take screenshot. File does not exist: {fullPath}");
    }

    private static bool IsImageMagickInstalled()
    {
        return ExecuteBashCommand("command -v import") == 0;
    }

    private static bool InstallImageMagick()
    {
        Console.WriteLine("Running: sudo apt update && sudo apt install -y imagemagick");
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
