using System;

namespace ZXing.Net.Maui;

public interface ICameraBarcodeReaderView : ICameraView
{
	BarcodeReaderOptions Options { get; }

	event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;

	bool IsDetecting { get; set; }
}