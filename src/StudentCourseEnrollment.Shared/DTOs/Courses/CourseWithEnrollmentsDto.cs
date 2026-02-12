using StudentCourseEnrollment.Shared.DTOs.Enrollments;

namespace StudentCourseEnrollment.Shared.DTOs.Courses;

public record CourseWithEnrollmentsDto(
    Guid Id,
    string Name,
    string Description,
    int Capacity,
    int CurrentEnrollments,
    DateTime CreatedAt,
    List<StudentEnrollmentDto> Enrollments
);


