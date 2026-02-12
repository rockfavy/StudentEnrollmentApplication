using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Api.Models;
using StudentCourseEnrollment.Shared.DTOs.Auth;

namespace StudentCourseEnrollment.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapProvisionUser();
    }

    public static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        EnrollmentContext db,
        IValidator<RegisterRequest> validator)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                return Results.ValidationProblem(errors);
            }

            if (await db.Students.AnyAsync(s => s.Email == request.Email))
            {
                return Results.Problem(
                    title: "Email already registered",
                    statusCode: 400
                );
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var student = new Student
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = passwordHash
            };

            db.Students.Add(student);
            await db.SaveChangesAsync();

            var response = new RegisterResponse(
                Id: student.Id,
                Email: student.Email,
                FirstName: student.FirstName,
                LastName: student.LastName
            );

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "An error occurred during registration",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    public static async Task<IResult> LoginAsync(
        LoginRequest request,
        EnrollmentContext db,
        JwtTokenService tokenService,
        IValidator<LoginRequest> validator)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                return Results.ValidationProblem(errors);
            }

            var student = await db.Students
                .FirstOrDefaultAsync(s => s.Email == request.Email);

            if (student == null)
            {
                return Results.Problem(
                    title: "Invalid credentials",
                    detail: "You do not have a valid account. Please register for a new account.",
                    statusCode: 401
                );
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, student.PasswordHash))
            {
                return Results.Problem(
                    title: "Invalid credentials",
                    detail: "Incorrect password. Please try again.",
                    statusCode: 401
                );
            }

            var roles = new[] { student.Role };
            var token = tokenService.GenerateToken(student.Id, student.Email, student.FirstName, student.LastName, roles);

            var response = new LoginResponse(
                Token: token,
                Id: student.Id,
                Email: student.Email,
                FirstName: student.FirstName,
                LastName: student.LastName
            );

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "An error occurred during login",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }
}
