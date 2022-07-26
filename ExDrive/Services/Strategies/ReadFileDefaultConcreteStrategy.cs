namespace ExDrive.Services
{
    public class ReadFileDefaultConcreteStrategy : IReadFileStrategy
    {
        public async Task Execute(HttpContext httpContext, MemoryStream memoryStream)
        {
            await httpContext.Response.WriteAsync("Sorry but we can't open this file :(");
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
