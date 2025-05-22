//-----------------------------------------------------------------------
// <copyright file="Config.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Trimble.Identity;

namespace CDMServicesUsageExamples
{
    /// <summary>
    /// The configuration.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// The authority URL.
        /// </summary>
        public static readonly string AuthorityUrl = "https://id.trimble.com/oauth/";

        /// <summary>
        /// The Organizer service URL.
        /// </summary>
        public static readonly string OrgServiceUrl = "https://org-api.us-east-1.connect.trimble.com/v1/";

        /// <summary>
        /// The Property Set service URL.
        /// </summary>
        public static readonly string PSetServiceUrl = "https://pset-api.us-east-1.connect.trimble.com/v1/";

        /// <summary>
        /// The client ID.
        /// </summary>
        public static readonly string ClientId = "4438bfff-847d-11e6-904c-02f285fc0101";

        /// <summary>
        /// The client key.
        /// </summary>
        public static readonly string ClientKey = "zldT0Op46kXwVp6_fBJzjEQ1VPga";

        /// <summary>
        /// The client key.
        /// </summary>
        public static readonly string AppName = "TC.SDK.Example";

        /// <summary>
        /// The redirect URL (needed for interactive authentication in apps with UI).
        /// </summary>
        public static readonly string RedirectUrl = "http://localhost";

        /// <summary>
        /// The client credentials.
        /// </summary>
        //public static ClientCredential ClientCredentials = new ClientCredential(ClientId, ClientKey, AppName)
        //{
        //    RedirectUri = new Uri(RedirectUrl)
        //};
    }
}
