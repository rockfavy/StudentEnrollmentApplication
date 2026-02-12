namespace StudentCourseEnrollment.Shared.DTOs.Auth;

public record LoginResponse(
    string Token,
    Guid Id,
    string Email,
    string FirstName,
    string LastName
);
