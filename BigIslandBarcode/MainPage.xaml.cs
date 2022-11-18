using System;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui;

namespace BigIslandBarcode;

public partial class MainPage : ContentPage
{
	DateTimeOffset? _lastResult = null;

    public MainPage()
	{
		InitializeComponent();

        barcodeView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = true
        };

        NavigatedTo += (s,e) => UpdateBarcodeValue();
	}

    void UpdateBarcodeValue()
    {
		barcodeGenerator.Value = RandomString(10);
    }

    protected void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
	{
		var now = DateTimeOffset.Now;

		if (_lastResult == null || now - _lastResult > TimeSpan.FromSeconds(10))
		{
			foreach (var barcode in e.Results)
				Console.WriteLine($"Barcodes: {barcode.Format} -> {barcode.Value}");

			MainThread.InvokeOnMainThreadAsync(() =>
			{
                try
                {
                    var r = e.Results.First();

                    barcodeGenerator.Value = r.Value;
                    barcodeGenerator.Format = r.Format;
                }
                catch
                {
                    //Stop things like `PHARMA_CODE` from throwing
                }
			});

			_lastResult = now;
		}
	}

	void SwitchClicked(object sender, EventArgs e)
	{
		barcodeView.CameraLocation = barcodeView.CameraLocation == CameraLocation.Rear ? CameraLocation.Front : CameraLocation.Rear;
	}

	void TorchClicked(object sender, EventArgs e)
	{
		barcodeView.IsTorchOn = !barcodeView.IsTorchOn;
	}

    void ChangeClicked(object sender, EventArgs e)
    {
		UpdateBarcodeValue();
    }

    async void PickClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new PickImagePage());
    }

    async void TakeClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new TakeImagePage());
    }


    static Random random = new Random();

    static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}