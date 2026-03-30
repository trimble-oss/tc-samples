Trimble Connect .NET SDK - Basic OAuth with Client Secret Sample

This sample demonstrates basic OAuth authentication using client ID and client secret for Trimble Connect.

---

## What This Sample Demonstrates

This sample shows basic OAuth authentication using:
- Client ID and Client Secret
- Authorization Code flow
- Traditional OAuth 2.0 flow (no PKCE)

## When to Use This Sample

- Server-side applications or backend services
- Applications where client secret can be securely stored
- Internal tools with controlled distribution
- Quick prototypes

## When NOT to Use This Sample

For **mobile and desktop applications**, use Serial PKCE instead:
- ✅ No client secret required (more secure for public clients)
- ✅ Enhanced security through code verifier rotation
- ✅ Better suited for native applications

See the [Serial PKCE sample](../SignIn.Maui.SerialPkce/) for the recommended approach for mobile/desktop apps.

---

## Authentication Samples

- **SignIn.Maui** (This sample): Basic OAuth with client secret - works correctly
- **SignIn.Maui.SerialPkce**: Serial PKCE - recommended for mobile/desktop apps

For a comprehensive guide on choosing the right authentication flow, see the [Developer Guide](../DEVELOPER_GUIDE.md).

---

 Getting Started

This sample demonstrates basic OAuth authentication with client secret within a .NET MAUI application.

 Requirements

* Visual Studio 2022 (latest version recommended)
* .NET Multi-platform App UI development workload installed in Visual Studio.
    * Ensure you have the necessary platform SDKs installed for your target platforms (e.g., Android SDK, Xcode for iOS/macOS).

 Setup & Configuration

To run this sample, you'll need to configure your Trimble Identity application details and handle platform-specific codesigning.

1.  Trimble Identity Application Setup:
    * You need a Client ID and a Redirect URI from a registered application in the [Trimble Developer Portal](https://developer.trimble.com/).
    * Configure these values within the project.

 Running the Sample

1.  Open the project: Open `SignIn.Maui.csproj` in Visual Studio 2022.
2.  Select Target: Choose your desired target platform (e.g., Android Emulator, iOS Simulator/Device, Windows Machine) from the Visual Studio toolbar.
3.  Run: Click the 'Run' button (green play icon) in Visual Studio.

---

 Support

See https://developer.trimble.com/docs/connect#support-and-community.