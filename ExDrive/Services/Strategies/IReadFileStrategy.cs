namespace ExDrive.Services
{
    public interface IReadFileStrategy : IDisposable
    {
        public Task Execute(HttpContext httpContext, MemoryStream memoryStream);
    }
}
