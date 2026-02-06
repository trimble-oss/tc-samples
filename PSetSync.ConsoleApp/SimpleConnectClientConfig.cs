using System;
using Trimble.Connect.Client;

namespace PSetSync.ConsoleApp
{
    /// <summary>
    /// Simple config that sets a single ServiceURI (required for SyncClient / ICredentialsProvider flow).
    /// </summary>
    public sealed class SimpleConnectClientConfig : TrimbleConnectClientConfig
    {
        readonly Uri _serviceUri;

        public SimpleConnectClientConfig(string serviceUri)
        {
            _serviceUri = new Uri(serviceUri);
            // Base ClientConfig requires ServiceURI to be set for TrimbleConnectClient(config, credentialsProvider)
            var baseType = GetType().BaseType;
            while (baseType != null)
            {
                var prop = baseType.GetProperty("ServiceURI");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(this, _serviceUri);
                    break;
                }
                baseType = baseType.BaseType;
            }
        }

        public override Uri GetServiceURIForRegion(string region) => _serviceUri;
    }
}
