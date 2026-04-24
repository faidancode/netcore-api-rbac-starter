using FluentAssertions;
using FluentValidation.TestHelper;
using netcore_api_rbac_starter.Modules.Auth.Dtos;
using netcore_api_rbac_starter.Modules.Auth.Validators;

namespace netcore_api_rbac_starter.Tests.Unit.Auth;

public class AuthValidatorTests
{
    // ─── LoginRequestValidator ────────────────────────────────────────────────

    private readonly LoginRequestValidator _loginValidator = new();

    [Fact]
    public void Login_ValidRequest_PassesValidation()
    {
        var result = _loginValidator.TestValidate(new LoginRequest("test@example.com", "password123"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    public void Login_InvalidEmail_FailsValidation(string email)
    {
        var result = _loginValidator.TestValidate(new LoginRequest(email, "password123"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]   // too short (< 6)
    public void Login_InvalidPassword_FailsValidation(string password)
    {
        var result = _loginValidator.TestValidate(new LoginRequest("test@example.com", password));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    // ─── RefreshRequestValidator ──────────────────────────────────────────────

    private readonly RefreshRequestValidator _refreshValidator = new();

    [Fact]
    public void Refresh_ValidToken_PassesValidation()
    {
        var result = _refreshValidator.TestValidate(new RefreshRequest("some-valid-token"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Refresh_EmptyToken_FailsValidation()
    {
        var result = _refreshValidator.TestValidate(new RefreshRequest(""));
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}