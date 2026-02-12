using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using StudentCourseEnrollment.Frontend.Services;

namespace StudentCourseEnrollment.Frontend.Authorization;

public class AuthorizationHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly NavigationManager _navigationManager;

    public AuthorizationHandler(
        AuthenticationStateProvider authenticationStateProvider,
        NavigationManager navigationManager)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_authenticationStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            var token = await customProvider.GetAccessTokenAsync();
            
            if (!string.IsNullOrWhiteSpace(token) && !request.Headers.Contains("Authorization"))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            if (_authenticationStateProvider is CustomAuthenticationStateProvider unauthorizedProvider)
            {
                await unauthorizedProvider.ClearAuthenticationStateAsync();
                _navigationManager.NavigateTo("/login", forceLoad: true);
            }
        }

        return response;
    }
}
