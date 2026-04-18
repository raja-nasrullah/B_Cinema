using System.ComponentModel.DataAnnotations;

namespace BookingCinema.Models
{
    public class Hall
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Hall Name")]
        public string Name { get; set; } // e.g., "Hall A", "IMAX Screen"

        [Required]
        [Range(1, 50)]
        public int Capacity { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation Property: A hall can have many showtimes
        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
    }
}