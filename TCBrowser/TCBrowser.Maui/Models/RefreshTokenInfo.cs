namespace TCBrowser.Maui.Models
{
    /// <summary>
    /// The refresh token DTO
    /// </summary>
    public class RefreshTokenInfo
    {
        private string refreshToken;

        private long tokenRefreshedTime;

        private bool isTokenEncrypted;

        public string RefreshToken
        {
            get
            {
                return refreshToken;
            }
            set
            {
                refreshToken = value;
            }
        }

        public long TokenRefreshedTime
        {
            get
            {
                return tokenRefreshedTime;
            }
            set
            {
                tokenRefreshedTime = value;
            }
        }

        public bool IsTokenEncrypted
        {
            get
            {
                return isTokenEncrypted;
            }
            set
            {
                isTokenEncrypted = value;
            }
        }

        public RefreshTokenInfo(string refreshToken, long tokenRefreshedTime, bool isTokenEncrypted)
        {
            this.RefreshToken = refreshToken;
            this.TokenRefreshedTime = tokenRefreshedTime;
            this.IsTokenEncrypted = isTokenEncrypted;
        }
    }
}
