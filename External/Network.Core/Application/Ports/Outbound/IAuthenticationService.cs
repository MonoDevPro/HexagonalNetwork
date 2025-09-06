using System.Threading.Tasks;

namespace Network.Core.Application.Ports.Outbound;

/// <summary>
/// Port for authentication and authorization services.
/// Handles JWT tokens, user validation, and connection authorization.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with provided credentials
    /// </summary>
    /// <param name="username">Username or identifier</param>
    /// <param name="credentials">Password, token, or other authentication data</param>
    /// <returns>Authentication result with token if successful</returns>
    Task<AuthenticationResult> AuthenticateAsync(string username, string credentials);
    
    /// <summary>
    /// Validates an authentication token
    /// </summary>
    /// <param name="token">JWT or other authentication token</param>
    /// <returns>Token validation result with user info</returns>
    Task<TokenValidationResult> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Checks if a user is authorized for a specific action or resource
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="action">Action being performed</param>
    /// <returns>True if authorized</returns>
    Task<bool> IsAuthorizedAsync(string userId, string resource, string action);
    
    /// <summary>
    /// Refreshes an authentication token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>New authentication result</returns>
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
}

/// <summary>
/// Result of authentication operation
/// </summary>
public class AuthenticationResult
{
    public bool IsSuccessful { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string? UserId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Result of token validation
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string[]? Roles { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExpiresAt { get; set; }
}