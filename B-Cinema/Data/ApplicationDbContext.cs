using BookingCinema.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingCinema.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Showtime> Showtimes { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- MOVIE CONFIGURATION ---
            modelBuilder.Entity<Movie>()
                .Property(e => e.Price)
                .HasColumnType("decimal(18,2)");

            // --- SHOWTIME RELATIONSHIPS ---
            // A Movie can have many Showtimes
            modelBuilder.Entity<Showtime>()
                .HasOne(s => s.Movie)
                .WithMany(m => m.Showtimes)
                .HasForeignKey(s => s.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- BOOKING RELATIONSHIPS ---
            // A Showtime can have many Bookings
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Showtime)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ShowtimeId)
                .OnDelete(DeleteBehavior.Cascade);

            // A User can have many Bookings
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            // Note: Using NoAction to prevent cycle errors in SQL Server

            // --- TICKET RELATIONSHIPS ---
            // Linking Ticket to User
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Linking Ticket to Movie
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Movie)
                .WithMany()
                .HasForeignKey(t => t.MovieId)
                .OnDelete(DeleteBehavior.NoAction);

            // Linking Ticket to Showtime
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Showtime)
                .WithMany()
                .HasForeignKey(t => t.ShowtimeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}