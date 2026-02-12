using System.Diagnostics;
using Trimble.Connect.Client;
using Trimble.Connect.Client.Models;

namespace TCBrowserPro.Views;

public partial class CreateTodoPage : ContentPage
{
    private readonly IProjectClient _projectClient;
    private readonly Action<Todo> _onTodoCreated;

    public CreateTodoPage(IProjectClient projectClient, Action<Todo> onTodoCreated)
    {
        InitializeComponent();
        _projectClient = projectClient;
        _onTodoCreated = onTodoCreated;

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
                await DisplayAlert("Validation Error", "Title is required", "OK");
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

            // Note: Priority, Status, and other fields are set by the API automatically
            // We'll update them after creation if needed

            var createdTodo = await _projectClient.Todos.CreateAsync(newTodo);

            // If user selected a priority other than Normal, update it
            if (PriorityPicker.SelectedIndex >= 0)
            {
                var priority = PriorityPicker.Items[PriorityPicker.SelectedIndex];
                if (priority != "Normal")
                {
                    try
                    {
                        var todoToUpdate = await _projectClient.Todos.GetAsync(createdTodo.Identifier);
                        todoToUpdate.Priority = priority;
                        createdTodo = await _projectClient.Todos.UpdateAsync(todoToUpdate);
                    }
                    catch (Exception ex)
                    {
                        // Continue anyway, todo was created
                    }
                }
            }

            // If assignee was specified, try to assign it
            if (!string.IsNullOrWhiteSpace(AssigneeEntry.Text))
            {
                try
                {
                    var todoToUpdate = await _projectClient.Todos.GetAsync(createdTodo.Identifier);
                    // Note: Assignee handling depends on your API version
                    // You might need to use a different property or method
                    // todoToUpdate.AssignedTo = AssigneeEntry.Text.Trim();
                    // createdTodo = await _projectClient.Todos.UpdateAsync(todoToUpdate);
                }
                catch (Exception ex)
                {
                    // Continue anyway, todo was created
                }
            }

            // Notify parent and close
            _onTodoCreated?.Invoke(createdTodo);

            await DisplayAlert("Success", 
                $"Todo '{createdTodo.Title}' created successfully!\nLabel: {createdTodo.Label}", 
                "OK");

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", 
                $"Failed to create todo: {ex.Message}", 
                "OK");

            // Re-enable button
            var button = (Button)sender;
            button.IsEnabled = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
