using StudentCourseEnrollment.Shared.DTOs;
using StudentCourseEnrollment.Shared.DTOs.Courses;

namespace StudentCourseEnrollment.Frontend.Clients.Interfaces;

public interface ICoursesClient
{
    Task<ItemsResult<CourseDto>> GetCoursesAsync(int page, int pageSize, string? searchString = null, string? sortBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);
    Task<CourseDto?> GetCourseAsync(Guid id);
    Task<CourseDto> CreateCourseAsync(CreateCourseRequest request);
    Task<CourseDto> UpdateCourseAsync(Guid id, UpdateCourseRequest request);
    Task<bool> DeleteCourseAsync(Guid id);
}

