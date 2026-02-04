using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCinema.Models
{
    public class Showtime
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MovieId { get; set; }

        [ForeignKey("MovieId")]
        public Movie? Movie { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime MovieDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan MovieTime { get; set; }

        // Navigation property for Bookings
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}