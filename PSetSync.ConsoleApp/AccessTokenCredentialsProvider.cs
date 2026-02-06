using System;
using System.Net.Http;
using System.Threading.Tasks;
using Trimble.Connect.Client;

namespace PSetSync.ConsoleApp
{
    /// <summary>
    /// Supplies a fixed OAuth access token to requests. Required by SyncClient (sync helper needs ICredentialsProvider).
    /// </summary>
    public sealed class AccessTokenCredentialsProvider : ICredentialsProvider
    {
        readonly string _accessToken;

        public AccessTokenCredentialsProvider(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentNullException(nameof(accessToken));
            _accessToken = accessToken.Trim();
        }

        public Task AuthorizeAsync(HttpRequestMessage request)
        {
            if (request == null) return Task.CompletedTask;
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            return Task.CompletedTask;
        }

        public Task InvalidateAndAuthorizeAsync(HttpRequestMessage request, HttpResponseMessage response)
        {
            // No refresh token; re-use same token (e.g. after 401 retry)
            return AuthorizeAsync(request);
        }
    }
}
