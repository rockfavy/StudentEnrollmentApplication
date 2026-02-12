namespace StudentCourseEnrollment.Shared.DTOs.Courses;

public record CourseDto(
    Guid Id,
    string Name,
    string Description,
    int Capacity,
    int CurrentEnrollments,
    DateTime CreatedAt
);
