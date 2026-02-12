using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StudentCourseEnrollment.Frontend.Helpers;
using StudentCourseEnrollment.Shared.DTOs.Auth;

namespace StudentCourseEnrollment.Frontend.Components.Pages;

public partial class Login
{
    private const string LoginErrorKey = "loginError";
    private const string LoginEmailKey = "loginEmail";

    [Inject] private Services.CustomAuthenticationStateProvider? AuthStateProvider { get; set; }
    [Inject] private IConfiguration? Configuration { get; set; }

    private LoginModel _loginModel = new();
    private string _errorMessage = string.Empty;
    private bool _isSubmitting;
    private string _previousEmail = string.Empty;
    private string _previousPassword = string.Empty;
    private bool showPassword;

    protected override async Task OnInitializedAsync()
    {
        _previousEmail = _loginModel.Email ?? string.Empty;
        _previousPassword = _loginModel.Password ?? string.Empty;

        string? storedEmail = null;
        string? storedError = null;

        try
        {
            storedEmail = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", LoginEmailKey);
        }
        catch (JSException)
        {
        }

        if (!string.IsNullOrWhiteSpace(storedEmail))
        {
            _loginModel.Email = storedEmail;
            _previousEmail = storedEmail;
            try
            {
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", LoginEmailKey);
            }
            catch (JSException)
            {
            }
        }

        try
        {
            storedError = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", LoginErrorKey);
        }
        catch (JSException)
        {
        }

        if (!string.IsNullOrWhiteSpace(storedError))
        {
            _errorMessage = storedError;
            try
            {
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", LoginErrorKey);
            }
            catch (JSException)
            {
            }
        }
    }

    private async Task HandleFormSubmit()
    {
        _errorMessage = string.Empty;
        _isSubmitting = true;
        StateHasChanged();

        if (string.IsNullOrWhiteSpace(_loginModel.Email) || string.IsNullOrWhiteSpace(_loginModel.Password))
        {
            _errorMessage = "Please fill in all required fields.";
            _isSubmitting = false;
            StateHasChanged();
            return;
        }

        var emailAttribute = new EmailAddressAttribute();
        if (!emailAttribute.IsValid(_loginModel.Email))
        {
            _errorMessage = "Please enter a valid email address.";
            _isSubmitting = false;
            StateHasChanged();
            return;
        }

        await HandleLogin();
    }

    private async Task HandleLogin()
    {
        try
        {
            var loginRequest = new LoginRequest(_loginModel.Email, _loginModel.Password);
            var response = await AuthClient.LoginAsync(loginRequest);

            if (response != null)
            {
                var claims = JwtTokenHelper.ParseClaimsFromJwt(response.Token);

                if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, response.Id.ToString()));
                }

                if (!claims.Any(c => c.Type == ClaimTypes.Email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, response.Email));
                }

                var fullName = $"{response.FirstName} {response.LastName}".Trim();
                if (!claims.Any(c => c.Type == ClaimTypes.Name))
                {
                    claims.Add(new Claim(ClaimTypes.Name, fullName));
                }
                if (!claims.Any(c => c.Type == "FirstName"))
                {
                    claims.Add(new Claim("FirstName", response.FirstName));
                }
                if (!claims.Any(c => c.Type == "LastName"))
                {
                    claims.Add(new Claim("LastName", response.LastName));
                }

                var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
                var user = new ClaimsPrincipal(identity);

                if (AuthStateProvider != null)
                {
                    await AuthStateProvider.SetAuthenticationStateAsync(user, response.Token);
                }

                _isSubmitting = false;
                StateHasChanged();
                Navigation.NavigateTo("/");
            }
            else
            {
                await StoreLoginFormState();
                _isSubmitting = false;
                await StoreErrorInSessionStorage("Invalid email or password.");
                Navigation.NavigateTo("/login", forceLoad: true);
            }
        }
        catch (HttpRequestException ex)
        {
            await StoreLoginFormState();
            _isSubmitting = false;
            var errorMsg = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "Invalid email or password. Please try again.";
            await StoreErrorInSessionStorage(errorMsg);
            Navigation.NavigateTo("/login", forceLoad: true);
        }
        catch (Exception ex)
        {
            await StoreLoginFormState();
            _isSubmitting = false;
            await StoreErrorInSessionStorage($"An error occurred: {ex.Message}");
            Navigation.NavigateTo("/login", forceLoad: true);
        }
    }

    private async Task StoreErrorInSessionStorage(string error)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", LoginErrorKey, error);
        }
        catch (JSException)
        {
        }
    }

    private async Task StoreLoginFormState()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", LoginEmailKey, _loginModel.Email ?? string.Empty);
        }
        catch (JSException)
        {
        }
    }

    private async Task OnEmailChanged(string newValue)
    {
        var current = newValue ?? string.Empty;

        if (!string.Equals(current, _previousEmail ?? string.Empty, StringComparison.Ordinal))
        {
            _previousEmail = current;
            _loginModel.Email = current;
            await ClearLoginError();
        }
    }

    private async Task OnPasswordChanged(string newValue)
    {
        var current = newValue ?? string.Empty;

        if (!string.Equals(current, _previousPassword ?? string.Empty, StringComparison.Ordinal))
        {
            _previousPassword = current;
            _loginModel.Password = current;
            await ClearLoginError();
        }
    }

    private async Task ClearLoginError()
    {
        if (string.IsNullOrEmpty(_errorMessage))
        {
            return;
        }

        _errorMessage = string.Empty;

        try
        {
            await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", LoginErrorKey);
        }
        catch (JSException)
        {
        }

        StateHasChanged();
    }

    private void TogglePasswordVisibility()
    {
        showPassword = !showPassword;
    }

    private class LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}

