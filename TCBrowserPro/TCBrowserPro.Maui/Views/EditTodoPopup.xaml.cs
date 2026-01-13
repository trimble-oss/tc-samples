using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Trimble.Connect.Client;
using Trimble.Connect.Client.Models;

namespace TCBrowserPro.Views;

public partial class EditTodoPopup : Popup
{
    private readonly IProjectClient _projectClient;
    private readonly Todo _originalTodo;

    public EditTodoPopup(IProjectClient projectClient, Todo todo)
    {
        InitializeComponent();
        _projectClient = projectClient;
        _originalTodo = todo;

        // Populate fields with existing todo data
        LoadTodoData();
    }

    private void LoadTodoData()
    {
        LabelText.Text = _originalTodo.Label ?? "N/A";
        TitleEntry.Text = _originalTodo.Title;
        DescriptionEditor.Text = _originalTodo.Description;

        // Set Status
        if (!string.IsNullOrEmpty(_originalTodo.Status))
        {
            var statusIndex = StatusPicker.Items.IndexOf(_originalTodo.Status);
            if (statusIndex >= 0)
                StatusPicker.SelectedIndex = statusIndex;
        }

        // Set Priority
        if (!string.IsNullOrEmpty(_originalTodo.Priority))
        {
            var priorityIndex = PriorityPicker.Items.IndexOf(_originalTodo.Priority);
            if (priorityIndex >= 0)
                PriorityPicker.SelectedIndex = priorityIndex;
        }

        // Set Due Date
        if (_originalTodo.DueDate.HasValue)
            DueDatePicker.Date = _originalTodo.DueDate.Value.DateTime;

        // Set Completion
        CompletionSlider.Value = _originalTodo.CompletionPercentage.HasValue 
            ? (double)_originalTodo.CompletionPercentage.Value 
            : 0;
        CompletionLabel.Text = $"{_originalTodo.CompletionPercentage ?? 0}%";
    }

    private void OnCompletionChanged(object sender, ValueChangedEventArgs e)
    {
        var value = (int)e.NewValue;
        CompletionLabel.Text = $"{value}%";

        // Auto-set status to Closed if 100%
        if (value == 100 && StatusPicker.SelectedIndex != StatusPicker.Items.IndexOf("Closed"))
        {
            StatusPicker.SelectedIndex = StatusPicker.Items.IndexOf("Closed");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
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

            // Get the latest version of the todo
            var currentTodo = await _projectClient.Todos.GetAsync(_originalTodo.Identifier);

            // Update fields
            currentTodo.Title = TitleEntry.Text.Trim();
            currentTodo.Description = DescriptionEditor.Text?.Trim() ?? string.Empty;

            if (StatusPicker.SelectedIndex >= 0)
                currentTodo.Status = StatusPicker.Items[StatusPicker.SelectedIndex];

            if (PriorityPicker.SelectedIndex >= 0)
                currentTodo.Priority = PriorityPicker.Items[PriorityPicker.SelectedIndex];

            currentTodo.DueDate = new DateTimeOffset(DueDatePicker.Date);
            currentTodo.CompletionPercentage = (long)CompletionSlider.Value;

            // Update the todo
            var updatedTodo = await _projectClient.Todos.UpdateAsync(currentTodo);

            await Application.Current.MainPage.DisplayAlert("Success", 
                $"Todo '{updatedTodo.Title}' updated successfully!", 
                "OK");

            // Close popup and return the updated todo
            await CloseAsync(updatedTodo);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", 
                $"Failed to update todo: {ex.Message}", 
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

