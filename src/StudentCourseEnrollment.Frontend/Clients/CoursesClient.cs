using System.Net;
using System.Net.Http.Json;
using StudentCourseEnrollment.Frontend.Clients.Interfaces;
using StudentCourseEnrollment.Shared.DTOs;
using StudentCourseEnrollment.Shared.DTOs.Courses;

namespace StudentCourseEnrollment.Frontend.Clients;

public class CoursesClient : ICoursesClient
{
    private readonly HttpClient _httpClient;

    public CoursesClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ItemsResult<CourseDto>> GetCoursesAsync(int page, int pageSize, string? searchString = null, string? sortBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            queryParams.Add($"searchString={Uri.EscapeDataString(searchString)}");
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
        }

        if (sortDirection.HasValue)
        {
            queryParams.Add($"sortDirection={sortDirection.Value}");
        }

        var queryString = string.Join("&", queryParams);
        var url = $"/api/courses?{queryString}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ItemsResult<CourseDto>>(cancellationToken: cancellationToken);
            return result ?? new ItemsResult<CourseDto>(Enumerable.Empty<CourseDto>(), 0);
        }
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ItemsResult<CourseDto>(Enumerable.Empty<CourseDto>(), 0);
        }
        
        throw new HttpRequestException($"Failed to retrieve courses: {response.StatusCode}");
    }

    public async Task<CourseDto?> GetCourseAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"/api/courses/{id}");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CourseDto>();
        }
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        
        throw new HttpRequestException($"Failed to retrieve course: {response.StatusCode}");
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/courses", request);

            if (response.IsSuccessStatusCode)
            {
                var course = await response.Content.ReadFromJsonAsync<CourseDto>();
                if (course == null)
                {
                    throw new HttpRequestException("Failed to parse course response.");
                }
                return course;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                throw new HttpRequestException($"Invalid request: {errorMessage}");
            }

            var error = await GetErrorMessageAsync(response);
            throw new HttpRequestException($"Failed to create course: {error}");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            throw new HttpRequestException("The request took too long to complete. Please check your internet connection and try again.");
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Unable to create course. Please try again later.", ex);
        }
    }

    public async Task<CourseDto> UpdateCourseAsync(Guid id, UpdateCourseRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/courses/{id}", request);

            if (response.IsSuccessStatusCode)
            {
                var course = await response.Content.ReadFromJsonAsync<CourseDto>();
                if (course == null)
                {
                    throw new HttpRequestException("Failed to parse course response.");
                }
                return course;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new HttpRequestException("Course not found.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                throw new HttpRequestException($"Invalid request: {errorMessage}");
            }

            var error = await GetErrorMessageAsync(response);
            throw new HttpRequestException($"Failed to update course: {error}");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            throw new HttpRequestException("The request took too long to complete. Please check your internet connection and try again.");
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Unable to update course. Please try again later.", ex);
        }
    }

    public async Task<bool> DeleteCourseAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/courses/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new HttpRequestException("Course not found.");
            }

            var errorMessage = await GetErrorMessageAsync(response);
            throw new HttpRequestException($"Failed to delete course: {errorMessage}");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            throw new HttpRequestException("The request took too long to complete. Please check your internet connection and try again.");
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Unable to delete course. Please try again later.", ex);
        }
    }

    private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            
            if (content.StartsWith("{") && content.Contains("\"title\""))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString()))
                {
                    return detail.GetString()!;
                }
                if (doc.RootElement.TryGetProperty("title", out var title))
                {
                    return title.GetString() ?? $"Error: {response.StatusCode}";
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
        
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request. Please check your input and try again.",
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
            HttpStatusCode.NotFound => "The requested course could not be found.",
            HttpStatusCode.InternalServerError => "A server error occurred. Please try again in a few moments.",
            HttpStatusCode.ServiceUnavailable => "The service is temporarily unavailable. Please try again later.",
            _ => "An error occurred while processing your request. Please try again."
        };
    }
}
