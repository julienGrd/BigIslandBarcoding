#if IOS || MACCATALYST

using System;
using System.Collections.Generic;
using System.Linq;
using AVFoundation;
using CoreFoundation;
using CoreVideo;
using Foundation;
using Microsoft.Maui.Graphics;
using UIKit;
using MSize = Microsoft.Maui.Graphics.Size;

namespace ZXing.Net.Maui;

internal partial class CameraManager
{
    AVCaptureSession _captureSession = null!;
    AVCaptureDevice? _captureDevice;
    AVCaptureInput? _captureInput = null;
    PreviewView _view = null!;
    AVCaptureVideoDataOutput? _videoDataOutput;
    AVCaptureVideoPreviewLayer _videoPreviewLayer = null!;
    CaptureDelegate? _captureDelegate;
    DispatchQueue? _dispatchQueue;
    static Dictionary<NSString, MSize> _availableResolutions => new()
    {
        { AVCaptureSession.Preset352x288, new MSize(352, 288) },
        { AVCaptureSession.PresetMedium, new MSize(480, 360) },
        { AVCaptureSession.Preset640x480, new MSize(640, 480) },
        { AVCaptureSession.Preset1280x720, new MSize(1280, 720) },
        { AVCaptureSession.Preset1920x1080, new MSize(1920, 1080) },
        { AVCaptureSession.Preset3840x2160, new MSize(3840, 2160) },
    };
    static AVCaptureDeviceType[] _cameraTypes => new[]
        {
            AVCaptureDeviceType.BuiltInDualCamera,
            AVCaptureDeviceType.BuiltInDualWideCamera,
            AVCaptureDeviceType.BuiltInDuoCamera,
            AVCaptureDeviceType.BuiltInLiDarDepthCamera,
            AVCaptureDeviceType.BuiltInMicrophone,
            AVCaptureDeviceType.BuiltInTelephotoCamera,
            AVCaptureDeviceType.BuiltInTripleCamera,
            AVCaptureDeviceType.BuiltInTrueDepthCamera,
            AVCaptureDeviceType.BuiltInUltraWideCamera,
            AVCaptureDeviceType.BuiltInWideAngleCamera,
            AVCaptureDeviceType.ExternalUnknown
        };

    public Size TargetCaptureResolution { get; private set; } = MSize.Zero;

    public NativePlatformCameraPreviewView CreatePlatformView()
    {
        _captureSession = new AVCaptureSession
        {
            SessionPreset = AVCaptureSession.Preset640x480
        };

        _videoPreviewLayer = new AVCaptureVideoPreviewLayer(_captureSession)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill
        };

        _view = new PreviewView(_videoPreviewLayer);

        return _view;
    }

    public void Connect()
    {
        UpdateCamera();

        if (_videoDataOutput == null)
        {
            _videoDataOutput = new AVCaptureVideoDataOutput();

            var videoSettings = NSDictionary.FromObjectAndKey(
                new NSNumber((int)CVPixelFormatType.CV32BGRA),
                CVPixelBuffer.PixelFormatTypeKey);

            _videoDataOutput.WeakVideoSettings = videoSettings;

            if (_captureDelegate == null)
            {
                _captureDelegate =
                    new CaptureDelegate(
                        cvPixelBuffer =>
                            FrameReady?.Invoke(
                                this,
                                new(
                                    new(
                                        new(cvPixelBuffer.Width, cvPixelBuffer.Height),
                                        cvPixelBuffer
                                    )
                                )
                            )
                    );
            }

            if (_dispatchQueue == null)
                _dispatchQueue = new DispatchQueue("CameraBufferQueue");

            _videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
            _videoDataOutput.SetSampleBufferDelegate(_captureDelegate, _dispatchQueue);
        }

        _captureSession.AddOutput(_videoDataOutput);
    }

    public void UpdateCamera()
    {
        if (_captureSession.Running)
            _captureSession.StopRunning();

        // Cleanup old input
        if (_captureInput != null && _captureSession.Inputs.Length > 0 && _captureSession.Inputs.Contains(_captureInput))
        {
            _captureSession.RemoveInput(_captureInput);
            _captureInput.Dispose();
            _captureInput = null;
        }

        // Cleanup old device
        if (_captureDevice != null)
        {
            _captureDevice.Dispose();
            _captureDevice = null;
        }

        var devices = AVCaptureDevice.DevicesWithMediaType(AVMediaTypes.Video.GetConstant());
        foreach (var device in devices)
        {
            if (CameraLocation == CameraLocation.Front &&
                device.Position == AVCaptureDevicePosition.Front)
            {
                _captureDevice = device;
                break;
            }
            else if (CameraLocation == CameraLocation.Rear && device.Position == AVCaptureDevicePosition.Back)
            {
                _captureDevice = device;
                break;
            }
        }

        if (_captureDevice == null)
            _captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);

        if (_captureDevice == null)
            throw new NotSupportedException("No capture device found");

        _captureInput = new AVCaptureDeviceInput(_captureDevice, out var err);

        _captureSession.SessionPreset = findBestAVCaptureSessionPreset(TargetCaptureResolution);
        _captureSession.AddInput(_captureInput);

        _captureSession.StartRunning();
    }


    public void Disconnect()
    {
        if (_captureSession.Running)
            _captureSession.StopRunning();

        if (_videoDataOutput != null)
        _captureSession.RemoveOutput(_videoDataOutput);

        // Cleanup old input
        if (_captureInput != null && _captureSession.Inputs.Length > 0 && _captureSession.Inputs.Contains(_captureInput))
        {
            _captureSession.RemoveInput(_captureInput);
            _captureInput.Dispose();
            _captureInput = null;
        }

        // Cleanup old device
        if (_captureDevice != null)
        {
            _captureDevice.Dispose();
            _captureDevice = null;
        }
    }

    public void UpdateTorch(bool on)
    {
        if (_captureDevice != null && _captureDevice.HasTorch && _captureDevice.TorchAvailable)
        {
            {
                var isOn = _captureDevice?.TorchActive ?? false;

                try
                {
                    if (on != isOn)
                    {
                        CaptureDevicePerformWithLockedConfiguration(device =>
                            device.TorchMode = on ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }

    public void Focus(Point point)
    {
        if (_captureDevice == null)
            return;

        var focusMode = AVCaptureFocusMode.AutoFocus;
        if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            focusMode = AVCaptureFocusMode.ContinuousAutoFocus;

        //See if it supports focusing on a point
        if (_captureDevice.FocusPointOfInterestSupported && !_captureDevice.AdjustingFocus)
        {
            CaptureDevicePerformWithLockedConfiguration(device =>
            {
                //Focus at the point touched
                device.FocusPointOfInterest = point;
                device.FocusMode = focusMode;
            });
        }
    }

    public void AutoFocus()
    {
        if (_captureDevice == null)
            return;

        var focusMode = AVCaptureFocusMode.AutoFocus;
        if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            focusMode = AVCaptureFocusMode.ContinuousAutoFocus;

        CaptureDevicePerformWithLockedConfiguration(device =>
        {
            if (device.FocusPointOfInterestSupported)
                device.FocusPointOfInterest = CoreGraphics.CGPoint.Empty;
            device.FocusMode = focusMode;
            device.UnlockForConfiguration();
        });
    }

    public void UpdateTargetCaptureResolution(Size targetCaptureResolution)
    {
        if (!targetCaptureResolution.Equals(TargetCaptureResolution))
        {
            TargetCaptureResolution = targetCaptureResolution;
            UpdateCamera();
        }
    }

    public void Dispose()
    {
    }

    void CaptureDevicePerformWithLockedConfiguration(Action<AVCaptureDevice> handler)
    {
        if (_captureDevice != null && _captureDevice.LockForConfiguration(out var _))
        {
            try
            {
                handler(_captureDevice);
            }
            finally
            {
                _captureDevice.UnlockForConfiguration();
            }
        }
    }

    NSString findBestAVCaptureSessionPreset(MSize target)
    {
        if (target == MSize.Zero || _captureDevice == null)
            return AVCaptureSession.Preset640x480;

        var current = new KeyValuePair<NSString, Size>(AVCaptureSession.Preset640x480, new Size(640, 480));
        var possibleResolutions = _availableResolutions.Where(res => _captureDevice.SupportsAVCaptureSessionPreset(res.Key));

        foreach (var r in possibleResolutions)
            if (r.Value.Width >= target.Width && r.Value.Height >= target.Height)
            {
                var targetWidthDistance = Math.Abs(target.Width - r.Value.Width);
                var targetHeightDistance = Math.Abs(target.Height - r.Value.Height);
                var currentWidthDistance = Math.Abs(current.Value.Width - r.Value.Width);
                var currentHeightDistance = Math.Abs(current.Value.Height - r.Value.Height);

                var targetDistance = targetWidthDistance + targetHeightDistance;
                var currentDistance = currentWidthDistance + currentHeightDistance;

                if (targetDistance < currentDistance)
                    current = r;
            }

        return current.Key;
    }
}

class PreviewView : UIView
{
    public PreviewView(AVCaptureVideoPreviewLayer layer) : base()
    {
        PreviewLayer = layer;

        PreviewLayer.Frame = Layer.Bounds;
        Layer.AddSublayer(PreviewLayer);
    }

    public readonly AVCaptureVideoPreviewLayer PreviewLayer;

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        PreviewLayer.Frame = Layer.Bounds;
    }
}

#endif