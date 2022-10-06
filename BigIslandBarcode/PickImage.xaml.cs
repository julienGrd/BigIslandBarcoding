﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Readers;

namespace BigIslandBarcode;

public partial class PickImagePage : ContentPage
{
	readonly IBarcodeReader _reader;
    bool _working = false;

    public PickImagePage()
	{
		InitializeComponent();

		_reader = 
            new ZXingBarcodeReader
            {
                Options = new BarcodeReaderOptions
                {
                    Formats = BarcodeFormats.All,
                    AutoRotate = true
                }
            };
    }

    async void PickClicked(object sender, EventArgs e)
        => await CallFilePicker();

    async Task CallFilePicker()
    {
		if (!_working)
		{
			_working = true;

			FileInfo.Text = "Awaiting file selection";
			ParseResult.Text = "";
			ImageOutput.Source = null;

			try
			{
                var result =
					await FilePicker.PickAsync(
						new PickOptions
						{
							FileTypes = FilePickerFileType.Images,
							PickerTitle = "Choose your barcode"
						}
					);

                if (result == null)
				{
					FileInfo.Text = "No file selected";
                    _working = false;
				}
				else
                {
                    ActivityContainer.IsVisible = true;
                    ActivityIndicator.IsRunning = true;
                    PickButton.IsEnabled = false;
                    barcodeGenerator.IsVisible = false;

                    var worker = new BackgroundWorker();
                    worker.DoWork += Worker_DoWork;
                    worker.RunWorkerCompleted += Worker_Completed;

                    worker.RunWorkerAsync(result);
                }
            }
			catch (Exception ex)
			{
				FileInfo.Text = $"Something's wrong: {ex.Message}";
				ParseResult.Text = $"Something's wrong: {ex.Message}";

                _working = false;
			}
        }
		else
        {
			await DisplayAlert("Task Already Running", "Please cancel the runnning task first", "Okay");
        }
    }

    async void Worker_DoWork(object? sender, DoWorkEventArgs e)
    {
        var fileResult = e.Argument as FileResult;
        BarcodeResult? result = null;

        if (fileResult != null)
        {

            await MainThread.InvokeOnMainThreadAsync(() => ActivityLabel.Text = "Loading file");

            using var stream = await fileResult.OpenReadAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FileInfo.Text = $"Name: {fileResult.FileName} - Size: {(stream.Length / 1024d):#.##} KiB";
                ActivityLabel.Text = "Decoding";
            });

            var decodeResult = _reader.Decode(stream);

            result = decodeResult?.FirstOrDefault();
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (fileResult != null)
                ImageOutput.Source = ImageSource.FromFile(fileResult.FullPath);

            if (result != null)
            {
                ParseResult.Text = $"Found Barcode\nValue: {result.Value}";

                barcodeGenerator.IsVisible = true;
                barcodeGenerator.Value = result.Value;
                barcodeGenerator.Format = result.Format;
            }
            else
            {
                ParseResult.Text = "No Barcode Found";
            }

            Worker_Completed(true, null!);
        });
    }

    void Worker_Completed(object? sender, RunWorkerCompletedEventArgs e)
    {
        if (sender is bool finished && finished == true)
        {
            ActivityContainer.IsVisible = false;
            ActivityIndicator.IsRunning = false;
            PickButton.IsEnabled = true;
            _working = false;
        }
    }
}