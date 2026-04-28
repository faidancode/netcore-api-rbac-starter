using FluentValidation.TestHelper;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Employees.Dtos;
using netcore_api_rbac_starter.Modules.Employees.Validators;

namespace netcore_api_rbac_starter.Tests.Unit.Employees;

public class EmployeesValidatorTests
{
    private readonly CreateEmployeeRequestValidator _createValidator = new();
    private readonly UpdateEmployeeRequestValidator _updateValidator = new();

    [Fact]
    public void Create_ValidRequest_PassesValidation()
    {
        var req = new CreateEmployeeRequest(
            FullName: "John Doe",
            Nip: "EMP-001",
            Gender: Gender.Male,
            PositionId: Guid.NewGuid(),
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: null
        );

        var result = _createValidator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Create_EmptyFullName_FailsValidation()
    {
        var req = new CreateEmployeeRequest(
            FullName: "",
            Nip: "EMP-001",
            Gender: Gender.Male,
            PositionId: Guid.NewGuid(),
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: null
        );

        var result = _createValidator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Create_TooLongFullName_FailsValidation()
    {
        var name = new string('A', 301);
        var req = new CreateEmployeeRequest(
            FullName: name,
            Nip: "EMP-001",
            Gender: Gender.Male,
            PositionId: Guid.NewGuid(),
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: null
        );

        var result = _createValidator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Update_TooLongNip_FailsValidation()
    {
        var nip = new string('N', 51);
        var req = new UpdateEmployeeRequest(
            FullName: null,
            Nip: nip,
            Gender: null,
            PositionId: null,
            DateOfJoining: null,
            DateOfActivePosition: null,
            EmployeeStatus: null,
            IsActive: null,
            UserId: null,
            DepartmentId: null,
            ManagerId: null
        );

        var result = _updateValidator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Nip);
    }

    [Fact]
    public void Update_ValidRequest_PassesValidation()
    {
        var req = new UpdateEmployeeRequest(
            FullName: "Jane Doe",
            Nip: null,
            Gender: null,
            PositionId: null,
            DateOfJoining: null,
            DateOfActivePosition: null,
            EmployeeStatus: null,
            IsActive: null,
            UserId: null,
            DepartmentId: null,
            ManagerId: null
        );

        var result = _updateValidator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
