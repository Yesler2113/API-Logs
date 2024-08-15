using LOGIN.Dtos;
using LOGIN.Dtos.RolDTOs;
using LOGIN.Dtos.UserDTOs;
using LOGIN.Entities;
using LOGIN.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

public class UserService : IUserService
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<UserService> _logger;

    //para saber el usuario que esta logueado
    private readonly HttpContext _httpContext;
    private readonly string _USER_ID;

    public UserService(
        UserManager<UserEntity> userManager,
        SignInManager<UserEntity> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IEmailService emailService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<UserService> logger,
        IHttpContextAccessor httpContextAccessor
        )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _emailService = emailService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _httpContext = httpContextAccessor.HttpContext;
        var idClaim = _httpContext.User.Claims.Where(x => x.Type == "UserId")
            .FirstOrDefault();
        _USER_ID = idClaim?.Value;
    }

    public async Task<ResponseDto<IdentityResult>> RegisterUserAsync(CreateUserDto userDto)
    {
        var user = new UserEntity
        {
            UserName = userDto.UserName,
            Email = userDto.Email,
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            CreatedDate = DateTime.UtcNow
        };

        // Verifica si el email ya está registrado
        if (await _userManager.FindByEmailAsync(userDto.Email) != null)
        {
            return new ResponseDto<IdentityResult>
            {
                StatusCode = 400,
                Status = false,
                Message = "Email is already registered.",
                Data = IdentityResult.Failed(new IdentityError { Description = "Email is already registered." })
            };
        }

        var result = await _userManager.CreateAsync(user, userDto.Password);

        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole { Name = "User" });
            }
            await _userManager.AddToRoleAsync(user, "User");

            return new ResponseDto<IdentityResult>
            {
                StatusCode = 201,
                Status = true,
                Message = "User registered successfully.",
                Data = result
            };
        }

        return new ResponseDto<IdentityResult>
        {
            StatusCode = 400,
            Status = false,
            Message = "User registration failed.",
            Data = result
        };
    }

    public async Task<ResponseDto<LoginResponseDto>> LoginUserAsync(LoginDto dto)
    {
        var result = await _signInManager.PasswordSignInAsync(
            dto.UserName,
            dto.Password,
            isPersistent: false,
            lockoutOnFailure: false
        );

        if (result.Succeeded)
        {
            var userEntity = await _userManager.FindByNameAsync(dto.UserName);

            var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userEntity.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("UserId", userEntity.Id)
        };

            var userRoles = await _userManager.GetRolesAsync(userEntity);
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = GenerateJwtTokenAsync(authClaims);

            return new ResponseDto<LoginResponseDto>
            {
                StatusCode = 200,
                Status = true,
                Message = "Inicio de sesión realizado satisfactoriamente",
                Data = new LoginResponseDto
                {
                    Username = userEntity.UserName,
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    TokenExpiration = token.ValidTo
                }
            };
        }

        return new ResponseDto<LoginResponseDto>
        {
            StatusCode = 400,
            Status = false,
            Message = "Fallo el inicio de sesión"
        };
    }

    public async Task<IdentityResult> CreateRoleAsync(CreateRoleDto roleDto)
    {
        if (!await _roleManager.RoleExistsAsync(roleDto.RoleName))
        {
            var role = new IdentityRole { Name = roleDto.RoleName };
            return await _roleManager.CreateAsync(role);
        }

        return IdentityResult.Failed(new IdentityError { Description = "Role already exists" });
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return null;
        }

        // Generar un código numérico aleatorio de 8 dígitos
        var token = new Random().Next(10000000, 99999999).ToString();

        // Guardar el token y la fecha de expiración en la base de datos
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(15); // El token expira en 15 minutos
        await _userManager.UpdateAsync(user);

        return token;
    }


    public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        return await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
    }


    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email) != null;
    }

    public async Task<bool> CheckUserNameExistsAsync(string userName)
    {
        return await _userManager.FindByNameAsync(userName) != null;
    }

    private JwtSecurityToken GenerateJwtTokenAsync(List<Claim> authClaims)
    {
        //var claims = new[]
        //{
        //    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //    //ERROR AQUI
        //    new Claim("UserId", user.Id),
        //    new Claim("username", user.UserName)
        //}.Union((await _userManager.GetRolesAsync(user)).Select(role => new Claim(ClaimTypes.Role, role)));

        var authSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            expires: DateTime.UtcNow.AddHours(1),
            claims: authClaims,
            signingCredentials: new SigningCredentials(
                authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return token;
    }
}