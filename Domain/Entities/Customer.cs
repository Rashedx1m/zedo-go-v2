namespace Domain.Entities;

public class Customer : BaseEntity<int>
{
    public int UserId { get; private set; }
    public decimal Rating { get; private set; } = 5.0m;
    public int TotalRides { get; private set; } = 0;

    public User User { get; private set; } = null!;
    public ICollection<Request> Requests { get; private set; } = new List<Request>();

    private Customer() { }

    public static Customer Create(int userId)
    {
        return new Customer
        {
            UserId = userId,
            Rating = 5.0m,
            TotalRides = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateRating(decimal newRating)
    {
        Rating = ((Rating * TotalRides) + newRating) / (TotalRides + 1);
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementRides()
    {
        TotalRides++;
        UpdatedAt = DateTime.UtcNow;
    }
}
