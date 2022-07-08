namespace ExDrive.Configuration
{
    public class AuthMessageSenderOptions
    {
        public string SendGridKey { get; set; }
        public AuthMessageSenderOptions()
        {
            SendGridKey = String.Empty;
        }
    }
}
