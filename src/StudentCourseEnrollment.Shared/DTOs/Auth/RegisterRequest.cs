using System.ComponentModel.DataAnnotations;

namespace StudentCourseEnrollment.Shared.DTOs.Auth;

public record RegisterRequest(
    [Required]
    [EmailAddress]
    string Email,
    
    [Required]
    [MinLength(1)]
    string FirstName,
    
    [Required]
    [MinLength(1)]
    string LastName,
    
    [Required]
    [MinLength(6)]
    string Password
);
