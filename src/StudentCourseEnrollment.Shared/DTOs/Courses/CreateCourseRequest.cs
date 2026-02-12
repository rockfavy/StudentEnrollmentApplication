using System.ComponentModel.DataAnnotations;

namespace StudentCourseEnrollment.Shared.DTOs.Courses;

public record CreateCourseRequest(
    [Required]
    [MinLength(2)]
    string Name,
    
    [Required]
    string Description,
    
    [Required]
    [Range(1, 1000)]
    int Capacity
);

