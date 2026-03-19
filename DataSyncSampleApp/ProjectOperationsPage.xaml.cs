using DataSyncSampleApp.Models;
using DataSyncSampleApp.Services;
using Trimble.Connect.Data;

namespace DataSyncSampleApp;

public partial class ProjectOperationsPage : ContentPage
{
    private IStorage? _storage;
    private string _dir = "";

    public ProjectOperationsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var p = AppSession.SelectedProject;
        HeaderLabel.Text = p == null ? "No project" : $"{p.Name}\n{p.Identifier}";
        _dir = p != null ? PSetOperationsHelper.GetProjectStoragePath(p.Identifier) : "";
        Append($"Storage folder:\n{_dir}\n");
        Append("Same flow as PSetSync.ConsoleApp: run 0.Init storage first, then 1.Pull … 10.SQL.");
        Append("Console 1–10 ≈ here 1–10 (0.Init is explicit open/create local DB before Pull).\n");
    }

    private void Append(string s) => Output.Text += s + "\n";

    private async void OnInitStorage(object sender, EventArgs e)
    {
        var sync = AppSession.SyncClient;
        if (sync == null || string.IsNullOrEmpty(_dir))
        {
            await DisplayAlert("Error", "No sync client or project.", "OK");
            return;
        }

        try
        {
            var (st, log) = await PSetOperationsHelper.OpenOrCreateStorageAsync(sync, _dir).ConfigureAwait(true);
            _storage?.Dispose();
            _storage = st;
            Append(log);
            Append("Ready for Pull / CRUD / tests.");
        }
        catch (Exception ex)
        {
            Append($"Init error: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Append($"Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            Append($"Stack: {ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))}");
        }
    }

    private async void OnPull(object sender, EventArgs e)
    {
        if (_storage == null || AppSession.SyncClient == null)
        {
            await DisplayAlert("Error", "Run 0.Init storage first.", "OK");
            return;
        }

        var lib = await DisplayPromptAsync("Pull", "Library ID", placeholder: "library GUID");
        if (string.IsNullOrWhiteSpace(lib)) return;
        var def = await DisplayPromptAsync("Pull", "Definition ID (empty = all)", initialValue: "");
        try
        {
            var r = await PSetOperationsHelper.PullAsync(AppSession.SyncClient, _storage, lib.Trim(),
                string.IsNullOrWhiteSpace(def) ? null : def.Trim()).ConfigureAwait(true);
            Append(r);
        }
        catch (Exception ex) { Append($"Pull: {ex.Message}"); }
    }

    private async void OnPush(object sender, EventArgs e)
    {
        Append(PSetOperationsHelper.PushStub());
        await Task.CompletedTask;
    }

    private async void OnCreate(object sender, EventArgs e)
    {
        if (_storage == null)
        {
            await DisplayAlert("Error", "Run 0.Init storage first.", "OK");
            return;
        }

        var lib = await DisplayPromptAsync("Create PSet", "Library ID");
        var def = await DisplayPromptAsync("Create PSet", "Definition ID");
        if (string.IsNullOrWhiteSpace(lib) || string.IsNullOrWhiteSpace(def)) return;
        var link = await DisplayPromptAsync("Create PSet", "Link ID (empty = auto)", initialValue: "");
        Append(PSetOperationsHelper.CreateTestPSet(_storage, _dir, lib.Trim(), def.Trim(),
            string.IsNullOrWhiteSpace(link) ? null : link.Trim()));
    }

    private async void OnUpdate(object sender, EventArgs e)
    {
        if (_storage == null)
        {
            await DisplayAlert("Error", "Run 0.Init storage first.", "OK");
            return;
        }

        var lib = await DisplayPromptAsync("Update", "Library ID");
        var idStr = await DisplayPromptAsync("Update", "PSet internal Id (long)");
        var json = await DisplayPromptAsync("Update", "New PSetProps JSON", initialValue: "{}");
        if (!long.TryParse(idStr, out var id) || string.IsNullOrWhiteSpace(lib)) return;
        Append(PSetOperationsHelper.UpdatePSet(_storage, _dir, lib.Trim(), id, json ?? "{}"));
    }

    private async void OnDelete(object sender, EventArgs e)
    {
        if (_storage == null)
        {
            await DisplayAlert("Error", "Run 0.Init storage first.", "OK");
            return;
        }

        var lib = await DisplayPromptAsync("Delete", "Library ID");
        var idStr = await DisplayPromptAsync("Delete", "PSet internal Id");
        if (!long.TryParse(idStr, out var id) || string.IsNullOrWhiteSpace(lib)) return;
        if (!await DisplayAlert("Confirm", $"Delete PSet {id}?", "Yes", "No")) return;
        Append(PSetOperationsHelper.DeletePSet(_storage, _dir, lib.Trim(), id));
    }

    private async void OnTestDbs(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_dir)) return;
        Append(PSetOperationsHelper.TestFourDatabases(_dir));
        await Task.CompletedTask;
    }

    private async void OnHealth(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_dir)) return;
        Append(PSetOperationsHelper.HealthCheck(_dir));
        await Task.CompletedTask;
    }

    private async void OnInfo(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_dir)) return;
        Append(PSetOperationsHelper.DisplayDbInfo(_dir));
        await Task.CompletedTask;
    }

    private async void OnVerifyEnc(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_dir)) return;
        Append(PSetOperationsHelper.VerifyEncryption(_dir));
        await Task.CompletedTask;
    }

    private async void OnSql(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_dir)) return;
        var db = await DisplayActionSheet("Database file", "Cancel", null, ".storage", ".catalog",
            "PSetCatalog.storage", "PSetProject.storage");
        if (db == null || db == "Cancel") return;
        var sql = await DisplayPromptAsync("SQL", "Statement (e.g. SELECT 1)");
        if (string.IsNullOrWhiteSpace(sql)) return;
        Append(PSetOperationsHelper.RunCustomSql(_dir, db, sql.Trim()));
    }

    protected override void OnDisappearing()
    {
        _storage?.Dispose();
        _storage = null;
        base.OnDisappearing();
    }
}
