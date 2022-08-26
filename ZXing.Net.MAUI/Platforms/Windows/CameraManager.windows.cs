using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui;

// compile with: Uno
internal partial class CameraManager
{
    public NativePlatformCameraPreviewView CreateNativeView()
	{
        return new();
	}

    public void Connect(){ }

    public void Disconnect() { }

	public void UpdateCamera() { }

	public void UpdateTorch(bool on) {}

	public void Focus(Point point) { }

	public void AutoFocus() { }

	public void Dispose() { }

    public IDrawable GetFrame()
    {
        return new NotAllowedDrawable();
    }
}