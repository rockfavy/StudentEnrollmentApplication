using FluentValidation;
using StudentCourseEnrollment.Shared.DTOs.Auth;

namespace StudentCourseEnrollment.Api.Validation;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}

