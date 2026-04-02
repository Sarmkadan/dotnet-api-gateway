// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace DotNetApiGateway.Examples;

/// <summary>
/// Example: JWT Authentication Configuration
/// Demonstrates JWT token generation and validation setup for the gateway.
/// </summary>
public class JwtAuthenticationExample
{
    private const string SecretKey = "super-secret-key-minimum-32-characters-long";
    private const string Issuer = "https://auth.example.com";
    private const string Audience = "api.gateway";

    public static async Task Main()
    {
        Console.WriteLine("=== DotNet API Gateway - JWT Authentication Example ===\n");

        // Step 1: Create test tokens
        Console.WriteLine("Step 1: Generate JWT Tokens");
        Console.WriteLine(new string('-', 60));

        var adminToken = GenerateToken("admin-user", new[] { "admin", "user" });
        var userToken = GenerateToken("regular-user", new[] { "user" });
        var expiredToken = GenerateExpiredToken("expired-user", new[] { "user" });

        Console.WriteLine("✓ Admin Token (expires in 1 hour):");
        Console.WriteLine($"  {adminToken}");
        Console.WriteLine();

        Console.WriteLine("✓ User Token (expires in 1 hour):");
        Console.WriteLine($"  {userToken}");
        Console.WriteLine();

        Console.WriteLine("✓ Expired Token (already expired):");
        Console.WriteLine($"  {expiredToken}");
        Console.WriteLine();

        // Step 2: Show gateway configuration
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 2: Gateway Configuration (appsettings.json)");
        Console.WriteLine(new string('-', 60));

        var gatewayConfig = @"
{
  ""GatewayConfiguration"": {
    ""JwtValidation"": {
      ""enabled"": true,
      ""issuer"": ""https://auth.example.com"",
      ""audience"": ""api.gateway"",
      ""secretKey"": ""super-secret-key-minimum-32-characters-long"",
      ""validateIssuer"": true,
      ""validateAudience"": true,
      ""validateLifetime"": true,
      ""clockSkewSeconds"": 5
    },
    ""Routes"": [
      {
        ""name"": ""public-api"",
        ""pattern"": ""^/api/public(/.*)?$"",
        ""requiresAuthentication"": false,
        ""targets"": [{ ""url"": ""http://backend:3000"" }]
      },
      {
        ""name"": ""user-api"",
        ""pattern"": ""^/api/users(/.*)?$"",
        ""requiresAuthentication"": true,
        ""requiredRoles"": [""user""],
        ""targets"": [{ ""url"": ""http://backend:3000"" }]
      },
      {
        ""name"": ""admin-api"",
        ""pattern"": ""^/api/admin(/.*)?$"",
        ""requiresAuthentication"": true,
        ""requiredRoles"": [""admin""],
        ""targets"": [{ ""url"": ""http://backend:3000"" }]
      }
    ]
  }
}";

        Console.WriteLine(gatewayConfig);
        Console.WriteLine();

        // Step 3: Demonstrate token validation
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 3: Token Validation Examples");
        Console.WriteLine(new string('-', 60));

        ValidateToken(adminToken, "Admin Token");
        ValidateToken(userToken, "User Token");
        ValidateToken(expiredToken, "Expired Token");
        ValidateToken("invalid-token", "Invalid Token");

        Console.WriteLine();

        // Step 4: Show request examples
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 4: API Request Examples");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine("\n✓ Public API (no authentication required):");
        Console.WriteLine("  curl http://localhost:5000/api/public/data");
        Console.WriteLine();

        Console.WriteLine("✓ User API (authentication required, 'user' role):");
        Console.WriteLine($"  curl -H \"Authorization: Bearer {userToken}\" \\");
        Console.WriteLine("       http://localhost:5000/api/users/123");
        Console.WriteLine();

        Console.WriteLine("✓ Admin API (authentication required, 'admin' role):");
        Console.WriteLine($"  curl -H \"Authorization: Bearer {adminToken}\" \\");
        Console.WriteLine("       http://localhost:5000/api/admin/users");
        Console.WriteLine();

        // Step 5: Decode token claims
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 5: Decoded Token Claims");
        Console.WriteLine(new string('-', 60));

        DecodeAndDisplayTokenClaims(adminToken);
        Console.WriteLine();

        // Step 6: Security best practices
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Step 6: Security Best Practices");
        Console.WriteLine(new string('-', 60));

        Console.WriteLine("✓ Store secrets securely:");
        Console.WriteLine("  - Use environment variables in production");
        Console.WriteLine("  - Use Azure Key Vault, AWS Secrets Manager, etc.");
        Console.WriteLine("  - Never hardcode secrets in configuration files");
        Console.WriteLine();

        Console.WriteLine("✓ Use HTTPS in production:");
        Console.WriteLine("  - Prevent token interception");
        Console.WriteLine("  - Enable TLS/SSL certificates");
        Console.WriteLine();

        Console.WriteLine("✓ Token expiration:");
        Console.WriteLine("  - Set reasonable expiration times (1-24 hours)");
        Console.WriteLine("  - Implement refresh token mechanism");
        Console.WriteLine("  - Monitor token validity");
        Console.WriteLine();

        Console.WriteLine("✓ Validation configuration:");
        Console.WriteLine("  - Validate issuer");
        Console.WriteLine("  - Validate audience");
        Console.WriteLine("  - Validate lifetime");
        Console.WriteLine("  - Validate signature");

        Console.WriteLine("\n✓ Example completed successfully!");
    }

    private static string GenerateToken(string userId, string[] roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateExpiredToken(string userId, string[] roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(-1), // Already expired
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static void ValidateToken(string token, string description)
    {
        Console.WriteLine($"\n{description}:");

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5)
            }, out SecurityToken validatedToken);

            Console.WriteLine("  ✓ Token is valid!");
            Console.WriteLine($"  ✓ Signed: {((JwtSecurityToken)validatedToken).Header["alg"]}");
        }
        catch (SecurityTokenExpiredException)
        {
            Console.WriteLine("  ✗ Token has expired");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            Console.WriteLine("  ✗ Token signature is invalid");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Validation failed: {ex.GetType().Name}");
        }
    }

    private static void DecodeAndDisplayTokenClaims(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Console.WriteLine($"\nToken Header:");
            Console.WriteLine($"  Algorithm: {jwtToken.Header["alg"]}");
            Console.WriteLine($"  Type: {jwtToken.Header["typ"]}");

            Console.WriteLine($"\nToken Payload:");
            Console.WriteLine($"  Issuer: {jwtToken.Issuer}");
            Console.WriteLine($"  Audience: {string.Join(", ", jwtToken.Audiences)}");
            Console.WriteLine($"  IssuedAt: {UnixTimeStampToDateTime(jwtToken.IssuedAt)}");
            Console.WriteLine($"  Expires: {jwtToken.ValidTo}");

            Console.WriteLine($"\nClaims:");
            foreach (var claim in jwtToken.Claims)
            {
                Console.WriteLine($"  {claim.Type}: {claim.Value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to decode token: {ex.Message}");
        }
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}

/// <summary>
/// To run this example:
///
/// 1. Ensure the gateway is running with JWT validation enabled
/// 2. Use the generated tokens in Authorization headers:
///
///    curl -H "Authorization: Bearer {token}" http://localhost:5000/api/protected
///
/// 3. Test with different tokens:
///    - Admin token: Has "admin" and "user" roles
///    - User token: Has only "user" role
///    - Expired token: Will be rejected
///
/// Security Note:
/// - Never share tokens
/// - Always use HTTPS
/// - Rotate secrets periodically
/// - Use short expiration times
/// </summary>
