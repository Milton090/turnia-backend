#nullable enable
using System.Security.Claims;
using TurniaCore.Data;
using TurniaCore.Data.Dto.Authentication;
using TurniaCore.Data.Dto.User;
using TurniaCore.Data.Entities.App.Queue;
using TurniaCore.Entities.Platform.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.String;

namespace TurniaCore.Services.User
{
    public class UserAccountService
    {
        private readonly QueueContext _context;
        private readonly UserManager<QueueUserProfile> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserAccountService(QueueContext context, UserManager<QueueUserProfile> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        #region User - Utility functions
        public async Task<string?> GetCurrentUserNameAsync()
        {
            try
            {
                var httpContextUser = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
                return await Task.Run(() => httpContextUser);
            }
            catch (Exception e)
            {
                if (e.InnerException != null) Console.WriteLine($"Get Current username failed: {e.InnerException.Message}");
            }

            return Empty;
        }
        public async Task<QueueUserProfile?> GetMyUserProfileAsync()
        {
            try
            {
                var httpContextUser = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
                if (httpContextUser == null)
                {
                    return null;
                }

                var identityUser = await _context
                    .Users
                    .Include(x => x.AssignedCounter)
                    .Where(u => u.Email == httpContextUser)
                    .FirstOrDefaultAsync();
                return identityUser;
            }
            catch (Exception e)
            {
                if (e.InnerException != null) Console.WriteLine(e.InnerException.Message);
                return null;
            }
        }
        public async Task<QueueUserProfile?> GetUserByEmail(string email)
        {
            try
            {
                if (IsNullOrWhiteSpace(email))
                {
                    return null;
                }

                var emailUpperCase = email.ToUpperInvariant();

                return await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == emailUpperCase);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                }

                throw;
            }
        }
        #endregion

        public async Task<List<QueueUserProfile>> GetUsers(string? searchTerm)
        {
            try
            {
                var query = _context.Users
                    .AsQueryable();

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await query.ToListAsync();
                }

                query = query
                    .Where(t =>
                        EF.Functions.ILike(t.UserName!, $"%{searchTerm}%") ||
                        EF.Functions.ILike(t.NormalizedEmail!, $"%{searchTerm}%") ||
                        EF.Functions.ILike(t.FirstName, $"%{searchTerm}%") ||
                        EF.Functions.ILike(t.LastName, $"%{searchTerm}%")
                )
                    .AsQueryable();
                return await query.ToListAsync();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<(bool, string)> ChangePassword(ChangePasswordDto model)
        {
            try
            {
                var myUser = await GetMyUserProfileAsync();
                if (myUser == null)
                {
                    return (false, "No se pudo obtener el perfil del usuario");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(myUser);
                var result = await _userManager.ResetPasswordAsync(myUser, token, model.NewPassword);

                if (result.Succeeded)
                {
                    return (true, "Contraseña actualizada");
                }

                return (false, "Error al cambiar la contraseña.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<List<UserDetailDto>> ReturnUserRoles(List<UserDetailDto>? users)
        {
            if (users == null)
            {
                return null!;
            }

            foreach (var user in users)
            {
                var roles = await _context.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();

                foreach (var role in roles)
                {
                    var roleName = _context.Roles.Where(x => x.Id == role.RoleId).FirstOrDefaultAsync().Result!.Name;
                    user.Role.Add(roleName!);
                }
            }

            return users;
        }

        public async Task<UserDetailDto> ReturnUserRole(UserDetailDto user)
        {
            if (user == null)
            {
                return null!;
            }

            var roles = await _context.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();

            foreach (var role in roles)
            {
                var roleName = _context.Roles.FirstOrDefaultAsync(x => x.Id == role.RoleId).Result!.Name;
                user.Role.Add(roleName!);
            }

            return user;
        }

        public async Task<QueueUserProfile> GetUserById(Guid id)
        {
            try
            {
                var user = await _context.Users
                    .Where(x => x.Id == id)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return null!;
                }

                return user;
            }
            catch
            {
                return null!;
            }
        }


        public async Task<List<Service>> GetServicesPerCounter(Guid? counterId)
        {
            try
            {
                var services = await _context.Services
                                .Where(s => s.Counters.Any(c => c.Id == counterId))
                                .ToListAsync();

                return services;
            }
            catch
            {
                return null!;
            }
        }
    }
}
