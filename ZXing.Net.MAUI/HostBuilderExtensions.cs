#if IOS || MACCATALYST
global using NativePlatformCameraPreviewView = global::UIKit.UIView;
global using NativePixelBufferData = global::CoreVideo.CVPixelBuffer;
#elif ANDROID
global using NativePlatformCameraPreviewView = global::AndroidX.Camera.View.PreviewView;
global using NativePixelBufferData = global::System.Collections.Generic.IEnumerable<byte>;
#elif WINDOWS
global using NativePlatformCameraPreviewView = global::Microsoft.UI.Xaml.Controls.Viewbox;
global using NativePixelBufferData = global::System.Collections.Generic.IEnumerable<byte>;
#endif

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace ZXing.Net.Maui;

public static class CameraViewExtensions
{
    static void AddHandlers(MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(ICameraView), typeof(CameraViewHandler));
            handlers.AddHandler(typeof(ICameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
        });
    }

	public static MauiAppBuilder UseBarcodeReader(this MauiAppBuilder builder)
    {
        AddHandlers(builder);

        builder.Services.AddTransient<Readers.IBarcodeReader, Readers.ZXingBarcodeReader>();

        return builder;
    }

    public static MauiAppBuilder UseBarcodeReader<TBarcodeReader>(this MauiAppBuilder builder) where TBarcodeReader : class, Readers.IBarcodeReader
	{
        AddHandlers(builder);

		builder.Services.AddTransient<Readers.IBarcodeReader, TBarcodeReader>();

		return builder;
	}
}