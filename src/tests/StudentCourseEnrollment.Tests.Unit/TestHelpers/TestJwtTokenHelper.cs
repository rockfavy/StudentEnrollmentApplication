using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace StudentCourseEnrollment.Tests.Unit.TestHelpers;

public static class TestJwtTokenHelper
{
    private const string TestSecretKey = "TestSecretKeyForLocalDevelopment-Minimum32Characters";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    public static string GenerateTestToken(Guid userId, string email, string firstName, string lastName, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var fullName = $"{firstName} {lastName}".Trim();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, fullName),
            new Claim("FirstName", firstName),
            new Claim("LastName", lastName),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
