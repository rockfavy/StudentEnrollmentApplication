using StudentCourseEnrollment.Shared.DTOs.Auth;

namespace StudentCourseEnrollment.Frontend.Clients.Interfaces;

public interface IAuthClient
{
    Task<RegisterResponse?> RegisterAsync(RegisterRequest request);
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<ProvisionUserResponse?> ProvisionUserAsync();
}

