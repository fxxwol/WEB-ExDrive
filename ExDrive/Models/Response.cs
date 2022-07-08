namespace ExDrive.Authentication
{
    public class Response
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public Response()
        {
            Status = String.Empty;
            Message = String.Empty;
        }
    }
}