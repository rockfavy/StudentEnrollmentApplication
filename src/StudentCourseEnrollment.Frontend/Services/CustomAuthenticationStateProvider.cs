using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using StudentCourseEnrollment.Frontend.Helpers;

namespace StudentCourseEnrollment.Frontend.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private string? _accessToken;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";
    private const string UserKey = "authUser";

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser.Identity?.IsAuthenticated != true)
        {
            await LoadAuthenticationStateAsync();
        }
        else
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            if (string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(_accessToken))
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                _accessToken = null;
            }
        }
        
        return new AuthenticationState(_currentUser);
    }

    public async Task SetAuthenticationStateAsync(ClaimsPrincipal user, string? accessToken = null)
    {
        _currentUser = new(new ClaimsIdentity());
        _accessToken = null;
        
        _currentUser = user;
        _accessToken = accessToken;
        
        if (accessToken != null)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, accessToken);
            
            var userJson = JsonSerializer.Serialize(new
            {
                Id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Email = user.FindFirst(ClaimTypes.Email)?.Value,
                Name = user.FindFirst(ClaimTypes.Name)?.Value
            });
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserKey, userJson);
        }
        
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task ClearAuthenticationStateAsync()
    {
        _currentUser = new(new ClaimsIdentity());
        _accessToken = null;
        
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserKey);
        
        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.clear");
        }
        catch { }
        
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public virtual async Task<string?> GetAccessTokenAsync()
    {
        if (_accessToken != null)
        {
            return _accessToken;
        }

        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                _accessToken = token;
            }
            return _accessToken;
        }
        catch
        {
            return null;
        }
    }

    public virtual string? GetAccessToken()
    {
        return _accessToken;
    }

    private async Task LoadAuthenticationStateAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);

            if (!string.IsNullOrWhiteSpace(token))
            {
                _accessToken = token;
                var claims = JwtTokenHelper.ParseClaimsFromJwt(token);

                if (claims.Any())
                {
                    var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
                    _currentUser = new ClaimsPrincipal(identity);
                }
                else
                {
                    _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }
            else
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }
        }
        catch
        {
            _accessToken = null;
            _currentUser = new(new ClaimsIdentity());
        }
    }
}
