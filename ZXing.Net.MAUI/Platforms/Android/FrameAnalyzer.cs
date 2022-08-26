using AndroidX.Camera.Core;
using Java.Nio;
using Microsoft.Maui.Graphics;
using System;

namespace ZXing.Net.Maui;

internal class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
	readonly Action<ByteBuffer, Size> _frameCallback;

	public FrameAnalyzer(Action<ByteBuffer, Size> callback)
	{
		_frameCallback = callback;
	}

	public void Analyze(IImageProxy image)
	{
		_frameCallback.Invoke(
            image.GetPlanes()[0].Buffer, 
			new(image.Width, image.Height)
		);

		image.Close();
	}
}