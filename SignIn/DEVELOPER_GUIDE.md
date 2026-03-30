# Developer Guide: Choosing Your Authentication Flow

This guide helps you understand the authentication options for Trimble Connect .NET MAUI applications and choose the right approach for your needs.

---

## Table of Contents

1. [Overview](#overview)
2. [Authentication Options](#authentication-options)
3. [Basic OAuth with Client Secret](#basic-oauth-with-client-secret)
4. [Standard PKCE](#standard-pkce)
5. [Serial PKCE (Recommended)](#serial-pkce-recommended)
6. [Comparison Table](#comparison-table)
7. [When to Use Which](#when-to-use-which)
8. [Implementation Guide](#implementation-guide)
9. [Code Examples](#code-examples)
10. [Troubleshooting](#troubleshooting)
11. [FAQ](#faq)

---

## Overview

This repository demonstrates different OAuth 2.0 authentication approaches for Trimble Connect:

1. **Basic OAuth with Client Secret** - Traditional OAuth flow with client ID and secret
2. **Standard PKCE** - PKCE with static code verifier
3. **Serial PKCE** - Enhanced PKCE with code verifier rotation (recommended)

### What is PKCE?

PKCE (Proof Key for Code Exchange, pronounced "pixie") is an OAuth 2.0 extension that adds an additional security layer to the authorization code flow. It prevents authorization code interception attacks by using a dynamically generated code verifier and code challenge.

---

## Authentication Options

### 1. Basic OAuth with Client Secret

**Sample**: `SignIn.Maui`

**Configuration**:
```csharp
var authCtx = new AuthContext(clientId, clientSecret, appName, redirectUri);
```

**Status**: ✅ Works correctly

**Characteristics**:
- Uses client ID and client secret
- No PKCE mode specified
- Suitable for confidential clients
- Refresh tokens work properly

### 2. Standard PKCE

**Sample**: `SignIn.Maui.SerialPkce` (when configured with `PkceMode.Pkce`)

**Configuration**:
```csharp
var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.Pkce);
```

**Status**: ⚠️ **Known Issues** - Refresh tokens not received

**Characteristics**:
- Uses PKCE with static code verifier
- No client secret required
- **Issue**: `OnTokenRefreshed` event not triggered
- **Issue**: Silent login doesn't work

### 3. Serial PKCE

**Sample**: `SignIn.Maui.SerialPkce` (when configured with `PkceMode.SerialPkce`)

**Configuration**:
```csharp
var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.SerialPkce);
```

**Status**: ✅ Fully functional

**Characteristics**:
- Uses PKCE with rotating code verifier
- No client secret required
- Refresh tokens work properly
- Enhanced security

---

## Basic OAuth with Client Secret

### How It Works

1. **Initial Authentication**:
   - User authenticates via browser
   - App exchanges authorization code for tokens using client ID and client secret
   - Access token and refresh token are returned

2. **Token Refresh**:
   - App uses the refresh token + client secret to get new access tokens
   - Refresh token is returned for future refreshes

### Implementation (SignIn.Maui)

```csharp
var clientId = "your-client-id";
var clientSecret = "your-client-secret";
var redirectUri = "http://localhost";
var appName = "YourApp";

var authCtx = new AuthContext(clientId, clientSecret, appName, redirectUri)
{
    AuthorityUri = new Uri("https://stage.id.trimblecloud.com/oauth/")
};
```

### Pros and Cons

**Pros**:
- ✅ Works correctly with refresh tokens
- ✅ Simpler implementation
- ✅ Well-established OAuth pattern

**Cons**:
- ❌ Requires client secret
- ❌ Client secret in mobile/desktop apps can be extracted
- ❌ Not ideal for public clients (mobile/desktop apps)
- ❌ Less secure than PKCE for native applications

### When to Use

- Backend services or server-side applications
- Applications where client secret can be securely stored
- Quick prototypes or internal tools
- When PKCE is not required by your security policy

---

## Standard PKCE

> **⚠️ Warning**: Standard PKCE mode has known issues with refresh token handling. The `OnTokenRefreshed` event is not triggered, preventing silent login from working. **Use Serial PKCE instead.**

### How It Was Intended to Work

1. **Initial Authentication**:
   - App generates a random `code_verifier`
   - App creates a `code_challenge` from the verifier (SHA256 hash)
   - User authenticates via browser
   - App exchanges authorization code + code verifier for tokens

2. **Token Refresh** (Currently Not Working):
   - App uses the refresh token to get new access tokens
   - The same code verifier is reused for all subsequent token refreshes
   - Code verifier remains constant throughout the session

### Configuration

```csharp
// ⚠️ This configuration has known issues
var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.Pkce)
{
    AuthorityUri = new Uri("https://stage.id.trimblecloud.com/oauth/")
};
```

### Known Issues

- **Refresh Token Not Received**: The `OnTokenRefreshed` event is not triggered after initial authentication
- **Silent Login Fails**: Without refresh token persistence, users must re-authenticate on every app restart
- **Limited Functionality**: Initial authentication works but token refresh is broken

### Current Status

**Do not use Standard PKCE mode (`PkceMode.Pkce`) for new applications.** Use Serial PKCE (`PkceMode.SerialPkce`) instead, which resolves these issues.

---

## Serial PKCE (Recommended)

> **✅ Recommended**: Serial PKCE is the fully functional and recommended authentication flow for public client applications (mobile and desktop apps).

### How It Works

1. **Initial Authentication**:
   - App generates a random `code_verifier`
   - App creates a `code_challenge` from the verifier (SHA256 hash)
   - User authenticates via browser
   - App exchanges authorization code + code verifier for tokens
   - Refresh token + code verifier are persisted together

2. **Token Refresh**:
   - App uses the refresh token + **current code verifier** to get new tokens
   - **A new code verifier is generated** for each refresh
   - The new code verifier must be persisted for the next refresh
   - Code verifier rotates with each token refresh

3. **Silent Login** (App Restart):
   - App loads persisted refresh token + code verifier
   - App automatically refreshes tokens without user interaction
   - User is signed in without needing to re-authenticate

### Configuration (SignIn.Maui.SerialPkce)

```csharp
var clientId = "your-client-id";
var redirectUri = "http://localhost";
var appName = "YourApp";

// Use PkceMode.SerialPkce for working refresh tokens
var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.SerialPkce)
{
    AuthorityUri = new Uri("https://stage.id.trimblecloud.com/oauth/")
};
```

### Key Characteristics

- **Code Verifier Lifecycle**: Generated initially, then rotated on every token refresh
- **Client Secret**: Not required (true public client)
- **Persistence**: Both refresh token AND code verifier must be persisted together
- **Security**: Enhanced security through code verifier rotation
- **Functionality**: ✅ Fully working with proper refresh token handling
- **OnTokenRefreshed Event**: ✅ Properly triggered

### Use Cases

- Mobile applications (iOS, Android)
- Desktop applications (Windows, macOS)
- Any public client that cannot securely store client secrets
- Applications requiring enhanced security
- Production applications requiring silent login
- **Recommended for all new native applications**

---

## Comparison Table

| Feature | Basic OAuth with Client Secret | Standard PKCE | Serial PKCE |
|---------|-------------------------------|---------------|-------------|
| **Sample Project** | SignIn.Maui | SignIn.Maui.SerialPkce (PkceMode.Pkce) | SignIn.Maui.SerialPkce (PkceMode.SerialPkce) |
| **Status** | ✅ Works | ⚠️ **Broken** | ✅ **Fully Functional** |
| **Refresh Tokens** | ✅ Working | ❌ Not received | ✅ Working |
| **Silent Login** | ✅ Working | ❌ Not working | ✅ Working |
| **Client Secret** | Required | Not required | Not required |
| **Code Verifier** | Not used | Static (but broken) | Rotates on every refresh |
| **Persistence Required** | Refresh token only | Refresh token only (but not received) | Refresh token + code verifier |
| **Security Level** | Standard (secret in app) | Enhanced (if it worked) | Enhanced |
| **Public Client Support** | No | Yes (but broken) | Yes |
| **Best For** | Server-side apps | ❌ Not recommended | ✅ All native apps |
| **Recommendation** | Use for server-side only | ❌ Do not use | ✅ Use for all native apps |

---

## When to Use Which

### Use Serial PKCE When:

- ✅ Building mobile applications (iOS, Android)
- ✅ Building desktop applications (Windows, macOS)
- ✅ You need working silent login functionality
- ✅ You want enhanced security through code verifier rotation
- ✅ You're building a public client that cannot securely store secrets
- ✅ **Recommended for all new native applications**

### Use Basic OAuth with Client Secret When:

- Building server-side applications or backend services
- You can securely store client secrets (not in mobile/desktop apps)
- You need the traditional OAuth flow
- You're building internal tools with controlled distribution

### Do NOT Use Standard PKCE (`PkceMode.Pkce`) Because:

- ❌ `OnTokenRefreshed` event is not triggered
- ❌ Refresh tokens are not received
- ❌ Silent login doesn't work
- ❌ Users must re-authenticate on every app restart
- ❌ This is a known issue with no current workaround

---

## Implementation Guide

### Option 1: Serial PKCE (Recommended for Native Apps)

#### 1. Configure AuthContext with Serial PKCE Mode

In your `ShellViewModel.cs`:

```csharp
var clientId = "your-client-id";
var redirectUri = "http://localhost";
var appName = "YourAppName";

// Use PkceMode.SerialPkce - this is the key to making it work
var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.SerialPkce)
{
    AuthorityUri = new Uri("https://stage.id.trimblecloud.com/oauth/")
};

loginContext.AuthContext = authCtx;
```

**Critical**: 
- Pass `null` for client secret (second parameter)
- Specify `PkceMode.SerialPkce` as the fifth parameter (NOT `PkceMode.Pkce`)

#### 2. Handle Token Refresh with Code Verifier Persistence

In your `LoginViewModel.cs`:

```csharp
public LoginViewModel(IAuthCodeCredentialsProvider loginContext, IShellViewModel shellViewModel)
{
    this.authCodeCredentialsProvider = loginContext;
    this.shellViewModel = shellViewModel;
    
    // Subscribe to token refresh event
    this.authCodeCredentialsProvider.OnTokenRefreshed += AuthCodeCredentialsProvider_OnTokenRefreshed;
}

private void AuthCodeCredentialsProvider_OnTokenRefreshed(string refreshToken, long timeInTicks)
{
    // In Serial PKCE, refreshToken contains both refresh token and code verifier
    // Format: "refreshToken|codeVerifier"
    var path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        "YourApp.SerialPkce", 
        "config.json"
    );
    
    var refreshTokenInfo = new RefreshTokenInfo(refreshToken, timeInTicks, true);
    
    try
    {
        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        string json = JsonConvert.SerializeObject(refreshTokenInfo);
        File.WriteAllText(path, json);
        
        Console.WriteLine("[OnTokenRefreshed] Saved refresh token and code verifier");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error saving refresh token and code verifier: {ex.Message}");
    }
}
```

**Important**: The `refreshToken` parameter contains both the refresh token and code verifier concatenated with a pipe separator. Save this entire string.

#### 3. Implement Silent Login

```csharp
public void DoSilentLogin()
{
    try
    {
        var refreshToken = string.Empty;
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            "YourApp.SerialPkce", 
            "config.json"
        );

        if (File.Exists(path))
        {
            using (var fileStream = File.OpenText(path))
            {
                using (var reader = new JsonTextReader(fileStream))
                {
                    var tokenInfo = JsonSerializer.CreateDefault(
                        new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local }
                    ).Deserialize<RefreshTokenInfo>(reader);
                    
                    refreshToken = tokenInfo?.RefreshToken;
                }
            }
        }

        if (!string.IsNullOrEmpty(refreshToken))
        {
            IsLoading = true;
            ShowLogin = false;

            Task.Run(async () =>
            {
                try
                {
                    // The SDK automatically parses the "refreshToken|codeVerifier" format
                    authCodeCredentialsProvider.WithRefreshToken(refreshToken);
                    
                    var accessToken = await authCodeCredentialsProvider.RefreshTokenAsync();
                    
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        // Navigate to main app view
                        var projectListViewModel = Application.Current.Handler.MauiContext
                            .Services.GetService<IProjectsListViewModel>();
                        await projectListViewModel.PopulateRegions();
                        
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Shell.Current.GoToAsync($"//{nameof(ProjectsView)}");
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Silent login failed: {ex.Message}");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsLoading = false;
                        ShowLogin = true;
                    });
                }
            });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DoSilentLogin error: {ex.Message}");
    }
}
```

#### 4. Handle Logout

```csharp
public void DoLogOut()
{
    Task.Run(async () =>
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // Clear tokens from provider
            await authCodeCredentialsProvider.Logout();
            
            // Delete persisted config file
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                "YourApp.SerialPkce", 
                "config.json"
            );
            
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing config on logout: {ex.Message}");
            }
            
            // Navigate back to login
            await Shell.Current.GoToAsync($"//{nameof(LoginView)}");
        });
    });
}
```

---

### Option 2: Basic OAuth with Client Secret

#### Configuration

In your `ShellViewModel.cs`:

```csharp
var clientId = "your-client-id";
var clientSecret = "your-client-secret";
var redirectUri = "http://localhost";
var appName = "YourApp";

var authCtx = new AuthContext(clientId, clientSecret, appName, redirectUri)
{
    AuthorityUri = new Uri("https://stage.id.trimblecloud.com/oauth/")
};

loginContext.AuthContext = authCtx;
```

#### Token Refresh Handling

```csharp
private void AuthCodeCredentialsProvider_OnTokenRefreshed(string refreshToken, long timeInTicks)
{
    // Save only the refresh token
    var path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        "YourApp", 
        "config.json"
    );
    
    var refreshTokenInfo = new RefreshTokenInfo(refreshToken, timeInTicks, true);
    
    if (!Directory.Exists(Path.GetDirectoryName(path)))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
    }

    string json = JsonConvert.SerializeObject(refreshTokenInfo);
    File.WriteAllText(path, json);
}
```

#### Silent Login

```csharp
public void DoSilentLogin()
{
    var refreshToken = LoadRefreshTokenFromStorage();
    
    if (!string.IsNullOrEmpty(refreshToken))
    {
        authCodeCredentialsProvider.WithRefreshToken(refreshToken);
        var accessToken = await authCodeCredentialsProvider.RefreshTokenAsync();
        
        if (!string.IsNullOrEmpty(accessToken))
        {
            // Navigate to main app view
        }
    }
}
```

---

## Code Examples

### Example 1: Serial PKCE (Recommended for Native Apps)

```csharp
// ShellViewModel.cs
public ShellViewModel(IAuthCodeCredentialsProvider loginContext)
{
    var clientId = "your-client-id";
    var redirectUri = "http://localhost";
    var appName = "MyApp";
    
    // Serial PKCE: no client secret, PkceMode.SerialPkce
    var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.SerialPkce)
    {
        AuthorityUri = new Uri("https://stage.id.trimblecloud.com/oauth/")
    };
    
    loginContext.AuthContext = authCtx;
}
```

### Example 2: Basic OAuth with Client Secret

```csharp
// ShellViewModel.cs
public ShellViewModel(IAuthCodeCredentialsProvider loginContext)
{
    var clientId = "your-client-id";
    var clientSecret = "your-client-secret";
    var redirectUri = "http://localhost";
    var appName = "MyApp";
    
    // Basic OAuth with client secret
    var authCtx = new AuthContext(clientId, clientSecret, appName, redirectUri)
    {
        AuthorityUri = new Uri("https://stage.id.trimblecloud.com/oauth/")
    };
    
    loginContext.AuthContext = authCtx;
}
```

### Example 3: What NOT to Do (Broken Standard PKCE)

```csharp
// ❌ DO NOT USE - Standard PKCE mode has known issues
var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.Pkce);

// Issues:
// - OnTokenRefreshed event is not triggered
// - Refresh tokens are not received
// - Silent login doesn't work
```

### Example 4: Switching Between Modes

The `SignIn.Maui.SerialPkce` project can be configured for different PKCE modes:

```csharp
// Standard PKCE (broken - don't use)
var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.Pkce);

// Serial PKCE (working - use this)
var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.SerialPkce);
```

**Key Point**: The only difference is the `PkceMode` parameter, but this makes all the difference in functionality!

---

## Troubleshooting

### Common Issues

#### Standard PKCE Mode Issues (`PkceMode.Pkce`)

**Issue**: `OnTokenRefreshed` event never fires
- **Cause**: Known issue with Standard PKCE mode (`PkceMode.Pkce`)
- **Solution**: Change to `PkceMode.SerialPkce` in your AuthContext configuration

**Issue**: Silent login doesn't work - users must sign in every time
- **Cause**: Refresh tokens are not being received due to the issue above
- **Solution**: Change to `PkceMode.SerialPkce`

**Issue**: Token refresh fails after app restart
- **Cause**: Refresh token not received in the first place
- **Solution**: Change to `PkceMode.SerialPkce`

#### Serial PKCE Mode (`PkceMode.SerialPkce`)

**Issue**: Token refresh fails with "invalid_grant"
- **Cause**: Code verifier not persisted or corrupted
- **Solution**: Ensure both refresh token and code verifier are saved in `OnTokenRefreshed` event

**Issue**: Silent login fails after app restart
- **Cause**: Code verifier not restored properly
- **Solution**: Verify the persisted data contains the combined "refreshToken|codeVerifier" format

**Issue**: Token refresh works once but fails on subsequent refreshes
- **Cause**: New code verifier not being persisted after refresh
- **Solution**: Ensure `OnTokenRefreshed` event is triggered and saves the new code verifier

**Issue**: Config file corruption
- **Cause**: Concurrent writes or incomplete serialization
- **Solution**: Add try-catch blocks and delete corrupted config files to allow fresh login

#### Basic OAuth with Client Secret

**Issue**: Authentication fails with "invalid_client"
- **Cause**: Client secret is incorrect or missing
- **Solution**: Verify client secret matches your Trimble Developer Portal configuration

**Issue**: Client secret exposed in mobile app
- **Cause**: Client secrets in mobile apps can be extracted through reverse engineering
- **Solution**: Use Serial PKCE instead, which doesn't require a client secret

### Debugging Tips

1. **Enable Logging**: Add console logging to track authentication flow
   ```csharp
   Console.WriteLine($"[ShellViewModel] Using PkceMode: {PkceMode.SerialPkce}");
   Console.WriteLine($"[DoSilentLogin] Starting silent login");
   Console.WriteLine($"[OnTokenRefreshed] Event triggered - saving tokens");
   ```

2. **Verify PKCE Mode**: Ensure you're using the correct mode
   ```csharp
   // ✅ Correct - Serial PKCE
   var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.SerialPkce);
   
   // ❌ Wrong - Standard PKCE (broken)
   var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.Pkce);
   ```

3. **Verify Persistence**: Check that config files are created
   - Basic OAuth: `%USERPROFILE%\YourApp\config.json`
   - Serial PKCE: `%USERPROFILE%\YourApp.SerialPkce\config.json`

4. **Monitor Event Handlers**: Ensure `OnTokenRefreshed` event is properly subscribed
   ```csharp
   Console.WriteLine("[LoginViewModel] Subscribing to OnTokenRefreshed");
   this.authCodeCredentialsProvider.OnTokenRefreshed += AuthCodeCredentialsProvider_OnTokenRefreshed;
   ```

5. **Test Token Refresh**: After initial login, check if `OnTokenRefreshed` is called
   - If using `PkceMode.Pkce`: Event will NOT be called (this is the bug)
   - If using `PkceMode.SerialPkce`: Event WILL be called

---

## Security Best Practices

### For Serial PKCE

1. **Secure Storage**: Use platform-specific secure storage mechanisms
   - **Windows**: Windows Credential Manager or Data Protection API (DPAPI)
   - **iOS**: Keychain Services
   - **Android**: Android Keystore System
   - **macOS**: Keychain Services

2. **Encrypt Before Persisting**: Even with secure storage, encrypt sensitive data
   ```csharp
   // Example using DPAPI on Windows
   var encryptedData = ProtectedData.Protect(
       Encoding.UTF8.GetBytes(refreshToken), 
       null, 
       DataProtectionScope.CurrentUser
   );
   ```

3. **Atomic Persistence**: Save refresh token and code verifier atomically
   ```csharp
   var tempPath = path + ".tmp";
   File.WriteAllText(tempPath, json);
   File.Move(tempPath, path, overwrite: true);
   ```

4. **Cleanup on Logout**: Always delete persisted config file
   ```csharp
   if (File.Exists(path))
   {
       File.Delete(path);
   }
   ```

5. **Error Recovery**: If silent login fails, fall back to interactive login
   ```csharp
   catch (Exception ex)
   {
       Console.WriteLine($"Silent login failed: {ex.Message}");
       File.Delete(path); // Delete corrupted config
       ShowLogin = true;
   }
   ```

### For Basic OAuth with Client Secret

1. **Never Embed Secrets in Code**: Use configuration files or environment variables
2. **Secure Storage**: Store client secrets in secure configuration systems
3. **Limit Distribution**: Only use in server-side or controlled environments
4. **Consider Migration**: For mobile/desktop apps, migrate to Serial PKCE

---

## Sample Projects

### SignIn.Maui.SerialPkce (PKCE Sample - Recommended)

- **Location**: `SignIn/SignIn.Maui.SerialPkce/`
- **Status**: ✅ Fully functional (when using `PkceMode.SerialPkce`)
- **Authentication Options**:
  - ✅ **Serial PKCE** (`PkceMode.SerialPkce`) - Working, recommended
  - ⚠️ **Standard PKCE** (`PkceMode.Pkce`) - Broken, do not use
- **Features**:
  - No client secret required
  - Working refresh token handling (Serial PKCE mode)
  - Code verifier rotation on token refresh
  - Combined refresh token + code verifier persistence
  - Working silent login functionality
  - Enhanced error handling and logging
- **Configuration**: Set `PkceMode.SerialPkce` in `ViewModels/ShellViewModel.cs`

### SignIn.Maui (Basic OAuth Sample)

- **Location**: `SignIn/SignIn.Maui/`
- **Status**: ✅ Works correctly
- **Authentication**: Basic OAuth with client ID and client secret
- **Features**:
  - Traditional OAuth flow
  - Refresh tokens work properly
  - Silent login works
- **Use Case**: Server-side applications or when client secret can be securely stored
- **Note**: Not recommended for mobile/desktop apps due to client secret security concerns

---

## Getting Started

### Prerequisites

- Visual Studio 2022 (latest version recommended)
- .NET Multi-platform App UI development workload
- Platform SDKs for your target platforms (Android SDK, Xcode for iOS/macOS)
- Trimble Developer Portal account with registered application

### Quick Start with Serial PKCE (Recommended)

1. **Register Your Application**:
   - Go to [Trimble Developer Portal](https://developer.trimble.com/)
   - Create or select your application
   - Note your Client ID
   - Configure your Redirect URI (e.g., `http://localhost`)
   - **Note**: Client secret is NOT required for Serial PKCE

2. **Open the Serial PKCE Project**:
   - Open `SignIn.Maui.SerialPkce/Trimble.Connect.SignIn.Maui.SerialPkce.csproj` in Visual Studio 2022

3. **Configure for Serial PKCE**:
   - Open `ViewModels/ShellViewModel.cs`
   - Verify the configuration uses `PkceMode.SerialPkce`:
     ```csharp
     var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.SerialPkce)
     ```
   - Update `clientId`, `redirectUri`, and `appName` with your values
   - **Do NOT add a client secret** - pass `null`

4. **Run the Application**:
   - Select your target platform (Android, iOS, Windows, macOS)
   - Click Run in Visual Studio
   - Test authentication and verify silent login works after app restart

---

## API Reference

### AuthContext Constructor Signatures

#### Basic OAuth with Client Secret
```csharp
public AuthContext(
    string clientId,
    string clientSecret,      // Required client secret
    string appName,
    string redirectUri
)
// Status: ✅ Works correctly
```

#### Standard PKCE (Not Recommended)
```csharp
public AuthContext(
    string clientId,
    string clientSecret,      // Pass null
    string appName,
    string redirectUri,
    PkceMode pkceMode         // PkceMode.Pkce
)
// Status: ⚠️ Has issues - OnTokenRefreshed not triggered
```

#### Serial PKCE (Recommended)
```csharp
public AuthContext(
    string clientId,
    string clientSecret,      // Pass null for public clients
    string appName,
    string redirectUri,
    PkceMode pkceMode         // PkceMode.SerialPkce
)
// Status: ✅ Fully functional
```

### PkceMode Enum

```csharp
public enum PkceMode
{
    Pkce,           // Standard PKCE - ⚠️ Has known issues, do not use
    SerialPkce      // Serial PKCE - ✅ Recommended for all native apps
}
```

### Key Methods

#### IAuthCodeCredentialsProvider

- `AcquireTokenAsync()`: Initiates interactive authentication flow
- `RefreshTokenAsync()`: Refreshes access token using refresh token (and code verifier for PKCE modes)
- `WithRefreshToken(string refreshToken)`: Sets refresh token for silent login
- `Logout()`: Clears all tokens and authentication state
- `Cancel()`: Cancels ongoing authentication flow

#### Events

- `OnTokenRefreshed`: Triggered when tokens are refreshed
  - **Basic OAuth**: ✅ Provides refresh token
  - **Standard PKCE** (`PkceMode.Pkce`): ❌ Not triggered (known issue)
  - **Serial PKCE** (`PkceMode.SerialPkce`): ✅ Provides combined "refreshToken|codeVerifier" string

---

## FAQ

### Q: Which authentication flow should I use for my mobile/desktop app?
**A**: Use **Serial PKCE** (`PkceMode.SerialPkce`). It's the only PKCE mode that works correctly and doesn't require a client secret.

### Q: What's the difference between the two sample projects?
**A**: 
- **SignIn.Maui**: Uses basic OAuth with client ID and client secret (no PKCE)
- **SignIn.Maui.SerialPkce**: Can be configured for PKCE modes (use `PkceMode.SerialPkce`)

### Q: Why doesn't Standard PKCE mode work?
**A**: Standard PKCE mode (`PkceMode.Pkce`) has a known issue where the `OnTokenRefreshed` event is not triggered, preventing refresh tokens from being received and persisted. This makes silent login impossible.

### Q: Can I use Standard PKCE mode (`PkceMode.Pkce`)?
**A**: No, Standard PKCE mode is broken. Always use `PkceMode.SerialPkce` instead.

### Q: Do I need a client secret for Serial PKCE?
**A**: No, Serial PKCE is designed for public clients and does not require a client secret. Pass `null` for the client secret parameter.

### Q: Should I use Basic OAuth or Serial PKCE?
**A**: 
- **For mobile/desktop apps**: Use Serial PKCE (no client secret, more secure)
- **For server-side apps**: Use Basic OAuth with client secret (if you can securely store the secret)

### Q: What happens if I lose the code verifier in Serial PKCE?
**A**: Token refresh will fail. The user must re-authenticate interactively to generate a new code verifier.

### Q: How is the code verifier stored in Serial PKCE?
**A**: The `OnTokenRefreshed` event provides a combined string in the format "refreshToken|codeVerifier". Save this entire string, and the SDK will automatically parse it when you call `WithRefreshToken()`.

### Q: Can I switch from Basic OAuth to Serial PKCE?
**A**: Yes, but users will need to re-authenticate. Update your AuthContext configuration and token persistence logic as shown in the implementation guide.

### Q: How often does the code verifier rotate in Serial PKCE?
**A**: The code verifier rotates every time the access token is refreshed (typically every hour, depending on token lifetime).

### Q: What should I persist for silent login?
- **Basic OAuth**: Refresh token only
- **Serial PKCE**: Combined refresh token + code verifier (as a single string from `OnTokenRefreshed`)

### Q: Does Serial PKCE work on all platforms?
**A**: Yes, Serial PKCE works on Windows, Android, iOS, and macOS.

### Q: What version of the SDK do I need?
**A**: Trimble.Identity.OAuth.AuthCode version 2.1.10 or later is required for Serial PKCE support.

### Q: Can I test both Standard and Serial PKCE modes?
**A**: Technically yes (by changing the `PkceMode` parameter), but Standard PKCE mode is broken, so only Serial PKCE will work properly.

---

## Version Requirements

- **Trimble.Identity.OAuth.AuthCode**: Version 2.1.10 or later (for Serial PKCE support)
- **Trimble.Connect.Client**: Version 2.7.4 or later
- **.NET**: .NET 9.0 or later
- **MAUI**: Microsoft.Maui.Controls 9.0.120 or later

---

## Additional Resources

- [Trimble Developer Portal](https://developer.trimble.com/)
- [Trimble Connect Documentation](https://developer.trimble.com/docs/connect)
- [OAuth 2.0 PKCE Specification (RFC 7636)](https://tools.ietf.org/html/rfc7636)
- [Support and Community](https://developer.trimble.com/docs/connect#support-and-community)

---

## Support

For questions, issues, or feedback:

- Visit the [Trimble Developer Portal](https://developer.trimble.com/)
- Check the [Trimble Connect Documentation](https://developer.trimble.com/docs/connect)
- Contact [Support and Community](https://developer.trimble.com/docs/connect#support-and-community)

---

**Last Updated**: March 2026
