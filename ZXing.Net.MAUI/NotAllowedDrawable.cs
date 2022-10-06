namespace ZXing.Net.Maui;

using Microsoft.Maui.Graphics;

internal class NotAllowedDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect)
    {
		var radius = 40d;
		var adjuster = radius - 12;
        var center = new Point(rect.Width / 2f, rect.Height / 2f);


        canvas.StrokeColor = Colors.Red;
		canvas.StrokeSize = 4;

        canvas.DrawCircle(center, radius);
        canvas.DrawLine(
			new Point(center.X - adjuster, center.Y - adjuster), 
			new Point(center.X + adjuster, center.Y + adjuster)
		);
    }
}