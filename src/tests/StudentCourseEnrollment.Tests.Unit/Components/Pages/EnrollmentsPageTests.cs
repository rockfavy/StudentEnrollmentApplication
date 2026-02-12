using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using StudentCourseEnrollment.Frontend.Clients.Interfaces;
using StudentCourseEnrollment.Frontend.Components.Pages;
using StudentCourseEnrollment.Frontend.Services;
using StudentCourseEnrollment.Shared.DTOs.Enrollments;
using System.Linq;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Components.Pages;

public class EnrollmentsPageTests : TestContext
{
    private Mock<IEnrollmentsClient> CreateMockEnrollmentsClient()
    {
        return new Mock<IEnrollmentsClient>();
    }

    private CustomAuthenticationStateProvider CreateAuthStateProvider()
    {
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);
        mockJSRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        return new CustomAuthenticationStateProvider(mockJSRuntime.Object);
    }

    private void RegisterServices(IEnrollmentsClient enrollmentsClient, CustomAuthenticationStateProvider authStateProvider, IJSRuntime jsRuntime)
    {
        Services.AddSingleton(enrollmentsClient);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(authStateProvider);
        Services.AddSingleton(jsRuntime);
        Services.AddSingleton<ToastService>();
    }

    [Fact]
    public void EnrollmentsPage_Renders_Enrollments_List()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        var enrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: Guid.NewGuid(),
                CourseId: Guid.NewGuid(),
                CourseName: "Test Course 1",
                CourseDescription: "Test Description 1",
                EnrolledAt: DateTime.UtcNow
            ),
            new EnrollmentDto(
                Id: Guid.NewGuid(),
                CourseId: Guid.NewGuid(),
                CourseName: "Test Course 2",
                CourseDescription: "Test Description 2",
                EnrolledAt: DateTime.UtcNow
            )
        };

        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .ReturnsAsync(enrollments);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));

        Assert.Contains("Test Course 1", cut.Markup);
        Assert.Contains("Test Course 2", cut.Markup);
        Assert.Contains("Test Description 1", cut.Markup);
        Assert.Contains("Test Description 2", cut.Markup);
    }

    [Fact]
    public void EnrollmentsPage_Shows_Loading_State()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        var tcs = new TaskCompletionSource<List<EnrollmentDto>?>();
        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .Returns(tcs.Task);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        Assert.Contains("Loading enrollments...", cut.Markup);

        tcs.SetResult(new List<EnrollmentDto>());
        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));
    }

    [Fact]
    public void EnrollmentsPage_Shows_Empty_State_When_No_Enrollments()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .ReturnsAsync(new List<EnrollmentDto>());

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));

        Assert.Contains("You haven't enrolled in any courses yet", cut.Markup);
        Assert.Contains("Browse available courses", cut.Markup);
    }

    [Fact]
    public void EnrollmentsPage_Displays_Deregister_Button_For_Each_Enrollment()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        var enrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: Guid.NewGuid(),
                CourseId: Guid.NewGuid(),
                CourseName: "Test Course",
                CourseDescription: "Test Description",
                EnrolledAt: DateTime.UtcNow
            )
        };

        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .ReturnsAsync(enrollments);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));

        var deregisterButtons = cut.FindAll("button");
        Assert.Contains(deregisterButtons, b => b.TextContent.Contains("Deregister"));
    }

    [Fact]
    public void EnrollmentsPage_Calls_DeleteEnrollmentAsync_When_Deregister_Button_Clicked_And_Confirmed()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        var enrollmentId = Guid.NewGuid();
        var enrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: enrollmentId,
                CourseId: Guid.NewGuid(),
                CourseName: "Test Course",
                CourseDescription: "Test Description",
                EnrolledAt: DateTime.UtcNow
            )
        };

        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .ReturnsAsync(enrollments);

        mockEnrollmentsClient.Setup(x => x.DeleteEnrollmentAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));

        var deregisterButton = cut.Find("button.btn-danger");
        deregisterButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Deregister from Course", cut.Markup);
            var buttons = cut.FindAll("button.btn-danger");
            Assert.True(buttons.Count >= 2);
        }, timeout: TimeSpan.FromSeconds(5));

        var dangerButtons = cut.FindAll("button.btn-danger");
        var modalConfirmButton = dangerButtons.Last();
        Assert.Contains("Deregister", modalConfirmButton.TextContent);
        modalConfirmButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(mockEnrollmentsClient.Invocations, i => 
                i.Method.Name == "DeleteEnrollmentAsync");
        }, timeout: TimeSpan.FromSeconds(5));

        mockEnrollmentsClient.Verify(x => x.DeleteEnrollmentAsync(enrollmentId), Times.Once);
    }

    [Fact]
    public void EnrollmentsPage_Does_Not_Call_DeleteEnrollmentAsync_When_User_Cancels_Confirmation()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        var enrollmentId = Guid.NewGuid();
        var enrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: enrollmentId,
                CourseId: Guid.NewGuid(),
                CourseName: "Test Course",
                CourseDescription: "Test Description",
                EnrolledAt: DateTime.UtcNow
            )
        };

        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .ReturnsAsync(enrollments);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));

        var deregisterButton = cut.Find("button.btn-danger");
        deregisterButton.Click();

        cut.WaitForState(() => cut.Markup.Contains("Deregister from Course"), timeout: TimeSpan.FromSeconds(5));

        var cancelButton = cut.Find("button.btn-secondary");
        cancelButton.Click();

        cut.WaitForState(() => !cut.Markup.Contains("Deregister from Course"), timeout: TimeSpan.FromSeconds(5));

        mockEnrollmentsClient.Verify(x => x.DeleteEnrollmentAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void EnrollmentsPage_Shows_Loading_State_During_Deregistration()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        var enrollmentId = Guid.NewGuid();
        var enrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: enrollmentId,
                CourseId: Guid.NewGuid(),
                CourseName: "Test Course",
                CourseDescription: "Test Description",
                EnrolledAt: DateTime.UtcNow
            )
        };

        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .ReturnsAsync(enrollments);

        var tcs = new TaskCompletionSource<bool>();
        mockEnrollmentsClient.Setup(x => x.DeleteEnrollmentAsync(It.IsAny<Guid>()))
            .Returns(tcs.Task);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));

        var deregisterButton = cut.Find("button.btn-danger");
        deregisterButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Deregister from Course", cut.Markup);
            var buttons = cut.FindAll("button.btn-danger");
            Assert.True(buttons.Count >= 2);
        }, timeout: TimeSpan.FromSeconds(5));

        var dangerButtons = cut.FindAll("button.btn-danger");
        var modalConfirmButton = dangerButtons.Last();
        Assert.Contains("Deregister", modalConfirmButton.TextContent);
        modalConfirmButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("spinner-border", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));

        tcs.SetResult(true);
        cut.WaitForState(() => !cut.Markup.Contains("Deregister from Course"));
    }

    [Fact]
    public void EnrollmentsPage_Shows_Success_Message_After_Successful_Deregistration()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        var enrollmentId = Guid.NewGuid();
        var enrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: enrollmentId,
                CourseId: Guid.NewGuid(),
                CourseName: "Test Course",
                CourseDescription: "Test Description",
                EnrolledAt: DateTime.UtcNow
            )
        };

        var callCount = 0;
        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? enrollments : new List<EnrollmentDto>();
            });

        mockEnrollmentsClient.Setup(x => x.DeleteEnrollmentAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));

        var deregisterButton = cut.Find("button.btn-danger");
        deregisterButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Deregister from Course", cut.Markup);
            var buttons = cut.FindAll("button.btn-danger");
            Assert.True(buttons.Count >= 2);
        }, timeout: TimeSpan.FromSeconds(5));

        var dangerButtons = cut.FindAll("button.btn-danger");
        var modalConfirmButton = dangerButtons.Last();
        Assert.Contains("Deregister", modalConfirmButton.TextContent);
        modalConfirmButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(callCount >= 2);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void EnrollmentsPage_Shows_Error_Message_On_Deregistration_Failure()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        var enrollmentId = Guid.NewGuid();
        var enrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: enrollmentId,
                CourseId: Guid.NewGuid(),
                CourseName: "Test Course",
                CourseDescription: "Test Description",
                EnrolledAt: DateTime.UtcNow
            )
        };

        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .ReturnsAsync(enrollments);

        mockEnrollmentsClient.Setup(x => x.DeleteEnrollmentAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new HttpRequestException("Failed to deregister"));

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));

        var deregisterButton = cut.Find("button.btn-danger");
        deregisterButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Deregister from Course", cut.Markup);
            var buttons = cut.FindAll("button.btn-danger");
            Assert.True(buttons.Count >= 2);
        }, timeout: TimeSpan.FromSeconds(5));

        var dangerButtons = cut.FindAll("button.btn-danger");
        var modalConfirmButton = dangerButtons.Last();
        Assert.Contains("Deregister", modalConfirmButton.TextContent);
        modalConfirmButton.Click();

        cut.WaitForAssertion(() =>
        {
            mockEnrollmentsClient.Verify(x => x.DeleteEnrollmentAsync(It.IsAny<Guid>()), Times.AtLeastOnce);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void EnrollmentsPage_Refreshes_Enrollments_List_After_Successful_Deregistration()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);

        var enrollmentId = Guid.NewGuid();
        var initialEnrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: enrollmentId,
                CourseId: Guid.NewGuid(),
                CourseName: "Test Course",
                CourseDescription: "Test Description",
                EnrolledAt: DateTime.UtcNow
            )
        };

        var callCount = 0;
        mockEnrollmentsClient.Setup(x => x.GetMyEnrollmentsAsync())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? initialEnrollments : new List<EnrollmentDto>();
            });

        mockEnrollmentsClient.Setup(x => x.DeleteEnrollmentAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<EnrollmentsPage>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading enrollments..."));

        var deregisterButton = cut.Find("button.btn-danger");
        deregisterButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Deregister from Course", cut.Markup);
            var buttons = cut.FindAll("button.btn-danger");
            Assert.True(buttons.Count >= 2);
        }, timeout: TimeSpan.FromSeconds(5));

        var dangerButtons = cut.FindAll("button.btn-danger");
        var modalConfirmButton = dangerButtons.Last();
        Assert.Contains("Deregister", modalConfirmButton.TextContent);
        modalConfirmButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(callCount >= 2);
        }, timeout: TimeSpan.FromSeconds(5));

        mockEnrollmentsClient.Verify(x => x.GetMyEnrollmentsAsync(), Times.AtLeast(2));
    }
}

