using FluentValidation;
using StudentCourseEnrollment.Shared.DTOs.Enrollments;

namespace StudentCourseEnrollment.Api.Validation;

public class DeregisterRequestValidator : AbstractValidator<DeregisterRequest>
{
    public DeregisterRequestValidator()
    {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty();
    }
}

