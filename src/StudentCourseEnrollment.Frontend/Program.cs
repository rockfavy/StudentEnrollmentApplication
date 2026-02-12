using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StudentCourseEnrollment.Frontend;
using StudentCourseEnrollment.Frontend.Authorization;
using StudentCourseEnrollment.Frontend.Clients;
using StudentCourseEnrollment.Frontend.Clients.Interfaces;
using StudentCourseEnrollment.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddSingleton<ToastService>();

var apiBaseAddress = builder.Configuration["ApiBaseAddress"];

if (string.IsNullOrEmpty(apiBaseAddress) && builder.HostEnvironment.IsDevelopment())
{
    apiBaseAddress = "http://localhost:49541";
}

if (string.IsNullOrEmpty(apiBaseAddress))
{
    throw new InvalidOperationException($"ApiBaseAddress is not configured for environment '{builder.HostEnvironment.Environment}'.");
}

if (!apiBaseAddress.EndsWith("/"))
{
    apiBaseAddress += "/";
}

builder.Services.AddHttpClient<IAuthClient, AuthClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
})
.AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddHttpClient<ICoursesClient, CoursesClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
})
.AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddHttpClient<IEnrollmentsClient, EnrollmentsClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
})
.AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddScoped<AuthorizationHandler>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
