using System.Net;
using System.Net.Http.Json;
using StudentCourseEnrollment.Frontend.Clients.Interfaces;
using StudentCourseEnrollment.Shared.DTOs.Courses;
using StudentCourseEnrollment.Shared.DTOs.Enrollments;

namespace StudentCourseEnrollment.Frontend.Clients;

public class EnrollmentsClient : IEnrollmentsClient
{
    private readonly HttpClient _httpClient;

    public EnrollmentsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EnrollResponse?> EnrollAsync(Guid courseId)
    {
        try
        {
            var request = new EnrollRequest(courseId);
            var response = await _httpClient.PostAsJsonAsync("/api/enrollments", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EnrollResponse>();
            }
            
            var errorMessage = await GetErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new HttpRequestException("The request took too long to complete. Please check your internet connection and try again.", ex);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Unable to enroll in course. Please try again later.", ex);
        }
    }

    public async Task<List<EnrollmentDto>?> GetMyEnrollmentsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/enrollments/me");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<EnrollmentDto>>();
                return result ?? new List<EnrollmentDto>();
            }
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException("You must be logged in to view your enrollments.");
            }
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<EnrollmentDto>();
            }
            
            var errorMessage = await GetErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
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
            throw new HttpRequestException($"Unable to retrieve your enrollments. Please try again later.", ex);
        }
    }

    public async Task<bool> DeleteEnrollmentAsync(Guid enrollmentId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/enrollments/{enrollmentId}");
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException("You must be logged in to deregister from a course.");
            }
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new HttpRequestException("Enrollment not found.");
            }
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new HttpRequestException("You do not have permission to deregister from this course.");
            }
            
            var errorMessage = await GetErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
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
            throw new HttpRequestException($"Unable to deregister from course. Please try again later.", ex);
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
            HttpStatusCode.NotFound => "The requested course or enrollment could not be found.",
            HttpStatusCode.Conflict => "You are already enrolled in this course.",
            HttpStatusCode.InternalServerError => "A server error occurred. Please try again in a few moments.",
            HttpStatusCode.ServiceUnavailable => "The service is temporarily unavailable. Please try again later.",
            _ => "An error occurred while processing your request. Please try again."
        };
    }

    public async Task<List<CourseWithEnrollmentsDto>?> GetAllCoursesWithEnrollmentsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/enrollments/admin/courses");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<CourseWithEnrollmentsDto>>();
                return result ?? new List<CourseWithEnrollmentsDto>();
            }
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException("You must be logged in to view courses with enrollments.");
            }
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new HttpRequestException("You do not have permission to view courses with enrollments.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<CourseWithEnrollmentsDto>();
            }

            var errorMessage = await GetErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
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
            throw new HttpRequestException($"Unable to retrieve courses with enrollments. Please try again later.", ex);
        }
    }
}
