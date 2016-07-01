namespace Examples.Mobile
{
    using System;

    using Trimble.Identity;

    /// <summary>
    /// Authentication parameters.
    /// </summary>
    public static class AuthParams
    {
#if true
        /// <summary>
        /// The service URI.
        /// </summary>
        public static readonly Uri AuthorityUri = new Uri("https://identity-stg.trimble.com/i/oauth2/");

        /// <summary>
        /// The client creadentials.
        /// </summary>
        public static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };

#else
        /// <summary>
        /// The service URI.
        /// </summary>
        public const string AuthorityUri = "https://identity.trimble.com/i/oauth2/";

        /// <summary>
        /// The client creadentials.
        /// </summary>
        public static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };
#endif
    }
}
