namespace StudentCourseEnrollment.Shared.DTOs.Auth;

public record RegisterResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName
);
