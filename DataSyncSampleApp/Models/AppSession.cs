using Trimble.Connect.Client;
using Trimble.Connect.Data.Sync;
using Trimble.Identity.OAuth.AuthCode;
using ClientProject = Trimble.Connect.Client.Models.Project;

namespace DataSyncSampleApp.Models;

/// <summary>Holds signed-in client, project, and sync context (set after Sign-In and project pick).</summary>
public static class AppSession
{
    public static AuthCodeCredentialsProvider? AuthProvider { get; set; }

    public static TrimbleConnectClient? ConnectClient { get; set; }

    public static ClientProject? SelectedProject { get; set; }

    public static SyncClient? SyncClient { get; set; }

    public static void Clear()
    {
        AuthProvider = null;
        ConnectClient = null;
        SelectedProject = null;
        SyncClient = null;
    }
}
