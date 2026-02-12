using Microsoft.Extensions.Logging;
using Moq;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Api.Features.Enrollments;
using StudentCourseEnrollment.Api.Models;
using StudentCourseEnrollment.Tests.Unit.TestHelpers;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Features.Enrollments;

public class EnrollmentServiceTests : IDisposable
{
    private readonly EnrollmentContext _dbContext;
    private readonly Mock<ILogger<EnrollmentService>> _loggerMock;
    private readonly EnrollmentService _service;

    public EnrollmentServiceTests()
    {
        _dbContext = TestDbContextFactory.CreateInMemoryContext();
        _loggerMock = new Mock<ILogger<EnrollmentService>>();
        _service = new EnrollmentService(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task GetStudentEnrollmentsAsync_With_No_Enrollments_Should_Return_Empty_List()
    {
        var studentId = Guid.NewGuid();

        var enrollments = await _service.GetStudentEnrollmentsAsync(studentId);

        Assert.NotNull(enrollments);
        Assert.Empty(enrollments);
    }

    [Fact]
    public async Task GetStudentEnrollmentsAsync_With_Enrollments_Should_Return_All_Enrollments()
    {
        var studentId = Guid.NewGuid();
        var course1 = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Course 1",
            Description = "Description 1",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        var course2 = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Course 2",
            Description = "Description 2",
            Capacity = 25,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.AddRange(course1, course2);

        var enrollment1 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = course1.Id,
            EnrolledAt = DateTime.UtcNow.AddDays(-10)
        };
        var enrollment2 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = course2.Id,
            EnrolledAt = DateTime.UtcNow.AddDays(-5)
        };
        _dbContext.Enrollments.AddRange(enrollment1, enrollment2);
        await _dbContext.SaveChangesAsync();

        var enrollments = await _service.GetStudentEnrollmentsAsync(studentId);

        Assert.NotNull(enrollments);
        Assert.Equal(2, enrollments.Count);
        Assert.All(enrollments, e => Assert.Equal(studentId, e.StudentId));
    }

    [Fact]
    public async Task GetStudentEnrollmentsAsync_Should_Include_Course_Details()
    {
        var studentId = Guid.NewGuid();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Test Course",
            Description = "Test Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };
        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();

        var enrollments = await _service.GetStudentEnrollmentsAsync(studentId);

        Assert.NotNull(enrollments);
        Assert.Single(enrollments);
        Assert.NotNull(enrollments[0].Course);
        Assert.Equal(course.Id, enrollments[0].Course.Id);
        Assert.Equal(course.Name, enrollments[0].Course.Name);
        Assert.Equal(course.Description, enrollments[0].Course.Description);
    }

    [Fact]
    public async Task GetStudentEnrollmentsAsync_Should_Only_Return_Enrollments_For_Specified_Student()
    {
        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Test Course",
            Description = "Test Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.Add(course);

        var enrollment1 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student1Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };
        var enrollment2 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student2Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };
        _dbContext.Enrollments.AddRange(enrollment1, enrollment2);
        await _dbContext.SaveChangesAsync();

        var enrollments = await _service.GetStudentEnrollmentsAsync(student1Id);

        Assert.NotNull(enrollments);
        Assert.Single(enrollments);
        Assert.Equal(student1Id, enrollments[0].StudentId);
    }

    [Fact]
    public async Task GetStudentEnrollmentsAsync_Should_Order_By_EnrolledAt_Descending()
    {
        var studentId = Guid.NewGuid();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Test Course",
            Description = "Test Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.Add(course);

        var enrollment1 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow.AddDays(-10)
        };
        var enrollment2 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow.AddDays(-5)
        };
        _dbContext.Enrollments.AddRange(enrollment1, enrollment2);
        await _dbContext.SaveChangesAsync();

        var enrollments = await _service.GetStudentEnrollmentsAsync(studentId);

        Assert.NotNull(enrollments);
        Assert.Equal(2, enrollments.Count);
        Assert.True(enrollments[0].EnrolledAt > enrollments[1].EnrolledAt);
    }

    [Fact]
    public async Task CheckDuplicateEnrollmentAsync_With_No_Existing_Enrollment_Should_Return_False()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var isDuplicate = await _service.CheckDuplicateEnrollmentAsync(studentId, courseId);

        Assert.False(isDuplicate);
    }

    [Fact]
    public async Task CheckDuplicateEnrollmentAsync_With_Existing_Enrollment_Should_Return_True()
    {
        var studentId = Guid.NewGuid();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Test Course",
            Description = "Test Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };
        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();

        var isDuplicate = await _service.CheckDuplicateEnrollmentAsync(studentId, course.Id);

        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task CheckDuplicateEnrollmentAsync_With_Different_Student_Should_Return_False()
    {
        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Test Course",
            Description = "Test Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student1Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };
        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();

        var isDuplicate = await _service.CheckDuplicateEnrollmentAsync(student2Id, course.Id);

        Assert.False(isDuplicate);
    }

    [Fact]
    public async Task CheckDuplicateEnrollmentAsync_With_Different_Course_Should_Return_False()
    {
        var studentId = Guid.NewGuid();
        var course1 = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Course 1",
            Description = "Description 1",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        var course2 = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Course 2",
            Description = "Description 2",
            Capacity = 25,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.AddRange(course1, course2);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = course1.Id,
            EnrolledAt = DateTime.UtcNow
        };
        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();

        var isDuplicate = await _service.CheckDuplicateEnrollmentAsync(studentId, course2.Id);

        Assert.False(isDuplicate);
    }

    [Fact]
    public async Task EnrollAsync_Should_Create_Enrollment_With_Correct_Properties()
    {
        var studentId = Guid.NewGuid();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Test Course",
            Description = "Test Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        var enrollment = await _service.EnrollAsync(studentId, course.Id);

        Assert.NotNull(enrollment);
        Assert.NotEqual(Guid.Empty, enrollment.Id);
        Assert.Equal(studentId, enrollment.StudentId);
        Assert.Equal(course.Id, enrollment.CourseId);
        Assert.True(enrollment.EnrolledAt <= DateTime.UtcNow);
        Assert.True(enrollment.EnrolledAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task EnrollAsync_Should_Persist_Enrollment_To_Database()
    {
        var studentId = Guid.NewGuid();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Test Course",
            Description = "Test Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        var enrollment = await _service.EnrollAsync(studentId, course.Id);

        var savedEnrollment = await _dbContext.Enrollments.FindAsync(enrollment.Id);
        Assert.NotNull(savedEnrollment);
        Assert.Equal(enrollment.Id, savedEnrollment.Id);
        Assert.Equal(studentId, savedEnrollment.StudentId);
        Assert.Equal(course.Id, savedEnrollment.CourseId);
    }

    [Fact]
    public async Task EnrollAsync_Should_Log_Enrollment_Information()
    {
        var studentId = Guid.NewGuid();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Test Course",
            Description = "Test Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        await _service.EnrollAsync(studentId, course.Id);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Student") && v.ToString()!.Contains("enrolled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
