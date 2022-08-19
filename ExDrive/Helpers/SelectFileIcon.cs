using ExDrive.Helpers.Constants;

namespace ExDrive.Helpers
{
    public class SelectFileIcon
    {
        public string Select(string fileId)
        {
            var format = new FindFileFormat().FindFormatWithoutDot(fileId);

            if (ConstantValues._formatsWithIcons.Contains(format))
            {
                return ConstantValues._iconsImagesPath + format + ".png";
            }
            else
            {
                return ConstantValues._defaultIconPath;
            }

            
        }

        public string SelectFavourite(string fileId)
        {
            var format = new FindFileFormat().FindFormatWithoutDot(fileId);

            if (ConstantValues._formatsWithIcons.Contains(format))
            {
                return ConstantValues._iconsImagesPath + format + "fav.png";
            }
            else
            {
                return ConstantValues._defaultFavouriteIconPath;
            }
        }
    }
}
