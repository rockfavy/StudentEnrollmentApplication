using Moq;
using StudentCourseEnrollment.Api.Features.Courses;
using StudentCourseEnrollment.Api.Features.Courses.Interfaces;
using StudentCourseEnrollment.Api.Models;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Features.Courses;

public class CourseServiceTests : IDisposable
{
    private readonly Mock<ICourseRepository> _repositoryMock;
    private readonly CourseService _service;

    public CourseServiceTests()
    {
        _repositoryMock = new Mock<ICourseRepository>();
        _service = new CourseService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetCoursesAsync_Should_Return_All_Courses_From_Repository()
    {
        var expectedCourses = new List<Course>
        {
            new Course
            {
                Id = Guid.NewGuid(),
                Name = "Course 1",
                Description = "Description 1",
                Capacity = 30,
                CreatedAt = DateTime.UtcNow
            },
            new Course
            {
                Id = Guid.NewGuid(),
                Name = "Course 2",
                Description = "Description 2",
                Capacity = 25,
                CreatedAt = DateTime.UtcNow
            }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(expectedCourses);

        var result = await _service.GetCoursesAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(expectedCourses[0].Id, result[0].Id);
        Assert.Equal(expectedCourses[1].Id, result[1].Id);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCourseAsync_With_Valid_Id_Should_Return_Course()
    {
        var courseId = Guid.NewGuid();
        var expectedCourse = new Course
        {
            Id = courseId,
            Name = "Test Course",
            Description = "Test Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync(expectedCourse);

        var result = await _service.GetCourseAsync(courseId);

        Assert.NotNull(result);
        Assert.Equal(courseId, result.Id);
        Assert.Equal("Test Course", result.Name);
        _repositoryMock.Verify(r => r.GetByIdAsync(courseId), Times.Once);
    }

    [Fact]
    public async Task GetCourseAsync_With_Invalid_Id_Should_Return_Null()
    {
        var courseId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync((Course?)null);

        var result = await _service.GetCourseAsync(courseId);

        Assert.Null(result);
        _repositoryMock.Verify(r => r.GetByIdAsync(courseId), Times.Once);
    }

    [Fact]
    public async Task CreateCourseAsync_Should_Assign_Id_And_CreatedAt_And_Return_Course()
    {
        var course = new Course
        {
            Name = "New Course",
            Description = "New Description",
            Capacity = 40
        };

        var createdCourse = new Course
        {
            Id = Guid.NewGuid(),
            Name = course.Name,
            Description = course.Description,
            Capacity = course.Capacity,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Course>()))
            .ReturnsAsync((Course c) => createdCourse);

        var result = await _service.CreateCourseAsync(course);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.Equal("New Course", result.Name);
        _repositoryMock.Verify(r => r.CreateAsync(It.Is<Course>(c => 
            c.Name == course.Name && 
            c.Description == course.Description && 
            c.Capacity == course.Capacity &&
            c.Id != Guid.Empty &&
            c.CreatedAt != default)), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseAsync_With_Valid_Id_Should_Update_And_Return_Course()
    {
        var courseId = Guid.NewGuid();
        var existingCourse = new Course
        {
            Id = courseId,
            Name = "Original Name",
            Description = "Original Description",
            Capacity = 30,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var updatedCourse = new Course
        {
            Id = courseId,
            Name = "Updated Name",
            Description = "Updated Description",
            Capacity = 35,
            CreatedAt = existingCourse.CreatedAt
        };

        var courseToUpdate = new Course
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Capacity = 35
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync(existingCourse);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Course>()))
            .ReturnsAsync((Course c) => updatedCourse);

        var result = await _service.UpdateCourseAsync(courseId, courseToUpdate);

        Assert.NotNull(result);
        Assert.Equal(courseId, result.Id);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Description", result.Description);
        Assert.Equal(35, result.Capacity);
        _repositoryMock.Verify(r => r.GetByIdAsync(courseId), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Course>(c => 
            c.Id == courseId &&
            c.Name == "Updated Name" &&
            c.Description == "Updated Description" &&
            c.Capacity == 35)), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseAsync_With_Invalid_Id_Should_Return_Null()
    {
        var courseId = Guid.NewGuid();
        var courseToUpdate = new Course
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Capacity = 35
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync((Course?)null);

        var result = await _service.UpdateCourseAsync(courseId, courseToUpdate);

        Assert.Null(result);
        _repositoryMock.Verify(r => r.GetByIdAsync(courseId), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCourseAsync_With_Valid_Id_Should_Return_True()
    {
        var courseId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.DeleteAsync(courseId))
            .ReturnsAsync(true);

        var result = await _service.DeleteCourseAsync(courseId);

        Assert.True(result);
        _repositoryMock.Verify(r => r.DeleteAsync(courseId), Times.Once);
    }

    [Fact]
    public async Task DeleteCourseAsync_With_Invalid_Id_Should_Return_False()
    {
        var courseId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.DeleteAsync(courseId))
            .ReturnsAsync(false);

        var result = await _service.DeleteCourseAsync(courseId);

        Assert.False(result);
        _repositoryMock.Verify(r => r.DeleteAsync(courseId), Times.Once);
    }

    [Fact]
    public async Task CourseExistsAsync_With_Existing_Id_Should_Return_True()
    {
        var courseId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ExistsAsync(courseId))
            .ReturnsAsync(true);

        var result = await _service.CourseExistsAsync(courseId);

        Assert.True(result);
        _repositoryMock.Verify(r => r.ExistsAsync(courseId), Times.Once);
    }

    [Fact]
    public async Task CourseExistsAsync_With_NonExistent_Id_Should_Return_False()
    {
        var courseId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ExistsAsync(courseId))
            .ReturnsAsync(false);

        var result = await _service.CourseExistsAsync(courseId);

        Assert.False(result);
        _repositoryMock.Verify(r => r.ExistsAsync(courseId), Times.Once);
    }

    public void Dispose()
    {
        _repositoryMock.Reset();
    }
}


