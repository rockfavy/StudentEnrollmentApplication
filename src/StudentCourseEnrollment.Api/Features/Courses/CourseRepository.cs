using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Api.Features.Courses.Interfaces;
using StudentCourseEnrollment.Api.Models;

namespace StudentCourseEnrollment.Api.Features.Courses;

public class CourseRepository : ICourseRepository
{
    private readonly EnrollmentContext _context;

    public CourseRepository(EnrollmentContext context)
    {
        _context = context;
    }

    public async Task<List<Course>> GetAllAsync()
    {
        return await _context.Courses.ToListAsync();
    }

    public async Task<Course?> GetByIdAsync(Guid id)
    {
        return await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Course> CreateAsync(Course course)
    {
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        return course;
    }

    public async Task<Course?> UpdateAsync(Course course)
    {
        var existingCourse = await _context.Courses.FindAsync(course.Id);
        if (existingCourse == null)
        {
            return null;
        }

        existingCourse.Name = course.Name;
        existingCourse.Description = course.Description;
        existingCourse.Capacity = course.Capacity;

        await _context.SaveChangesAsync();
        return existingCourse;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var course = await _context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (course == null)
        {
            return false;
        }

        if (course.Enrollments != null && course.Enrollments.Any())
        {
            throw new InvalidOperationException($"Cannot delete course '{course.Name}' because it has {course.Enrollments.Count} active enrollment(s). Please remove all enrollments first.");
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Courses.AnyAsync(c => c.Id == id);
    }

    public async Task<int> GetCurrentEnrollmentCountAsync(Guid courseId)
    {
        return await _context.Enrollments
            .CountAsync(e => e.CourseId == courseId);
    }
}

