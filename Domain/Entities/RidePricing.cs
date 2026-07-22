namespace Domain.Entities;

public class RidePricing : BaseEntity<int>
{
    public string Name { get; private set; } = string.Empty;
    public decimal BaseFare { get; private set; }
    public decimal PricePerKm { get; private set; }
    public decimal PricePerMinute { get; private set; }
    public decimal MinimumFare { get; private set; }
    public bool IsActive { get; private set; }

    private RidePricing() { }

    public static RidePricing Create(string name, decimal baseFare, decimal pricePerKm, decimal pricePerMinute, decimal minimumFare = 10.0m)
    {
        if (baseFare < 0 || pricePerKm < 0 || pricePerMinute < 0)
            throw new ArgumentException("الأسعار يجب أن تكون أكبر من أو تساوي صفر");

        return new RidePricing
        {
            Name = name,
            BaseFare = baseFare,
            PricePerKm = pricePerKm,
            PricePerMinute = pricePerMinute,
            MinimumFare = minimumFare,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, decimal baseFare, decimal pricePerKm, decimal pricePerMinute, decimal minimumFare)
    {
        Name = name;
        BaseFare = baseFare;
        PricePerKm = pricePerKm;
        PricePerMinute = pricePerMinute;
        MinimumFare = minimumFare;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    public decimal CalculateCost(double distanceKm, int durationMinutes)
    {
        var cost = BaseFare + ((decimal)distanceKm * PricePerKm) + (durationMinutes * PricePerMinute);
        return Math.Max(cost, MinimumFare);
    }

    public RideCostBreakdown GetCostBreakdown(double distanceKm, int durationMinutes)
    {
        var distanceCost = (decimal)distanceKm * PricePerKm;
        var timeCost = durationMinutes * PricePerMinute;
        var subtotal = BaseFare + distanceCost + timeCost;
        var total = Math.Max(subtotal, MinimumFare);

        return new RideCostBreakdown
        {
            BaseFare = BaseFare,
            DistanceKm = distanceKm,
            PricePerKm = PricePerKm,
            DistanceCost = distanceCost,
            DurationMinutes = durationMinutes,
            PricePerMinute = PricePerMinute,
            TimeCost = timeCost,
            Subtotal = subtotal,
            MinimumFare = MinimumFare,
            Total = total
        };
    }
}

public class RideCostBreakdown
{
    public decimal BaseFare { get; set; }
    public double DistanceKm { get; set; }
    public decimal PricePerKm { get; set; }
    public decimal DistanceCost { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PricePerMinute { get; set; }
    public decimal TimeCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal MinimumFare { get; set; }
    public decimal Total { get; set; }
}
