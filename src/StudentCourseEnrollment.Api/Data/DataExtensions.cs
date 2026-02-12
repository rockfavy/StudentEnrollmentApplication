using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Data;
using StudentCourseEnrollment.Api.Models;
using RoleEnum = StudentCourseEnrollment.Shared.Role;

namespace StudentCourseEnrollment.Api.Data;

public static class DataExtensions
{
    public static IServiceCollection AddEnrollmentContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EnrollmentContext>(options =>
            options.UseInMemoryDatabase("EnrollmentDb"));
        
        return services;
    }

    public static async Task InitializeDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentContext>();
        
        var adminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var student1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var student2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var student3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var student4Id = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var adminEmail = "admin@example.com";
        var existingAdmin = await context.Students.FirstOrDefaultAsync(s => s.Email == adminEmail);
        if (existingAdmin == null)
        {
            var admin = new Student
            {
                Id = adminId,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = RoleEnum.Admin.ToString()
            };
            context.Students.Add(admin);
            await context.SaveChangesAsync();
        }
        else if (existingAdmin.Role != RoleEnum.Admin.ToString())
        {
            existingAdmin.Role = RoleEnum.Admin.ToString();
            await context.SaveChangesAsync();
        }

        var existingStudentIds = await context.Students
            .Where(s => s.Id == student1Id || s.Id == student2Id || s.Id == student3Id || s.Id == student4Id)
            .Select(s => s.Id)
            .ToListAsync();

        if (existingStudentIds.Count < 4)
        {
            var students = new List<Student>();
            
            if (!existingStudentIds.Contains(student1Id))
            {
                students.Add(new Student
                {
                    Id = student1Id,
                    Email = "john.doe@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    PasswordHash = "hashed_password_123",
                    Role = RoleEnum.Student.ToString()
                });
            }
            
            if (!existingStudentIds.Contains(student2Id))
            {
                students.Add(new Student
                {
                    Id = student2Id,
                    Email = "jane.smith@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    PasswordHash = "hashed_password_456",
                    Role = RoleEnum.Student.ToString()
                });
            }
            
            if (!existingStudentIds.Contains(student3Id))
            {
                students.Add(new Student
                {
                    Id = student3Id,
                    Email = "bob.johnson@example.com",
                    FirstName = "Bob",
                    LastName = "Johnson",
                    PasswordHash = "hashed_password_789",
                    Role = RoleEnum.Student.ToString()
                });
            }
            
            if (!existingStudentIds.Contains(student4Id))
            {
                students.Add(new Student
                {
                    Id = student4Id,
                    Email = "alice.williams@example.com",
                    FirstName = "Alice",
                    LastName = "Williams",
                    PasswordHash = "hashed_password_012",
                    Role = RoleEnum.Student.ToString()
                });
            }

            if (students.Count > 0)
            {
                try
                {
                    await context.Students.AddRangeAsync(students);
                    await context.SaveChangesAsync();
                }
                catch (ArgumentException)
                {
                }
            }
        }

        await DataSeeder.SeedCoursesAsync(context);

        var course1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var course2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var course3Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var course4Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var course5Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        var enrollmentIds = new[]
        {
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Guid.Parse("10000000-0000-0000-0000-000000000003"),
            Guid.Parse("10000000-0000-0000-0000-000000000004"),
            Guid.Parse("10000000-0000-0000-0000-000000000005"),
            Guid.Parse("10000000-0000-0000-0000-000000000006"),
            Guid.Parse("10000000-0000-0000-0000-000000000007"),
            Guid.Parse("10000000-0000-0000-0000-000000000008")
        };

        var existingEnrollmentIds = await context.Enrollments
            .Where(e => enrollmentIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (existingEnrollmentIds.Count < enrollmentIds.Length)
        {
            var enrollments = new List<Enrollment>();
            var enrollment1Id = enrollmentIds[0];
            var enrollment2Id = enrollmentIds[1];
            var enrollment3Id = enrollmentIds[2];
            var enrollment4Id = enrollmentIds[3];
            var enrollment5Id = enrollmentIds[4];
            var enrollment6Id = enrollmentIds[5];
            var enrollment7Id = enrollmentIds[6];
            var enrollment8Id = enrollmentIds[7];

            if (!existingEnrollmentIds.Contains(enrollment1Id))
            {
                enrollments.Add(new Enrollment
                {
                    Id = enrollment1Id,
                    StudentId = student1Id,
                    CourseId = course1Id,
                    EnrolledAt = DateTime.UtcNow.AddDays(-10)
                });
            }

            if (!existingEnrollmentIds.Contains(enrollment2Id))
            {
                enrollments.Add(new Enrollment
                {
                    Id = enrollment2Id,
                    StudentId = student1Id,
                    CourseId = course2Id,
                    EnrolledAt = DateTime.UtcNow.AddDays(-5)
                });
            }

            if (!existingEnrollmentIds.Contains(enrollment3Id))
            {
                enrollments.Add(new Enrollment
                {
                    Id = enrollment3Id,
                    StudentId = student2Id,
                    CourseId = course1Id,
                    EnrolledAt = DateTime.UtcNow.AddDays(-8)
                });
            }

            if (!existingEnrollmentIds.Contains(enrollment4Id))
            {
                enrollments.Add(new Enrollment
                {
                    Id = enrollment4Id,
                    StudentId = student2Id,
                    CourseId = course3Id,
                    EnrolledAt = DateTime.UtcNow.AddDays(-3)
                });
            }

            if (!existingEnrollmentIds.Contains(enrollment5Id))
            {
                enrollments.Add(new Enrollment
                {
                    Id = enrollment5Id,
                    StudentId = student3Id,
                    CourseId = course4Id,
                    EnrolledAt = DateTime.UtcNow.AddDays(-7)
                });
            }

            if (!existingEnrollmentIds.Contains(enrollment6Id))
            {
                enrollments.Add(new Enrollment
                {
                    Id = enrollment6Id,
                    StudentId = student3Id,
                    CourseId = course5Id,
                    EnrolledAt = DateTime.UtcNow.AddDays(-2)
                });
            }

            if (!existingEnrollmentIds.Contains(enrollment7Id))
            {
                enrollments.Add(new Enrollment
                {
                    Id = enrollment7Id,
                    StudentId = student4Id,
                    CourseId = course2Id,
                    EnrolledAt = DateTime.UtcNow.AddDays(-6)
                });
            }

            if (!existingEnrollmentIds.Contains(enrollment8Id))
            {
                enrollments.Add(new Enrollment
                {
                    Id = enrollment8Id,
                    StudentId = student4Id,
                    CourseId = course3Id,
                    EnrolledAt = DateTime.UtcNow.AddDays(-4)
                });
            }

            if (enrollments.Count > 0)
            {
                try
                {
                    await context.Enrollments.AddRangeAsync(enrollments);
                    await context.SaveChangesAsync();
                }
                catch (ArgumentException)
                {
                }
            }
        }
    }
}
