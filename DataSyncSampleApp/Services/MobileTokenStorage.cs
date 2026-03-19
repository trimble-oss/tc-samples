using Newtonsoft.Json;

namespace DataSyncSampleApp.Services;

public static class MobileTokenStorage
{
    private static string Path => System.IO.Path.Combine(
        FileSystem.AppDataDirectory, "oauth_tokens.json");

    public static string? LoadRefreshToken()
    {
        try
        {
            if (File.Exists(Path))
            {
                var j = JsonConvert.DeserializeObject<TokenDto>(File.ReadAllText(Path));
                return j?.RefreshToken;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    public static void SaveRefreshToken(string refreshToken)
    {
        try
        {
            var dto = new TokenDto { RefreshToken = refreshToken, SavedUtc = DateTime.UtcNow };
            File.WriteAllText(Path, JsonConvert.SerializeObject(dto, Formatting.Indented));
        }
        catch { /* ignore */ }
    }

    private class TokenDto
    {
        public string? RefreshToken { get; set; }
        public DateTime SavedUtc { get; set; }
    }
}
