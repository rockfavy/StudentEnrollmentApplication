namespace StudentCourseEnrollment.Shared.DTOs.Enrollments;

public record EnrollResponse(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    DateTime EnrolledAt
);

