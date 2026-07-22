using Domain.Enums;

namespace Domain.Entities;

public class User : BaseEntity<int>
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Customer;
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }

    public Customer? Customer { get; private set; }
    public Driver? Driver { get; private set; }

    private User() { }

    public static User Create(string fullName, string email, string phone, string passwordHash, UserRole role)
    {
        return new User
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string fullName, string phone)
    {
        FullName = fullName;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin() => LastLoginAt = DateTime.UtcNow;
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    public bool IsCustomer => Role == UserRole.Customer;
    public bool IsDriver => Role == UserRole.Driver;
    public bool IsAdmin => Role == UserRole.Admin;
}
