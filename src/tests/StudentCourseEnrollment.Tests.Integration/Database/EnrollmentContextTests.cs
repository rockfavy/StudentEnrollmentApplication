using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Models;
using StudentCourseEnrollment.Tests.Integration.TestHelpers;
using Xunit;

namespace StudentCourseEnrollment.Tests.Integration.Database;

public class EnrollmentContextTests : TestBase
{
    [Fact]
    public async Task Student_With_Duplicate_Email_Should_Allow_Duplicate_In_InMemory()
    {
        var student1 = TestDataBuilders.CreateStudent(email: "test@example.com");
        var student2 = TestDataBuilders.CreateStudent(email: "test@example.com");

        await DbContext.Students.AddAsync(student1);
        await DbContext.SaveChangesAsync();

        await DbContext.Students.AddAsync(student2);
        await DbContext.SaveChangesAsync();

        var students = await DbContext.Students
            .Where(s => s.Email == "test@example.com")
            .ToListAsync();

        Assert.Equal(2, students.Count);
    }

    [Fact]
    public async Task Enrollment_With_Duplicate_StudentId_And_CourseId_Should_Allow_Duplicate_In_InMemory()
    {
        var student = TestDataBuilders.CreateStudent();
        var course = TestDataBuilders.CreateCourse();
        var enrollment1 = TestDataBuilders.CreateEnrollment(studentId: student.Id, courseId: course.Id);
        var enrollment2 = TestDataBuilders.CreateEnrollment(studentId: student.Id, courseId: course.Id);

        await SeedDataAsync(student, course, enrollment1);
        
        await DbContext.Enrollments.AddAsync(enrollment2);
        await DbContext.SaveChangesAsync();

        var enrollments = await DbContext.Enrollments
            .Where(e => e.StudentId == student.Id && e.CourseId == course.Id)
            .ToListAsync();

        Assert.Equal(2, enrollments.Count);
    }

    [Fact]
    public async Task Delete_Student_Should_Cascade_Delete_Enrollments()
    {
        var student = TestDataBuilders.CreateStudent();
        var course = TestDataBuilders.CreateCourse();
        var enrollment = TestDataBuilders.CreateEnrollment(studentId: student.Id, courseId: course.Id);

        await SeedDataAsync(student, course, enrollment);

        DbContext.Students.Remove(student);
        await DbContext.SaveChangesAsync();

        var remainingEnrollments = await DbContext.Enrollments
            .Where(e => e.StudentId == student.Id)
            .ToListAsync();

        Assert.Empty(remainingEnrollments);
    }

    [Fact]
    public async Task Delete_Course_Should_Cascade_Delete_Enrollments()
    {
        var student = TestDataBuilders.CreateStudent();
        var course = TestDataBuilders.CreateCourse();
        var enrollment = TestDataBuilders.CreateEnrollment(studentId: student.Id, courseId: course.Id);

        await SeedDataAsync(student, course, enrollment);

        DbContext.Courses.Remove(course);
        await DbContext.SaveChangesAsync();

        var remainingEnrollments = await DbContext.Enrollments
            .Where(e => e.CourseId == course.Id)
            .ToListAsync();

        Assert.Empty(remainingEnrollments);
    }

    [Fact]
    public async Task Student_With_Empty_Email_Should_Allow_Empty_String_In_InMemory()
    {
        var student = new Student
        {
            Id = Guid.NewGuid(),
            Email = string.Empty,
            FirstName = "Test",
            LastName = "Student"
        };

        await DbContext.Students.AddAsync(student);
        await DbContext.SaveChangesAsync();

        var savedStudent = await DbContext.Students.FindAsync(student.Id);
        Assert.NotNull(savedStudent);
        Assert.Equal(string.Empty, savedStudent.Email);
    }

    [Fact]
    public async Task Course_With_Empty_Name_Should_Allow_Empty_String_In_InMemory()
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            Capacity = 50
        };

        await DbContext.Courses.AddAsync(course);
        await DbContext.SaveChangesAsync();

        var savedCourse = await DbContext.Courses.FindAsync(course.Id);
        Assert.NotNull(savedCourse);
        Assert.Equal(string.Empty, savedCourse.Name);
    }
}

