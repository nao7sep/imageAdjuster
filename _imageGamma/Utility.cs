using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace _imageGamma
{
    public static class Utility
    {
        // https://en.wikipedia.org/wiki/Relative_luminance
        public static double GetLuminance (byte red, byte green, byte blue) => 0.2126 * red + 0.7152 * green + 0.0722 * blue;

        public static int [] GetLuminanceHistogramData (Image <Rgba32> image)
        {
            int xWidth = image.Width,
                xHeight = image.Height;

            int [] xHistogramData = new int [256];

            image.ProcessPixelRows (accessor =>
            {
                // Parallel.For doesnt work well here because we'd need to lock the array.
                for (int y = 0; y < xHeight; y ++)
                {
                    Span <Rgba32> xRow = accessor.GetRowSpan (y);

                    for (int x = 0; x < xWidth; x ++)
                    {
                        Rgba32 xPixel = xRow [x];
                        int xLuminance = (int) Math.Round (GetLuminance (xPixel.R, xPixel.G, xPixel.B));
                        xHistogramData [xLuminance] ++;
                    }
                }
            });

            return xHistogramData;
        }
    }
}
