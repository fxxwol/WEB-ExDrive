namespace ExDrive.Services
{
    public class ReadFileTxtConcreteStrategy : IReadFileStrategy
    {
        public async Task Execute(HttpContext httpContext, MemoryStream memoryStream)
        {
            httpContext.Response.ContentType = "text/plain";
            
            await httpContext.Response.Body.WriteAsync(memoryStream.ToArray());
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
