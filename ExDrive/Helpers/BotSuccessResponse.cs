namespace ExDrive.Helpers
{
    public class BotSuccessResponse
    {
        public async Task<HttpResponse> Write(HttpResponse httpResponse, string message)
        {
            httpResponse.Clear();

            httpResponse.StatusCode = 200;

            httpResponse.ContentType = "text/xml";

            await httpResponse.WriteAsync(message);

            return httpResponse;
        }
    }
}
