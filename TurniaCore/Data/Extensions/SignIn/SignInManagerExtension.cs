using TurniaCore.Data.Dto.Authentication;
using TurniaCore.Data.Entities.App.Shared;
using TurniaCore.Entities.Platform.User;
using TurniaCore.Libraries.Utilities;
using TurniaCore.Services.Authentication;


using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace TurniaCore.Data.Extensions;

public class SignInManagerExtension : SignInManager<QueueUserProfile>
{
    private readonly QueueContext _queueContext = null!;
    private readonly UserManager<QueueUserProfile> _userManager;
    private readonly JwtTokenGenerationService _jwtTokenGeneration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SignInManagerExtension(
        UserManager<QueueUserProfile> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<QueueUserProfile> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<QueueUserProfile>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<QueueUserProfile> confirmation,
        QueueContext queueContext,
        IConfiguration configuration,
        JwtTokenGenerationService jwtTokenGeneration
        ) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        _queueContext = queueContext;
        _userManager = userManager;
        _jwtTokenGeneration = jwtTokenGeneration;
        _httpContextAccessor = contextAccessor;
    }


    public enum GenerateJwtMode
    {
        GenerateByUsername = 1,
        GenerateByAccessToken = 2
    }

    #region JWT Token / Refresh Token Generation
    public async Task<(bool, AuthTokenDto?, string)> GenerateJwtToken(string userName)
    {
        return await GenerateJwtToken(GenerateJwtMode.GenerateByUsername, userName, null);
    }


    public async Task<(bool, AuthTokenDto?, string)> GenerateJwtToken(string refreshToken, string oldAccessToken)
    {
        return await GenerateJwtToken(GenerateJwtMode.GenerateByAccessToken, oldAccessToken, refreshToken);
    }
    #endregion


    #region Internal Functions

    #region JWT Token Generation

    private async Task<(bool, AuthTokenDto?, string)> GenerateJwtToken(GenerateJwtMode mode, string usernameOrAccessToken, string? oldRefreshToken = null)
    {
        QueueUserProfile? user = null;

        #region Mode parameter validation
        switch (mode)
        {
            case GenerateJwtMode.GenerateByUsername:
                #region Mode - User
                user = await _userManager.FindByEmailAsync(usernameOrAccessToken);
                if (user == null)
                {
                    return (false, null, $"The user {usernameOrAccessToken} could not be found");
                }
                #endregion
                break;
            case GenerateJwtMode.GenerateByAccessToken:
                #region Mode - Access Token
                if (string.IsNullOrWhiteSpace(oldRefreshToken))
                {
                    return (false, null, $"Old refresh token is null or empty.");
                }
                var refreshTokenExists = await RefreshTokenExistsAsync(oldRefreshToken);
                if (refreshTokenExists == false)
                {
                    return (false, null, $"Old refresh token does not exist.");
                }
                var claimsPrincipal = await _jwtTokenGeneration.GetClaimsPrincipalFromExpiredToken(usernameOrAccessToken);
                if (claimsPrincipal == null)
                {
                    return (false, null, $"The {usernameOrAccessToken} could not be extracted from the JWT token");
                }
                var userName = claimsPrincipal?.Name;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return (false, null, $"The userName extracted from the claims principal is empty.");
                }
                user = await _userManager.FindByEmailAsync(userName);
                if (user == null)
                {
                    return (false, null, $"The user for {userName} could not be found");
                }
                #endregion
                break;
            default:
                throw new ArgumentException($"SigninManagerExtension Exception: The mode parameter is invalid");
        }
        #endregion


#region Claims
        var roles = await _userManager.GetRolesAsync(user);

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return (false, null, $"The user's email is empty");
        }

        
        var claimsIdentities = new ClaimsIdentity();

        claimsIdentities.AddClaim(new Claim(ClaimTypes.Name, user.Email!));

        claimsIdentities.AddClaims(_jwtTokenGeneration.GenerateClaimsIdentities(roles).Claims);
        #endregion


        #region JwtToken - Generate Values and create/update refresh token
        var expiry = _jwtTokenGeneration.GetTokenExpiryTime();
        var jwtAccessToken = _jwtTokenGeneration.GenerateToken(claimsIdentities, expiry.Item2);
        var newRefreshToken = _jwtTokenGeneration.GenerateRefreshToken();
        var userAgent = _httpContextAccessor.HttpContext!.Request.Headers["User-Agent"];
        var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress!.ToString();

        var authTokenResponse = new AuthTokenDto
        {
            token_type = "bearer",
            access_token = jwtAccessToken,
            expires = expiry.Item2,
            refresh_token = newRefreshToken
        };

        var createUpdateRefreshTokenResult = await CreateUpdateRefreshTokenAsync(user, expiry.Item1, expiry.Item2, oldRefreshToken, newRefreshToken, userAgent!, ipAddress);
        if(createUpdateRefreshTokenResult == false)
        {
            return (false, null, $"Could not store/update the Refresh Token");
        }

        return (true, authTokenResponse, $"Token successfully generated");
        #endregion
    }

    #endregion


    #region JWT Token Blacklist functions
    public async Task<bool> BlackListJwtTokenAsync(QueueUserProfile user, DateTime expireTime, string jwtTokenToBlackList)
    {
        var tokenHash = Utils.GetSha512Hash(jwtTokenToBlackList);
        var jwtTokenBlacklisted = await _queueContext.JwtBlacklistedTokens.Where(t => string.Equals(tokenHash, t.TokenHash, StringComparison.OrdinalIgnoreCase)).FirstOrDefaultAsync();
        if (jwtTokenBlacklisted == null)
        {
            var tokenToBlackList = new JwtBlacklistedTokens()
            {
                User = user,
                TokenExpiry = expireTime,
                TokenHash = tokenHash
            };
            await _queueContext.AddAsync(tokenToBlackList);
            var saveRes = await _queueContext.SaveChangesAsync();
            return saveRes > 0;
        }
        return false;
    }

    public async Task<bool> JwtTokenIsBlacklistedAsync(string jwtTokenToCheck)
    {
        var tokenHash = Utils.GetSha512Hash(jwtTokenToCheck);
        var jwtTokenBlacklisted = await _queueContext.JwtBlacklistedTokens.Where(t => string.Equals(tokenHash, t.TokenHash, StringComparison.OrdinalIgnoreCase)).FirstOrDefaultAsync();
        if(jwtTokenBlacklisted == null)
        {
            return false;
        }
        return true;
    }
    #endregion

    #region Refresh Token Generation

    public async Task<bool> RefreshTokenExistsAsync(string refreshToken)
    {
        var refreshTokenInDb = await _queueContext.JwtRefreshTokens.Where(r => r.RefreshToken == refreshToken && r.Revoked == false && r.Exchanged == false)
            .FirstOrDefaultAsync();
        if (refreshTokenInDb == null)
        {
            return false;
        }
        return true;
    }


    private async Task<bool> CreateUpdateRefreshTokenAsync(QueueUserProfile user, DateTime created, DateTime expireTime, string? oldRefreshToken, string newRefreshToken, string metaUserAgent, string metaFromIpAddress)
    {
        if (oldRefreshToken != null)
        {
            var refreshTokenInDb = await _queueContext.JwtRefreshTokens.Where(r => r.RefreshToken == oldRefreshToken && r.Revoked == false && r.Exchanged == false).FirstOrDefaultAsync();
            if (refreshTokenInDb == null)
            {
                return false;
            }

            refreshTokenInDb.Exchanged = true;
            refreshTokenInDb.ExchangedDateTime = DateTime.UtcNow;
        }


        var refreshTokenEntry = new JwtRefreshTokens
        {
            User = user,
            Created = created,
            ExpireTime = expireTime,
            RefreshToken = newRefreshToken,
            Revoked = false,
            RevokedDateTime = null,
            Exchanged = false,
            ExchangedDateTime = null,
            MetaUserAgent = metaUserAgent,
            MetaFromIpAddress = metaFromIpAddress
        };
        _queueContext.JwtRefreshTokens.Add(refreshTokenEntry);
        var result = await _queueContext.SaveChangesAsync();

        return result > 0;
    }

    #endregion


#endregion
}
