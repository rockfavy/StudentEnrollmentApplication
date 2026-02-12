namespace StudentCourseEnrollment.Api.Models;

public class Course
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
