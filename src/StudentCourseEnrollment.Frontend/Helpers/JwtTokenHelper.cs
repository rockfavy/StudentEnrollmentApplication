using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace StudentCourseEnrollment.Frontend.Helpers;

public static class JwtTokenHelper
{
    public static List<Claim> ParseClaimsFromJwt(string token)
    {
        var claims = new List<Claim>();

        if (string.IsNullOrWhiteSpace(token))
        {
            return claims;
        }

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return claims;
            }

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var json = Encoding.UTF8.GetString(jsonBytes);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (keyValuePairs == null)
            {
                return claims;
            }

            foreach (var kvp in keyValuePairs)
            {
                var claimType = kvp.Key switch
                {
                    "sub" => ClaimTypes.NameIdentifier,
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" => ClaimTypes.NameIdentifier,
                    "email" => ClaimTypes.Email,
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" => ClaimTypes.Email,
                    "name" => ClaimTypes.Name,
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" => ClaimTypes.Name,
                    "role" => ClaimTypes.Role,
                    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" => ClaimTypes.Role,
                    _ => kvp.Key
                };
                
                if (kvp.Value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                claims.Add(new Claim(claimType, item.GetString()!));
                            }
                        }
                    }
                    else if (element.ValueKind == JsonValueKind.String)
                    {
                        claims.Add(new Claim(claimType, element.GetString()!));
                    }
                    else if (element.ValueKind == JsonValueKind.Number)
                    {
                        claims.Add(new Claim(claimType, element.GetRawText()));
                    }
                }
                else if (kvp.Value is string stringValue)
                {
                    claims.Add(new Claim(claimType, stringValue));
                }
            }
        }
        catch
        {
            return claims;
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
