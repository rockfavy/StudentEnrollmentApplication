using FluentValidation;
using StudentCourseEnrollment.Shared.DTOs.Enrollments;

namespace StudentCourseEnrollment.Api.Validation;

public class EnrollRequestValidator : AbstractValidator<EnrollRequest>
{
    public EnrollRequestValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty();
    }
}

