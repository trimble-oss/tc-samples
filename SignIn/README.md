# Trimble Connect Authentication Samples

This repository contains .NET MAUI sample applications demonstrating different OAuth 2.0 authentication flows for Trimble Connect.

## ⚠️ Important Notice for PKCE Users

**Standard PKCE mode (`PkceMode.Pkce`) has known issues with refresh token handling.** When using the `SignIn.Maui.SerialPkce` project, always configure it with `PkceMode.SerialPkce`, not `PkceMode.Pkce`.

## Available Samples

### 1. Serial PKCE Sample (`SignIn.Maui.SerialPkce`) ✅ Recommended for Native Apps
Demonstrates Serial PKCE authentication with code verifier rotation.

- **Status**: ✅ Fully functional (when using `PkceMode.SerialPkce`)
- **Authentication**: Serial PKCE (no client secret required)
- **Best for**: Mobile and desktop applications
- **Security**: Enhanced security through code verifier rotation
- **Features**: Working refresh tokens, silent login, true public client
- **Configuration**: Use `PkceMode.SerialPkce` in AuthContext
- **Documentation**: [SignIn.Maui.SerialPkce/readme.md](SignIn.Maui.SerialPkce/readme.md)

### 2. Basic OAuth with Client Secret Sample (`SignIn.Maui`)
Traditional OAuth authentication flow with client secret.

- **Status**: ✅ Works correctly
- **Authentication**: Basic OAuth with client ID and client secret
- **Best for**: Server-side applications, backend services
- **Features**: Working refresh tokens, silent login
- **Note**: Not recommended for mobile/desktop apps (client secret security concerns)
- **Documentation**: [SignIn.Maui/readme.md](SignIn.Maui/readme.md)

## Quick Start

1. **Choose Your Authentication Flow**:
   - **For mobile/desktop apps**: Use Serial PKCE (no client secret, enhanced security)
   - **For server-side apps**: Use Basic OAuth with client secret (if you can securely store it)
   - Read the [Developer Guide](DEVELOPER_GUIDE.md) for detailed comparison

2. **Get Your Credentials**:
   - Register your application at [Trimble Developer Portal](https://developer.trimble.com/)
   - Obtain your Client ID and Redirect URI
   - For Basic OAuth: Also obtain Client Secret
   - For Serial PKCE: Client secret is NOT required

3. **Open the Appropriate Sample**:
   - For mobile/desktop: Open `SignIn.Maui.SerialPkce/Trimble.Connect.SignIn.Maui.SerialPkce.csproj`
   - For server-side: Open `SignIn.Maui/Trimble.Connect.SignIn.Maui.csproj`

4. **Configure and Run**:
   - Update `ViewModels/ShellViewModel.cs` with your credentials
   - Select your target platform
   - Run the application

## Documentation

### Comprehensive Guide
**[DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)** - Complete guide covering:
- Detailed explanation of all authentication flows
- Basic OAuth vs Standard PKCE vs Serial PKCE
- Why Standard PKCE mode is broken and Serial PKCE works
- Step-by-step implementation instructions
- Code examples and best practices
- Troubleshooting and FAQ

### Sample-Specific Documentation
- **Basic OAuth**: [SignIn.Maui/readme.md](SignIn.Maui/readme.md)
- **Serial PKCE**: [SignIn.Maui.SerialPkce/readme.md](SignIn.Maui.SerialPkce/readme.md)

## Key Differences at a Glance

| Feature | Basic OAuth with Client Secret | Standard PKCE Mode | Serial PKCE Mode |
|---------|-------------------------------|-------------------|------------------|
| **Sample Project** | SignIn.Maui | SignIn.Maui.SerialPkce (PkceMode.Pkce) | SignIn.Maui.SerialPkce (PkceMode.SerialPkce) |
| **Status** | ✅ Works | ⚠️ **Broken** | ✅ **Fully Functional** |
| **Authentication Type** | Basic OAuth | PKCE with static verifier | PKCE with rotating verifier |
| **Client Secret** | Required | Not required | Not required |
| **Code Verifier** | Not used | Static (but broken) | Rotates on every refresh |
| **Refresh Tokens** | ✅ Working | ❌ Not received | ✅ Working |
| **Silent Login** | ✅ Working | ❌ Not working | ✅ Working |
| **Public Client Support** | No (requires secret) | Yes (but broken) | Yes |
| **Security** | Standard | Enhanced (if it worked) | Enhanced |
| **Best For** | Server-side apps | ❌ Do not use | Mobile/desktop apps |
| **Recommendation** | Use for server-side | ❌ Do not use | ✅ Use for native apps |

## Requirements

- Visual Studio 2022 (latest version)
- .NET 9.0 or later
- .NET Multi-platform App UI development workload
- Platform SDKs (Android SDK, Xcode for iOS/macOS)
- Trimble Developer Portal account

## Support

For questions, issues, or feedback, visit:
- [Trimble Developer Portal](https://developer.trimble.com/)
- [Trimble Connect Documentation](https://developer.trimble.com/docs/connect)
- [Support and Community](https://developer.trimble.com/docs/connect#support-and-community)

---

**Note**: These samples use staging environment endpoints. Update the `AuthorityUri` for production use.
