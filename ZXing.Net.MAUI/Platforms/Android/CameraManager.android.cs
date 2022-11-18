using Android.Content;
using Android.Graphics;
using Android.Views;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using Java.Util;
using Java.Util.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ZXing.Net.Maui;

internal partial class CameraManager
{
    Preview _cameraPreview;
    ImageAnalysis _imageAnalyzer;
    PreviewView _previewView;
    IExecutorService? _cameraExecutor;
    CameraSelector? _cameraSelector = null;
    ProcessCameraProvider? _cameraProvider;
    ICamera _camera;
    Timer? _timer;

    Context _context;

    internal CameraManager(Context context, CameraLocation cameraLocation)
        : this(cameraLocation)
    {
        _context = context;
    }

    [MemberNotNull("_previewView")]
    [MemberNotNull("_cameraExecutor")]
    public NativePlatformCameraPreviewView CreateNativeView()
    {
        _previewView = new PreviewView(_context);
        _cameraExecutor = Executors.NewSingleThreadExecutor()!;

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
                .SetOutputImageRotationEnabled(true)
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
            AutoFocus();
            try
            {
                setupAutoFocusTimer();
                ((View)_previewView.Parent).SetOnTouchListener(new TapFocusTouchListener(this));
            }
            catch { }

        }), ContextCompat.GetMainExecutor(_context)); //GetMainExecutor: returns an Executor that runs on the main thread.
    }

    private class TapFocusTouchListener : Java.Lang.Object, View.IOnTouchListener
    {

        private CameraManager _cameraManager;

        public TapFocusTouchListener(CameraManager cameraManager)
        {
            this._cameraManager = cameraManager;
        }

        public bool OnTouch(View? v, MotionEvent? e)
        {
            if (e?.Action == MotionEventActions.Down)
            {
                Point point = new Point((int)e.GetX(), (int)e.GetY());
                _cameraManager.Focus(point);
                return true;
            }

            return false;
        }
    }

    private void setupAutoFocusTimer()
    {
        if (_timer != null)
        {
            _timer.Cancel();
            _timer.Dispose();
            _timer = null;
        }

        try
        {
            _timer = new Timer();
            var task = new AFTimerTask(this);
            _timer.ScheduleAtFixedRate(task, 5000, 5000);
        }
        catch
        {
            // Auto focus failed, continue without it
        }
    }

    private class AFTimerTask : TimerTask
    {
        private CameraManager _cameraManager;

        public AFTimerTask(CameraManager manager)
        {
            this._cameraManager = manager;
        }

        public override void Run()
        {
            _cameraManager.AutoFocus();
        }
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
                _camera = _cameraProvider.BindToLifecycle(lifecycleOwner, _cameraSelector, _cameraPreview, _imageAnalyzer);                // if not, this should be sufficient as a fallback
            else if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is AndroidX.Lifecycle.ILifecycleOwner maLifecycleOwner)
                _camera = _cameraProvider.BindToLifecycle(maLifecycleOwner, _cameraSelector, _cameraPreview, _imageAnalyzer);
        }
    }

    public void UpdateTorch(bool on)
    {
        _camera?.CameraControl?.EnableTorch(on);
    }

    public void Focus(Point point)
    {
        if (_camera != null && _previewView.LayoutParameters != null)
        {
            _camera.CameraControl?.CancelFocusAndMetering();

            var factory =
                    new SurfaceOrientedMeteringPointFactory(
                        _previewView.LayoutParameters.Width,
                        _previewView.LayoutParameters.Height
                    );
            var fpoint = factory.CreatePoint(point.X, point.Y);
            var action = new FocusMeteringAction.Builder(fpoint, FocusMeteringAction.FlagAf)
                                    .DisableAutoCancel()
                                    .Build();

            _camera.CameraControl?.StartFocusAndMetering(action);
        }
    }

    public void AutoFocus()
    {
        _camera?.CameraControl?.CancelFocusAndMetering();
        var factory = new SurfaceOrientedMeteringPointFactory(1f, 1f);
        var fpoint = factory.CreatePoint(.5f, .5f);
        var action = new FocusMeteringAction.Builder(fpoint, FocusMeteringAction.FlagAf)
                                //.DisableAutoCancel()
                                .Build();

        _camera?.CameraControl?.StartFocusAndMetering(action);
    }

    public void Dispose()
    {
        _cameraExecutor?.Shutdown();
        _cameraExecutor?.Dispose();
    }
}