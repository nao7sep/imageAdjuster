using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace _imageAdjuster
{
    public static class Utility
    {
        // https://en.wikipedia.org/wiki/Relative_luminance
        public static byte GetLuminance (byte red, byte green, byte blue) => (byte) Math.Round (0.2126 * red + 0.7152 * green + 0.0722 * blue);

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
                        byte xLuminance = GetLuminance (xPixel.R, xPixel.G, xPixel.B);
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

        public static byte [] CreateLookupTable (int minValue, int maxValue)
        {
            byte [] xLookupTable = new byte [256];

            // minValue and maxValue are inclusive.
            // If minValue is 0 and maxValue is 255, no scaling will be done.
            double xScale = 255.0 / (maxValue - minValue);

            for (int temp = 0; temp <= 255; temp ++)
            {
                if (temp < minValue)
                    xLookupTable [temp] = 0;

                else if (temp > maxValue)
                    xLookupTable [temp] = 255;

                // minValue must be mapped to 0 and maxValue must be mapped to 255.
                // The equation is technically: (temp - minValue) * 255 / (maxValue - minValue)
                // If temp == maxValue, the result will be 255.
                else xLookupTable [temp] = (byte) Math.Round ((temp - minValue) * xScale);
            }

            return xLookupTable;
        }

        public static void ApplyLookupTable (Image <Rgba32> image, byte [] lookupTable)
        {
            int xWidth = image.Width,
                xHeight = image.Height;

            image.ProcessPixelRows (accessor =>
            {
                // ParallelRowIterator may be applicable if we want to parallelize this.
                // https://stackoverflow.com/questions/71388492/right-way-to-parallelize-pixel-access-across-multiple-images-using-imagesharp

                for (int y = 0; y < xHeight; y ++)
                {
                    Span <Rgba32> xRow = accessor.GetRowSpan (y);

                    for (int x = 0; x < xWidth; x ++)
                    {
                        Rgba32 xPixel = xRow [x];
                        xPixel.R = lookupTable [xPixel.R];
                        xPixel.G = lookupTable [xPixel.G];
                        xPixel.B = lookupTable [xPixel.B];
                        xRow [x] = xPixel;
                    }
                }
            });
        }
    }
}
