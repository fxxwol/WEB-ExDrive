using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;

namespace ExDrive.Services
{
    public class ReadFileDocxConcreteStrategy : IReadFileStrategy
    {
        public async Task Execute(HttpContext httpContext, MemoryStream memoryStream)
        {
            DocxDocument = new WordDocument(memoryStream, FormatType.Docx);

            DocxRenderer.Settings = new DocIORendererSettings()
            {
                AutoTag = true,
                PreserveFormFields = true,
                ExportBookmarks = ExportBookmarkType.Headings
            };

            PdfDocument = DocxRenderer.ConvertToPDF(DocxDocument);

            PdfDocument.Save(DocxMemoryStream);

            httpContext.Response.ContentType = "Application/pdf";

            DocxMemoryStream.Position = 0;

            await httpContext.Response.Body.WriteAsync(DocxMemoryStream.ToArray());
        }

        void IDisposable.Dispose()
        {
            DocxDocument.Dispose();

            DocxRenderer.Dispose();

            PdfDocument.Dispose();

            DocxMemoryStream.Dispose();

            GC.SuppressFinalize(this);
        }

        private WordDocument DocxDocument { get; set; } = new();
        private DocIORenderer DocxRenderer { get; set; } = new();
        private PdfDocument PdfDocument { get; set; } = new();
        private MemoryStream DocxMemoryStream { get; set; } = new();
    }
}
