namespace ExDrive.Services
{
    public class ReadFileJpegConcreteStrategy : IReadFileStrategy
    {
        public async Task Execute(HttpContext httpContext, MemoryStream memoryStream)
        {
            httpContext.Response.ContentType = "image/jpeg";
            
            await httpContext.Response.Body.WriteAsync(memoryStream.ToArray());
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
