using System.IO;
using System.Linq;

namespace ZXing.Net.Maui.Readers;

public class ZXingBarcodeReader : IBarcodeReader
{
	readonly BarcodeReaderGeneric _zxingReader;
	BarcodeReaderOptions? _options;

	public ZXingBarcodeReader()
	{
		_zxingReader = new BarcodeReaderGeneric();
	}

	public BarcodeReaderOptions Options
	{

		get => _options ??= new BarcodeReaderOptions();
		set
		{
			_options = value;
			_zxingReader.Options.PossibleFormats = Options.Formats.ToZXingList();
			_zxingReader.Options.TryHarder = Options.TryHarder;
			_zxingReader.AutoRotate = Options.AutoRotate;
		}
	}

    public BarcodeResult[]? Decode(PixelBufferHolder image)
    {
        var ls = GetLuminanceSource(image);

        if (Options.Multiple)
            return _zxingReader.DecodeMultiple(ls)?.ToBarcodeResults();

        var b = _zxingReader.Decode(ls)?.ToBarcodeResult();
        if (b != null)
            return new[] { b };

        return null;
    }

    public BarcodeResult[]? Decode(Stream stream)
		=> Decode(PixelBufferHolder.FromStream(stream));

    static LuminanceSource GetLuminanceSource(PixelBufferHolder image)
    {
        var w = (int)image.Size.Width;
        var h = (int)image.Size.Height;

#if MACCATALYST || IOS
        if (image.PixelBuffer != null)
            return new CVPixelBufferBGRA32LuminanceSource(image.PixelBuffer, w, h);
#endif

        return 
            new RGBLuminanceSource(
                image.Data.ToArray(), 
                w, h
#if ANDROID || MACCATALYST || IOS
                , RGBLuminanceSource.BitmapFormat.Gray8
#endif
            );
    }
}