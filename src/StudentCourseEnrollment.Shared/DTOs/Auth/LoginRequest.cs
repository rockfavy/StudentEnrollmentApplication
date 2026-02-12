using System.ComponentModel.DataAnnotations;

namespace StudentCourseEnrollment.Shared.DTOs.Auth;

public record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,
    
    [Required]
    string Password
);

