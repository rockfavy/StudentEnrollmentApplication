using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Models;

namespace StudentCourseEnrollment.Api.Data;

public class DataSeeder
{
    public static List<Course> CreateSampleCourses()
    {
        var baseDate = DateTime.UtcNow;
        var courses = new List<Course>();
        var courseNames = new[]
        {
            ("Introduction to Programming", "Learn the fundamentals of programming with C#", 30),
            ("Database Design", "Design and implement relational databases", 25),
            ("Web Development", "Build modern web applications with ASP.NET Core", 35),
            ("Software Engineering", "Principles and practices of software development", 20),
            ("Data Structures and Algorithms", "Essential data structures and algorithm design", 28),
            ("Object-Oriented Programming", "Master OOP concepts and design patterns", 32),
            ("Mobile App Development", "Create cross-platform mobile applications", 24),
            ("Cloud Computing", "Introduction to cloud services and architecture", 26),
            ("Machine Learning Basics", "Fundamentals of ML and data science", 22),
            ("Cybersecurity Fundamentals", "Learn about security threats and protection", 18),
            ("DevOps Practices", "CI/CD pipelines and deployment strategies", 30),
            ("UI/UX Design", "Design principles and user experience", 28),
            ("API Development", "RESTful APIs and microservices architecture", 25),
            ("Testing and Quality Assurance", "Software testing methodologies", 20),
            ("Project Management", "Agile and Scrum methodologies", 30),
            ("Version Control Systems", "Git and collaborative development", 22),
            ("Containerization", "Docker and Kubernetes basics", 24),
            ("Frontend Frameworks", "React, Angular, and Vue.js overview", 28),
            ("Backend Development", "Server-side programming and databases", 30),
            ("Full Stack Development", "End-to-end web application development", 26),
            ("Game Development", "Introduction to game programming", 20),
            ("Blockchain Technology", "Cryptocurrency and smart contracts", 18),
            ("Artificial Intelligence", "AI concepts and applications", 22),
            ("Network Programming", "Socket programming and protocols", 24),
            ("System Administration", "Linux and server management", 20)
        };

        for (int i = 0; i < courseNames.Length; i++)
        {
            courses.Add(new Course
            {
                Id = Guid.NewGuid(),
                Name = courseNames[i].Item1,
                Description = courseNames[i].Item2,
                Capacity = courseNames[i].Item3,
                CreatedAt = baseDate.AddMonths(-(courseNames.Length - i))
            });
        }

        return courses;
    }

    public static async Task SeedCoursesAsync(EnrollmentContext context)
    {
        if (!await context.Courses.AnyAsync())
        {
            try
            {
                var courses = CreateSampleCourses();
                await context.Courses.AddRangeAsync(courses);
                await context.SaveChangesAsync();
            }
            catch (ArgumentException)
            {
                throw;
            }
        }
    }
}
