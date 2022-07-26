namespace ExDrive.Services
{
    public class ReadFilePngConcreteStrategy : IReadFileStrategy
    {
        public async Task Execute(HttpContext httpContext, MemoryStream memoryStream)
        {
            httpContext.Response.ContentType = "image/png";

            await httpContext.Response.Body.WriteAsync(memoryStream.ToArray());
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
