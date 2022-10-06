using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ZXing.Net.Maui.Controls;

public class BarcodeGeneratorView : GraphicsView
{
    public static readonly BindableProperty ForegroundColorProperty = 
        BindableProperty.Create(nameof(ForegroundColor), typeof(Color), typeof(BarcodeGeneratorView), Colors.Black);

    public static readonly BindableProperty BarcodeMarginProperty = 
        BindableProperty.Create(nameof(BarcodeMargin), typeof(int), typeof(BarcodeGeneratorView), 1);

    public static readonly BindableProperty FormatProperty = 
        BindableProperty.Create(nameof(Format), typeof(BarcodeFormat), typeof(BarcodeGeneratorView), BarcodeFormat.QrCode);

    public static readonly BindableProperty ValueProperty = 
        BindableProperty.Create(nameof(Value), typeof(string), typeof(BarcodeGeneratorView), "Barcode Generator - ZXing.Net.Maui");

    BarcodeWriter _writer;

    public Color ForegroundColor
    {
        get => (Color)GetValue(ForegroundColorProperty);
        set => SetValue(ForegroundColorProperty, value);
    }

    public int BarcodeMargin
    {
        get => (int)GetValue(BarcodeMarginProperty);
        set => SetValue(BarcodeMarginProperty, value);
    }

    public BarcodeFormat Format
    {
        get => (BarcodeFormat)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public BarcodeGeneratorView()
    {
        _writer = new BarcodeWriter();

        _writer.ForegroundColor = ForegroundColor;
        _writer.BackgroundColor = BackgroundColor;
        _writer.Options.Margin = BarcodeMargin;
        _writer.Format = Format.ToZXingFormat();

        GetBarcode();
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (
            new[] { "Value", "WidthRequest", "HeightRequest", "Format", "BarcodeMargin" }
                .Contains(propertyName)
        )
        {
            if (propertyName == "WidthRequest" && WidthRequest >= 0)
                _writer.Options.Width = (int)WidthRequest;

            if (propertyName == "HeightRequest" && HeightRequest >= 0)
                _writer.Options.Height = (int)HeightRequest;

            _writer.Options.Margin = BarcodeMargin;
            _writer.Format = Format.ToZXingFormat();

            GetBarcode();
        }
        else if (
            propertyName == "ForegroundColor" ||
            propertyName == "BackgroundColor"
        )
        {
            _writer.ForegroundColor = ForegroundColor;
            _writer.BackgroundColor = BackgroundColor;

            Invalidate();
        }
    }

    void GetBarcode()
    {
        Drawable = _writer.Write(Value);

        Invalidate();
    }
}