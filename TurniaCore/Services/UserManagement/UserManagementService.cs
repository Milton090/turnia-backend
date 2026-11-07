using TurniaCore.Data;
using TurniaCore.Data.Dto.UserManagement;
using TurniaCore.Data.Entities.Platform.User;
using TurniaCore.Entities.Platform.User;
using TurniaCore.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.String;

namespace TurniaCore.Services.UserManagement
{
    public class UserManagementService
    {
        private readonly UserAccountService _userAccountService;
        private readonly UserManager<QueueUserProfile> _userManager;
        private readonly QueueContext _queueContext;
        private readonly RoleManager<QueueUserRole> _roleManager;

        public UserManagementService(UserAccountService userAccountService, UserManager<QueueUserProfile> userManager, QueueContext queueContext, RoleManager<QueueUserRole> roleManager)
        {
            _userAccountService = userAccountService;
            _userManager = userManager;
            _queueContext = queueContext;
            _roleManager = roleManager;
        }

        public async Task<(bool, dynamic)> ChangeUserPassword(ChangeUserPassword model)
        {
            try
            {
                var myUser = await _userAccountService.GetUserByEmail(model.UserName);
                if (myUser == null)
                {
                    return (false, "No se pudo obtener el perfil del usuario");
                }

                var passwordValidation = await ValidatePassword(model.NewPassword);

                if (!passwordValidation.Item1)
                {
                    return (false, passwordValidation.Item2!);
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

        public async Task<(bool, string)> ToggleUserActivation(ToggleUserActivation model)
        {
            try
            {
                var user = await _userAccountService.GetUserById(model.UserId);

                if (user == null)
                {
                    return (false, "El usuario no existe");
                }

                user.Active = model.Activate;

                var result = await _queueContext.SaveChangesAsync();

                if (result > 0)
                {
                    return (true, "Se actualizó el estado del usuario");
                }

                return (false, "No se pudo cambiar el estado del usuario");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<bool> EditUser(EditUser model)
        {
            try
            {
                var user = await _userAccountService.GetUserById(model.UserId);

                if (user == null)
                {
                    return false;
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.UserName;
                user.UserName = model.UserName;
                user.NormalizedEmail = model.UserName.ToUpper();
                user.NormalizedUserName = model.UserName.ToUpper();

                await AssignCounterAsync(user, model.AssignedCounterId);

                var result = await _queueContext.SaveChangesAsync();

                if (result > 0)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool, dynamic)> CreateUserAsync(string userName, string? password, string firstName, string lastName, bool isActive, string UserRole, Guid? assignedCounterId)
        {
            try
            {
                var myUser = await _userAccountService.GetMyUserProfileAsync();
                if (myUser == null)
                {
                    return (false, "No se pudo obtener el perfil del usuario");
                }

                if (IsNullOrWhiteSpace(userName) || IsNullOrWhiteSpace(firstName) || IsNullOrWhiteSpace(lastName))
                {
                    return (false, $"Complete todos los campos obligatorios");
                }

                var userExists = await _queueContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.NormalizedUserName == userName.ToUpperInvariant());

                if (userExists != null)
                {
                    return (false, $"El usuario {userName} ya existe");
                }

                var newUser = new QueueUserProfile()
                {
                    UserName = userName,
                    Email = userName,
                    FirstName = firstName,
                    LastName = lastName,
                    Active = isActive,
                    Created = DateTime.Now
                };

                if (UserRole == null)
                {
                    return (false, $"Rol inválido {UserRole}");
                }

                var role = await _roleManager.FindByNameAsync(UserRole);

                if (role == null)
                {
                    return (false, $"Rol inválido {UserRole}");
                }

                if (IsNullOrWhiteSpace(password))
                {
                    return (false, "La contraseña es obligatoria");
                }

                var passwordValidation = await ValidatePassword(password);

                if (!passwordValidation.Item1)
                {
                    return (false, passwordValidation.Item2!);
                }

                await AssignCounterAsync(newUser, assignedCounterId);

                IdentityResult userCreateResult = await _userManager.CreateAsync(newUser, password);


                if (userCreateResult.Succeeded)
                {
                    string text = $"Usuario {newUser.UserName} creado por {myUser.UserName}";

                    var userFromDb = await _userManager.FindByEmailAsync(newUser.Email);
                    if (userFromDb == null)
                    {
                        return (false, $"No se pudo obtener el usuario para asignarle el rol");
                    }

                    await _userManager.AddToRoleAsync(userFromDb, role.Name!);

                    return (true, text);
                }

                var textError = Join(", ", userCreateResult.Errors.Select(s => s.Description));
                if (IsNullOrWhiteSpace(textError))
                {
                    textError = "Error al crear el usuario";
                }

                return (false, textError);

            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        private async Task AssignCounterAsync(QueueUserProfile user, Guid? counterId)
        {
            if (counterId != null)
            {
                var usersWithCounter = await _queueContext.Users
                    .Where(u => u.AssignedCounterId == counterId && u.Id != user.Id)
                    .ToListAsync();

                foreach (var userWithCounter in usersWithCounter)
                {
                    userWithCounter.AssignedCounterId = null;
                }
            }

            user.AssignedCounterId = counterId;
        }

        public async Task<(bool, List<string>?)> ValidatePassword(string password)
        {
            try
            {
                List<string> errors = new List<string>();
                var validators = _userManager.PasswordValidators;
                foreach (var validator in validators)
                {
                    var result = await validator.ValidateAsync(_userManager, null!, password);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            errors.Add(error.Description);
                        }
                        return (false, errors);
                    }
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, new List<string>() { ex.Message });
            }
        }


        public async Task<(QueueUserProfile, string)> GetUserDetail(Guid userId)
        {
            try
            {
                var loggedInUser = await _userAccountService.GetMyUserProfileAsync();

                if (loggedInUser == null)
                {
                    return (null!, "Usuario no autenticado");
                }

                var user = await _queueContext.Users
                    .Where(x => x.Id == userId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return (null!, "El usuario no existe");
                }

                return (user, "Success");
            }
            catch (Exception e)
            {
                return (null!, e.Message);
            }
        }

        public async Task<(bool, dynamic)> AddUserRoles(Role model)
        {
            try
            {
                var user = await _userAccountService.GetUserById(model.UserId);

                if (user == null)
                {
                    return (false, $"El usuario {model.UserId} no existe");
                }

                var roleList = new List<string>();

                foreach (var role in model.RoleName)
                {
                    var result = await _userManager.IsInRoleAsync(user, role);

                    if (!result)
                    {
                        var results = await _userManager.AddToRoleAsync(user, role);

                        if (!results.Succeeded)
                        {
                            roleList.Add(role);
                        }
                    }
                }

                if (roleList.Count > 0)
                {
                    return (true, $"El usuario ya tiene estos roles: {roleList}");
                }

                return (true, $"Roles agregados");

            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        public async Task<(bool, dynamic)> RemoveUserRoles(Role model)
        {
            try
            {
                var user = await _userAccountService.GetUserById(model.UserId);

                if (user == null)
                {
                    return (false, $"El usuario {model.UserId} no existe");
                }

                var roleList = new List<string>();

                foreach (var role in model.RoleName)
                {
                    var result = await _userManager.IsInRoleAsync(user, role);

                    if (result)
                    {
                        var results = await _userManager.RemoveFromRoleAsync(user, role);

                        if (!results.Succeeded)
                        {
                            roleList.Add(role);
                        }
                    }
                }

                if (roleList.Count > 0)
                {
                    return (true, $"El usuario no tiene estos roles: {roleList}");
                }

                return (true, $"Roles quitados");

            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }
    }
}
