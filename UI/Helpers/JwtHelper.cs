using System.Text.Json;
using System.Text;

public static class JwtHelper
{
    public static Dictionary<string, object> DecodePayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            throw new ArgumentException("Invalid JWT format");

        var payload = parts[1];
        payload = PadBase64(payload);

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        return result ?? new Dictionary<string, object>();
    }

    private static string PadBase64(string input)
    {
        // Pad the base64 string if necessary
        int padding = 4 - input.Length % 4;
        if (padding < 4)
        {
            input += new string('=', padding);
        }
        return input.Replace('-', '+').Replace('_', '/');
    }
}
