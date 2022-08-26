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
    AVCaptureSession _captureSession;
    AVCaptureDevice? _captureDevice;
    AVCaptureInput? _captureInput = null;
    PreviewView _view;
    AVCaptureVideoDataOutput _videoDataOutput;
    AVCaptureVideoPreviewLayer _videoPreviewLayer;
    CaptureDelegate _captureDelegate;
    DispatchQueue _dispatchQueue;
    static Dictionary<NSString, MSize> Resolutions => new()
    {
        { AVCaptureSession.Preset352x288, new MSize(352, 288) },
        { AVCaptureSession.PresetMedium, new MSize(480, 360) },
        { AVCaptureSession.Preset640x480, new MSize(640, 480) },
        { AVCaptureSession.Preset1280x720, new MSize(1280, 720) },
        { AVCaptureSession.Preset1920x1080, new MSize(1920, 1080) },
        { AVCaptureSession.Preset3840x2160, new MSize(3840, 2160) },
    };
    static AVCaptureDeviceType[] CameraTypes => new[] 
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
        if (_captureSession != null)
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

            if (CameraLocation == CameraLocation.Front)
            {
                using var frontCameras = 
                    AVCaptureDeviceDiscoverySession.Create(
                        CameraTypes, 
                        AVMediaTypes.Video, 
                        AVCaptureDevicePosition.Front
                    );
                _captureDevice = frontCameras?.Devices?.FirstOrDefault();
            }
            else if (CameraLocation == CameraLocation.Rear)
            {
                using var backCameras = 
                    AVCaptureDeviceDiscoverySession.Create(
                        CameraTypes, 
                        AVMediaTypes.Video, 
                        AVCaptureDevicePosition.Back
                    );
                _captureDevice = backCameras?.Devices?.FirstOrDefault();
            }

            if (_captureDevice == null)
                _captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video.GetConstant()!);

            if (_captureDevice == null)
                throw new NotSupportedException("No capture device found");

            _captureInput = new AVCaptureDeviceInput(_captureDevice, out var _);

            _captureSession.AddInput(_captureInput);

            _captureSession.StartRunning();
        }
    }


    public void Disconnect()
    {
        _captureSession.RemoveOutput(_videoDataOutput);
        _captureSession.StopRunning();
    }

    public void UpdateTorch(bool on)
    {
        if (_captureDevice != null && _captureDevice.HasTorch && _captureDevice.TorchAvailable)
        {
            _captureDevice.LockForConfiguration(out var _);
            _captureDevice.TorchMode = on ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
            _captureDevice.UnlockForConfiguration();
        }
    }

    public void Focus(Point point)
    {
        if (_captureDevice == null)
            return;

        //See if it supports focusing on a point
        if (_captureDevice.FocusPointOfInterestSupported && !_captureDevice.AdjustingFocus)
        {
            //Lock device to config
            if (_captureDevice.LockForConfiguration(out var _))
            {
                //Focus at the point touched
                _captureDevice.FocusPointOfInterest = point;
                _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;
                _captureDevice.UnlockForConfiguration();
            }
        }
    }

    public void AutoFocus()
    {
        if (_captureDevice == null)
            return;

        var focusMode = AVCaptureFocusMode.AutoFocus;
        if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            focusMode = AVCaptureFocusMode.ContinuousAutoFocus;

        //Lock device to config
        if (_captureDevice.LockForConfiguration(out var _))
        {
            if (_captureDevice.FocusPointOfInterestSupported)
                _captureDevice.FocusPointOfInterest = CoreGraphics.CGPoint.Empty;
            _captureDevice.FocusMode = focusMode;
            _captureDevice.UnlockForConfiguration();
        }
    }

    public void Dispose()
    {
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