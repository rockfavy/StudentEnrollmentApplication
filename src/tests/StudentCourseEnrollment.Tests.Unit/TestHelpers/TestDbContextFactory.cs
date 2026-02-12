using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollment.Api.Data;

namespace StudentCourseEnrollment.Tests.Unit.TestHelpers;

public static class TestDbContextFactory
{
    public static EnrollmentContext CreateInMemoryContext(string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();
        
        var options = new DbContextOptionsBuilder<EnrollmentContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new EnrollmentContext(options);
    }
}

