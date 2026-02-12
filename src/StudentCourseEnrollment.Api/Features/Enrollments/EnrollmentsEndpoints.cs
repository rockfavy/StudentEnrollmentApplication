using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Shared.DTOs.Enrollments;
using StudentCourseEnrollment.Shared.DTOs.Courses;

namespace StudentCourseEnrollment.Api.Features.Enrollments;

public static class EnrollmentsEndpoints
{
    public static void MapEnrollments(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/enrollments");

        group.MapGet("/me", GetMyEnrollmentsAsync).RequireAuthorization();
        group.MapPost("/", EnrollAsync).RequireAuthorization("CanEnroll");
        group.MapDelete("/{enrollmentId}", DeleteEnrollmentAsync).RequireAuthorization("CanEnroll");
        group.MapGet("/admin/courses", GetAllCoursesWithEnrollmentsAsync).RequireAuthorization("CanManageCourses");
    }

    public static async Task<IResult> GetMyEnrollmentsAsync(
        HttpContext httpContext,
        EnrollmentService service)
    {
        try
        {
            var studentIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (studentIdClaim == null || !Guid.TryParse(studentIdClaim.Value, out var studentId))
            {
                return Results.Unauthorized();
            }

            var enrollments = await service.GetStudentEnrollmentsAsync(studentId);

            var enrollmentDtos = enrollments.Select(e => new EnrollmentDto(
                Id: e.Id,
                CourseId: e.CourseId,
                CourseName: e.Course.Name,
                CourseDescription: e.Course.Description,
                EnrolledAt: e.EnrolledAt
            )).ToList();

            return Results.Ok(enrollmentDtos);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "An error occurred while retrieving enrollments",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    public static async Task<IResult> EnrollAsync(
        EnrollRequest request,
        HttpContext httpContext,
        EnrollmentService service,
        EnrollmentContext db)
    {
        try
        {
            var studentIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (studentIdClaim == null || !Guid.TryParse(studentIdClaim.Value, out var studentId))
            {
                return Results.Unauthorized();
            }

            var course = await db.Courses
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId);
            
            if (course == null)
            {
                return Results.Problem(
                    title: "Course not found",
                    detail: "This course is no longer available.",
                    statusCode: 404
                );
            }

            var isDuplicate = await service.CheckDuplicateEnrollmentAsync(studentId, request.CourseId);
            if (isDuplicate)
            {
                return Results.Problem(
                    title: "Already enrolled",
                    detail: "You are already enrolled in this course.",
                    statusCode: 400
                );
            }

            var currentEnrollments = course.Enrollments?.Count ?? 0;
            if (currentEnrollments >= course.Capacity)
            {
                return Results.Problem(
                    title: "Course full",
                    detail: $"This course is full. Capacity: {course.Capacity} students.",
                    statusCode: 400
                );
            }

            var enrollment = await service.EnrollAsync(studentId, request.CourseId);

            var response = new EnrollResponse(
                Id: enrollment.Id,
                StudentId: enrollment.StudentId,
                CourseId: enrollment.CourseId,
                EnrolledAt: enrollment.EnrolledAt
            );

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "An error occurred",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    public static async Task<IResult> DeleteEnrollmentAsync(
        Guid enrollmentId,
        HttpContext httpContext,
        EnrollmentService service)
    {
        try
        {
            var studentIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (studentIdClaim == null || !Guid.TryParse(studentIdClaim.Value, out var studentId))
            {
                return Results.Unauthorized();
            }

            var deleted = await service.DeleteEnrollmentAsync(enrollmentId, studentId);
            if (!deleted)
            {
                return Results.NotFound();
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "An error occurred while deleting enrollment",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    public static async Task<IResult> GetAllCoursesWithEnrollmentsAsync(
        EnrollmentContext db,
        EnrollmentService service)
    {
        try
        {
            var enrollments = await db.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Student)
                .OrderBy(e => e.Course.Name)
                .ThenBy(e => e.EnrolledAt)
                .ToListAsync();

            var coursesWithEnrollments = enrollments
                .GroupBy(e => e.Course)
                .Select(g => new CourseWithEnrollmentsDto(
                    Id: g.Key.Id,
                    Name: g.Key.Name,
                    Description: g.Key.Description,
                    Capacity: g.Key.Capacity,
                    CurrentEnrollments: g.Count(),
                    CreatedAt: g.Key.CreatedAt,
                    Enrollments: g.Select(e => new StudentEnrollmentDto(
                        EnrollmentId: e.Id,
                        StudentId: e.Student.Id,
                        StudentFirstName: e.Student.FirstName,
                        StudentLastName: e.Student.LastName,
                        StudentEmail: e.Student.Email,
                        EnrolledAt: e.EnrolledAt
                    )).ToList()
                ))
                .ToList();

            return Results.Ok(coursesWithEnrollments);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "An error occurred while retrieving courses with enrollments",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }
}
