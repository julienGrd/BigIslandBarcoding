using Microsoft.Maui.Graphics;
using ZXing.Common;
using ZXing.Rendering;

namespace ZXing.Net.Maui;

internal class BarcodeBitmapRenderer : IBarcodeRenderer<BarcodeDrawable>
{
    public Color ForegroundColor { get; set; } = Colors.Black;
    public Color BackgroundColor { get; set; } = Colors.White;

    public BarcodeDrawable Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content)
        => Render(matrix, format, content, new EncodingOptions());

    public BarcodeDrawable Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content, EncodingOptions options)
        => new BarcodeDrawable(this, matrix);
}