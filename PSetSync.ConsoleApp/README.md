# PSet Library Sync - Sample Console Application

This sample console application demonstrates how to use the **Trimble Connect .NET SDK** to synchronize **PSet (Property Set) Libraries** between cloud storage and local Windows desktop applications.

## What are PSets?

PSets (Property Sets) in Trimble Connect consist of three levels:
- **Libraries**: Top-level containers for related PSets
- **Definitions**: Schemas that define the structure of PSets
- **PSets**: Individual property set instances containing data

This sample shows how to sync these entities for offline use in desktop applications.

## How to use this sample

1. **Build** the project (e.g. `dotnet build` or build in Visual Studio).
2. **Run** `PSetSync.ConsoleApp.exe` (from `bin\Debug` or `bin\Release`).
3. **Sign in**: If `AccessToken` is not set in App.config, the app opens a browser for Trimble Identity OAuth sign-in. Otherwise it uses the token from App.config.
4. **Select project**: Pick a project by number or enter its ID.
5. **Choose operation** (1–5). For **Pull** (option **1**):
   - Enter **Library ID** (required).
   - Enter **Definition ID** (optional; press Enter to pull all definitions in the library).
   - The app pulls library, definition(s), and PSets into local storage and prints a summary.

**Note:** **Push** (option 2) is not yet implemented; the app displays "Yet to be implemented." Create/Update/Delete (options 3–5) modify local storage only; uploading those changes requires Push, which is planned.

## Target Framework
- .NET Framework 4.8

## Prerequisites

1. **Trimble Connect Account**: You need a Trimble Connect account with access to at least one project.

2. **Access Token**: Obtain an access token through OAuth2 authentication or from Trimble Developer Console.

3. **Project with PSet Libraries**: Your project should have PSet libraries configured.

4. **NuGet Packages**: Install the following NuGet packages:
   ```
   Install-Package Trimble.Connect.Client
   Install-Package Trimble.Connect.Data
   Install-Package Trimble.Connect.Data.Sync
   Install-Package Trimble.Connect.PSet.Client
   ```

## Configuration

### Step 1: Update App.config

Edit `App.config` and add your access token and project ID:

```xml
<appSettings>
  <add key="TrimbleConnect.AccessToken" value="YOUR_ACCESS_TOKEN_HERE" />
  <add key="TrimbleConnect.ProjectId" value="YOUR_PROJECT_ID_HERE" />
</appSettings>
```

### How to Get Access Token

**Option 1: From Trimble Developer Console** (for testing)
1. Go to Trimble Developer Console
2. Navigate to your application
3. Generate a test token

**Option 2: Implement OAuth2 Flow** (for production)
```csharp
// Implement proper OAuth2 authorization code flow
// See Trimble Identity documentation
```

## Features Demonstrated

### 1. **List PSet Libraries**
```csharp
var libraries = await psetClient.GetLibrariesAsync(new GetLibrariesRequest());
foreach (var library in libraries)
{
    Console.WriteLine($"{library.Name} (ID: {library.Id})");
}
```

### 2. **List Definitions in a Library**
```csharp
var definitions = await psetClient.GetDefinitionsAsync(new GetDefinitionsRequest
{
    LibraryId = libraryId
});
```

### 3. **Pull PSets from Cloud**
```csharp
var psets = await remoteStorage.PullAsync(
    storageState: (IStorageState)localStorage,
    libraryId: libraryId);
```

### 4. **Access Local PSets**
```csharp
using (var psetStorage = new PSetProjectStorage(localStorage.DatabasePath, localStorage as Storage))
{
    var psets = psetStorage.PSets.GetAll()
        .Where(p => !p.IsDeleted && p.LibraryId == libraryId);
}
```

### 5. **Create New PSet**
```csharp
var newPSet = new PSetEntity
{
    Link = "frn:test:pset:guid",
    LibraryId = libraryId,
    DefinitionId = definitionId,
    Version = 1,
    Data = "{\"property\": \"value\"}",
    Created = DateTimeOffset.UtcNow,
    Modified = DateTimeOffset.UtcNow
};
psetStorage.PSets.Insert(newPSet, isRemoteState: false);
```

### 6. **Push Changes to Cloud**
```csharp
await remoteStorage.PushAsync(
    storageState: (IStorageState)localStorage,
    libraryId: libraryId);
```

## Usage Example

```csharp
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Trimble.Connect.Client;
using Trimble.Connect.Data;
using Trimble.Connect.Data.Sync;
using Trimble.Connect.PSet.Client;

class Program
{
    static async Task Main()
    {
        // 1. Get configuration
        var accessToken = ConfigurationManager.AppSettings["TrimbleConnect.AccessToken"];
        var projectId = ConfigurationManager.AppSettings["TrimbleConnect.ProjectId"];

        // 2. Create client with token
        var client = new TrimbleConnectClient();
        client.AuthenticationHandler.SetAccessToken(accessToken);
        var projectClient = await client.GetProjectClientAsync(projectId);

        // 3. Create sync client
        var remoteStorage = await SyncClient.CreateAsync(projectClient);
        var psetClient = remoteStorage.PSetClient;

        // 4. Create local storage
        var localPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyApp", "PSetSync", projectId);
        
        using (var localStorage = await remoteStorage.CreateStorageAsync(localPath))
        {
            // 5. List libraries
            var libraries = await psetClient.GetLibrariesAsync(new GetLibrariesRequest());
            var library = libraries.First();
            
            // 6. Pull PSets
            var psets = await remoteStorage.PullAsync(
                storageState: (IStorageState)localStorage,
                libraryId: library.Id);
            
            Console.WriteLine($"Pulled {psets.Count()} PSets");
            
            // 7. Work with local PSets
            using (var psetStorage = new PSetProjectStorage(
                localStorage.DatabasePath, 
                localStorage as Storage))
            {
                var localPSets = psetStorage.PSets.GetAll()
                    .Where(p => !p.IsDeleted && p.LibraryId == library.Id);
                
                foreach (var pset in localPSets)
                {
                    Console.WriteLine($"Link: {pset.Link}");
                    Console.WriteLine($"Data: {pset.Data}");
                }
            }
            
            // 8. Push changes
            await remoteStorage.PushAsync(
                storageState: (IStorageState)localStorage,
                libraryId: library.Id);
        }
    }
}
```

## Key Concepts

### PSet Storage Hierarchy

```
Project
└── PSet Libraries
    ├── Library 1
    │   ├── Definition 1
    │   │   ├── PSet Instance 1
    │   │   ├── PSet Instance 2
    │   │   └── ...
    │   └── Definition 2
    │       └── ...
    └── Library 2
        └── ...
```

### Local Storage
- Uses SQLite database via `PSetProjectStorage`
- Stores libraries, definitions, and PSets locally
- Enables offline access and fast queries
- Automatically syncs with cloud on Pull/Push operations

### Remote Storage
- Accessed via `PSetClient` from `SyncClient`
- Provides access to PSet API
- Handles authentication and API communication

### Sync Operations
- **Pull**: Downloads PSets from cloud to local for a specific library
- **Push**: Uploads local PSet changes to cloud for a specific library
- **Library-scoped**: All operations are scoped to a specific library ID
- **Incremental**: Only transfers changed PSets

### PSet Entity Properties
- `Link`: Unique identifier for the PSet (FRN format)
- `LibraryId`: ID of the containing library
- `DefinitionId`: ID of the definition schema
- `Version`: Version number for conflict resolution
- `Data`: JSON string containing the actual property data
- `Created/Modified`: Timestamps
- `IsDeleted`: Soft delete flag

## Best Practices

1. **Always specify library ID** when pulling/pushing PSets
2. **Use `PSetProjectStorage` with `using`** for proper disposal
3. **Handle sync errors gracefully** - network issues are common
4. **Store access token securely** - never hardcode in source
5. **Sync regularly** - pull before working, push after changes
6. **Use FRN format for links** - `frn:namespace:type:id`
7. **Version management** - increment version on updates
8. **Validate JSON data** - ensure Data property contains valid JSON

## Common Operations

### Get All PSets for a Definition
```csharp
using (var psetStorage = new PSetProjectStorage(dbPath, storage))
{
    var psets = psetStorage.PSets.GetAll()
        .Where(p => !p.IsDeleted && 
                    p.LibraryId == libraryId && 
                    p.DefinitionId == definitionId);
}
```

### Update a PSet
```csharp
pset.Data = "{\"updatedProperty\": \"newValue\"}";
pset.Version++;
pset.Modified = DateTimeOffset.UtcNow;
psetStorage.PSets.Update(pset, isRemoteState: false);
```

### Delete a PSet
```csharp
psetStorage.PSets.Delete(pset);
```

### Query PSets by Link
```csharp
var pset = psetStorage.PSets.GetAll()
    .FirstOrDefault(p => p.Link == targetLink && !p.IsDeleted);
```

## Troubleshooting

### "Access token invalid"
- Verify token is correct and not expired
- Ensure token has proper format (may need "Bearer " prefix)
- Check token has access to the specified project

### "Library not found"
- Verify library ID is correct
- Check user has access to the library
- Ensure library exists in the project

### "Database is locked"
- Ensure only one `PSetProjectStorage` instance per database
- Use `using` statements for proper disposal
- Check for file system permissions

### "PSet not syncing"
- Verify `isRemoteState` parameter is correct (false for local changes)
- Check library ID matches
- Ensure PSet has valid Link, LibraryId, and DefinitionId

### "JSON parsing error"
- Validate Data property contains valid JSON
- Escape special characters properly
- Use JSON serialization libraries (Newtonsoft.Json)

## API Reference

### Key Classes
- `SyncClient`: Main sync client with PSet support
- `PSetClient`: Direct access to PSet API
- `PSetProjectStorage`: Local PSet storage manager
- `PSetEntity`: Local PSet model
- `Library`: PSet library model
- `Definition`: PSet definition model

### Key Methods
- `PSetClient.GetLibrariesAsync()`: List all libraries
- `PSetClient.GetDefinitionsAsync()`: List definitions in a library
- `SyncClient.PullAsync(libraryId)`: Pull PSets from cloud
- `SyncClient.PushAsync(libraryId)`: Push PSets to cloud
- `PSetProjectStorage.PSets.GetAll()`: Query local PSets
- `PSetProjectStorage.PSets.Insert()`: Create new PSet
- `PSetProjectStorage.PSets.Update()`: Update existing PSet

## Support

For detailed API documentation, visit:
- [Trimble Connect Developer Portal](https://developer.connect.trimble.com/)
- [SDK Documentation](https://docs.connect.trimble.com/)
- [PSet API Documentation](https://developer.connect.trimble.com/pset-api)

## License

This sample code is provided as-is for demonstration purposes.
