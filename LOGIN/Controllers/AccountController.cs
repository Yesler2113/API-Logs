﻿using LOGIN.Dtos.RolDTOs;
using LOGIN.Dtos.UserDTOs;
using LOGIN.Entities;
using LOGIN.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using LOGIN.Dtos;

[Route("api/account")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IUserService userService,
        IEmailService emailService,
        IConfiguration configuration,
        UserManager<UserEntity> userManager,
        SignInManager<UserEntity> signInManager,
        ILogger<AccountController> logger)
    {
        _userService = userService;
        _emailService = emailService;
        _configuration = configuration;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ResponseDto<CreateUserDto>>> Register([FromBody] CreateUserDto model)
    {
        if (ModelState.IsValid)
        {
            var response = await _userService.RegisterUserAsync(model);

            if (response.Status)
            {
                return Ok(new { Message = response.Message });
            }

            return BadRequest(new { Message = response.Message, Errors = response.Data.Errors });
        }

        return BadRequest(ModelState);
    }

    [HttpPost("login")]
    public async Task<ActionResult<ResponseDto<LoginResponseDto>>> Login([FromBody] LoginDto model)
    {
        var authResponse = await _userService.LoginUserAsync(model);

        //if (authResponse.Status)
        //{
        //    await _logsService.LogLoginAsync(dto.Email);
        //}

        return StatusCode(authResponse.StatusCode, authResponse);
    }


    [HttpPost("create-role")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userService.CreateRoleAsync(model);

        if (result.Succeeded)
        {
            return Ok(new { Result = "Role created successfully" });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("generate-password-reset-token")]
    public async Task<IActionResult> GeneratePasswordResetToken([FromBody] ForgotPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var token = await _userService.GeneratePasswordResetTokenAsync(model.Email);

        if (token == null)
        {
            return BadRequest("User not found");
        }

        await _emailService.SendEmailAsync(model.Email, "Reset Password", $"Your password reset token is: {token}");

        return Ok(new { Result = "Password reset token sent" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || user.PasswordResetToken != model.Token || user.PasswordResetTokenExpires < DateTime.UtcNow)
        {
            return BadRequest(new { code = "InvalidToken", description = "Invalid or expired token." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user); // Generar token para usar con Identity
        var resetPassResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (!resetPassResult.Succeeded)
        {
            return BadRequest(resetPassResult.Errors);
        }

        // Limpiar el token y la fecha de expiración
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;
        await _userManager.UpdateAsync(user);

        return Ok(new { Result = "Password has been reset" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userName = User.Identity.Name;
        var user = await _userManager.FindByNameAsync(userName);

        if (user == null)
        {
            return NotFound("User not found");
        }

        var userDto = new
        {
            user.UserName,
            user.Email,
            user.FirstName,
            user.LastName,
        };

        return Ok(userDto);
    }

    private string GenerateJwtToken(UserEntity user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName)
        };

        var userRoles = _userManager.GetRolesAsync(user).Result;
        foreach (var role in userRoles)
        {
            claims = claims.Append(new Claim(ClaimTypes.Role, role)).ToArray();
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:ValidIssuer"],
            audience: _configuration["JwtSettings:ValidAudience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryMinutes"])),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}