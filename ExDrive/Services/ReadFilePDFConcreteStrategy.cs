namespace ExDrive.Services
{
    public class ReadFilePDFConcreteStrategy : IReadFileStrategy
    {
        public async Task Execute(HttpContext httpContext, MemoryStream memoryStream)
        {
            httpContext.Response.ContentType = "Application/pdf";

            await httpContext.Response.Body.WriteAsync(memoryStream.ToArray());
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
