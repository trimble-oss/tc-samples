using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Trimble.Connect.Client;
using Trimble.Connect.Client.Models;

namespace TCBrowserPro.Views;

public partial class CreateTodoPopup : Popup
{
    private readonly IProjectClient _projectClient;

    public CreateTodoPopup(IProjectClient projectClient)
    {
        InitializeComponent();
        _projectClient = projectClient;

        // Set default due date to 7 days from now
        DueDatePicker.Date = DateTime.Now.AddDays(7);
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(TitleEntry.Text))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error", "Title is required", "OK");
                return;
            }

            // Disable button to prevent double-clicks
            var button = (Button)sender;
            button.IsEnabled = false;

            // Create todo object with only the fields that API accepts
            var newTodo = new Todo
            {
                Title = TitleEntry.Text.Trim(),
                Description = DescriptionEditor.Text?.Trim() ?? string.Empty,
                DueDate = new DateTimeOffset(DueDatePicker.Date)
            };

            var createdTodo = await _projectClient.Todos.CreateAsync(newTodo);

            // Update Status and Priority if they differ from defaults
            bool needsUpdate = false;
            var todoToUpdate = await _projectClient.Todos.GetAsync(createdTodo.Identifier);

            // Update Status if not "Open"
            if (StatusPicker.SelectedIndex >= 0)
            {
                var status = StatusPicker.Items[StatusPicker.SelectedIndex];
                if (status != "Open")
                {
                    todoToUpdate.Status = status;
                    needsUpdate = true;
                }
            }

            // Update Priority if not "Normal"
            if (PriorityPicker.SelectedIndex >= 0)
            {
                var priority = PriorityPicker.Items[PriorityPicker.SelectedIndex];
                if (priority != "Normal")
                {
                    todoToUpdate.Priority = priority;
                    needsUpdate = true;
                }
            }

            // Only update if something changed
            if (needsUpdate)
            {
                try
                {
                    createdTodo = await _projectClient.Todos.UpdateAsync(todoToUpdate);
                }
                catch (Exception ex)
                {
                    // Could not update status/priority
                }
            }

            await Application.Current.MainPage.DisplayAlert("Success", 
                $"Todo '{createdTodo.Title}' created successfully!\nLabel: {createdTodo.Label}", 
                "OK");

            // Close popup and return the created todo
            await CloseAsync(createdTodo);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", 
                $"Failed to create todo: {ex.Message}", 
                "OK");

            // Re-enable button
            var button = (Button)sender;
            button.IsEnabled = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }
}
