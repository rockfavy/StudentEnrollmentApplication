namespace StudentCourseEnrollment.Shared.DTOs.Courses;

public record CreateCourseResponse(
    Guid Id,
    string Name,
    string Description,
    int Capacity
);

