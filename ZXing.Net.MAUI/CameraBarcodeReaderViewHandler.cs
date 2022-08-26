using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;

namespace ZXing.Net.Maui;

public partial class CameraBarcodeReaderViewHandler : BaseCameraViewHandler<ICameraBarcodeReaderView>
{
	public static new PropertyMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraViewMapper = new ()
	{
        [nameof(ICameraBarcodeReaderView.Options)] = MapOptions,
		[nameof(ICameraBarcodeReaderView.IsDetecting)] = MapIsDetecting,
		[nameof(ICameraBarcodeReaderView.IsTorchOn)] = (handler, virtualView) => handler.CameraManager?.UpdateTorch(virtualView.IsTorchOn),
		[nameof(ICameraBarcodeReaderView.CameraLocation)] = (handler, virtualView) => handler.CameraManager?.UpdateCameraLocation(virtualView.CameraLocation)
	};

	public event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;

	protected Readers.IBarcodeReader BarcodeReader
		=> Services!.GetService<Readers.IBarcodeReader>()!;

	internal override void CameraManager_FrameReady(object? sender, CameraFrameBufferEventArgs e)
	{
		base.CameraManager_FrameReady(sender, e);

		if (VirtualView.IsDetecting)
		{
			var barcodes = BarcodeReader.Decode(e.Data);

			if (barcodes?.Any() ?? false)
				BarcodesDetected?.Invoke(this, new BarcodeDetectionEventArgs(barcodes));
		}
	}

	public static void MapOptions(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
		=> handler.BarcodeReader.Options = cameraBarcodeReaderView.Options;

	public static void MapIsDetecting(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
	{ }
}