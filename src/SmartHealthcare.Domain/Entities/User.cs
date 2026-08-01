using SmartHealthcare.Domain.Common;
using SmartHealthcare.Domain.Enums;

namespace SmartHealthcare.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    private User() { } // EF Core constructor

    public User(string firstName, string lastName, string email, string passwordHash, UserRole role)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        MarkUpdated();
    }

    public void SetActiveStatus(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
