using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Api.Models;
using StudentCourseEnrollment.Shared;

namespace StudentCourseEnrollment.Api.Features.Auth;

public class UserProvisioningService
{
    private readonly EnrollmentContext _context;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(EnrollmentContext context, ILogger<UserProvisioningService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Student?> ProvisionUserFromClaimsAsync(ClaimsPrincipal user)
    {
        try
        {
            var emailClaim = user.FindFirst(ClaimTypes.Email) ?? user.FindFirst("email") ?? user.FindFirst("preferred_username");
            if (emailClaim == null || string.IsNullOrWhiteSpace(emailClaim.Value))
            {
                _logger.LogWarning("Cannot provision user: No email claim found");
                return null;
            }

            var email = emailClaim.Value;
            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email);

            if (existingStudent != null)
            {
                return existingStudent;
            }

            var firstName = user.FindFirst(ClaimTypes.GivenName)?.Value 
                ?? user.FindFirst("given_name")?.Value 
                ?? user.FindFirst("FirstName")?.Value
                ?? ExtractFirstNameFromName(user);
            
            var lastName = user.FindFirst(ClaimTypes.Surname)?.Value 
                ?? user.FindFirst("family_name")?.Value 
                ?? user.FindFirst("LastName")?.Value
                ?? ExtractLastNameFromName(user);

            if (string.IsNullOrWhiteSpace(firstName))
            {
                firstName = email.Split('@')[0];
            }
            if (string.IsNullOrWhiteSpace(lastName))
            {
                lastName = "User";
            }

            var role = Role.Student.ToString();
            var roleClaim = user.FindFirst(ClaimTypes.Role) ?? user.FindFirst("roles");
            if (roleClaim != null && !string.IsNullOrWhiteSpace(roleClaim.Value))
            {
                var roles = roleClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (roles.Contains(Role.Admin.ToString(), StringComparer.OrdinalIgnoreCase))
                {
                    role = Role.Admin.ToString();
                }
            }

            var student = new Student
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = string.Empty,
                Role = role
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Auto-provisioned student {Email} from Entra ID", email);

            return student;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning user from Entra ID claims");
            return null;
        }
    }

    private string? ExtractFirstNameFromName(ClaimsPrincipal user)
    {
        var nameClaim = user.FindFirst(ClaimTypes.Name) ?? user.FindFirst("name");
        if (nameClaim != null && !string.IsNullOrWhiteSpace(nameClaim.Value))
        {
            var parts = nameClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : null;
        }
        return null;
    }

    private string? ExtractLastNameFromName(ClaimsPrincipal user)
    {
        var nameClaim = user.FindFirst(ClaimTypes.Name) ?? user.FindFirst("name");
        if (nameClaim != null && !string.IsNullOrWhiteSpace(nameClaim.Value))
        {
            var parts = nameClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : null;
        }
        return null;
    }
}

