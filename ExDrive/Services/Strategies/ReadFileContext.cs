namespace ExDrive.Services
{
    public class ReadFileContext
    {
        private IReadFileStrategy? _readFileStrategy;

        public void SetStrategy(IReadFileStrategy readFileStrategy)
        {
            _readFileStrategy = readFileStrategy;
        }

        public async Task ExecuteStrategy(HttpContext httpContext, MemoryStream memoryStream)
        {
            if (_readFileStrategy == null)
            {
                throw new ArgumentNullException("_readFileStrategy", "Tried to ExecuteStrategy but _readFileStrategy is null");
            }

            await _readFileStrategy.Execute(httpContext, memoryStream);
        }

        public void Dispose()
        {
            if (_readFileStrategy != null)
            {
                _readFileStrategy.Dispose();
            }
        }
    }
}
