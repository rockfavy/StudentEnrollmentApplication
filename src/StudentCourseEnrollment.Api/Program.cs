using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Api.Features.Auth;
using StudentCourseEnrollment.Api.Features.Courses;
using StudentCourseEnrollment.Api.Features.Courses.Interfaces;
using StudentCourseEnrollment.Api.Features.Enrollments;
using StudentCourseEnrollment.Api.Shared;
using StudentCourseEnrollment.Api.Shared.ErrorHandling;
using StudentCourseEnrollment.Shared;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services
            .AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SecretKey), "Jwt:SecretKey is required.")
            .ValidateOnStart();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = builder.Configuration
                    .GetSection(JwtOptions.SectionName)
                    .Get<JwtOptions>()
                    ?? throw new InvalidOperationException("Jwt configuration is missing.");

                if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
                {
                    throw new InvalidOperationException("Jwt:SecretKey must be configured.");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("CanEnroll", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Role.Student.ToString());
            });

            options.AddPolicy("CanManageCourses", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Role.Admin.ToString());
            });

            options.FallbackPolicy = null;
        });

        builder.Services.AddScoped<JwtTokenService>();
        builder.Services.AddScoped<UserProvisioningService>();
        builder.Services.AddScoped<ICourseRepository, CourseRepository>();
        builder.Services.AddScoped<CourseService>();
        builder.Services.AddScoped<EnrollmentService>();

        builder.Services.AddEnrollmentContext(builder.Configuration);
        builder.Services.AddHealthChecks();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors();
        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapAuth();
        app.MapCourses();
        app.MapEnrollments();

        await app.InitializeDbAsync();
        app.Run();
    }
}
