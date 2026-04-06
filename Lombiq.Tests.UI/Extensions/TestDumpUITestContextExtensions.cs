using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class TestDumpUITestContextExtensions
{
    /// <summary>
    /// Appends a local directory's whole content to be collected in the test dump.
    /// </summary>
    /// <param name="directoryPath">The full file system path of the directory.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendDirectoryToTestDump(
        this UITestContext context,
        string directoryPath,
        string messageIfExists = null) =>
        RecursivelyAppendFolderContent(context, directoryPath, string.Empty, messageIfExists);

    /// <summary>
    /// Appends a local file's content to be collected in the test dump.
    /// </summary>
    /// <param name="filePath">The full file system path of the file.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendTestDump(
        this UITestContext context,
        string filePath,
        string messageIfExists = null) =>
        context.AppendTestDump(
            Path.GetFileName(filePath),
            context => Task.FromResult((Stream)File.OpenRead(filePath)),
            messageIfExists);

    /// <summary>
    /// Appends stream as file content to be collected in the test dump.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="action">Gets called in test dump collection.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendTestDump(
        this UITestContext context,
        string fileName,
        Func<UITestContext, Task<Stream>> action,
        string messageIfExists = null) =>
        context.AppendTestDumpInternal(
            fileName,
            new TestDumpItem(() => action(context)),
            messageIfExists);

    /// <summary>
    /// Appends string as file content to be collected in the test dump.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="content">File content.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendTestDump(
        this UITestContext context,
        string fileName,
        string content,
        string messageIfExists = null) =>
        context.AppendTestDumpInternal(
            fileName,
            new TestDumpItem(
                () => Task.FromResult(
                    new MemoryStream(
                        Encoding.UTF8.GetBytes(content)) as Stream)),
            messageIfExists);

    /// <summary>
    /// Appends generic content as file content to be collected in the test dump.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="content">File content.</param>
    /// <param name="getStream">Function to get a new <see cref="Stream"/> from content.</param>
    /// <param name="dispose">Action to dispose the content, if required. Can be null.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendTestDump<TContent>(
        this UITestContext context,
        string fileName,
        TContent content,
        Func<TContent, Task<Stream>> getStream,
        Action<TContent> dispose = null,
        string messageIfExists = null) =>
        context.AppendTestDumpInternal(
            fileName,
            new TestDumpItemGeneric<TContent>(content, getStream, dispose),
            messageIfExists);

    /// <summary>
    /// Appends <see cref="Image"/> as file content to be collected in the test dump.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="image">File content. The <see cref="Image"/> will be disposed at the end.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendTestDump(
        this UITestContext context,
        string fileName,
        Image image,
        string messageIfExists = null) => context
        .AppendTestDump(
            fileName,
            image,
            _ => Task.FromResult(image.ToStream()),
            messageIfExists: messageIfExists);

    /// <summary>
    /// Appends a local file's content to be collected in the test dump. Suffixes the file name with an index in case of
    /// duplicates.
    /// </summary>
    /// <param name="filePath">The full file system path of the file.</param>
    public static void AppendTestDumpKeepingDuplicates(
        this UITestContext context,
        string filePath) =>
        context.AppendTestDumpKeepingDuplicatesInternal(
            Path.GetFileName(filePath),
            new TestDumpItem(() => Task.FromResult((Stream)File.OpenRead(filePath))));

    /// <summary>
    /// Appends stream as file content to be collected in the test dump. Suffixes the file name with an index in case of
    /// duplicates.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="action">Gets called in test dump collection.</param>
    public static void AppendTestDumpKeepingDuplicates(
        this UITestContext context,
        string fileName,
        Func<UITestContext, Task<Stream>> action) =>
        context.AppendTestDumpKeepingDuplicatesInternal(
            fileName,
            new TestDumpItem(() => action(context)));

    /// <summary>
    /// Appends string as file content to be collected in the test dump. Suffixes the file name with an index in case of
    /// duplicates.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="content">File content.</param>
    public static void AppendTestDumpKeepingDuplicates(
        this UITestContext context,
        string fileName,
        string content) =>
        context.AppendTestDumpKeepingDuplicatesInternal(
            fileName,
            new TestDumpItem(
                () => Task.FromResult(
                    new MemoryStream(
                        Encoding.UTF8.GetBytes(content)) as Stream)));

    /// <summary>
    /// Appends generic content as file content to be collected in the test dump. Suffixes the file name with an index
    /// in case of duplicates.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="content">File content.</param>
    /// <param name="getStream">Function to get a new <see cref="Stream"/> from content.</param>
    /// <param name="dispose">Action to dispose the content, if required. Can be null.</param>
    public static void AppendTestDumpKeepingDuplicates<TContent>(
        this UITestContext context,
        string fileName,
        TContent content,
        Func<TContent, Task<Stream>> getStream,
        Action<TContent> dispose = null) =>
        context.AppendTestDumpKeepingDuplicatesInternal(
            fileName,
            new TestDumpItemGeneric<TContent>(content, getStream, dispose));

    /// <summary>
    /// Appends <see cref="Image"/> as file content to be collected in the test dump. Suffixes the file name with an
    /// index in case of duplicates. The <see cref="Image"/> will be disposed at the end.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="image">File content. The <see cref="Image"/> will be disposed at the end.</param>
    public static void AppendTestDumpKeepingDuplicates(
        this UITestContext context,
        string fileName,
        Image image) => context
        .AppendTestDumpKeepingDuplicates(
            fileName,
            image,
            _ => Task.FromResult(image.ToStream()));

    private static void AppendTestDumpKeepingDuplicatesInternal(
        this UITestContext context,
        string fileName,
        ITestDumpItem item)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = fileName;
        var i = 0;

        while (context.TestDumpContainer.ContainsKey(uniqueFileName))
        {
            i++;
            uniqueFileName = $"{fileNameWithoutExtension}_{i.ToTechnicalString()}.{extension}";
        }

        context.AppendTestDumpInternal(uniqueFileName, item);
    }

    private static void AppendTestDumpInternal(
        this UITestContext context,
        string fileName,
        ITestDumpItem item,
        string messageIfExists = null)
    {
        if (context.TestDumpContainer.ContainsKey(fileName))
        {
            throw new TestDumpItemAlreadyExistsException(fileName, messageIfExists);
        }

        context.TestDumpContainer.Add(fileName, item);
    }

    private static void RecursivelyAppendFolderContent(
        UITestContext context,
        string directoryPath,
        string testDumpDirectoryPath,
        string messageIfExists = null)
    {
        foreach (var filePath in Directory.GetFiles(directoryPath))
        {
            context.AppendTestDump(
                Path.Combine(testDumpDirectoryPath, Path.GetFileName(filePath)),
                context => Task.FromResult((Stream)File.OpenRead(filePath)),
                messageIfExists);
        }

        foreach (var subDirectoryPath in Directory.GetDirectories(directoryPath))
        {
            RecursivelyAppendFolderContent(
                context,
                subDirectoryPath,
                Path.Combine(testDumpDirectoryPath, Path.GetFileName(subDirectoryPath)),
                messageIfExists);
        }
    }
}
