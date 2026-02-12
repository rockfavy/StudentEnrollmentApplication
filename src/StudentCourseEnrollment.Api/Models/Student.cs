namespace StudentCourseEnrollment.Api.Models;

public class Student
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = StudentCourseEnrollment.Shared.Role.Student.ToString();
    
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    
    public string FullName => $"{FirstName} {LastName}".Trim();
}
