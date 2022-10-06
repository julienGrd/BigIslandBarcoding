using Microsoft.Maui.Graphics;
using ZXing.Common;

namespace ZXing.Net.Maui;

internal class BarcodeDrawable : IDrawable
{
    readonly BarcodeBitmapRenderer _renderer;
    readonly BitMatrix _matrix;

    public BarcodeDrawable(BarcodeBitmapRenderer renderer, BitMatrix matrix)
    {
        this._renderer = renderer;
        this._matrix = matrix;
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var width = _matrix.Width;
        var height = _matrix.Height;

        canvas.FillColor = _renderer.BackgroundColor;
        canvas.FillRectangle(0, 0, width, height);

        canvas.FillColor = _renderer.ForegroundColor;

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                if (_matrix[x, y])
                    canvas.FillRectangle(x, y, 1, 1);

        canvas.SaveState();
    }
}
