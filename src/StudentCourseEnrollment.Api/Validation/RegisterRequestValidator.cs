using FluentValidation;
using StudentCourseEnrollment.Shared.DTOs.Auth;

namespace StudentCourseEnrollment.Api.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MinimumLength(2);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MinimumLength(2);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6);
    }
}

