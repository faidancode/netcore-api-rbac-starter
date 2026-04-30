using FluentValidation;
using netcore_api_rbac_starter.Modules.Roles.Dtos;

namespace netcore_api_rbac_starter.Modules.Roles.Validators;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("PermissionIds must not be null.");
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name cannot be empty.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("PermissionIds must not be null.");
    }
}

public class AssignPermissionsRequestValidator : AbstractValidator<AssignPermissionsRequest>
{
    public AssignPermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("PermissionIds must not be null.");
    }
}
