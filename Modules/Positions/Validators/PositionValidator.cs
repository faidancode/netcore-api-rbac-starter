using FluentValidation;
using netcore_api_rbac_starter.Modules.Positions.Dtos;

namespace netcore_api_rbac_starter.Modules.Positions.Validators;

public class CreatePositionRequestValidator : AbstractValidator<CreatePositionRequest>
{
    public CreatePositionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Position name is required.")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("DepartmentId is required.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.")
            .When(x => x.Description != null);
    }
}

public class UpdatePositionRequestValidator : AbstractValidator<UpdatePositionRequest>
{
    public UpdatePositionRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(3).WithMessage("Name must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.")
            .When(x => x.Description != null);
    }
}

public class PositionListQueryValidator : AbstractValidator<ListPositionQuery>
{
    public PositionListQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.Limit).ValidLimit();

        RuleFor(x => x.Sort)
            .Matches(@"^[a-zA-Z]+:(asc|desc)$")
            .When(x => !string.IsNullOrEmpty(x.Sort));
    }
}
