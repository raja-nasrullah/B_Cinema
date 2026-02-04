using BookingCinema.Data;
using BookingCinema.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace BookingCinema.Controllers
{
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _context;
        public MovieController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Movie/Index
        public IActionResult Index()
        {
            // Customer can see movies
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var movies = _context.Movies.ToList();
            return View(movies);
        }

        // GET: /Movie/Details/5
        public IActionResult Details(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var movie = _context.Movies.FirstOrDefault(m => m.Id == id);
            if (movie == null)
                return NotFound();

            return View(movie);
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId") != null;
        }
    }
}
