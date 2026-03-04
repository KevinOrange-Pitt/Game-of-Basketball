using System.Collections.ObjectModel;
using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
	private readonly DatabaseService _databaseService;
	public ObservableCollection<DatabaseItem> Items { get; } = new();

	public MainPage()
	{
		_databaseService = new DatabaseService(new HttpClient
		{
			BaseAddress = new Uri("http://127.0.0.1:5117/")
		});
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnShowDatabaseClicked(object? sender, EventArgs e)
	{
		var actionButton = sender as Button;
		if (actionButton is not null)
		{
			actionButton.IsEnabled = false;
			actionButton.Text = "Loading...";
		}

		try
		{
			var records = await _databaseService.GetPlayersAsync();
			await BindPlayersAsync(records);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("API Error", $"Could not load players from local API.\n\n{ex.Message}", "OK");
		}
		finally
		{
			if (actionButton is not null)
			{
				actionButton.IsEnabled = true;
				actionButton.Text = "Show Players";
			}
		}
	}

	private async Task BindPlayersAsync(List<DatabaseItem> records)
	{
			Items.Clear();
			foreach (var record in records)
			{
				Items.Add(record);
			}

			if (Items.Count == 0)
			{
				await DisplayAlertAsync("No Players", "Connected successfully, but no players were found.", "OK");
			}
	}
}
