using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace StudentCourseEnrollment.Frontend.Helpers;

public static class HttpResponseMessageExtensions
{
    public static async Task<string> ReadErrorMessageAsync(
        this HttpResponseMessage response,
        Func<HttpStatusCode, string> defaultMessageFactory)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(content) && content.StartsWith("{", StringComparison.Ordinal))
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("errors", out var errorsElement) &&
                    errorsElement.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();

                    foreach (var property in errorsElement.EnumerateObject())
                    {
                        var field = property.Name;
                        var fieldErrors = property.Value.EnumerateArray()
                            .Select(e => e.GetString())
                            .Where(e => !string.IsNullOrWhiteSpace(e));

                        foreach (var message in fieldErrors)
                        {
                            messages.Add($"{field}: {message}");
                        }
                    }

                    if (messages.Count > 0)
                    {
                        return string.Join(Environment.NewLine, messages);
                    }
                }

                if (root.TryGetProperty("detail", out var detailElement) &&
                    !string.IsNullOrWhiteSpace(detailElement.GetString()))
                {
                    return detailElement.GetString()!;
                }

                if (root.TryGetProperty("title", out var titleElement))
                {
                    return titleElement.GetString() ?? $"Error: {response.StatusCode}";
                }
            }

            if (!string.IsNullOrWhiteSpace(content) && content.Length < 200)
            {
                return content;
            }
        }
        catch
        {
        }

        return defaultMessageFactory(response.StatusCode);
    }
}

