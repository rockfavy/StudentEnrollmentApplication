using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Api.Features.Auth;
using StudentCourseEnrollment.Api.Models;
using StudentCourseEnrollment.Api.Shared;
using StudentCourseEnrollment.Shared;
using StudentCourseEnrollment.Shared.DTOs;
using StudentCourseEnrollment.Shared.DTOs.Courses;
using StudentCourseEnrollment.Tests.Integration.TestHelpers;
using Xunit;

namespace StudentCourseEnrollment.Tests.Integration.Endpoints;

public class CoursesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JwtTokenService _tokenService;

    public CoursesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        var options = Options.Create(new JwtOptions
        {
            SecretKey = "TestSecretKeyForIntegrationTests-Minimum32Characters",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });
        _tokenService = new JwtTokenService(options);
    }

    private HttpClient CreateAuthenticatedAdminClient()
    {
        var client = _factory.CreateClient();
        var adminId = Guid.NewGuid();
        var token = _tokenService.GenerateToken(adminId, "admin@test.com", "Admin", "User", new[] { Role.Admin.ToString() });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Course> CreateAndSaveCourse(EnrollmentContext context, string name, string description, int capacity)
    {
        var course = TestDataBuilders.CreateCourse(
            id: Guid.NewGuid(),
            name: name,
            description: description,
            capacity: capacity);
        context.Courses.Add(course);
        await context.SaveChangesAsync();
        return course;
    }

    private void AssertCourseProperties(CourseDto course, string expectedName, string expectedDescription, int expectedCapacity)
    {
        Assert.Equal(expectedName, course.Name);
        Assert.Equal(expectedDescription, course.Description);
        Assert.Equal(expectedCapacity, course.Capacity);
    }

    private void AssertCourseEntityProperties(Course course, string expectedName, string expectedDescription, int expectedCapacity)
    {
        Assert.Equal(expectedName, course.Name);
        Assert.Equal(expectedDescription, course.Description);
        Assert.Equal(expectedCapacity, course.Capacity);
    }

    [Fact]
    public async Task GetCourses_Should_Return_List_Of_Courses()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        
        if (!await context.Courses.AnyAsync())
        {
            var course1 = TestDataBuilders.CreateCourse(
                name: "Test Course 1",
                description: "Description 1",
                capacity: 30
            );
            var course2 = TestDataBuilders.CreateCourse(
                name: "Test Course 2",
                description: "Description 2",
                capacity: 25
            );
            context.Courses.AddRange(course1, course2);
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/courses?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ItemsResult<CourseDto>>();
        Assert.NotNull(result);
        Assert.True(result.Items.Any());
    }

    [Fact]
    public async Task GetCourses_Should_Return_Empty_List_When_No_Courses_Exist()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        
        context.Courses.RemoveRange(context.Courses);
        await context.SaveChangesAsync();

        var response = await client.GetAsync("/api/courses?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ItemsResult<CourseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetCourses_Should_Return_Courses_With_Correct_Properties()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        
        context.Courses.RemoveRange(context.Courses);
        await context.SaveChangesAsync();
        
        var testCourse = await CreateAndSaveCourse(context, "Advanced Programming", "Advanced programming concepts", 40);
        testCourse.CreatedAt = DateTime.UtcNow.AddDays(-10);
        context.Courses.Update(testCourse);
        await context.SaveChangesAsync();

        var response = await client.GetAsync("/api/courses?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ItemsResult<CourseDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        
        var course = result.Items.First();
        Assert.Equal(testCourse.Id, course.Id);
        AssertCourseProperties(course, testCourse.Name, testCourse.Description, testCourse.Capacity);
        Assert.Equal(testCourse.CreatedAt, course.CreatedAt);
    }

    [Fact]
    public async Task GetCourse_With_Valid_Id_Should_Return_Course()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        
        var testCourse = await CreateAndSaveCourse(context, "Web Development", "Build modern web applications", 35);
        testCourse.CreatedAt = DateTime.UtcNow.AddMonths(-2);
        await context.SaveChangesAsync();

        var response = await client.GetAsync($"/api/courses/{testCourse.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var course = await response.Content.ReadFromJsonAsync<CourseDto>();
        Assert.NotNull(course);
        Assert.Equal(testCourse.Id, course.Id);
        AssertCourseProperties(course, testCourse.Name, testCourse.Description, testCourse.Capacity);
        Assert.Equal(testCourse.CreatedAt, course.CreatedAt);
    }

    [Fact]
    public async Task GetCourse_With_Invalid_Id_Should_Return_NotFound()
    {
        var client = _factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/courses/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCourse_With_Empty_Guid_Should_Return_NotFound()
    {
        var client = _factory.CreateClient();
        var emptyGuid = Guid.Empty;

        var response = await client.GetAsync($"/api/courses/{emptyGuid}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_With_Valid_Request_Should_Return_Created()
    {
        var client = CreateAuthenticatedAdminClient();
        var request = new CreateCourseRequest(
            Name: "New Test Course",
            Description: "This is a new test course",
            Capacity: 50
        );

        var response = await client.PostAsJsonAsync("/api/courses", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var course = await response.Content.ReadFromJsonAsync<CourseDto>();
        Assert.NotNull(course);
        Assert.NotEqual(Guid.Empty, course.Id);
        AssertCourseProperties(course, request.Name, request.Description, request.Capacity);
        Assert.True(course.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateCourse_Should_Persist_Course_To_Database()
    {
        var client = CreateAuthenticatedAdminClient();
        var request = new CreateCourseRequest(
            Name: "Persistent Course",
            Description: "This course should be saved",
            Capacity: 40
        );

        var response = await client.PostAsJsonAsync("/api/courses", request);
        response.EnsureSuccessStatusCode();
        var createdCourse = await response.Content.ReadFromJsonAsync<CourseDto>();
        Assert.NotNull(createdCourse);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var courseInDb = await context.Courses.FirstOrDefaultAsync(c => c.Id == createdCourse.Id);

        Assert.NotNull(courseInDb);
        AssertCourseEntityProperties(courseInDb, request.Name, request.Description, request.Capacity);
    }

    [Fact]
    public async Task UpdateCourse_With_Valid_Id_Should_Return_Updated_Course()
    {
        var client = CreateAuthenticatedAdminClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        
        var existingCourse = await CreateAndSaveCourse(context, "Original Course", "Original Description", 30);

        var request = new UpdateCourseRequest(
            Name: "Updated Course Name",
            Description: "Updated Description",
            Capacity: 45
        );

        var response = await client.PutAsJsonAsync($"/api/courses/{existingCourse.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedCourse = await response.Content.ReadFromJsonAsync<CourseDto>();
        Assert.NotNull(updatedCourse);
        Assert.Equal(existingCourse.Id, updatedCourse.Id);
        AssertCourseProperties(updatedCourse, request.Name, request.Description, request.Capacity);
    }

    [Fact]
    public async Task UpdateCourse_Should_Update_Course_In_Database()
    {
        var client = CreateAuthenticatedAdminClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        
        var existingCourse = await CreateAndSaveCourse(context, "Course To Update", "Original Description", 30);

        var request = new UpdateCourseRequest(
            Name: "Updated Course",
            Description: "New Description",
            Capacity: 50
        );

        var response = await client.PutAsJsonAsync($"/api/courses/{existingCourse.Id}", request);
        response.EnsureSuccessStatusCode();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var courseInDb = await verifyContext.Courses.FirstOrDefaultAsync(c => c.Id == existingCourse.Id);
        Assert.NotNull(courseInDb);
        AssertCourseEntityProperties(courseInDb, request.Name, request.Description, request.Capacity);
    }

    [Fact]
    public async Task UpdateCourse_With_Invalid_Id_Should_Return_NotFound()
    {
        var client = CreateAuthenticatedAdminClient();
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateCourseRequest(
            Name: "Updated Name",
            Description: "Updated Description",
            Capacity: 40
        );

        var response = await client.PutAsJsonAsync($"/api/courses/{nonExistentId}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCourse_With_Valid_Id_Should_Return_NoContent()
    {
        var client = CreateAuthenticatedAdminClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        
        var courseToDelete = await CreateAndSaveCourse(context, "Course To Delete", "This course will be deleted", 25);

        var response = await client.DeleteAsync($"/api/courses/{courseToDelete.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCourse_Should_Remove_Course_From_Database()
    {
        var client = CreateAuthenticatedAdminClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        
        var courseToDelete = await CreateAndSaveCourse(context, "Course To Remove", "This course should be removed", 30);
        var courseId = courseToDelete.Id;

        var response = await client.DeleteAsync($"/api/courses/{courseId}");
        response.EnsureSuccessStatusCode();

        var courseInDb = await context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        Assert.Null(courseInDb);
    }

    [Fact]
    public async Task DeleteCourse_With_Invalid_Id_Should_Return_NotFound()
    {
        var client = CreateAuthenticatedAdminClient();
        var nonExistentId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/api/courses/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
