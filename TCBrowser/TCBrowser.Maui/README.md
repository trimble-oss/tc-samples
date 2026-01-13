# TCBrowser - Trimble Connect Sample Application

A .NET MAUI sample application demonstrating how to integrate with Trimble Connect services to browse projects, files, todos, and views.

## Overview

TCBrowser is a cross-platform sample application built with .NET MAUI that showcases integration with the Trimble Connect SDK. It provides a user-friendly interface for:

- **Projects**: Browse and view Trimble Connect projects
- **Files**: Navigate file hierarchies, view files in browser
- **Todos**: View and open todos in the web interface
- **Views**: View and open 3D views in the web interface

## Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** (17.8 or later) with .NET MAUI workload, or
- **Visual Studio Code** with C# extension and .NET MAUI extensions
- **Trimble Connect Account** with appropriate permissions
- **Trimble Connect Client Credentials** (Client ID and Client Secret)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd tc-samples/TCBrowser
```

### 2. Configure Environment

**⚠️ IMPORTANT**: You must configure your Trimble Connect client credentials before running the application.

1. Copy `environments.json.example` to `environments.json`:
   ```bash
   cp TCBrowser.Maui/environments.json.example TCBrowser.Maui/environments.json
   ```

2. Edit `environments.json` and replace the placeholder values:
   ```json
   {
     "DefaultEnvironment": "Production",
     "Environments": {
       "Production": {
         "AuthorityUri": "https://id.trimble.com/oauth/",
         "ServiceUri": "https://app.connect.trimble.com/tc/api/2.0/",
         "WebAppUri": "https://app.connect.trimble.com",
         "WebViewerUri": "https://web.connect.trimble.com",
         "EnvironmentName": "Production",
         "ClientId": "YOUR_PRODUCTION_CLIENT_ID",
         "ClientSecret": "YOUR_PRODUCTION_CLIENT_SECRET"
       }
     }
   }
   ```

3. **Obtain Client Credentials**:
   - Contact your Trimble Connect administrator, or
   - Use the Trimble Connect Developer Portal to register your application
   - Ensure credentials are registered for the environment you plan to use (Production/Stage)

**⚠️ Security Note**: Never commit `environments.json` with real credentials to version control. The file is already in `.gitignore`.

### 3. Build the Application

```bash
cd TCBrowser.Maui
dotnet restore
dotnet build
```

### 4. Run the Application

**Windows:**
```bash
dotnet run --framework net8.0-windows10.0.19041.0
```

**Android:**
- Connect an Android device or start an emulator
- Run: `dotnet build -t:Run -f net8.0-android`

**iOS/MacCatalyst:**
- Requires macOS and Xcode
- Configure code signing in the project file (see Configuration section)
- Run: `dotnet build -t:Run -f net8.0-ios` or `net8.0-maccatalyst`

## Configuration

### Environment Selection

The application includes an environment picker on the login screen that allows you to switch between different Trimble Connect environments (Production, Stage, etc.). Your selection is saved and will be used for subsequent logins.

### Code Signing (iOS/MacCatalyst)

For iOS and MacCatalyst builds, you need to configure code signing:

1. Open `TCBrowser.csproj`
2. Find the `PropertyGroup` sections for iOS/MacCatalyst
3. Configure your Apple Developer certificate:
   ```xml
   <CodesignKey>Apple Development: Your Name (YOUR_CERT_ID)</CodesignKey>
   ```

See the project file for detailed comments on code signing configuration.

## Project Structure

```
TCBrowser.Maui/
├── Models/              # Data models (EnvironmentConfig, ProjectMetaData, etc.)
├── ViewModels/         # MVVM view models
├── Views/              # XAML pages
├── UserControls/       # Reusable UI controls
├── Services/           # Business logic services (ConfigService, etc.)
├── Converters/         # Value converters for data binding
├── Platforms/          # Platform-specific code
├── Resources/           # Images, fonts, styles
├── environments.json   # Environment configuration (create from .example)
└── config.README.md    # Detailed configuration documentation
```

## Features

### Projects View
- Browse all accessible Trimble Connect projects
- Filter by region
- View project thumbnails and metadata

### Project Details
- **Files Tab**: Navigate file/folder hierarchy, open files in web browser
- **Todos Tab**: View todos, open in web interface
- **Views Tab**: View 3D views, open in web viewer

### Environment Management
- Switch between Production and Stage environments
- Environment selection persists across sessions
- All URIs and credentials managed per environment

## Troubleshooting

### "Invalid Client" or "Unregistered Client" Error
- Verify your `ClientId` and `ClientSecret` are correct in `environments.json`
- Ensure credentials are registered for the selected environment
- Contact your Trimble Connect administrator to verify credentials

### Authentication Fails
- Check that `ClientId` and `ClientSecret` are not null in `environments.json`
- Verify the `AuthorityUri` matches the environment you're using
- Ensure your credentials have necessary permissions

### Build Errors
- Ensure .NET 8.0 SDK is installed
- Verify .NET MAUI workload is installed: `dotnet workload install maui`
- For iOS/MacCatalyst: Ensure Xcode and code signing are configured

### Environment Picker Not Showing
- Verify `environments.json` exists and is valid JSON
- Check that at least one environment is configured
- Ensure the file is included as a MauiAsset in the project

## Documentation

- **Configuration Guide**: See `config.README.md` for detailed environment configuration
- **Trimble Connect SDK**: [Developer Portal](https://developer.connect.trimble.com/)
- **.NET MAUI Documentation**: [Microsoft Docs](https://learn.microsoft.com/dotnet/maui/)

## Dependencies

- **.NET MAUI**: 8.0.100
- **CommunityToolkit.Maui**: 9.0.0
- **CommunityToolkit.Mvvm**: 8.2.2
- **Trimble.Connect.Client**: 2.8.1
- **Trimble.Identity.OAuth.AuthCode**: 2.1.8
- **Newtonsoft.Json**: 13.0.3

## Contributing

This is a sample application provided as-is for demonstration purposes. For issues or questions:

- **Trimble Connect Support**: connect-integrate@trimble.com
- **Repository Issues**: Use the repository's issue tracker

## License

See the repository's LICENSE file for details.

## Support

For Trimble Connect SDK questions:
- **Email**: connect-integrate@trimble.com
- **Developer Portal**: https://developer.connect.trimble.com/

---

**Note**: This is a sample application. Always follow security best practices when handling credentials and user data in production applications.

