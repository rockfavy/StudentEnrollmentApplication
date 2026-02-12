using System.ComponentModel.DataAnnotations;

namespace StudentCourseEnrollment.Shared.DTOs.Enrollments;

public record DeregisterRequest(
    [Required]
    Guid EnrollmentId
);

