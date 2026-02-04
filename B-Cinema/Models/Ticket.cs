using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCinema.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public int MovieId { get; set; }

        [ForeignKey("MovieId")]
        public Movie? Movie { get; set; }

        [Required]
        public int ShowtimeId { get; set; }

        [ForeignKey("ShowtimeId")]
        public Showtime? Showtime { get; set; }

        // Additional ticket-specific info
        public string TicketNumber { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        public DateTime IssuedAt { get; set; } = DateTime.Now;
    }
}