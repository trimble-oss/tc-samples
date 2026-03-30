Trimble Connect .NET SDK - Serial PKCE Sample

This sample demonstrates how to implement Serial PKCE authentication in a .NET MAUI application using the Trimble Connect .NET SDK.

## What is Serial PKCE?

Serial PKCE is an enhanced OAuth 2.0 authentication flow that rotates the code verifier on every token refresh, providing additional security for public clients. Unlike Standard PKCE, Serial PKCE does not require a client secret, making it ideal for mobile and desktop applications.

For a detailed comparison between Standard PKCE and Serial PKCE, see the [Developer Guide](../DEVELOPER_GUIDE.md).

---

## Key Features

- **Code Verifier Rotation**: Automatically generates a new code verifier on each token refresh
- **Public Client**: No client secret required
- **Silent Login**: Supports automatic re-authentication using persisted refresh token and code verifier
- **Enhanced Security**: Reduces risk of token theft through code verifier rotation
- **Cross-Platform**: Works on Windows, Android, iOS, and macOS

---

## Getting Started

### Requirements

* Visual Studio 2022 (latest version recommended)
* .NET Multi-platform App UI development workload installed in Visual Studio
* Necessary platform SDKs installed for your target platforms (e.g., Android SDK, Xcode for iOS/macOS)
* Trimble Developer Portal account with registered application

### Setup & Configuration

1. **Trimble Identity Application Setup**:
   - Register your application at the [Trimble Developer Portal](https://developer.trimble.com/)
   - Obtain your Client ID
   - Configure your Redirect URI (e.g., `http://localhost`)
   - **Note**: Client secret is NOT required for Serial PKCE

2. **Configure the Sample**:
   - Open `ViewModels/ShellViewModel.cs`
   - Update the following values:
     ```csharp
     var clientId = "your-client-id";        // Your Client ID
     var redirectUri = "http://localhost";   // Your Redirect URI
     var appName = "YourAppName";            // Your Application Name
     ```
   - **Do NOT add a client secret** - Serial PKCE is designed for public clients

### Running the Sample

1. Open the project: Open `Trimble.Connect.SignIn.Maui.SerialPkce.csproj` in Visual Studio 2022
2. Select Target: Choose your desired target platform (e.g., Android Emulator, iOS Simulator/Device, Windows Machine) from the Visual Studio toolbar
3. Run: Click the 'Run' button (green play icon) in Visual Studio

---

## How It Works

### Authentication Flow

1. **Initial Sign In**:
   - User clicks "Sign In" button
   - App generates a code verifier and code challenge
   - Browser opens for user authentication
   - After successful authentication, app receives authorization code
   - App exchanges code + code verifier for access token and refresh token
   - Refresh token + code verifier are persisted together

2. **Token Refresh**:
   - When access token expires, app uses refresh token + current code verifier
   - Authorization server validates and issues new tokens
   - **New code verifier is generated** and returned with new refresh token
   - New refresh token + new code verifier are persisted

3. **Silent Login** (App Restart):
   - App loads persisted refresh token + code verifier
   - App automatically refreshes tokens without user interaction
   - User is signed in without needing to re-authenticate

4. **Sign Out**:
   - App clears all tokens from memory
   - Persisted config file is deleted
   - User must sign in again

### Key Implementation Details

#### AuthContext Configuration

```csharp
var authCtx = new AuthContext(clientId, null, appName, redirectUri, PkceMode.SerialPkce)
{
    AuthorityUri = new Uri("https://stage.id.trimblecloud.com/oauth/")
};
```

**Important Parameters**:
- `clientId`: Your application's Client ID from Trimble Developer Portal
- `null`: No client secret (second parameter must be null)
- `PkceMode.SerialPkce`: Enables Serial PKCE mode with code verifier rotation

#### Token Persistence

The `OnTokenRefreshed` event provides a combined string containing both the refresh token and code verifier:

```csharp
private void AuthCodeCredentialsProvider_OnTokenRefreshed(string refreshToken, long timeInTicks)
{
    // refreshToken contains: "actualRefreshToken|codeVerifier"
    // Save this combined string for silent login
    var refreshTokenInfo = new RefreshTokenInfo(refreshToken, timeInTicks, true);
    // ... persist to secure storage
}
```

#### Silent Login

```csharp
public void DoSilentLogin()
{
    var refreshToken = LoadFromStorage(); // Load combined "token|verifier" string
    
    if (!string.IsNullOrEmpty(refreshToken))
    {
        // SDK automatically parses the combined format
        authCodeCredentialsProvider.WithRefreshToken(refreshToken);
        var accessToken = await authCodeCredentialsProvider.RefreshTokenAsync();
        // ... handle success
    }
}
```

---

## Project Structure

```
SignIn.Maui.SerialPkce/
├── ViewModels/
│   ├── ShellViewModel.cs          # AuthContext configuration with Serial PKCE
│   ├── LoginViewModel.cs          # Authentication logic and token management
│   └── ProjectsListViewModel.cs   # Post-authentication functionality
├── Models/
│   └── RefreshTokenInfo.cs        # Token persistence model
├── LoginView.xaml                 # Login UI
├── ProjectsView.xaml              # Main app view after authentication
└── MauiProgram.cs                 # Dependency injection setup
```

---

## Differences from Standard PKCE Sample

| Aspect | Standard PKCE | Serial PKCE (This Sample) |
|--------|---------------|---------------------------|
| **Client Secret** | Required | Not required (null) |
| **AuthContext** | `new AuthContext(id, secret, name, uri)` | `new AuthContext(id, null, name, uri, PkceMode.SerialPkce)` |
| **Code Verifier** | Static (reused) | Rotates on every refresh |
| **Persistence** | Refresh token only | Refresh token + code verifier |
| **OnTokenRefreshed** | Returns refresh token | Returns "refreshToken\|codeVerifier" |
| **Security** | Standard | Enhanced |

---

## Testing the Sample

### Test Scenarios

1. **Initial Authentication**:
   - Launch the app
   - Click "Sign In"
   - Complete authentication in browser
   - Verify successful login and navigation to Projects view

2. **Token Refresh**:
   - Wait for access token to expire (or force refresh)
   - Verify automatic token refresh without user interaction
   - Check that new code verifier is persisted

3. **Silent Login**:
   - Close and restart the application
   - Verify automatic sign-in without browser interaction
   - Confirm navigation to Projects view

4. **Sign Out**:
   - Click sign out
   - Verify return to login screen
   - Restart app and confirm user must sign in again

---

## Troubleshooting

### Common Issues

**Silent login fails after app restart**:
- Check that config file exists at `%USERPROFILE%\SignInSample.SerialPkce\config.json`
- Verify file contains valid JSON with refresh token
- Check console logs for error messages

**Token refresh fails with "invalid_grant"**:
- Code verifier may be corrupted or missing
- Delete config file and perform fresh login
- Ensure `OnTokenRefreshed` event is properly saving data

**Browser doesn't launch on Windows**:
- Ensure default browser is set
- Check redirect URI matches configuration
- Verify no firewall blocking localhost

---

## Support

See https://developer.trimble.com/docs/connect#support-and-community.

---

## Additional Documentation

- [Developer Guide](../DEVELOPER_GUIDE.md) - Comprehensive guide to choosing between Standard and Serial PKCE
- [Trimble Connect API Documentation](https://developer.trimble.com/docs/connect)
- [OAuth 2.0 PKCE Specification](https://tools.ietf.org/html/rfc7636)
