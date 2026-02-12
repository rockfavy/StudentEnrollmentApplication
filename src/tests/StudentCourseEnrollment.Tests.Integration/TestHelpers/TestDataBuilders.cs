using StudentCourseEnrollment.Api.Models;

namespace StudentCourseEnrollment.Tests.Integration.TestHelpers;

public static class TestDataBuilders
{
    public static Student CreateStudent(
        Guid? id = null,
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        string? passwordHash = null)
    {
        return new Student
        {
            Id = id ?? Guid.NewGuid(),
            Email = email ?? $"test{Guid.NewGuid():N}@example.com",
            FirstName = firstName ?? "Test",
            LastName = lastName ?? "Student",
            PasswordHash = passwordHash ?? "hashed_password"
        };
    }

    public static Course CreateCourse(
        Guid? id = null,
        string? name = null,
        string? description = null,
        int? capacity = null,
        DateTime? createdAt = null)
    {
        return new Course
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? "Test Course",
            Description = description ?? "Test Description",
            Capacity = capacity ?? 50,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    public static Enrollment CreateEnrollment(
        Guid? id = null,
        Guid? studentId = null,
        Guid? courseId = null,
        DateTime? enrolledAt = null)
    {
        return new Enrollment
        {
            Id = id ?? Guid.NewGuid(),
            StudentId = studentId ?? Guid.NewGuid(),
            CourseId = courseId ?? Guid.NewGuid(),
            EnrolledAt = enrolledAt ?? DateTime.UtcNow
        };
    }
}
