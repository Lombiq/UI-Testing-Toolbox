using System;
using System.IO;

namespace Lombiq.Tests.UI.Helpers;

public static class FileUploadHelper
{
    private static readonly string BasePath = Path.Combine(Environment.CurrentDirectory, "SampleUploadFiles");

    public static readonly string SamplePdfPath = Path.Combine(BasePath, "Document.pdf");
    public static readonly string SamplePngPath = Path.Combine(BasePath, "Image.png");
    public static readonly string SampleDocxPath = Path.Combine(BasePath, "UploadingTestFileDOCX.docx");
    public static readonly string SampleXlsxPath = Path.Combine(BasePath, "UploadingTestFileXLSX.xlsx");
}
