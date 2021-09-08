using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions
{
    public static class FileUploadUITestContextExtensions
    {
        /// <summary>
        /// Uploads a file using the UI component found by the given selector.
        /// </summary>
        /// <param name="by">Selector to the file upload UI component.</param>
        /// <param name="filePath">Path to the file to upload.</param>
        public static void UploadFile(this UITestContext context, By by, string filePath) =>
            context.Get(by).SendKeys(filePath);

        /// <summary>
        /// Uploads a file using the UI component found by the given ID.
        /// </summary>
        /// <param name="id">ID of the file upload UI component.</param>
        /// <param name="filePath">Path to the file to upload.</param>
        public static void UploadFileByIdOfAnyVisibility(this UITestContext context, string id, string filePath) =>
            context.UploadFile(By.Id(id).OfAnyVisibility(), filePath);

        /// <summary>
        /// Uploads a sample PDF file using the UI component found by the given ID.
        /// </summary>
        /// <param name="id">ID of the file upload UI component.</param>
        public static void UploadSamplePdfByIdOfAnyVisibility(this UITestContext context, string id) =>
            context.UploadFileByIdOfAnyVisibility(id, FileUploadHelper.SamplePdfPath);

        /// <summary>
        /// Uploads a sample PNG file using the UI component found by the given ID.
        /// </summary>
        /// <param name="id">ID of the file upload UI component.</param>
        public static void UploadSamplePngByIdOfAnyVisibility(this UITestContext context, string id) =>
            context.UploadFileByIdOfAnyVisibility(id, FileUploadHelper.SamplePngPath);

        /// <summary>
        /// Uploads a sample DOCX file using the UI component found by the given ID.
        /// </summary>
        /// <param name="id">ID of the file upload UI component.</param>
        public static void UploadSampleDocxByIdOfAnyVisibility(this UITestContext context, string id) =>
            context.UploadFileByIdOfAnyVisibility(id, FileUploadHelper.SampleDocxPath);

        /// <summary>
        /// Uploads a sample XLSX file using the UI component found by the given ID.
        /// </summary>
        /// <param name="id">ID of the file upload UI component.</param>
        public static void UploadSampleXlsxByIdOfAnyVisibility(this UITestContext context, string id) =>
            context.UploadFileByIdOfAnyVisibility(id, FileUploadHelper.SampleXlsxPath);
    }
}
