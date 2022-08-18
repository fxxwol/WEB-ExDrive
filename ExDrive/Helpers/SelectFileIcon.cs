using ExDrive.Models;
using ExDrive.Helpers.Constants;

namespace ExDrive.Helpers
{
    public class SelectFileIcon
    {
        public string Select(string fileId)
        {
            var type = new FindFileFormat().FindFormat(fileId);

            return ConstantValues._iconsImagesPath + type + ".png";
        }

        public string SelectFavourite(string fileId)
        {
            var type = new FindFileFormat().FindFormat(fileId);

            return ConstantValues._iconsImagesPath + type + "fav.png";
        }
    }
}
