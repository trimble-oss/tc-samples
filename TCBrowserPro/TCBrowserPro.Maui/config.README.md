# Environment Configuration

The application supports environment-based configuration through an `environments.json` file and an environment picker in the login screen. This allows you to easily switch between different Trimble Connect environments (Production, Stage, Development, etc.) without modifying code.

## ⚠️ IMPORTANT: Setup Required Before First Use

**You must create your own `environments.json` file with your Trimble Connect client credentials before using this application.**

1. Copy `environments.json.example` to `environments.json` in the project root
2. Replace the placeholder values (`YOUR_PRODUCTION_CLIENT_ID`, `YOUR_PRODUCTION_CLIENT_SECRET`, etc.) with your actual Trimble Connect client credentials
3. Obtain your client credentials from your Trimble Connect administrator or through the Trimble Connect developer portal

**The repository does not include any client credentials for security reasons. You must provide your own.**

## Configuration Files

### environments.json

The `environments.json` file defines all available environments and should be placed in the root of the `TCBrowserPro.Maui` project. It contains a dictionary of environments, each with their own configuration.

**Template Structure:**

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
    },
    "Stage": {
      "AuthorityUri": "https://stage.id.trimblecloud.com/oauth/",
      "ServiceUri": "https://app.stage.connect.trimble.com/tc/api/2.0/",
      "WebAppUri": "https://app.stage.connect.trimble.com",
      "WebViewerUri": "https://web.stage.connect.trimble.com",
      "EnvironmentName": "Stage",
      "ClientId": "YOUR_STAGE_CLIENT_ID",
      "ClientSecret": "YOUR_STAGE_CLIENT_SECRET"
    }
  }
}
```

## Configuration Properties

Each environment configuration contains:

- **AuthorityUri**: The OAuth authority URI for authentication
  - Production: `https://id.trimble.com/oauth/`
  - Stage: `https://stage.id.trimblecloud.com/oauth/`

- **ServiceUri**: The Trimble Connect API service URI
  - Production: `https://app.connect.trimble.com/tc/api/2.0/`
  - Stage: `https://app.stage.connect.trimble.com/tc/api/2.0/`

- **WebAppUri**: The web application URI (used for origin in viewer URLs)
  - Production: `https://app.connect.trimble.com`
  - Stage: `https://app.stage.connect.trimble.com`

- **WebViewerUri**: The web viewer URI (for opening views in browser)
  - Production: `https://web.connect.trimble.com`
  - Stage: `https://web.stage.connect.trimble.com`

- **EnvironmentName**: A descriptive name for the environment (e.g., "Production", "Stage", "Development")

- **ClientId**: **REQUIRED** - Your Trimble Connect client ID for this environment. Must be obtained from your Trimble Connect administrator.
- **ClientSecret**: **REQUIRED** - Your Trimble Connect client secret for this environment. Must be obtained from your Trimble Connect administrator.

**Note**: There is only one ClientId and ClientSecret per environment, regardless of platform. The application will use the same credentials for all platforms (Windows, Mac, Android, iOS) for a given environment.

## Environment Picker

The login screen includes an environment picker that allows users to select which environment to use before signing in. The selected environment is saved and will be used for subsequent logins until changed.

## Obtaining Client Credentials

To obtain your Trimble Connect client credentials:

1. **Contact your Trimble Connect administrator** - They can provide you with client IDs and secrets for the environments you need access to
2. **Use the Trimble Connect Developer Portal** - If you have access, you can register your own client applications
3. **For Production environment** - Client credentials must be registered with Trimble Connect and approved for production use
4. **For Stage/Development environments** - Contact your administrator for stage environment credentials

**⚠️ Security Note**: Never commit your `environments.json` file with real credentials to version control. Always use `environments.json.example` as a template and keep your actual credentials secure.

## How It Works

1. The `ConfigService` loads all environments from `environments.json` at startup
2. It first tries to load from the app's data directory (allows runtime changes)
3. If not found, it loads from the embedded MauiAsset (the environments.json file in the project)
4. If neither is available, it falls back to default Production and Stage environments (without credentials - will fail authentication)
5. The user's selected environment preference is saved in `userpreferences.json` in the app's data directory
6. When the user changes the environment in the picker, the `ShellViewModel` is reinitialized with the new environment configuration

## Changing Environments

### Option 1: Use the Environment Picker (Recommended)
1. On the login screen, use the environment picker dropdown
2. Select the desired environment (e.g., "Production" or "Stage")
3. The selection is automatically saved and will be used for future logins

### Option 2: Modify environments.json in the project
Edit the `environments.json` file in the project root and rebuild the application.

### Option 3: Runtime configuration (for testing)
Place an `environments.json` file in the app's data directory:
- Windows: `%LOCALAPPDATA%\com.tcbrowserpro.maui\environments.json`
- Android: App's internal storage
- iOS: App's Documents directory

The runtime config file takes precedence over the embedded one.

## Default Behavior

If no `environments.json` file is found or if it's invalid, the application defaults to **Production** environment with these URIs:
- AuthorityUri: `https://id.trimble.com/oauth/`
- ServiceUri: `https://app.connect.trimble.com/tc/api/2.0/`
- WebAppUri: `https://app.connect.trimble.com`
- WebViewerUri: `https://web.connect.trimble.com`

**However, without valid ClientId and ClientSecret, authentication will fail.** You must provide credentials in your `environments.json` file.

## Adding New Environments

To add a new environment (e.g., Development), add it to the `Environments` dictionary in `environments.json`:

```json
{
  "DefaultEnvironment": "Production",
  "Environments": {
    "Production": { ... },
    "Stage": { ... },
    "Development": {
      "AuthorityUri": "https://dev.id.trimblecloud.com/oauth/",
      "ServiceUri": "https://app.dev.connect.trimble.com/tc/api/2.0/",
      "WebAppUri": "https://app.dev.connect.trimble.com",
      "WebViewerUri": "https://web.dev.connect.trimble.com",
      "EnvironmentName": "Development",
      "ClientId": "YOUR_DEV_CLIENT_ID",
      "ClientSecret": "YOUR_DEV_CLIENT_SECRET"
    }
  }
}
```

The new environment will automatically appear in the environment picker.

## Troubleshooting

### "Invalid Client" or "Unregistered Client" Error
- Ensure your `ClientId` and `ClientSecret` are correct for the selected environment
- Verify that your client credentials are registered for the environment you're trying to use (Production vs Stage)
- Contact your Trimble Connect administrator to verify your credentials are valid

### Authentication Fails
- Check that `ClientId` and `ClientSecret` are not null in your `environments.json`
- Verify the `AuthorityUri` matches the environment you're using
- Ensure your credentials have the necessary permissions for the Trimble Connect API
