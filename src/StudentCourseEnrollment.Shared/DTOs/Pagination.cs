using System.Text.Json.Serialization;

namespace StudentCourseEnrollment.Shared.DTOs;

public record ItemsResult<T>(IEnumerable<T> Items, int TotalItems);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortDirection
{
    Asc = 2,
    Desc = 1
}

