//-----------------------------------------------------------------------
// <copyright file="Config.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CDMServicesDemo
{
    /// <summary>
    /// The configuration.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// The authority URL.
        /// </summary>
        public static readonly string AuthorityUrl = "https://identity.trimble.com/i/oauth2/";

        /// <summary>
        /// The Organizer service URL.
        /// </summary>
        public static readonly string OrgServiceUrl = "https://org-api.connect.trimble.com/v1/";

        /// <summary>
        /// The Property Set service URL.
        /// </summary>
        public static readonly string PSetServiceUrl = "https://pset-api.connect.trimble.com/v1/";

        /// <summary>
        /// The client ID.
        /// </summary>
        public static readonly string ClientId = "<ClientID>";

        /// <summary>
        /// The client key.
        /// </summary>
        public static readonly string ClientKey = "<ClientKey>";

        /// <summary>
        /// The user name (needed for non-interactive authentication in back-end apps with no UI).
        /// </summary>
        public static readonly string UserName = "<UserName>";

        /// <summary>
        /// The user password (needed for non-interactive authentication in back-end apps with no UI).
        /// </summary>
        public static readonly string UserPassword = "<Password>";

        /// <summary>
        /// The redirect url (needed for interactive authentication in apps with UI).
        /// </summary>
        public static readonly string RedirectUrl = "http://localhost";
    }
}
