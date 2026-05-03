namespace CinemaApp.Models;

public enum TicketStatus
{
    Active,
    Used,
    Cancelled
}

public class Ticket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public string SeatRow { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public SeatType SeatType { get; set; } = SeatType.Standard;
    public decimal Price { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Active;
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public string SeatDisplay => $"{SeatRow}{SeatNumber}";
    public string MovieTitle => Session?.Movie?.Title ?? "";
    public DateTime SessionTime => Session?.StartTime ?? DateTime.MinValue;
    public string HallName => Session?.Hall?.Name ?? "";
}
