using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Api.Models;

namespace StudentCourseEnrollment.Api.Features.Enrollments;

public class EnrollmentService
{
    private readonly EnrollmentContext _context;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(EnrollmentContext context, ILogger<EnrollmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CheckDuplicateEnrollmentAsync(Guid studentId, Guid courseId)
    {
        return await _context.Enrollments
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
    }

    public async Task<Enrollment> EnrollAsync(Guid studentId, Guid courseId)
    {
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Student {StudentId} enrolled in course {CourseId} at {EnrolledAt}",
            studentId,
            courseId,
            enrollment.EnrolledAt
        );

        return enrollment;
    }

    public async Task<List<Enrollment>> GetStudentEnrollmentsAsync(Guid studentId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteEnrollmentAsync(Guid enrollmentId, Guid studentId)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == studentId);

        if (enrollment == null)
        {
            return false;
        }

        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Student {StudentId} deregistered from enrollment {EnrollmentId} at {DeregisteredAt}",
            studentId,
            enrollmentId,
            DateTime.UtcNow
        );

        return true;
    }

    public async Task<List<Enrollment>> GetCourseEnrollmentsAsync(Guid courseId)
    {
        return await _context.Enrollments
            .Include(e => e.Student)
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync();
    }

    public async Task<List<Enrollment>> GetAllEnrollmentsAsync()
    {
        return await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .OrderBy(e => e.Course.Name)
            .ThenBy(e => e.EnrolledAt)
            .ToListAsync();
    }
}
