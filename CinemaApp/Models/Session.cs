namespace CinemaApp.Models;

public class Session
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
    public int HallId { get; set; }
    public Hall Hall { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Format { get; set; } = "2D";
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
