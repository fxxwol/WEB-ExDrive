using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;

namespace ExDrive.Services
{
    public class ReadFileDOCConcreteStrategy : IReadFileStrategy
    {
        public async Task Execute(HttpContext httpContext, MemoryStream memoryStream)
        {
            DocDocument = new WordDocument(memoryStream, FormatType.Doc);

            DocRenderer.Settings = new DocIORendererSettings()
            {
                AutoTag = true,
                PreserveFormFields = true,
                ExportBookmarks = ExportBookmarkType.Headings
            };

            PdfDocument = DocRenderer.ConvertToPDF(DocDocument);

            PdfDocument.Save(DocMemoryStream);

            httpContext.Response.ContentType = "Application/pdf";

            DocMemoryStream.Position = 0;

            await httpContext.Response.Body.WriteAsync(DocMemoryStream.ToArray());
        }

        void IDisposable.Dispose()
        {
            DocDocument.Dispose();

            DocRenderer.Dispose();

            PdfDocument.Dispose();

            DocMemoryStream.Dispose();

            GC.SuppressFinalize(this);
        }

        private WordDocument DocDocument { get; set; } = new();
        private DocIORenderer DocRenderer { get; set; } = new();
        private PdfDocument PdfDocument { get; set; } = new();
        private MemoryStream DocMemoryStream { get; set; } = new();
    }
}
