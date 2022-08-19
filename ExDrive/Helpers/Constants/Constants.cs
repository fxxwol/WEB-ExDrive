namespace ExDrive.Helpers.Constants
{
    public class ConstantValues
    {
        public static readonly string _temporaryFilesFolderPath = "C:\\Users\\Public\\tmpfiles\\";
        public static readonly string _temporaryArchiveFolderPath = "C:\\Users\\Public\\getlink\\";
        public static readonly string _temporaryFilesContainerLink = "https://exdrivefile.blob.core.windows.net/botfiles/";
        public static readonly string _trashcanContainerName = "trashcan";
        public static readonly string _iconsImagesPath = "~/css/images/";
        public static readonly string _defaultIconPath = _iconsImagesPath + "file.png";
        public static readonly string _defaultFavouriteIconPath = _iconsImagesPath + "filefav.png";
        public static readonly HashSet<string> _formatsWithIcons = new()
        {
            "doc",
            "docx",
            "gif",
            "jpg",
            "jpeg",
            "png",
            "mov",
            "mp3",
            "mp4",
            "pdf",
            "txt",
            "webm",
            "xls",
            "xlsx",
            "zip"
        };
    }
}
