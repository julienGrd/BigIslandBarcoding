using System;
using Microsoft.Maui.Controls;

namespace ZXing.Net.Maui.Controls;

public partial class CameraBarcodeReaderView : CameraView, ICameraBarcodeReaderView
{
	public event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;

	protected override void OnHandlerChanging(HandlerChangingEventArgs args)
	{
		base.OnHandlerChanging(args);
		if (args.OldHandler is CameraBarcodeReaderViewHandler oldHandler)
			oldHandler.BarcodesDetected -= Handler_BarcodesDetected;

		if (args.NewHandler is CameraBarcodeReaderViewHandler newHandler)
			newHandler.BarcodesDetected += Handler_BarcodesDetected;
	}

	void Handler_BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
		=> BarcodesDetected?.Invoke(this, e);

	public static readonly BindableProperty OptionsProperty =
		BindableProperty.Create(nameof(Options), typeof(BarcodeReaderOptions), typeof(CameraBarcodeReaderView), defaultValueCreator: bindableObj => new BarcodeReaderOptions());

	public BarcodeReaderOptions Options
	{
		get => (BarcodeReaderOptions)GetValue(OptionsProperty);
		set => SetValue(OptionsProperty, value);
	}

	public static readonly BindableProperty IsDetectingProperty =
		BindableProperty.Create(nameof(IsDetecting), typeof(bool), typeof(CameraBarcodeReaderView), defaultValue: true);

	public bool IsDetecting
	{
		get => (bool)GetValue(IsDetectingProperty);
		set => SetValue(IsDetectingProperty, value);
	}
}