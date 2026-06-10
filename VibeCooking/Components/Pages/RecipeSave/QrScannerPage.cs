namespace VibeCooking.Components.Pages.RecipeSave;

using BarcodeScanning;
using System.Text.Json;
using VibeCooking.Models;
using VibeCooking.Services;
using VibeCooking.Utilities;

public partial class QrScannerPage : ContentPage
{
	private readonly IQrScanResultService _qrService;

	public QrScannerPage(IQrScanResultService qrService)
	{
		InitializeComponent();
		_qrService = qrService;
	}

	private void CameraView_OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
	{
		if (e.BarcodeResults.Count == 0)
			return;

		var text = e.BarcodeResults.First().RawValue;

		_ = HandleQrAsync(text);
	}

	private async Task HandleQrAsync(string qrText)
	{
		string decompressedJson = CompressionUtility.DecompressFromBase64(qrText);

		if (!string.IsNullOrEmpty(decompressedJson))
		{
			SavedRecipeModel? savedRecipe = JsonSerializer.Deserialize<SavedRecipeModel>(decompressedJson);

			if (savedRecipe != null)
			{
				_qrService.Publish(savedRecipe);

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await Application.Current.MainPage.Navigation.PopModalAsync();
				});
			}
		}
	}

	private async void OnCloseClicked(object sender, EventArgs e)
	{
		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			await Application.Current.MainPage.Navigation.PopModalAsync();
		});
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		cameraView.CameraEnabled = true;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

		cameraView.CameraEnabled = false;
	}   
}