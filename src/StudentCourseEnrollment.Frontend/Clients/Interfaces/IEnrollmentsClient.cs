using StudentCourseEnrollment.Shared.DTOs.Courses;
using StudentCourseEnrollment.Shared.DTOs.Enrollments;

namespace StudentCourseEnrollment.Frontend.Clients.Interfaces;

public interface IEnrollmentsClient
{
    Task<EnrollResponse?> EnrollAsync(Guid courseId);
    Task<List<EnrollmentDto>?> GetMyEnrollmentsAsync();
    Task<bool> DeleteEnrollmentAsync(Guid enrollmentId);
    Task<List<CourseWithEnrollmentsDto>?> GetAllCoursesWithEnrollmentsAsync();
}
