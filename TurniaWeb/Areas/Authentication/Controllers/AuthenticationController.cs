using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TurniaCore.Entities.Platform.User;
using TurniaCore.Data.Entities.Platform.User;
using TurniaCore.Services.User;
using TurniaCore.Data.Dto.Authentication;
using TurniaCore.Services.Authentication;
using TurniaCore.Data.Extensions;
using TurniaCore.Services.Queue;

namespace TurniaWeb.Areas.Authentication.Controllers;

[Area("Authentication"), ApiController, Route("api/[controller]")]
public class AuthenticationController : Controller
{
    private readonly SignInManagerExtension _signInManagerExtension;
    private readonly UserAccountService _userAccountService;
    private readonly RoleManager<QueueUserRole> _roleManager;
    private readonly JwtTokenGenerationService _jwtTokenGenerationService;
    private readonly UserManager<QueueUserProfile> _userManager;
    private readonly QueueService _queueService;

    public AuthenticationController(
        UserManager<QueueUserProfile> userManager,
        SignInManagerExtension signInManagerExtension,
        RoleManager<QueueUserRole> roleManager,
        JwtTokenGenerationService jwtTokenGenerationService,
        UserAccountService userAccountService,
        QueueService queueService
        )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenGenerationService = jwtTokenGenerationService;
        _signInManagerExtension = signInManagerExtension;
        _userAccountService = userAccountService;
        _queueService = queueService;
    }

    #region Authentication - JWT

    [AllowAnonymous]
    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate(AuthDto model)
    {
        try
        {
            #region Check username/password for null/empty/whitespace
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Unauthorized();
            }
            #endregion

            #region Check user is valid and enabled
            var userToCheck = await _userManager.FindByEmailAsync(model.Username);
            if (userToCheck == null)
            {
                return Unauthorized();
            }
            if (userToCheck.Active == false)
            {
                return Unauthorized();
            }
            #endregion

            #region Check sign-in
            var result = await _signInManagerExtension.PasswordSignInAsync(model.Username, model.Password, false, false);
            if (result.Succeeded)
            {
                var tokenResult = await _signInManagerExtension.GenerateJwtToken(model.Username);
                if (tokenResult.Item1)
                {
                    return Ok(tokenResult.Item2);
                }
                return Unauthorized();
            }
            else if (result.IsLockedOut || result.IsNotAllowed)
            {
                return Unauthorized();
            }
            #endregion

            #region Default response
            return BadRequest();
            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest();
        }
    }

    [AllowAnonymous]
    [HttpPost("refreshtoken")]
    public async Task<IActionResult> RefreshToken(RefreshTokenDto model)
    {
        try
        {
            #region Check refresh/access tokens for null/empty/whitespace
            if (string.IsNullOrWhiteSpace(model.access_token) || string.IsNullOrWhiteSpace(model.refresh_token))
            {
                return Unauthorized();
            }
            #endregion

            #region Check user is valid and enabled
            var principal = await _jwtTokenGenerationService.GetClaimsPrincipalFromExpiredToken(model.access_token);
            if (principal == null)
            {
                return Unauthorized();
            }
            var userName = principal?.Name;
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Unauthorized();
            }
            var userToCheck = await _userManager.FindByEmailAsync(userName);
            if (userToCheck == null)
            {
                return Unauthorized();
            }
            if (userToCheck.Active == false)
            {
                return Unauthorized();
            }
            #endregion

            #region Check if we can generate new token with supplied refresh token
            var tokenResult = await _signInManagerExtension.GenerateJwtToken(model.refresh_token, model.access_token);
            if (tokenResult.Item1 == false)
            {
                return Unauthorized();
            }

            return Created(string.Empty, tokenResult.Item2);

            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    #endregion

    #region Authentication - Obtain User Profile
    [Authorize]
    [HttpPost("userprofile")]
    public async Task<IActionResult> GetMyUserProfile()
    {
        var myUserProfile = await _userAccountService.GetMyUserProfileAsync();
        if (myUserProfile == null)
        {
            throw new InvalidOperationException($"User profile not found");
        }

        var attendingTicket = await _queueService.GetCurrentlyAttendingTicket();
        var callingTicket = await _queueService.GetCurrentlyCallingTicket();

        var userProfile = new UserProfileDto();
        userProfile.FirstName = myUserProfile.FirstName;
        userProfile.LastName = myUserProfile.LastName;
        userProfile.Username = myUserProfile.UserName!;

        if (myUserProfile.AssignedCounter != null)
        {
            userProfile.IsAssignedToCounter = true;
            userProfile.Counter = new UserProfileCounterDto()
            {
                Name = myUserProfile.AssignedCounter.Name,
                Description = myUserProfile.AssignedCounter.Description,
                IsActive = myUserProfile.AssignedCounter.IsActive,
            };
            userProfile.CounterId = myUserProfile.AssignedCounter.Id;
        }

        if (callingTicket != null)
        {
            userProfile.IsCallingTicket = true;
            userProfile.TicketId = callingTicket.Id;
            userProfile.TicketNumber = callingTicket.TicketNumber;
            userProfile.TicketStatus = (UserProfileDto.TicketStatusEnum?)callingTicket.TicketStatus;
        }

        if (attendingTicket != null)
        {
            userProfile.IsAttendingTicket = true;
            userProfile.TicketId = attendingTicket.Id;
            userProfile.TicketNumber = attendingTicket.TicketNumber;
            userProfile.TicketStatus = (UserProfileDto.TicketStatusEnum?)attendingTicket.TicketStatus;
            userProfile.TicketAttendingStartDateTime = attendingTicket.TicketAttendingStartDateTime;
        }

        return Ok(userProfile);
    }

    #endregion

    #region Authentication - Obtain User Roles
    [Authorize]
    [HttpGet("getauthenticateduserroles")]
    public async Task<IActionResult> GetAuthenticatedUserRoles()
    {
        var userName = User.Identity!.Name;
        var user = await _userManager.FindByEmailAsync(userName!);
        var roles = await _userManager.GetRolesAsync(user!);
        return Ok(roles);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("getallroles")]
    public async Task<IActionResult> GetAllRoles()
    {
        try
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return Ok(roles);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    #endregion

    [Authorize]
    [HttpPost("changepassword")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
    {
        try
        {
            if (model.NewPassword != model.ConfirmPassword)
                return BadRequest("La nueva contraseña y la confirmación deben ser iguales");

            var changePassword = await _userAccountService.ChangePassword(model);
            if (changePassword.Item1)
            {
                return Ok(changePassword.Item2);
            }

            return BadRequest(changePassword.Item2);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
