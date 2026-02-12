namespace StudentCourseEnrollment.Shared.DTOs.Enrollments;

public record StudentEnrollmentDto(
    Guid EnrollmentId,
    Guid StudentId,
    string StudentFirstName,
    string StudentLastName,
    string StudentEmail,
    DateTime EnrolledAt
);


