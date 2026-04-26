using FluentValidation;
using netcore_api_rbac_starter.Modules.Departments.Dtos;

namespace netcore_api_rbac_starter.Modules.Departments.Validators;

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name)
     .NotEmpty().WithMessage("Department name is required.")
     .MinimumLength(3).WithMessage("Name must be at least 3 characters.")
     .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name)
    .MinimumLength(3).WithMessage("Name must be at least 3 characters.")
    .MaximumLength(200).WithMessage("Name must not exceed 200 characters.")
    .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}