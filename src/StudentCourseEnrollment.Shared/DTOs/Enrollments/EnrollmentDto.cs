namespace StudentCourseEnrollment.Shared.DTOs.Enrollments;

public record EnrollmentDto(
    Guid Id,
    Guid CourseId,
    string CourseName,
    string CourseDescription,
    DateTime EnrolledAt
);

