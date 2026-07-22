using Domain.Enums;

namespace Domain.Entities;

public class Payment : BaseEntity<int>
{
    public int RequestId { get; private set; }
    public int CustomerId { get; private set; }
    public int DriverId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal CompanyCommission { get; private set; }
    public decimal DriverEarning { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? TransactionId { get; private set; }

    public Request Request { get; private set; } = null!;
    public Customer Customer { get; private set; } = null!;
    public Driver Driver { get; private set; } = null!;

    private Payment() { }

    public const decimal DefaultCommissionPercentage = 20.0m;

    public static Payment Create(int requestId, int customerId, int driverId, decimal amount, PaymentMethod method, decimal commissionPercentage = DefaultCommissionPercentage)
    {
        var commission = amount * (commissionPercentage / 100);
        var driverEarning = amount - commission;

        return new Payment
        {
            RequestId = requestId,
            CustomerId = customerId,
            DriverId = driverId,
            Amount = amount,
            CompanyCommission = commission,
            DriverEarning = driverEarning,
            Method = method,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkCompleted(string? transactionId = null)
    {
        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Refund()
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException("يمكن استرداد الدفعات المكتملة فقط");

        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }
}
