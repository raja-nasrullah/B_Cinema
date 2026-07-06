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

        [Required]
        public int ShowtimeId { get; set; }

        [ForeignKey("ShowtimeId")]
        public Showtime? Showtime { get; set; }

        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string SelectedSeats { get; set; } // e.g. "A1,A2,A3"
        public DateTime BookingDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Confirmed"; // Confirmed, Pending, Cancelled

        // Navigation Property: One booking can result in multiple individual tickets
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}