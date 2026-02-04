using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCinema.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        // Remove MovieId and Movie property! 
        // We get the movie via the Showtime.

        [Required]
        public int ShowtimeId { get; set; }

        [ForeignKey("ShowtimeId")]
        public Showtime? Showtime { get; set; }

        public DateTime BookingDate { get; set; }
    }
}