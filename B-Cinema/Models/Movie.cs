using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using BookingCinema.Models;

namespace BookingCinema.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public int Duration { get; set; } // minutes

        public decimal Price { get; set; }

        public string? ImagePath { get; set; }

        // Navigation Property
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    }
}
