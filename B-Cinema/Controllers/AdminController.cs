using BookingCinema.Data;
using BookingCinema.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using B_Cinema.Models;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace BookingCinema.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- AUTHENTICATION HELPER ---
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        // --- DASHBOARD ---
        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel
            {
                // Per instruction: ID 1 is not treated as a manageable user
                UserCount = _context.Users.Count(u => u.Id != 1),
                BookingCount = _context.Bookings.Count(),
                MovieCount = _context.Movies.Count(),
                TicketCount = _context.Tickets.Count()
            };

            return View(model);
        }

        // --- MOVIE MANAGEMENT ---

        public IActionResult AllMovies()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(_context.Movies.ToList());
        }

        [HttpGet]
        public IActionResult AddMovie()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMovie(Movie model, IFormFile? MovieImage)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                if (MovieImage != null && MovieImage.Length > 0)
                {
                    model.ImagePath = SaveImage(MovieImage);
                }

                _context.Movies.Add(model);
                _context.SaveChanges();
                return RedirectToAction("AllMovies");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult EditMovie(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var movie = _context.Movies.Find(id);
            if (movie == null) return NotFound();
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMovie(Movie model, IFormFile? MovieImage)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var movieInDb = _context.Movies.Find(model.Id);
                if (movieInDb == null) return NotFound();

                movieInDb.Title = model.Title;
                movieInDb.Description = model.Description;
                movieInDb.Duration = model.Duration;
                movieInDb.Price = model.Price;

                if (MovieImage != null && MovieImage.Length > 0)
                {
                    movieInDb.ImagePath = SaveImage(MovieImage);
                }

                _context.SaveChanges();
                return RedirectToAction("AllMovies");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMovie(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var movie = _context.Movies.Find(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                _context.SaveChanges();
            }
            return RedirectToAction("AllMovies");
        }

        // --- USER MANAGEMENT ---

        public IActionResult AllUsers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            // Filtering out User ID 1 as per requirements
            var users = _context.Users.Where(u => u.Id != 1).ToList();
            return View(users);
        }

        [HttpGet]
        public IActionResult AddUser()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Roles = new List<string> { "Admin", "User" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User user)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                user.Password = ComputeSha256Hash(user.Password);
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction("AllUsers");
            }
            ViewBag.Roles = new List<string> { "Admin", "User" };
            return View(user);
        }

        [HttpGet]
        public IActionResult EditUser(int id)
        {
            if (!IsAdmin() || id == 1) return RedirectToAction("AllUsers");
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            ViewBag.Roles = new List<string> { "Admin", "User" };
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(User model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var userInDb = _context.Users.Find(model.Id);
            if (userInDb == null || userInDb.Id == 1) return NotFound();

            if (ModelState.IsValid)
            {
                userInDb.Name = model.Name;
                userInDb.Email = model.Email;
                userInDb.Role = model.Role;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    userInDb.Password = ComputeSha256Hash(model.Password);
                }

                _context.SaveChanges();
                return RedirectToAction("AllUsers");
            }
            ViewBag.Roles = new List<string> { "Admin", "User" };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            if (!IsAdmin() || id == 1) return RedirectToAction("AllUsers");
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
            return RedirectToAction("AllUsers");
        }

        // --- TICKET MANAGEMENT ---

        public IActionResult AllTickets()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var tickets = _context.Tickets
                .Include(t => t.Movie)
                .Include(t => t.User)
                .Include(t => t.Showtime)
                .ToList();
            return View(tickets);
        }

        [HttpGet]
        public IActionResult AddTicket()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Users = _context.Users.Where(u => u.Id != 1).ToList();
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Showtimes = _context.Showtimes.Include(s => s.Movie).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddTicket(Ticket model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(model.TicketNumber))
                    model.TicketNumber = "TKT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                model.IssuedAt = DateTime.Now;
                _context.Tickets.Add(model);
                _context.SaveChanges();
                return RedirectToAction("AllTickets");
            }
            ViewBag.Users = _context.Users.Where(u => u.Id != 1).ToList();
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Showtimes = _context.Showtimes.Include(s => s.Movie).ToList();
            return View(model);
        }
        // GET: /Admin/EditTicket/5
        [HttpGet]
        public IActionResult EditTicket(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var ticket = _context.Tickets.Find(id);
            if (ticket == null) return NotFound();

            // Populate dropdowns for the edit form
            // Ensuring User 1 is excluded from the list of assignable users
            ViewBag.Users = _context.Users.Where(u => u.Id != 1).ToList();
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Showtimes = _context.Showtimes.Include(s => s.Movie).ToList();

            return View(ticket);
        }

        // POST: /Admin/EditTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTicket(Ticket model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var ticketInDb = _context.Tickets.Find(model.Id);
                if (ticketInDb == null) return NotFound();

                // Update properties
                ticketInDb.UserId = model.UserId;
                ticketInDb.MovieId = model.MovieId;
                ticketInDb.ShowtimeId = model.ShowtimeId;
                ticketInDb.TicketNumber = model.TicketNumber;

                // Usually, IssuedAt is kept as the original creation date, 
                // but you can update it if you want to track modification time:
                // ticketInDb.IssuedAt = DateTime.Now;

                _context.SaveChanges();
                return RedirectToAction("AllTickets");
            }

            // If validation fails, reload dropdowns and return the view
            ViewBag.Users = _context.Users.Where(u => u.Id != 1).ToList();
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Showtimes = _context.Showtimes.Include(s => s.Movie).ToList();
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTicket(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var ticket = _context.Tickets.Find(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
                _context.SaveChanges();
            }
            return RedirectToAction("AllTickets");
        }

        public IActionResult TicketDetail(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var ticket = _context.Tickets
                .Include(t => t.Movie)
                .Include(t => t.User)
                .Include(t => t.Showtime)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null) return NotFound();
            return View(ticket);
        }

        // --- SHARED HELPERS ---

        private string SaveImage(IFormFile image)
        {
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/movie-posters");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(stream);
            }
            return "/movie-posters/" + fileName;
        }

        private string ComputeSha256Hash(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return "";
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(rawData);
                var hash = sha256.ComputeHash(bytes);
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }
        // --- SHOWTIME MANAGEMENT ---

        public IActionResult AllShowtimes()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // Grouping showtimes by Movie so one movie appears once with a list of times
            var groupedShowtimes = _context.Showtimes
                .Include(s => s.Movie)
                .ToList()
                .GroupBy(s => s.MovieId)
                .ToList();

            return View(groupedShowtimes);
        }

        [HttpGet]
        public IActionResult AddShowtime()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // We need the list of movies for the dropdown
            ViewBag.Movies = _context.Movies.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddShowtime(Showtime model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Showtimes.Add(model);
                _context.SaveChanges();
                return RedirectToAction("AllShowtimes");
            }

            ViewBag.Movies = _context.Movies.ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult EditShowtime(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var showtime = _context.Showtimes.Find(id);
            if (showtime == null) return NotFound();

            ViewBag.Movies = _context.Movies.ToList();
            return View(showtime);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditShowtime(Showtime model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var showtimeInDb = _context.Showtimes.Find(model.Id);
                if (showtimeInDb == null) return NotFound();

                showtimeInDb.MovieId = model.MovieId;
                showtimeInDb.MovieDate = model.MovieDate;
                showtimeInDb.MovieTime = model.MovieTime;

                _context.SaveChanges();
                return RedirectToAction("AllShowtimes");
            }

            ViewBag.Movies = _context.Movies.ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteShowtime(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var showtime = _context.Showtimes.Find(id);
            if (showtime != null)
            {
                _context.Showtimes.Remove(showtime);
                _context.SaveChanges();
            }
            return RedirectToAction("AllShowtimes");
        }

    }
}