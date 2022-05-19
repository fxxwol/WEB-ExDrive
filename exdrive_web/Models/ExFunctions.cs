namespace exdrive_web.Models
{
    public class ExFunctions
    {
        public static readonly string virusTotalToken = "335345c7f7785bccec51c7bf5f24323abef49d75be054ef14c885fe38f7eabd2";
        public static readonly string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=exdrivefiles;AccountKey=zW8bG071a7HbJ4+D5Pxruz4rL47KEx0XwExd7m5CmYtCNdu8A71/rVvvY/ld8hwJ4nObLnAcDB27KZV/0L92TA==;EndpointSuffix=core.windows.net";
        public static readonly string sqlConnectionString = "Server=tcp:exdrive1.database.windows.net,1433;Initial Catalog=ExDrive;Persist Security Info=False;User ID=senyakappa;Password=d3ltA_Pr0mi[neNs32ngO;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        public static string FindFormat(string filename)
        {
            string format = "";
            for (int i = filename.LastIndexOf('.'); i < filename.Length; i++)
                format += filename.ElementAt(i);

            return format;
        }
    }
}
