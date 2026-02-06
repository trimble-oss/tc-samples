using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PSetSync.ConsoleApp
{
    /// <summary>
    /// Obtains a Trimble Identity OAuth 2.0 access token via browser login using
    /// Authorization Code flow (same config as Other Samples: ClientId, ClientKey, RedirectUrl, AppName for scope).
    /// </summary>
    public static class TrimbleOAuthHelper
    {
        /// <summary>
        /// Gets an access token by opening the browser for Trimble Identity login (Authorization Code flow).
        /// Same as Other Samples: clientId, clientKey, redirectUri, scope (openid + AppName) from Config.
        /// </summary>
        public static async Task<string> GetAccessTokenViaBrowserAsync(
            string clientId,
            string clientSecret,
            string redirectUri,
            string scope,
            string authorityUrl = null)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("ClientId is required. Set ClientId in Config (same as Other Samples).", nameof(clientId));
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException("ClientKey is required. Set ClientKey in Config (same as Other Samples).", nameof(clientSecret));
            if (string.IsNullOrWhiteSpace(redirectUri))
                throw new ArgumentException("RedirectUrl is required. Set RedirectUrl in Config (same as Other Samples).", nameof(redirectUri));

            var oauthBase = (authorityUrl ?? "https://id.trimble.com/oauth/").TrimEnd('/') + "/";
            // Scope from Config.AppName (same as Other Samples: "openid" and application name, space-delimited)
            var scopeValue = string.IsNullOrWhiteSpace(scope) ? "openid" : scope.Trim();
            redirectUri = redirectUri.Trim();

            var state = Guid.NewGuid().ToString("N");

            // Start local listener on the same host/port as redirect_uri so we receive the callback.
            // Use a non-privileged port (e.g. 8765); http://localhost (port 80) requires admin on Windows.
            var callbackUri = new Uri(redirectUri);
            var listenPrefix = callbackUri.Scheme + "://" + callbackUri.Authority + "/";

            HttpListener listener = null;
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(listenPrefix);
                listener.Start();
            }
            catch (System.Net.HttpListenerException ex) when (ex.HResult == unchecked((int)0x80004005) || ex.Message.IndexOf("Access", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException(
                    "Cannot start local HTTP listener for OAuth callback (Access is denied). On Windows, listening on port 80 requires Administrator. " +
                    "Set RedirectUrl in App.config to a URL with a non-privileged port, e.g. http://localhost:8765/ (and register that exact URL for your app at Trimble Connect Integrations).", ex);
            }

            // Authorization Code flow: no PKCE (same as Other Samples). AuthorityUrl ends with /oauth/
            var authorizeUrl = oauthBase + "authorize?" +
                "client_id=" + Uri.EscapeDataString(clientId) +
                "&response_type=code" +
                "&scope=" + Uri.EscapeDataString(scopeValue) +
                "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                "&state=" + Uri.EscapeDataString(state);

            try
            {
                Console.WriteLine("Opening browser for Trimble Identity sign-in...");
                Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true });

                var context = await listener.GetContextAsync().ConfigureAwait(false);
                var request = context.Request;
                var query = request.Url?.Query ?? "";
                var code = ParseQueryParam(query, "code");
                var error = ParseQueryParam(query, "error");
                var errorDesc = ParseQueryParam(query, "error_description");

                var response = context.Response;
                var successHtml = @"<!DOCTYPE html><html><head><title>Sign-in successful</title></head><body><p>Sign-in successful. You can close this window and return to the application.</p></body></html>";
                var buffer = Encoding.UTF8.GetBytes(successHtml);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 200;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                if (!string.IsNullOrEmpty(error))
                {
                    var msg = string.IsNullOrEmpty(errorDesc) ? error : errorDesc;
                    if (string.Equals(error, "invalid_client", StringComparison.OrdinalIgnoreCase) || (msg != null && msg.IndexOf("Unregistered", StringComparison.OrdinalIgnoreCase) >= 0))
                        throw new InvalidOperationException(
                            "OAuth error: Unregistered client (invalid_client).\n\n" +
                            "1. Use the exact ClientId and ClientKey from Trimble Connect Integrations for this app: https://www.trimble.com/en/products/trimble-connect/integrations\n" +
                            "2. Add the exact RedirectUrl to that app's allowed redirect URIs (e.g. http://localhost:8765/ with trailing slash). Ask connect-support@trimble.com to add it if needed.\n" +
                            "3. Ensure the app is registered for Trimble Identity / OAuth (same environment as AuthorityUrl: https://id.trimble.com/oauth/).");
                    throw new InvalidOperationException("OAuth error: " + msg);
                }
                if (string.IsNullOrEmpty(code))
                    throw new InvalidOperationException("No authorization code received. Sign-in may have been cancelled or failed.");

                listener.Stop();
                listener.Close();

                // Exchange code for token (Authorization Code: Basic client_id:client_secret)
                var tokenUrl = oauthBase + "token";
                var accessToken = await PostTokenRequestAsync(tokenUrl, clientId, clientSecret, "authorization_code", new Dictionary<string, string>
                {
                    { "code", code },
                    { "redirect_uri", redirectUri },
                    { "grant_type", "authorization_code" }
                }).ConfigureAwait(false);
                return accessToken;
            }
            catch (HttpListenerException)
            {
                listener.Stop();
                listener.Close();
                throw;
            }
        }

        static string ParseQueryParam(string query, string name)
        {
            if (string.IsNullOrEmpty(query)) return null;
            if (query.StartsWith("?", StringComparison.Ordinal)) query = query.Substring(1);
            foreach (var pair in query.Split('&'))
            {
                var eq = pair.IndexOf('=');
                if (eq < 0) continue;
                var key = Uri.UnescapeDataString(pair.Substring(0, eq).Trim());
                if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(pair.Substring(eq + 1).Trim());
            }
            return null;
        }

        static async Task<string> PostTokenRequestAsync(string tokenUrl, string clientId, string clientSecret, string grantType, Dictionary<string, string> form)
        {
            form["grant_type"] = grantType;
            form["client_id"] = clientId;

            var content = new StringBuilder();
            foreach (var kv in form)
            {
                if (content.Length > 0) content.Append('&');
                content.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
            }
            var body = content.ToString();

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId + ":" + clientSecret));

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                client.Headers[HttpRequestHeader.Accept] = "application/json";
                client.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
                try
                {
                    var response = await client.UploadStringTaskAsync(new Uri(tokenUrl), "POST", body).ConfigureAwait(false);
                    var json = JObject.Parse(response);
                    var token = json["access_token"]?.ToString();
                    if (string.IsNullOrEmpty(token))
                        throw new InvalidOperationException("Token response did not contain access_token. Response: " + response);
                    return token;
                }
                catch (WebException ex) when (ex.Response is HttpWebResponse resp && resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    string errBody = null;
                    try
                    {
                        using (var r = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                            errBody = r.ReadToEnd();
                    }
                    catch { }
                    var errMsg = errBody;
                    if (!string.IsNullOrEmpty(errBody))
                    {
                        try
                        {
                            var errJson = JObject.Parse(errBody);
                            var err = errJson["error"]?.ToString();
                            var desc = errJson["error_description"]?.ToString();
                            if (string.Equals(err, "invalid_grant", StringComparison.OrdinalIgnoreCase) &&
                                !string.IsNullOrEmpty(desc) && desc.IndexOf("redirect_uri", StringComparison.OrdinalIgnoreCase) >= 0)
                                throw new InvalidOperationException(
                                    "Token error: redirect_uri is invalid. RedirectUrl must match exactly what is registered in Trimble Connect Integrations (including trailing slash or not). Try setting RedirectUrl in App.config to http://localhost:8765 with no trailing slash.", ex);
                        }
                        catch (InvalidOperationException) { throw; }
                        catch { }
                    }
                    throw new InvalidOperationException("Token request failed: " + (errMsg ?? ex.Message), ex);
                }
            }
        }
    }
}
