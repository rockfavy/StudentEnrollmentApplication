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
using StudentCourseEnrollment.Shared.DTOs.Auth;
using StudentCourseEnrollment.Shared.DTOs.Enrollments;
using StudentCourseEnrollment.Tests.Integration.TestHelpers;
using Xunit;

namespace StudentCourseEnrollment.Tests.Integration.Endpoints;

public class EnrollmentsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JwtTokenService _tokenService;

    public EnrollmentsEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<string> RegisterAndLoginAsync(HttpClient client, string email, string password)
    {
        var registerRequest = new RegisterRequest(
            Email: email,
            FirstName: "Test",
            LastName: "User",
            Password: password
        );
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        var loginRequest = new LoginRequest(
            Email: email,
            Password: password
        );
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return loginResult!.Token;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task GetMyEnrollments_With_Valid_Token_Should_Return_Enrollments()
    {
        var client = _factory.CreateClient();
        var email = $"enrollments{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        var authenticatedClient = CreateAuthenticatedClient(token);
        var response = await authenticatedClient.GetAsync("/api/enrollments/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrollments = await response.Content.ReadFromJsonAsync<List<EnrollmentDto>>();
        Assert.NotNull(enrollments);
    }

    [Fact]
    public async Task GetMyEnrollments_Without_Token_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/enrollments/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEnrollments_With_Enrollments_Should_Return_Correct_Data()
    {
        var client = _factory.CreateClient();
        var email = $"enrollments{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var student = await context.Students.FirstOrDefaultAsync(s => s.Email == email);
        Assert.NotNull(student);

        var course = await context.Courses.FirstOrDefaultAsync();
        if (course != null)
        {
            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                CourseId = course.Id,
                EnrolledAt = DateTime.UtcNow
            };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();
        }

        var authenticatedClient = CreateAuthenticatedClient(token);
        var response = await authenticatedClient.GetAsync("/api/enrollments/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrollments = await response.Content.ReadFromJsonAsync<List<EnrollmentDto>>();
        Assert.NotNull(enrollments);
        if (course != null)
        {
            Assert.Contains(enrollments!, e => e.CourseId == course.Id);
        }
    }

    [Fact]
    public async Task Enroll_With_Valid_Token_And_Course_Should_Return_Success()
    {
        var client = _factory.CreateClient();
        var email = $"enroll{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var course = await context.Courses.FirstOrDefaultAsync();
        Assert.NotNull(course);

        var authenticatedClient = CreateAuthenticatedClient(token);
        var request = new EnrollRequest(course.Id);
        var response = await authenticatedClient.PostAsJsonAsync("/api/enrollments", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrollment = await response.Content.ReadFromJsonAsync<EnrollResponse>();
        Assert.NotNull(enrollment);
        Assert.NotEqual(Guid.Empty, enrollment.Id);
        Assert.Equal(course.Id, enrollment.CourseId);
    }

    [Fact]
    public async Task Enroll_Without_Token_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var course = await context.Courses.FirstOrDefaultAsync();
        Assert.NotNull(course);

        var request = new EnrollRequest(course.Id);
        var response = await client.PostAsJsonAsync("/api/enrollments", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enroll_With_Duplicate_Enrollment_Should_Return_BadRequest()
    {
        var client = _factory.CreateClient();
        var email = $"duplicate{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var student = await context.Students.FirstOrDefaultAsync(s => s.Email == email);
        Assert.NotNull(student);

        var course = await context.Courses.FirstOrDefaultAsync();
        Assert.NotNull(course);

        var authenticatedClient = CreateAuthenticatedClient(token);
        var request = new EnrollRequest(course.Id);

        var firstResponse = await authenticatedClient.PostAsJsonAsync("/api/enrollments", request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await authenticatedClient.PostAsJsonAsync("/api/enrollments", request);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Enroll_With_NonExistent_Course_Should_Return_NotFound()
    {
        var client = _factory.CreateClient();
        var email = $"nonexistent{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        var authenticatedClient = CreateAuthenticatedClient(token);
        var nonExistentCourseId = Guid.NewGuid();
        var request = new EnrollRequest(nonExistentCourseId);
        var response = await authenticatedClient.PostAsJsonAsync("/api/enrollments", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enroll_Should_Return_Enrollment_With_Correct_Properties()
    {
        var client = _factory.CreateClient();
        var email = $"properties{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var student = await context.Students.FirstOrDefaultAsync(s => s.Email == email);
        Assert.NotNull(student);

        var course = await context.Courses.FirstOrDefaultAsync();
        Assert.NotNull(course);

        var authenticatedClient = CreateAuthenticatedClient(token);
        var request = new EnrollRequest(course.Id);
        var response = await authenticatedClient.PostAsJsonAsync("/api/enrollments", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrollment = await response.Content.ReadFromJsonAsync<EnrollResponse>();
        Assert.NotNull(enrollment);
        Assert.NotEqual(Guid.Empty, enrollment.Id);
        Assert.Equal(student.Id, enrollment.StudentId);
        Assert.Equal(course.Id, enrollment.CourseId);
        Assert.True(enrollment.EnrolledAt <= DateTime.UtcNow);
        Assert.True(enrollment.EnrolledAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task Enroll_Should_Persist_Enrollment_To_Database()
    {
        var client = _factory.CreateClient();
        var email = $"persist{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var student = await context.Students.FirstOrDefaultAsync(s => s.Email == email);
        Assert.NotNull(student);

        var course = await context.Courses.FirstOrDefaultAsync();
        Assert.NotNull(course);

        var authenticatedClient = CreateAuthenticatedClient(token);
        var request = new EnrollRequest(course.Id);
        var response = await authenticatedClient.PostAsJsonAsync("/api/enrollments", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrollment = await response.Content.ReadFromJsonAsync<EnrollResponse>();
        Assert.NotNull(enrollment);

        var savedEnrollment = await context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == enrollment.Id);
        Assert.NotNull(savedEnrollment);
        Assert.Equal(student.Id, savedEnrollment.StudentId);
        Assert.Equal(course.Id, savedEnrollment.CourseId);
    }

    [Fact]
    public async Task DeleteEnrollment_With_Valid_Token_And_Ownership_Should_Return_NoContent()
    {
        var client = _factory.CreateClient();
        var email = $"delete{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var student = await context.Students.FirstOrDefaultAsync(s => s.Email == email);
        Assert.NotNull(student);

        var course = await context.Courses.FirstOrDefaultAsync();
        Assert.NotNull(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var authenticatedClient = CreateAuthenticatedClient(token);
        var response = await authenticatedClient.DeleteAsync($"/api/enrollments/{enrollment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var deletedEnrollment = await context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == enrollment.Id);
        Assert.Null(deletedEnrollment);
    }

    [Fact]
    public async Task DeleteEnrollment_Without_Token_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var enrollment = await context.Enrollments.FirstOrDefaultAsync();
        
        if (enrollment != null)
        {
            var response = await client.DeleteAsync($"/api/enrollments/{enrollment.Id}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task DeleteEnrollment_With_NonExistent_Enrollment_Should_Return_NotFound()
    {
        var client = _factory.CreateClient();
        var email = $"nonexistent{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        var authenticatedClient = CreateAuthenticatedClient(token);
        var nonExistentEnrollmentId = Guid.NewGuid();
        var response = await authenticatedClient.DeleteAsync($"/api/enrollments/{nonExistentEnrollmentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEnrollment_With_Other_Student_Enrollment_Should_Return_NotFound()
    {
        var client = _factory.CreateClient();
        var email1 = $"student1{Guid.NewGuid()}@example.com";
        var email2 = $"student2{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token1 = await RegisterAndLoginAsync(client, email1, password);
        var token2 = await RegisterAndLoginAsync(client, email2, password);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var student1 = await context.Students.FirstOrDefaultAsync(s => s.Email == email1);
        Assert.NotNull(student1);

        var course = await context.Courses.FirstOrDefaultAsync();
        Assert.NotNull(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student1.Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var authenticatedClient2 = CreateAuthenticatedClient(token2);
        var response = await authenticatedClient2.DeleteAsync($"/enrollments/{enrollment.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var existingEnrollment = await context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == enrollment.Id);
        Assert.NotNull(existingEnrollment);
    }

    [Fact]
    public async Task DeleteEnrollment_Should_Remove_Enrollment_From_Database()
    {
        var client = _factory.CreateClient();
        var email = $"remove{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var token = await RegisterAndLoginAsync(client, email, password);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        var student = await context.Students.FirstOrDefaultAsync(s => s.Email == email);
        Assert.NotNull(student);

        var course = await context.Courses.FirstOrDefaultAsync();
        Assert.NotNull(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var enrollmentBeforeDelete = await context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == enrollment.Id);
        Assert.NotNull(enrollmentBeforeDelete);

        var authenticatedClient = CreateAuthenticatedClient(token);
        var response = await authenticatedClient.DeleteAsync($"/api/enrollments/{enrollment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var enrollmentAfterDelete = await context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == enrollment.Id);
        Assert.Null(enrollmentAfterDelete);
    }
}
