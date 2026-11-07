using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TurniaCore.Services.Authentication;

public class JwtTokenGenerationService
{
    private readonly string? _key;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly int _jwtTokenExpiryTimeMinutes;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtTokenGenerationService(string? key, string? issuer, string? audience, int jwtTokenExpiryTimeMinutes)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new JwtTokenException("JWT Service Configuration error: Key, issuer, audience parameters cannot be null");
        }

        if (jwtTokenExpiryTimeMinutes == 0)
        {
            throw new JwtTokenException("JWT Service Configuration error: Token expiry time cannot be zero");
        }

        _key = key;
        _issuer = issuer;
        _audience = audience;
        _jwtTokenExpiryTimeMinutes = jwtTokenExpiryTimeMinutes;

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateActor = false,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateTokenReplay = true,
            ValidateLifetime = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidIssuer = issuer,
            ValidAudience = audience
        };
    }

    public string GenerateToken(ClaimsIdentity claims, DateTime expires)
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key!));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var jwtHandler = new JsonWebTokenHandler();
            var jwtTokenDesc = new SecurityTokenDescriptor
            {
                Issuer = _issuer,
                Audience = _audience,
                Subject = claims,
                NotBefore = utcNow,
                Expires = expires,
                SigningCredentials = signingCredentials,
            };
            var jwtString = jwtHandler.CreateToken(jwtTokenDesc);
            return jwtString;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


public string GenerateRefreshToken()
    {
        var random = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);
        return Convert.ToBase64String(random);
    }


    public async Task<ClaimsIdentity> GetClaimsPrincipalFromExpiredToken(string token)
    {
        try
        {
            var tokenHandler = new JsonWebTokenHandler();
            var principal =  await tokenHandler.ValidateTokenAsync(token, _tokenValidationParameters);
            var jwtSecurityToken = principal.SecurityToken as JsonWebToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }
            return principal.ClaimsIdentity;
        }
        catch (Exception e)

        {
            Console.WriteLine(e);
            throw;
        }
    }

    public (DateTime, DateTime) GetTokenExpiryTime()
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(_jwtTokenExpiryTimeMinutes);
        return (now, expiry);
    }


public List<Claim> GenerateClaims(IEnumerable<string> roles)
    {
        var claimsList = new List<Claim>();
        claimsList.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claimsList;
    }

    public ClaimsIdentity GenerateClaimsIdentities(IEnumerable<string> roles)
    {
        var claimsList = new List<Claim>();

        foreach (var role in roles)
        {
            claimsList.Add(new Claim(ClaimTypes.Role, role));
        }

        var resultClaimsIdentity = new ClaimsIdentity(claimsList);

        return resultClaimsIdentity;
    }
}

public class JwtTokenException : Exception
{
    public JwtTokenException(string message) : base(message) { }
}
