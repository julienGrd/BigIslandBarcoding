using Microsoft.Maui.Graphics;

namespace ZXing.Net.Maui;

class BarcodeWriter : BarcodeWriter<BarcodeDrawable>, IBarcodeWriter
{
    readonly BarcodeBitmapRenderer _bitmapRenderer = new BarcodeBitmapRenderer();

    public BarcodeWriter() => Renderer = _bitmapRenderer;

    public Color ForegroundColor
    {
        get => _bitmapRenderer.ForegroundColor;
        set => _bitmapRenderer.ForegroundColor = value;
    }

    public Color BackgroundColor
    {
        get => _bitmapRenderer.BackgroundColor;
        set => _bitmapRenderer.BackgroundColor = value;
    }
}