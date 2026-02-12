namespace StudentCourseEnrollment.Shared.DTOs.Auth;

public record ProvisionUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role
);

