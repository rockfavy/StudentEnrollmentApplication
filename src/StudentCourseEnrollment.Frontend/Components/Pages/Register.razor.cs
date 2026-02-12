using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StudentCourseEnrollment.Shared.DTOs.Auth;

namespace StudentCourseEnrollment.Frontend.Components.Pages;

public partial class Register
{
    [Inject] private IConfiguration? Configuration { get; set; }

    private RegisterModel _registerModel = new();
    private string _errorMessage = string.Empty;
    private bool _isSubmitting;
    private bool showPassword;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var storedEmail = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "registerEmail");
            if (!string.IsNullOrWhiteSpace(storedEmail))
            {
                _registerModel.Email = storedEmail;
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "registerEmail");
            }

            var storedFirstName = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "registerFirstName");
            if (!string.IsNullOrWhiteSpace(storedFirstName))
            {
                _registerModel.FirstName = storedFirstName;
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "registerFirstName");
            }

            var storedLastName = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "registerLastName");
            if (!string.IsNullOrWhiteSpace(storedLastName))
            {
                _registerModel.LastName = storedLastName;
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "registerLastName");
            }

            var storedError = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "registerError");
            if (!string.IsNullOrWhiteSpace(storedError))
            {
                _errorMessage = storedError;
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "registerError");
            }
        }
        catch
        {
        }
    }

    private async Task HandleRegister()
    {
        _errorMessage = string.Empty;
        _isSubmitting = true;

        try
        {
            var registerRequest = new RegisterRequest(_registerModel.Email, _registerModel.FirstName, _registerModel.LastName, _registerModel.Password);
            var response = await AuthClient.RegisterAsync(registerRequest);

            if (response != null)
            {
                Navigation.NavigateTo("/login?registered=1");
            }
            else
            {
                _errorMessage = "Registration failed. Please try again.";
                _isSubmitting = false;
                await StoreErrorInSessionStorage(_errorMessage);
                Navigation.NavigateTo("/register", forceLoad: true);
            }
        }
        catch (HttpRequestException ex)
        {
            _errorMessage = ex.Message;
            _isSubmitting = false;
            await StoreRegisterFormState();
            await StoreErrorInSessionStorage(_errorMessage);
            Navigation.NavigateTo("/register", forceLoad: true);
        }
        catch (Exception ex)
        {
            _errorMessage = $"An error occurred: {ex.Message}";
            _isSubmitting = false;
            await StoreRegisterFormState();
            await StoreErrorInSessionStorage(_errorMessage);
            Navigation.NavigateTo("/register", forceLoad: true);
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task StoreErrorInSessionStorage(string error)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", "registerError", error);
        }
        catch
        {
        }
    }

    private async Task StoreRegisterFormState()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", "registerEmail", _registerModel.Email ?? string.Empty);
            await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", "registerFirstName", _registerModel.FirstName ?? string.Empty);
            await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", "registerLastName", _registerModel.LastName ?? string.Empty);
        }
        catch
        {
        }
    }

    private void TogglePasswordVisibility()
    {
        showPassword = !showPassword;
    }

    private class RegisterModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

