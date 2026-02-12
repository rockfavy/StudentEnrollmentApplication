using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Tests.Integration.TestHelpers;
using Xunit;

namespace StudentCourseEnrollment.Tests.Integration.Database;

public class DatabaseSeedingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DatabaseSeedingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InitializeDbAsync_Should_Seed_Students()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();

        var students = await context.Students.ToListAsync();
        
        Assert.NotEmpty(students);
        Assert.True(students.Count >= 4);
    }

    [Fact]
    public async Task InitializeDbAsync_Should_Seed_Courses()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();

        var courses = await context.Courses.ToListAsync();
        
        Assert.NotEmpty(courses);
        Assert.True(courses.Count >= 5);
    }

    [Fact]
    public async Task InitializeDbAsync_Should_Seed_Enrollments()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();

        var enrollments = await context.Enrollments.ToListAsync();
        
        Assert.NotEmpty(enrollments);
        Assert.True(enrollments.Count >= 5);
    }
}
