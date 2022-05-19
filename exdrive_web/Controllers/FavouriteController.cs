using exdrive_web.Models;
using JWTAuthentication.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace exdrive_web.Controllers
{
    public class FavouriteController : Controller
    {
        private string? _userId;

        private static List<NameInstance>? _fav = new List<NameInstance>();
        private static List<NameInstance>? _searchResult = new List<NameInstance>();

        private static ApplicationDbContext _applicationDbContext;
        public FavouriteController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }
        public IActionResult Favourite()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _fav = new List<NameInstance>(Fav.GetFavourite(_userId));
            _searchResult = null;
            return View(_fav);
        }
        public ActionResult FileClick(string afile)
        {
            int position = Int32.Parse(afile);

            // if there was no search, file from main List is selected
            // (user is not in search mode)
            if (_searchResult == null)
            {
                if (_fav != null)
                {
                    _fav.ElementAt(position).IsSelected ^= true;
                    return View("Trashcan", _fav);
                }
            }
            else
                _searchResult.ElementAt(position).IsSelected ^= true;

            return View("Trashcan", _searchResult);
        }

    }
}
