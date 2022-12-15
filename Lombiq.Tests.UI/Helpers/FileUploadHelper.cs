using System;
using System.IO;

namespace Lombiq.Tests.UI.Helpers;

public static class FileUploadHelper
{
    private static readonly string BasePath = Path.Combine(Environment.CurrentDirectory, "SampleUploadFiles");

    public static readonly string SamplePdfFileName = "Document.pdf";
    public static readonly string SamplePngFileName = "Image.png";
    public static readonly string SampleDocxFileName = "UploadingTestFileDOCX.docx";
    public static readonly string SampleXlsxFileName = "UploadingTestFileXLSX.xlsx";

    public static readonly string SamplePdfPath = Path.Combine(BasePath, SamplePdfFileName);
    public static readonly string SamplePngPath = Path.Combine(BasePath, SamplePngFileName);
    public static readonly string SampleDocxPath = Path.Combine(BasePath, SampleDocxFileName);
    public static readonly string SampleXlsxPath = Path.Combine(BasePath, SampleXlsxFileName);
}
