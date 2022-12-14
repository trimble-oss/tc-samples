//-----------------------------------------------------------------------
// <copyright file="Config.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Trimble.Identity;

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
        public static readonly string AuthorityUrl = AuthorityUris.ProductionUri;

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
        /// The client key.
        /// </summary>
        public static readonly string AppName = "<Name>";

        /// <summary>
        /// The redirect URL (needed for interactive authentication in apps with UI).
        /// </summary>
        public static readonly string RedirectUrl = "http://localhost";

        /// <summary>
        /// The client credentials.
        /// </summary>
        public static ClientCredential ClientCredentials = new ClientCredential(ClientId, ClientKey, AppName)
        {
            RedirectUri = new Uri(RedirectUrl)
        };
    }
}
