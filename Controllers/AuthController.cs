using laoyu_blog_backend.Dtos.Auth;
using laoyu_blog_backend.Models;
using laoyu_blog_backend.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace laoyu_blog_backend.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(LoginResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(
            dto.Email.Trim());

        if (user is null
            || !await _userManager.CheckPasswordAsync(
                user,
                dto.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid email or password.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var token = _jwtTokenService.CreateToken(
            user,
            roles);

        return Ok(new LoginResponseDto(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc));
    }
}