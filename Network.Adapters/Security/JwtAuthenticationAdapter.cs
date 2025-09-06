using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Network.Core.Application.Ports.Outbound;

namespace Network.Adapters.Security;

/// <summary>
/// JWT-based authentication service adapter.
/// Provides token-based authentication and authorization for network connections.
/// </summary>
public class JwtAuthenticationAdapter : IAuthenticationService
{
    private readonly ILogger<JwtAuthenticationAdapter> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _tokenLifetime;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;

    public JwtAuthenticationAdapter(ILogger<JwtAuthenticationAdapter> logger)
    {
        _logger = logger;
        
        // In production, these should come from configuration
        _secretKey = "your-256-bit-secret-key-here-make-it-long-enough-for-security";
        _issuer = "HexagonalNetwork";
        _audience = "HexagonalNetwork.Clients";
        _tokenLifetime = TimeSpan.FromHours(1);
        
        _tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string credentials)
    {
        try
        {
            // In production, this should validate against a user database
            // For demo purposes, accept any non-empty username/password
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(credentials))
            {
                return new AuthenticationResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Username and password are required"
                };
            }

            var userId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.Add(_tokenLifetime);
            
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Player"),
                new Claim("jti", Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = _signingCredentials
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = _tokenHandler.WriteToken(token);
            
            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation("User {Username} authenticated successfully", username);

            return new AuthenticationResult
            {
                IsSuccessful = true,
                Token = tokenString,
                RefreshToken = refreshToken,
                UserId = userId,
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user {Username}", username);
            return new AuthenticationResult
            {
                IsSuccessful = false,
                ErrorMessage = "Authentication failed"
            };
        }
    }

    public async Task<Core.Application.Ports.Outbound.TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secretKey)),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = principal.FindFirst(ClaimTypes.Name)?.Value;
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            
            return new Core.Application.Ports.Outbound.TokenValidationResult
            {
                IsValid = true,
                UserId = userId,
                Username = username,
                Roles = roles,
                ExpiresAt = validatedToken.ValidTo
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new Core.Application.Ports.Outbound.TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return new Core.Application.Ports.Outbound.TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid token"
            };
        }
    }

    public async Task<bool> IsAuthorizedAsync(string userId, string resource, string action)
    {
        // Basic authorization logic - in production, this should check against proper RBAC
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        // For demo purposes, allow all authenticated users to perform basic actions
        return action switch
        {
            "connect" => true,
            "send_packet" => true,
            "admin" => false, // Require admin role
            _ => true
        };
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
    {
        // In production, validate refresh token against database
        // For demo purposes, assume valid and generate new token
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new AuthenticationResult
            {
                IsSuccessful = false,
                ErrorMessage = "Invalid refresh token"
            };
        }

        // Generate new token with extended lifetime
        var userId = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.Add(_tokenLifetime);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Player"),
            new Claim("jti", Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = _signingCredentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = _tokenHandler.WriteToken(token);
        var newRefreshToken = GenerateRefreshToken();

        return new AuthenticationResult
        {
            IsSuccessful = true,
            Token = tokenString,
            RefreshToken = newRefreshToken,
            UserId = userId,
            ExpiresAt = expiresAt
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}