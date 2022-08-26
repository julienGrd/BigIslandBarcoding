using System;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace ZXing.Net.Maui;

public abstract class BaseCameraViewHandler<T> : ViewHandler<T, NativePlatformCameraPreviewView>
	where T : class, ICameraView
{
	public static PropertyMapper<T, BaseCameraViewHandler<T>> CameraViewMapper = new()
	{
		[nameof(ICameraView.IsTorchOn)] = (handler, virtualView) => handler.CameraManager?.UpdateTorch(virtualView.IsTorchOn),
		[nameof(ICameraView.CameraLocation)] = (handler, virtualView) => handler.CameraManager?.UpdateCameraLocation(virtualView.CameraLocation)
	};

	public static CommandMapper<T, BaseCameraViewHandler<T>> CameraCommandMapper = new()
	{
		[nameof(ICameraView.Focus)] = MapFocus,
		[nameof(ICameraView.AutoFocus)] = MapAutoFocus,
	};

	internal CameraManager? CameraManager;

	public event EventHandler<CameraFrameBufferEventArgs> FrameReady;

	public BaseCameraViewHandler() : base(CameraViewMapper)
	{
	}

	public BaseCameraViewHandler(PropertyMapper? mapper = null) 
		: base(mapper ?? CameraViewMapper)
	{
	}

	protected override async void ConnectHandler(NativePlatformCameraPreviewView nativeView)
	{
		base.ConnectHandler(nativeView);

		if (CameraManager != null)
		{
			if (await CameraManager.CheckPermissions())
				CameraManager.Connect();

			CameraManager.FrameReady += CameraManager_FrameReady;
		}
	}

	protected override void DisconnectHandler(NativePlatformCameraPreviewView nativeView)
	{
		if (CameraManager != null)
		{
			CameraManager.FrameReady -= CameraManager_FrameReady;
			CameraManager.Disconnect();
		}

		base.DisconnectHandler(nativeView);
	}

	internal virtual void CameraManager_FrameReady(object? sender, CameraFrameBufferEventArgs e)
		=> FrameReady?.Invoke(this, e);

	public void Dispose()
		=> CameraManager?.Dispose();

	public void Focus(Point point)
		=> CameraManager?.Focus(point);

	public void AutoFocus()
		=> CameraManager?.AutoFocus();

	public static void MapFocus(BaseCameraViewHandler<T> handler, ICameraView cameraBarcodeReaderView, object? parameter)
	{
		if (parameter is not Point point)
			throw new ArgumentException("Invalid parameter", "point");

		handler.Focus(point);
	}

	public static void MapAutoFocus(BaseCameraViewHandler<T> handler, ICameraView cameraBarcodeReaderView, object? parameters)
		=> handler.AutoFocus();

	protected override NativePlatformCameraPreviewView CreatePlatformView()
	{
		if (CameraManager == null)
			CameraManager = new(
#if ANDROID
                (MauiContext!.Context as Android.Content.ContextWrapper)!.BaseContext!,
#endif
				VirtualView?.CameraLocation ?? CameraLocation.Rear
			);

#if IOS || MACCATALYST
		var v = CameraManager.CreatePlatformView();
#elif ANDROID
		var v = CameraManager.CreateNativeView();
#else
		var v = CameraManager.CreateNativeView();
#endif
    return v;
	}
}