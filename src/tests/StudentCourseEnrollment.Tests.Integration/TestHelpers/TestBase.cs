using StudentCourseEnrollment.Api.Data;

namespace StudentCourseEnrollment.Tests.Integration.TestHelpers;

public abstract class TestBase : IDisposable
{
    protected EnrollmentContext DbContext { get; }
    protected string DatabaseName { get; }

    protected TestBase()
    {
        DatabaseName = Guid.NewGuid().ToString();
        DbContext = TestDbContextFactory.CreateInMemoryContext(DatabaseName);
    }

    protected async Task SeedDataAsync(params object[] entities)
    {
        foreach (var entity in entities)
        {
            DbContext.Add(entity);
        }
        await DbContext.SaveChangesAsync();
    }

    protected async Task ClearDatabaseAsync()
    {
        DbContext.Students.RemoveRange(DbContext.Students);
        DbContext.Courses.RemoveRange(DbContext.Courses);
        DbContext.Enrollments.RemoveRange(DbContext.Enrollments);
        await DbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
