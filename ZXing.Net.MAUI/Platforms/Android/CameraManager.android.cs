using Android.Content;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using Java.Util.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ZXing.Net.Maui;

internal partial class CameraManager
{
    Preview _cameraPreview;
	ImageAnalysis _imageAnalyzer;
	PreviewView _previewView;
	IExecutorService? _cameraExecutor;
	CameraSelector _cameraSelector = null;
	ProcessCameraProvider _cameraProvider;
	ICamera _camera;

	Context _context;

	internal CameraManager(Context context, CameraLocation cameraLocation)
		: this(cameraLocation)
    {
		_context = context;
    }

    [MemberNotNull("_previewView")]
	public NativePlatformCameraPreviewView CreateNativeView()
	{
		_previewView = new PreviewView(_context);
		_cameraExecutor = Executors.NewSingleThreadExecutor();

		return _previewView;
	}

	public void Connect()
	{
		var cameraProviderFuture = ProcessCameraProvider.GetInstance(_context);

        cameraProviderFuture.AddListener(new Java.Lang.Runnable(() =>
		{
			// Used to bind the lifecycle of cameras to the lifecycle owner
			_cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

			if (_previewView == null)
				CreateNativeView();

			// Preview
			_cameraPreview = new Preview.Builder().Build();
			_cameraPreview.SetSurfaceProvider(_previewView.SurfaceProvider);

			// Frame by frame analyze
			_imageAnalyzer = new ImageAnalysis.Builder()
				.SetDefaultResolution(new Android.Util.Size(640, 480))
				.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
				.Build();

			_imageAnalyzer.SetAnalyzer(
				_cameraExecutor, 
				new FrameAnalyzer((buffer, size) =>
					FrameReady?.Invoke(
						this, 
						new CameraFrameBufferEventArgs(new Readers.PixelBufferHolder(size, buffer))
					)
				)
			);

			UpdateCamera();

		}), ContextCompat.GetMainExecutor(_context)); //GetMainExecutor: returns an Executor that runs on the main thread.
	}

	public void Disconnect()
	{ }

	public void UpdateCamera()
	{
		if (_cameraProvider != null)
		{
			// Unbind use cases before rebinding
			_cameraProvider.UnbindAll();

			var cameraLocation = CameraLocation;

			// Select back _camera as a default, or front _camera otherwise
			if (cameraLocation == CameraLocation.Rear && _cameraProvider.HasCamera(CameraSelector.DefaultBackCamera))
				_cameraSelector = CameraSelector.DefaultBackCamera;
			else if (cameraLocation == CameraLocation.Front && _cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera))
				_cameraSelector = CameraSelector.DefaultFrontCamera;
			else
				_cameraSelector = CameraSelector.DefaultBackCamera;

			if (_cameraSelector == null)
				throw new System.Exception("Camera not found");

			// The _context here SHOULD be something that's a lifecycle owner
			if (_context is AndroidX.Lifecycle.ILifecycleOwner lifecycleOwner)
				_camera = _cameraProvider.BindToLifecycle(lifecycleOwner, _cameraSelector, _cameraPreview, _imageAnalyzer);
		}
	}

	public void UpdateTorch(bool on)
	{
		_camera?.CameraControl?.EnableTorch(on);
	}

	public void Focus(Android.Graphics.Point point)
	{

	}

	public void AutoFocus()
	{

    }

    public void Dispose()
	{
		_cameraExecutor?.Shutdown();
		_cameraExecutor?.Dispose();
	}
}