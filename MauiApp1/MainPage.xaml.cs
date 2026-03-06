using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
	private readonly DatabaseService _databaseService;

	public string StatusMessage { get; set; } = "Press the button to fetch one record from the API.";
	public string PlayerLine { get; set; } = "Player: -";
	public string TeamLine { get; set; } = "Team: -";
	public string PointsLine { get; set; } = "Points: -";

	public MainPage()
	{
		_databaseService = new DatabaseService(new HttpClient
		{
			BaseAddress = new Uri("http://127.0.0.1:5117/")
		});
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnLoadMilestoneClicked(object? sender, EventArgs e)
	{
		var actionButton = sender as Button;
		if (actionButton is not null)
		{
			actionButton.IsEnabled = false;
			actionButton.Text = "Loading...";
		}

		try
		{
			var record = await _databaseService.GetMilestoneRecordAsync();
			ApplyRecord(record);
			StatusMessage = record is null
				? "API responded, but no rows were found in dbo.Players."
				: "Success: MAUI -> API -> SQL is working.";
		}
		catch (Exception ex)
		{
			StatusMessage = "Could not load record from API.";
			ApplyRecord(null);
			await DisplayAlertAsync("API Error", $"Could not load milestone record from local API.\n\n{ex.Message}", "OK");
		}
		finally
		{
			OnPropertyChanged(nameof(StatusMessage));
			OnPropertyChanged(nameof(PlayerLine));
			OnPropertyChanged(nameof(TeamLine));
			OnPropertyChanged(nameof(PointsLine));

			if (actionButton is not null)
			{
				actionButton.IsEnabled = true;
				actionButton.Text = "Load Milestone Record";
			}
		}
	}

	private void ApplyRecord(DatabaseItem? record)
	{
		if (record is null)
		{
			PlayerLine = "Player: -";
			TeamLine = "Team: -";
			PointsLine = "Points: -";
			return;
		}

		PlayerLine = $"Player: {record.PlayerName} (Id: {record.Id})";
		TeamLine = $"Team: {record.Team}";
		PointsLine = $"Points: {record.Points}";
	}
}
