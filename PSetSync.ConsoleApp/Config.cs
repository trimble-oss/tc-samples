//-----------------------------------------------------------------------
// <copyright file="Config.cs" company="Trimble Inc.">
//     Copyright (c) Trimble Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Configuration;

namespace PSetSync.ConsoleApp
{
    /// <summary>
    /// The configuration. Same model as Other Samples - set values in App.config or replace defaults below.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// The authority URL (OAuth base, e.g. https://id.trimble.com/oauth/).
        /// </summary>
        public static readonly string AuthorityUrl = GetAppSetting("AuthorityUrl", "https://id.trimble.com/oauth/");

        /// <summary>
        /// The Organizer service URL.
        /// </summary>
        public static readonly string OrgServiceUrl = GetAppSetting("OrgServiceUrl", "https://org-api.us-east-1.connect.trimble.com/v1/");

        /// <summary>
        /// The Property Set service URL.
        /// </summary>
        public static readonly string PSetServiceUrl = GetAppSetting("PSetServiceUrl", "https://pset-api.us-east-1.connect.trimble.com/v1/");

        /// <summary>
        /// The client ID. Set in App.config key "ClientId" (same as Other Samples).
        /// </summary>
        public static readonly string ClientId = GetAppSetting("ClientId", "");

        /// <summary>
        /// The client key (client secret). Set in App.config key "ClientKey" (same as Other Samples).
        /// </summary>
        public static readonly string ClientKey = GetAppSetting("ClientKey", "");

        /// <summary>
        /// The app name (scope). Set in App.config key "AppName" (same as   ).
        /// </summary>
        public static readonly string AppName = GetAppSetting("AppName", "");

        /// <summary>
        /// The redirect URL (needed for interactive authentication). Must match exactly what is registered in Trimble (no trailing slash: use http://localhost:8765 not http://localhost:8765/).
        /// </summary>
        public static readonly string RedirectUrl = GetAppSetting("RedirectUrl", "http://localhost:8765");

        /// <summary>
        /// Trimble Connect API base URL (for TrimbleConnectClient). Use full API base including /tc/api/2.0/ so requests go to e.g. .../tc/api/2.0/users/me.
        /// If you get INVALID_URL_OR_METHOD, try https://app.connect.trimble.com/ (root) instead.
        /// </summary>
        public static readonly string ConnectServiceUrl = GetAppSetting("ConnectServiceUrl", "https://app.connect.trimble.com/tc/api/2.0/");

        static string GetAppSetting(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            var trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            // If App.config has a placeholder (e.g. <ClientID>), treat as not set and use default from Config.cs
            if (string.IsNullOrEmpty(trimmed) || IsPlaceholderValue(trimmed))
                return defaultValue;
            return trimmed;
        }

        /// <summary>Returns true if the value looks like a placeholder (e.g. &lt;ClientID&gt;, &lt;ClientKey&gt;).</summary>
        static bool IsPlaceholderValue(string v)
        {
            return (v.Length >= 3 && v.StartsWith("<") && v.EndsWith(">")) ||
                   v.IndexOf("YOUR_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   v.IndexOf("ENTER_YOUR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   v.IndexOf("_HERE", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
