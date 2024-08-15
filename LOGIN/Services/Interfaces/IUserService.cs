﻿using LOGIN.Dtos;
using LOGIN.Dtos.RolDTOs;
using LOGIN.Dtos.UserDTOs;
using Microsoft.AspNetCore.Identity;

namespace LOGIN.Services.Interfaces
{
    public interface IUserService
    {
        Task<bool> CheckEmailExistsAsync(string email);
        Task<bool> CheckUserNameExistsAsync(string userName);
        Task<IdentityResult> CreateRoleAsync(CreateRoleDto roleDto);
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<ResponseDto<LoginResponseDto>> LoginUserAsync(LoginDto dto);
        Task<ResponseDto<IdentityResult>> RegisterUserAsync(CreateUserDto userDto);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    }
}