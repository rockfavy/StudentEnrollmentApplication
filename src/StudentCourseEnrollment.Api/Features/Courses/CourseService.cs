using StudentCourseEnrollment.Api.Features.Courses.Interfaces;
using StudentCourseEnrollment.Api.Models;

namespace StudentCourseEnrollment.Api.Features.Courses;

public class CourseService
{
    private readonly ICourseRepository _repository;

    public CourseService(ICourseRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Course>> GetCoursesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Course?> GetCourseAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Course> CreateCourseAsync(Course course)
    {
        course.Id = Guid.NewGuid();
        course.CreatedAt = DateTime.UtcNow;
        return await _repository.CreateAsync(course);
    }

    public async Task<Course?> UpdateCourseAsync(Guid id, Course course)
    {
        var existingCourse = await _repository.GetByIdAsync(id);
        if (existingCourse == null)
        {
            return null;
        }

        existingCourse.Name = course.Name;
        existingCourse.Description = course.Description;
        existingCourse.Capacity = course.Capacity;

        return await _repository.UpdateAsync(existingCourse);
    }

    public async Task<bool> DeleteCourseAsync(Guid id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<bool> CourseExistsAsync(Guid id)
    {
        return await _repository.ExistsAsync(id);
    }
}
