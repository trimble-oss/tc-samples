namespace SignIn
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
        public static readonly Uri AuthorityUri = new Uri(AuthorityUris.StagingUri);

        /// <summary>
        /// The client credentials.
        /// </summary>
        public static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("tcps://localhost")
        };

#else
        /// <summary>
        /// The service URI.
        /// </summary>
        public static readonly Uri AuthorityUri = new Uri(AuthorityUris.ProductionUri);

        /// <summary>
        /// The client credentials.
        /// </summary>
        public static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("tcps://localhost")
        };
#endif
    }
}
