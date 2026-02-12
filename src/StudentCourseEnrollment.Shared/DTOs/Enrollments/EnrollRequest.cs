using System.ComponentModel.DataAnnotations;

namespace StudentCourseEnrollment.Shared.DTOs.Enrollments;

public record EnrollRequest(
    [Required]
    Guid CourseId
);

