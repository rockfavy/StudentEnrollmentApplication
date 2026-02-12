using System.Net;
using System.Net.Http.Json;
using StudentCourseEnrollment.Frontend.Clients.Interfaces;
using StudentCourseEnrollment.Shared.DTOs.Auth;
using StudentCourseEnrollment.Frontend.Helpers;

namespace StudentCourseEnrollment.Frontend.Clients;

public class AuthClient : IAuthClient
{
    private readonly HttpClient _httpClient;

    public AuthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RegisterResponse>();
            }

            var errorMessage = await response.ReadErrorMessageAsync(statusCode => statusCode switch
            {
                HttpStatusCode.BadRequest => "Invalid registration details. Please check your input and try again.",
                HttpStatusCode.Conflict => "This email is already registered. Please use a different email or log in.",
                HttpStatusCode.InternalServerError => "A server error occurred. Please try again in a few moments.",
                HttpStatusCode.ServiceUnavailable => "The service is temporarily unavailable. Please try again later.",
                _ => "An error occurred while processing your registration. Please try again."
            });
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
            throw new HttpRequestException($"Unable to connect to the server. Please check your internet connection and try again.", ex);
        }
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }

        var errorMessage = await response.ReadErrorMessageAsync(statusCode => statusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request. Please check your input and try again.",
            HttpStatusCode.Unauthorized => "Invalid email or password. Please try again.",
            HttpStatusCode.Conflict => "This email is already registered. Please use a different email or log in.",
            HttpStatusCode.InternalServerError => "A server error occurred. Please try again in a few moments.",
            HttpStatusCode.ServiceUnavailable => "The service is temporarily unavailable. Please try again later.",
            _ => "An error occurred while processing your request. Please try again."
        });
        throw new HttpRequestException(errorMessage);
    }

    public async Task<ProvisionUserResponse?> ProvisionUserAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/auth/provision", null);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProvisionUserResponse>();
            }

            var errorMessage = await response.ReadErrorMessageAsync(statusCode => statusCode switch
            {
                HttpStatusCode.BadRequest => "Could not provision user with the provided information.",
                HttpStatusCode.Unauthorized => "You must be logged in to provision a user.",
                HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
                HttpStatusCode.NotFound => "The provisioning endpoint could not be found.",
                HttpStatusCode.InternalServerError => "A server error occurred while provisioning the user.",
                HttpStatusCode.ServiceUnavailable => "The service is temporarily unavailable. Please try again later.",
                _ => "An error occurred while provisioning the user. Please try again."
            });
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
            throw new HttpRequestException($"Unable to connect to the server. Please check your internet connection and try again.", ex);
        }
    }

}
