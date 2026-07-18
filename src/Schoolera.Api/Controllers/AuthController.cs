using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Schoolera.Api.Resources;
using Schoolera.Application.Auth.Commands.ForgotPassword;
using Schoolera.Application.Auth.Commands.Login;
using Schoolera.Application.Auth.Commands.Logout;
using Schoolera.Application.Auth.Commands.RegisterParent;
using Schoolera.Application.Auth.Commands.RegisterSchoolOwner;
using Schoolera.Application.Auth.Commands.ResendVerification;
using Schoolera.Application.Auth.Commands.ResetPassword;
using Schoolera.Application.Auth.Commands.Verify;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Auth.Queries.GetCurrentUser;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(
    ISender mediator,
    IStringLocalizer<AuthMessages> authMessages,
    ILogger<AuthController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpPost("register/parent")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<RegisterResultDto>>> RegisterParent(
        [FromBody] RegisterParentCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("register/school-owner")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<RegisterResultDto>>> RegisterSchoolOwner(
        [FromBody] RegisterSchoolOwnerCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<CurrentUserDto>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting("auth-logout")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MessageResultDto>>> Logout(CancellationToken cancellationToken)
    {
        await Mediator.Send(new LogoutCommand(), cancellationToken);
        return Ok(Result<MessageResultDto>.Success(
            new MessageResultDto(authMessages["LogoutSuccess"].Value)));
    }

    [HttpGet("me")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<CurrentUserDto?>>> Me(CancellationToken cancellationToken)
    {
        // XSRF-TOKEN cookie is written by AntiforgeryCookieMiddleware for SPA clients.
        return FromResult(await Mediator.Send(new GetCurrentUserQuery(), cancellationToken));
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-verify")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MessageResultDto>>> Verify(
        [FromBody] VerifyCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-resend-verification")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MessageResultDto>>> ResendVerification(
        [FromBody] ResendVerificationCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MessageResultDto>>> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MessageResultDto>>> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }
}
