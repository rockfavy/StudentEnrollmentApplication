using FluentValidation;
using StudentCourseEnrollment.Shared.DTOs.Courses;

namespace StudentCourseEnrollment.Api.Validation;

public class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2);

        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 1000);
    }
}

