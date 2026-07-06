using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BookingCinema.Data;
using BookingCinema.Models;

namespace BookingCinema.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Booking
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Hall)
                .Include(b => b.Tickets)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // GET: Create
        public IActionResult Create(int? movieId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue)
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);
                ViewBag.UserName = user?.Name;
            }

            PopulateDropDowns(movieId);
            return View();
        }

        // POST: Create (FIXED)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShowtimeId,SelectedSeats")] Booking booking)
        {
            // ✅ 1. SESSION USER (SAFE INT)
            var userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            booking.UserId = userId.Value;

            // ✅ 2. Validate seats
            if (string.IsNullOrEmpty(booking.SelectedSeats))
            {
                ModelState.AddModelError("", "Please select at least one seat.");
                PopulateDropDowns();
                return View(booking);
            }

            var selectedSeats = booking.SelectedSeats.Split(',');
            booking.Quantity = selectedSeats.Length;

            // ✅ 3. Get showtime
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == booking.ShowtimeId);

            if (showtime == null)
            {
                ModelState.AddModelError("", "Selected showtime no longer exists.");
                PopulateDropDowns();
                return View(booking);
            }

            if (!ModelState.IsValid)
            {
                PopulateDropDowns(showtime.MovieId);
                return View(booking);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ✅ 4. Already booked seats
                var alreadyBookedSeats = await _context.Tickets
                    .Where(t => t.ShowtimeId == booking.ShowtimeId)
                    .Select(t => t.SeatNumber)
                    .ToListAsync();

                var conflictSeats = selectedSeats.Intersect(alreadyBookedSeats).ToList();

                if (conflictSeats.Any())
                {
                    ModelState.AddModelError("", "Some seats already booked: " + string.Join(", ", conflictSeats));
                    PopulateDropDowns(showtime.MovieId);
                    return View(booking);
                }

                // ✅ 5. Booking details
                booking.BookingDate = DateTime.Now;
                booking.TotalAmount = booking.Quantity * (showtime.Movie?.Price ?? 0);
                booking.Status = "Confirmed";

                // ✅ 6. Save booking
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // ✅ 7. Create tickets
                foreach (var seat in selectedSeats)
                {
                    _context.Tickets.Add(new Ticket
                    {
                        UserId = booking.UserId,
                        MovieId = showtime.MovieId,
                        ShowtimeId = booking.ShowtimeId,
                        SeatNumber = seat,
                        IssuedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ 8. Redirect
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Something went wrong while processing booking.");
                PopulateDropDowns(showtime.MovieId);
                return View(booking);
            }
        }

        // Get booked seats
        public async Task<JsonResult> GetBookedSeats(int showtimeId)
        {
            var bookedSeats = await _context.Tickets
                .Where(t => t.ShowtimeId == showtimeId)
                .Select(t => t.SeatNumber)
                .ToListAsync();

            return Json(bookedSeats);
        }

        // Cancel booking
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
                return NotFound();

            booking.Status = "Cancelled";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Dropdown helper
        private void PopulateDropDowns(int? movieId = null)
        {
            var query = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .AsQueryable();

            if (movieId.HasValue)
            {
                query = query.Where(s => s.MovieId == movieId);
            }

            var showtimeList = query.Select(s => new
            {
                Id = s.Id,
                Display = $"{s.Movie.Title} | {s.Hall.Name} | {s.MovieDate.ToShortDateString()} @ {s.MovieTime}"
            }).ToList();

            ViewData["ShowtimeId"] = new SelectList(showtimeList, "Id", "Display");
        }
       
    }

}