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

        /// <summary>
        /// Both MinValue and MaxValue are inclusive, meaning that they are the lower and upper bounds of the range that should be preserved.
        /// </summary>
        public static (int MinValue, int MaxValue) GetContrastStretchingLimits (int [] histogramData, int totalPixelCount, double lowerCutoffPercentage, double upperCutoffPercentage)
        {
            int xMinValue = 0;

            if (lowerCutoffPercentage >= 0)
            {
                int xLowerCutoffPixelCount = (int) Math.Round (totalPixelCount * lowerCutoffPercentage / 100),
                    xCurrentPixelCount = 0;

                for (int temp = 0; temp <= 255; temp ++)
                {
                    if ((xCurrentPixelCount += histogramData [temp]) > xLowerCutoffPixelCount)
                    {
                        xMinValue = temp;
                        break;
                    }
                }
            }

            int xMaxValue = 255;

            if (upperCutoffPercentage >= 0)
            {
                int xUpperCutoffPixelCount = (int) Math.Round (totalPixelCount * upperCutoffPercentage / 100),
                    xCurrentPixelCount = 0;

                for (int temp = 255; temp >= 0; temp --)
                {
                    if ((xCurrentPixelCount += histogramData [temp]) > xUpperCutoffPixelCount)
                    {
                        xMaxValue = temp;
                        break;
                    }
                }
            }

            if (xMinValue >= xMaxValue)
            {
                xMinValue = 0;
                xMaxValue = 255;
            }

            return (xMinValue, xMaxValue);
        }

    #if DEBUG
        public static void TestGetContrastStretchingLimits ()
        {
            static void _Test (int [] firstPart, int [] lastPart, double lowerCutoffPercentage, double upperCutoffPercentage)
            {
                int [] xHistogramData = new int [256];
                Array.Copy (firstPart, 0, xHistogramData, 0, firstPart.Length);
                Array.Copy (lastPart, 0, xHistogramData, 256 - lastPart.Length, lastPart.Length);

                var xLimits = GetContrastStretchingLimits (xHistogramData, totalPixelCount: 100, lowerCutoffPercentage, upperCutoffPercentage);

                Console.WriteLine ($"First: {string.Join (", ", firstPart)}, Last: {string.Join (", ", lastPart)}, Lower: {lowerCutoffPercentage}, Upper: {upperCutoffPercentage} => Min: {xLimits.MinValue}, Max: {xLimits.MaxValue}");
            }

            // The following results are from a GetContrastStretchingLimits without the xMinValue >= xMaxValue check.

            _Test ([0, 1, 2], [2, 1, 0], -1, -1); // => Min: 0, Max: 255
            _Test ([0, 1, 2], [2, 1, 0], 0, 0); // => Min: 1, Max: 254
            _Test ([0, 1, 2], [2, 1, 0], 1, 1); // => Min: 2, Max: 253
            _Test ([0, 1, 2], [2, 1, 0], 2, 2); // => Min: 2, Max: 253

            _Test ([0, 1, 2], [2, 1, 0], 3, 3); // => Min: 253, Max: 2

            _Test ([100], [], -1, -1); // => Min: 0, Max: 255
            _Test ([100], [], 0, 0); // => Min: 0, Max: 0
            _Test ([100], [], 1, 1); // => Min: 0, Max: 0

            _Test ([], [100], -1, -1); // => Min: 0, Max: 255
            _Test ([], [100], 0, 0); // => Min: 255, Max: 255
            _Test ([], [100], 1, 1); // => Min: 255, Max: 255
        }
    #endif
    }
}
