using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Api.Models;
using StudentCourseEnrollment.Api.Shared;
using StudentCourseEnrollment.Shared.DTOs;
using StudentCourseEnrollment.Shared.DTOs.Courses;

using static Microsoft.AspNetCore.Http.TypedResults;

namespace StudentCourseEnrollment.Api.Features.Courses;

public static class CoursesEndpoints
{
    public static void MapCourses(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses");

        group.MapGet("/", GetCoursesAsync);
        group.MapGet("/{id}", GetCourseAsync);
        group.MapPost("/", CreateCourseAsync).RequireAuthorization("CanManageCourses");
        group.MapPut("/{id}", UpdateCourseAsync).RequireAuthorization("CanManageCourses");
        group.MapDelete("/{id}", DeleteCourseAsync).RequireAuthorization("CanManageCourses");
    }

    public static async Task<Results<Ok<ItemsResult<CourseDto>>, BadRequest>> GetCoursesAsync(
        EnrollmentContext context,
        int page = 0,
        int pageSize = 10,
        string? searchString = null,
        string? sortBy = null,
        SortDirection? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (page < 0)
        {
            return BadRequest();
        }

        if (pageSize <= 0)
        {
            return BadRequest();
        }

        if (pageSize > 100)
        {
            return BadRequest();
        }

        IQueryable<Course> result = context.Courses
            .AsNoTracking()
            .AsQueryable();

        if (searchString is not null)
        {
            result = result.Where(c =>
                c.Name.ToLower().Contains(searchString.ToLower()) ||
                c.Description.ToLower().Contains(searchString.ToLower()));
        }

        var totalCount = await result.CountAsync(cancellationToken);

        if (sortBy is not null)
        {
            result = result.OrderBy(sortBy, sortDirection);
        }
        else
        {
            result = result.OrderBy(x => x.CreatedAt);
        }

        var courses = await result
            .Include(c => c.Enrollments)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var courseDtos = courses.Select(c => new CourseDto(
            c.Id,
            c.Name,
            c.Description,
            c.Capacity,
            c.Enrollments?.Count ?? 0,
            c.CreatedAt
        )).ToList();

        return Ok(new ItemsResult<CourseDto>(courseDtos, totalCount));
    }

    public static async Task<IResult> GetCourseAsync(Guid id, EnrollmentContext context, CancellationToken cancellationToken = default)
    {
        var course = await context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (course == null)
        {
            return Results.NotFound();
        }

        var courseDto = new CourseDto(
            course.Id,
            course.Name,
            course.Description,
            course.Capacity,
            course.Enrollments?.Count ?? 0,
            course.CreatedAt
        );

        return Results.Ok(courseDto);
    }

    public static async Task<IResult> CreateCourseAsync(
        CreateCourseRequest request,
        CourseService service,
        IValidator<CreateCourseRequest> validator,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        var course = new Course
        {
            Name = request.Name,
            Description = request.Description,
            Capacity = request.Capacity
        };

        var createdCourse = await service.CreateCourseAsync(course);

        var courseDto = new CourseDto(
            createdCourse.Id,
            createdCourse.Name,
            createdCourse.Description,
            createdCourse.Capacity,
            0,
            createdCourse.CreatedAt
        );

        return Results.Created($"/api/courses/{createdCourse.Id}", courseDto);
    }

    public static async Task<IResult> UpdateCourseAsync(
        Guid id,
        UpdateCourseRequest request,
        CourseService service,
        EnrollmentContext context,
        IValidator<UpdateCourseRequest> validator,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        var existingCourse = await context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (existingCourse == null)
        {
            return Results.NotFound();
        }

        var currentEnrollments = existingCourse.Enrollments?.Count ?? 0;
        if (request.Capacity < currentEnrollments)
        {
            return Results.Problem(
                title: "Invalid capacity",
                detail: $"Cannot set capacity to {request.Capacity}. Course currently has {currentEnrollments} enrolled students.",
                statusCode: 400
            );
        }

        var course = new Course
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            Capacity = request.Capacity
        };

        var updatedCourse = await service.UpdateCourseAsync(id, course);

        if (updatedCourse == null)
        {
            return Results.NotFound();
        }

        var courseDto = new CourseDto(
            existingCourse.Id,
            existingCourse.Name,
            existingCourse.Description,
            existingCourse.Capacity,
            existingCourse.Enrollments?.Count ?? 0,
            existingCourse.CreatedAt
        );

        return Results.Ok(courseDto);
    }

    public static async Task<IResult> DeleteCourseAsync(
        Guid id,
        CourseService service,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await service.DeleteCourseAsync(id);

            if (!deleted)
            {
                return Results.NotFound();
            }

            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Cannot delete course",
                detail: ex.Message,
                statusCode: 400
            );
        }
    }
}
