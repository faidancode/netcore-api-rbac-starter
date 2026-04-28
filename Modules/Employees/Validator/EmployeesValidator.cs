using FluentValidation;
using netcore_api_rbac_starter.Modules.Employees.Dtos;

namespace netcore_api_rbac_starter.Modules.Employees.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(300).WithMessage("Full name must not exceed 300 characters.");

        RuleFor(x => x.Nip)
            .NotEmpty().WithMessage("NIP is required.")
            .MaximumLength(50).WithMessage("NIP must not exceed 50 characters.");

        RuleFor(x => x.PositionId)
            .NotEmpty().WithMessage("PositionId is required.");

        RuleFor(x => x.DateOfJoining)
            .NotEmpty().WithMessage("Date of joining is required.");
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(300).WithMessage("Full name must not exceed 300 characters.")
            .When(x => x.FullName != null);

        RuleFor(x => x.Nip)
            .MaximumLength(50).WithMessage("NIP must not exceed 50 characters.")
            .When(x => x.Nip != null);
    }
}