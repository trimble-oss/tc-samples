namespace SignIn.Maui.SerialPkce.Models
{
    /// <summary>
    /// The refresh token DTO
    /// </summary>
    public class RefreshTokenInfo
    {
        private string refreshToken;

        private string codeVerifier;

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

        public string CodeVerifier
        {
            get
            {
                return codeVerifier;
            }
            set
            {
                codeVerifier = value;
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

        public RefreshTokenInfo(string refreshToken, string codeVerifier, long tokenRefreshedTime, bool isTokenEncrypted)
        {
            this.RefreshToken = refreshToken;
            this.CodeVerifier = codeVerifier;
            this.TokenRefreshedTime = tokenRefreshedTime;
            this.IsTokenEncrypted = isTokenEncrypted;
        }

        // Backward compatibility constructor for old event signature
        public RefreshTokenInfo(string refreshToken, long tokenRefreshedTime, bool isTokenEncrypted)
            : this(refreshToken, null, tokenRefreshedTime, isTokenEncrypted)
        {
        }
    }
}
