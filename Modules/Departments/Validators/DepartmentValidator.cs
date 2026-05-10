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
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.");
    }
}

public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(3).WithMessage("Name must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.");
    }
}

public class DepartmentListQueryValidator : AbstractValidator<ListDepartmentQuery>
{
    public DepartmentListQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.Limit).ValidLimit();

        RuleFor(x => x.Sort)
            .Matches(@"^[a-zA-Z]+:(asc|desc)$")
            .When(x => !string.IsNullOrWhiteSpace(x.Sort));
    }
}
