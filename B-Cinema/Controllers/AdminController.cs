using BookingCinema.Data;
using BookingCinema.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore; // Added for .Include()
using System.Linq;
using B_Cinema.Models;
using System.Text;
using System.Security.Cryptography;

namespace BookingCinema.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel
            {
                // We exclude User 1 from count if you want a true reflection of active users
                UserCount = _context.Users.Count(u => u.Id != 1),
                BookingCount = _context.Bookings.Count(),
                MovieCount = _context.Movies.Count(),
                TicketCount = _context.Tickets.Count() // Make sure this is in your ViewModel
            };

            return View(model);
        }

        // --- TICKET MANAGEMENT SECTION ---

        // GET: /Admin/AllTickets
        public IActionResult AllTickets()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            // We use Include to get Movie, User, and Showtime details for the table
            var tickets = _context.Tickets
                .Include(t => t.Movie)
                .Include(t => t.User)
                .Include(t => t.Showtime)
                .ToList();

            return View(tickets);
        }

        // GET: /Admin/AddTicket
        public IActionResult AddTicket()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // Populate dropdowns - Excluding User 1
            ViewBag.Users = _context.Users.Where(u => u.Id != 1).ToList();
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Showtimes = _context.Showtimes.Include(s => s.Movie).ToList();

            return View();
        }
        // POST: /Admin/AddTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddTicket(Ticket model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                // Auto-generate a unique Ticket Number if not provided
                if (string.IsNullOrEmpty(model.TicketNumber))
                {
                    model.TicketNumber = "TKT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                }

                model.IssuedAt = DateTime.Now;

                _context.Tickets.Add(model);
                _context.SaveChanges();
                return RedirectToAction("AllTickets");
            }

            // Reload dropdowns if there is a validation error
            ViewBag.Users = _context.Users.Where(u => u.Id != 1).ToList();
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Showtimes = _context.Showtimes.Include(s => s.Movie).ToList();
            return View(model);
        }
        // GET: /Admin/EditTicket/5
        public IActionResult EditTicket(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            // Prepare dropdown data
            // Note: We filter out User 1 as per your requirement
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
                var ticket = _context.Tickets.FirstOrDefault(t => t.Id == model.Id);
                if (ticket == null) return NotFound();

                // Update properties
                ticket.UserId = model.UserId;
                ticket.MovieId = model.MovieId;
                ticket.ShowtimeId = model.ShowtimeId;
                ticket.TicketNumber = model.TicketNumber; // Usually tickets are unique, but editable here

                _context.SaveChanges();
                return RedirectToAction("AllTickets");
            }

            // If we reach here, something failed; reload dropdowns
            ViewBag.Users = _context.Users.Where(u => u.Id != 1).ToList();
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Showtimes = _context.Showtimes.Include(s => s.Movie).ToList();
            return View(model);
        }

        // POST: /Admin/DeleteTicket/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTicket(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            _context.Tickets.Remove(ticket);
            _context.SaveChanges();

            // Redirect back to the list to show it's gone
            return RedirectToAction("AllTickets");
        }

        // GET: /Admin/TicketDetail/5
        public IActionResult TicketDetail(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var ticket = _context.Tickets
                .Include(t => t.Movie)
                .Include(t => t.User)
                .Include(t => t.Showtime)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null) return NotFound();

            // Explicitly naming the view file here
            return View("TicketDetail", ticket);
        }

        // --- EXISTING MOVIE METHODS ---

        public IActionResult AllMovies()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(_context.Movies.ToList());
        }

        public IActionResult AddMovie()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult AddMovie(Movie model, IFormFile MovieImage)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                if (MovieImage != null && MovieImage.Length > 0)
                {
                    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/movie-posters");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(MovieImage.FileName);
                    var filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        MovieImage.CopyTo(stream);

                    model.ImagePath = "/movie-posters/" + fileName;
                }

                _context.Movies.Add(model);
                _context.SaveChanges();
                return RedirectToAction("AllMovies");
            }
            return View(model);
        }

        // --- USER MANAGEMENT SECTION ---

        [HttpGet]
        public IActionResult AddUser()
        {
            // Remember to populate your ViewBag for the dropdown
            ViewBag.Roles = new List<string> { "Admin", "User", "Manager" };
            return View();
        }

        // POST: Admin/AddUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User user)
        {
            // 1. Check if the data sent from the form is valid based on your Model rules
            if (ModelState.IsValid)
            {
                // 2. Hash the password (don't save plain text passwords!)
                user.Password = ComputeSha256Hash(user.Password);

                // 3. Add the user object to the Users table tracking
                _context.Users.Add(user);

                // 4. Push the changes to the actual Database
                _context.SaveChanges();

                // 5. Redirect back to the list
                return RedirectToAction("AllUsers");
            }

            // If we reach here, validation failed; reload the Roles for the dropdown
            ViewBag.Roles = new List<string> { "Admin", "User", "Manager" };
            return View(user);
        }
        public IActionResult AllUsers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            // Filter out User ID 1 if you don't want the admin to edit the system account
            var users = _context.Users.Where(u => u.Id != 1).ToList();
            return View(users);
        }

        // Helper: IsAdmin Check
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        // Helper: SHA256 hashing
        private string ComputeSha256Hash(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) rawData = "";
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(rawData);
                var hash = sha256.ComputeHash(bytes);
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }

        // Note: Rest of your Edit/Delete User/Movie methods stay the same
    }
}