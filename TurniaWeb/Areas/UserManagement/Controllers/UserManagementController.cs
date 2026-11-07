using AutoMapper;
using TurniaCore.Data.Constants;
using TurniaCore.Data.Dto.User;
using TurniaCore.Data.Dto.UserManagement;
using TurniaCore.Services.User;
using TurniaCore.Services.UserManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TurniaWeb.Areas.UserManagement.Controllers
{
    [Area("Usermanagement"), ApiController, Route("api/[controller]")]

    public class UserManagementController : Controller
    {
        private readonly UserAccountService _userAccountService;
        private readonly UserManagementService _userManagementService;
        private readonly IMapper _mapper;
        public UserManagementController(UserAccountService userAccountService, UserManagementService userManagementService, IMapper mapper)
        {
            _userAccountService = userAccountService;
            _userManagementService = userManagementService;
            _mapper = mapper;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = IdentityDefinitions.QueueWebAppAdmin)]
        [HttpPost("changeuserpassword")]
        public async Task<IActionResult> ChangeUserPassword(ChangeUserPassword model)
        {
            try
            {
                if (model.NewPassword != model.ConfirmPassword)
                {
                    return BadRequest("La nueva contraseña y la confirmación no coinciden");
                }

                var userName = await _userAccountService.GetCurrentUserNameAsync();
                if (userName == null) return BadRequest(ModelState);

                var changePassword = await _userManagementService.ChangeUserPassword(model);
                if (changePassword.Item1)
                {
                    return Ok(changePassword.Item2);
                }

                return BadRequest(changePassword.Item2);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = IdentityDefinitions.QueueWebAppAdmin)]
        [HttpPost("toggleactivation")]
        public async Task<IActionResult> ToggleActivation(ToggleUserActivation model)
        {
            try
            {
                var result = await _userManagementService.ToggleUserActivation(model);
                if (result.Item1)
                {
                    return Ok(result.Item2);
                }

                return BadRequest(result.Item2);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("removerole")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> RemoveRole(Role model)
        {
            var result = await _userManagementService.RemoveUserRoles(model);
            if (result.Item1)
            {
                return Ok(result.Item2);
            }

            return BadRequest(result.Item2);
        }

        [HttpPost("addrole")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> AddRole(Role model)
        {
            var result = await _userManagementService.AddUserRoles(model);
            if (result.Item1)
            {
                return Ok(result.Item2);
            }

            return BadRequest(result.Item2);
        }

        [HttpPost("edituser")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> EditUser(EditUser model)
        {
            try
            {
                var result = await _userManagementService.EditUser(model);

                if (result)
                {
                    return Ok("Cambios del usuario guardados");
                }

                return BadRequest("No se pudieron guardar los cambios del usuario");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("createuser")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> CreateUser(CreateUser model)
        {
            var result = await _userManagementService.CreateUserAsync(model.UserName, model.Password, model.FirstName, model.LastName, model.Active, model.UserRole, model.AssignedCounterId);

            if (result.Item1)
            {
                return Created("", result.Item2);
            }

            return BadRequest(result.Item2);
        }


        [HttpGet("getusers/{searchTerm?}")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> GetUsers(string? searchTerm)
        {
            var result = await _userAccountService.GetUsers(searchTerm);
            var mapper = _mapper.Map<List<UserDetailDto>>(result);
            var addRoles = await _userAccountService.ReturnUserRoles(mapper);
            return Ok(addRoles);
        }

        [HttpGet("getuser/{userId}")]
        [Authorize(Roles = IdentityDefinitions.QueueWebAppAdmin)]
        public async Task<IActionResult> GetUserDetail(Guid userId)
        {
            var result = await _userManagementService.GetUserDetail(userId);

            if (result.Item1 == null)
            {
                return NotFound(result.Item2);
            }

            var mapper = _mapper.Map<UserDetailDto>(result.Item1);
            var addRole = await _userAccountService.ReturnUserRole(mapper);

            return Ok(mapper);
        }
    }
}
