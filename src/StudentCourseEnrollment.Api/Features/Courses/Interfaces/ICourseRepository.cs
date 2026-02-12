using StudentCourseEnrollment.Api.Models;

namespace StudentCourseEnrollment.Api.Features.Courses.Interfaces;

public interface ICourseRepository
{
    Task<List<Course>> GetAllAsync();
    Task<Course?> GetByIdAsync(Guid id);
    Task<Course> CreateAsync(Course course);
    Task<Course?> UpdateAsync(Course course);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}

