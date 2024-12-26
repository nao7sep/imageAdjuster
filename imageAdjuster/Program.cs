using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace imageAdjuster
{
    class Program
    {
        static void Main (string [] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine ("Usage: imageAdjuster.exe <image1> <image2> ...");
                    return;
                }

                foreach (string xFilePath in args)
                {
                    try
                    {
                        var xImageInfo = Image.Identify (xFilePath);
                    }

                    catch (Exception xException)
                    {
                        throw new Exception ($"Invalid image file: {xFilePath}", xException);
                    }
                }

                List <(string FilePath, int [] HistogramData, (int MinValue, int MaxValue) Limits0, (int MinValue, int MaxValue) Limits1)> xAnalysisData = [];

                int xCount = 0;

                foreach (string xFilePath in args.Order (StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        xCount ++;
                        Console.Write ($"\rAnalyzing image {xCount} of {args.Length}...");

                        using var xImage = Image.Load <Rgba32> (xFilePath);
                        var xHistogramData = Utility.GetLuminanceHistogramData (xImage);
                        var xLimits0 = Utility.GetContrastStretchingLimits (xHistogramData, xImage.Width * xImage.Height, 0, 0);
                        var xLimits1 = Utility.GetContrastStretchingLimits (xHistogramData, xImage.Width * xImage.Height, 1, 1);

                        xAnalysisData.Add ((xFilePath, xHistogramData, xLimits0, xLimits1));
                    }

                    catch (Exception xException)
                    {
                        throw new Exception ($"Failed to handle image file: {xFilePath}", xException);
                    }
                }

                StringBuilder xHtmlBuilder = new ();

                string _IndentLevelToString (int indentLevel) =>
                    new (' ', indentLevel * 4);

                string _AttributesToString (string [] attributes) =>
                    string.Concat (Enumerable.Range (0, attributes.Length / 2).Select (x => $" {attributes [x * 2]}=\"{attributes [x * 2 + 1]}\""));

                void _OpenElement (int indentLevel, string name, params string [] attributes) =>
                    xHtmlBuilder.AppendLine ($"{_IndentLevelToString (indentLevel)}<{name}{_AttributesToString (attributes)}>");

                void _AddElement (int indentLevel, string name, string value, params string [] attributes) =>
                    xHtmlBuilder.AppendLine ($"{_IndentLevelToString (indentLevel)}<{name}{_AttributesToString (attributes)}>{value}</{name}>");

                void _CloseElement (int indentLevel, string name) =>
                    xHtmlBuilder.AppendLine ($"{_IndentLevelToString (indentLevel)}</{name}>");

                int xIndentLevel = 0;

                _OpenElement (xIndentLevel, "html");
                _OpenElement (++ xIndentLevel, "head");
                _AddElement (++ xIndentLevel, "title", "Image Analysis Report");
                _OpenElement (xIndentLevel, "style");
                xHtmlBuilder.AppendLine ($"{_IndentLevelToString (++ xIndentLevel)}body {{ margin: 0; }}");
                xHtmlBuilder.AppendLine ($"{_IndentLevelToString (xIndentLevel)}table {{ margin: 20px; border-collapse: collapse; }}");
                xHtmlBuilder.AppendLine ($"{_IndentLevelToString (xIndentLevel)}table, th, td {{ border: 1px solid gray; }}");
                xHtmlBuilder.AppendLine ($"{_IndentLevelToString (xIndentLevel)}th, td {{ padding: 10px; }}");
                xHtmlBuilder.AppendLine ($"{_IndentLevelToString (xIndentLevel)}.warning {{ background-color: yellow; }}");
                _CloseElement (-- xIndentLevel, "style");
                _CloseElement (-- xIndentLevel, "head");

                _OpenElement (xIndentLevel, "body");
                _OpenElement (++ xIndentLevel, "table");
                _OpenElement (++ xIndentLevel, "tr");
                _AddElement (++ xIndentLevel, "th", "Average/Image", "rowspan", "2");
                _AddElement (xIndentLevel, "th", "0%", "colspan", "2");
                _AddElement (xIndentLevel, "th", "1%", "colspan", "2");
                _CloseElement (-- xIndentLevel, "tr");

                _OpenElement (xIndentLevel, "tr");
                _AddElement (++ xIndentLevel, "th", "Min");
                _AddElement (xIndentLevel, "th", "Max");
                _AddElement (xIndentLevel, "th", "Min");
                _AddElement (xIndentLevel, "th", "Max");
                _CloseElement (-- xIndentLevel, "tr");

                int xLimit0MinValueAverage = (int) Math.Round (xAnalysisData.Average (x => x.Limits0.MinValue)),
                    xLimit0MaxValueAverage = (int) Math.Round (xAnalysisData.Average (x => x.Limits0.MaxValue)),
                    xLimit1MinValueAverage = (int) Math.Round (xAnalysisData.Average (x => x.Limits1.MinValue)),
                    xLimit1MaxValueAverage = (int) Math.Round (xAnalysisData.Average (x => x.Limits1.MaxValue));

                _OpenElement (xIndentLevel, "tr");
                _AddElement (++ xIndentLevel, "td", "Average");
                _AddElement (xIndentLevel, "td", xLimit0MinValueAverage.ToString ());
                _AddElement (xIndentLevel, "td", xLimit0MaxValueAverage.ToString ());
                _AddElement (xIndentLevel, "td", xLimit1MinValueAverage.ToString ());
                _AddElement (xIndentLevel, "td", xLimit1MaxValueAverage.ToString ());
                _CloseElement (-- xIndentLevel, "tr");

                foreach (var xEntry in xAnalysisData)
                {
                    // Supposing the average values are going to be applied,
                    // if images are going to be more heavily adjusted than suggested, they are marked with a warning.

                    _OpenElement (xIndentLevel, "tr");
                    _AddElement (++ xIndentLevel, "td", Path.GetFileName (xEntry.FilePath));
                    _AddElement (xIndentLevel, "td", xEntry.Limits0.MinValue.ToString (), (xEntry.Limits0.MinValue < xLimit0MinValueAverage) ? ["class", "warning"] : []);
                    _AddElement (xIndentLevel, "td", xEntry.Limits0.MaxValue.ToString (), (xEntry.Limits0.MaxValue > xLimit0MaxValueAverage) ? ["class", "warning"] : []);
                    _AddElement (xIndentLevel, "td", xEntry.Limits1.MinValue.ToString (), (xEntry.Limits1.MinValue < xLimit1MinValueAverage) ? ["class", "warning"] : []);
                    _AddElement (xIndentLevel, "td", xEntry.Limits1.MaxValue.ToString (), (xEntry.Limits1.MaxValue > xLimit1MaxValueAverage) ? ["class", "warning"] : []);
                    _CloseElement (-- xIndentLevel, "tr");
                }

                _CloseElement (-- xIndentLevel, "table");
                _CloseElement (-- xIndentLevel, "body");
                _CloseElement (-- xIndentLevel, "html");

                string xHtmlFileName = "Analyzed-" + DateTime.UtcNow.ToString ("yyyyMMdd'T'HHmmss'Z'") + ".htm",
                       xHtmlFilePath = Path.Join (Path.GetDirectoryName (args [0]), "Adjusted", xHtmlFileName);

                Directory.CreateDirectory (Path.GetDirectoryName (xHtmlFilePath)!);
                File.WriteAllText (xHtmlFilePath, xHtmlBuilder.ToString (), Encoding.UTF8);

                Console.WriteLine ($"\rAnalysis report saved to: {xHtmlFilePath}");

                while (true)
                {
                    int xMinValue = 0;

                    while (true)
                    {
                        Console.Write ("Min Value: ");
                        string? xMinValueString = Console.ReadLine ();

                        if (int.TryParse (xMinValueString, out xMinValue) && xMinValue >= 0 && xMinValue <= 255)
                            break;
                    }

                    int xMaxValue = 255;

                    while (true)
                    {
                        Console.Write ("Max Value: ");
                        string? xMaxValueString = Console.ReadLine ();

                        // If xMaxValue == xMinValue, CreateLookupTable still wont crash for a reason described in its comment, but it will not do anything useful.
                        if (int.TryParse (xMaxValueString, out xMaxValue) && xMaxValue >= 0 && xMaxValue <= 255 && xMaxValue > xMinValue)
                            break;
                    }

                    var xLookupTable = Utility.CreateLookupTable (xMinValue, xMaxValue);

                    List <(string FilePath, string AdjustedFilePath)> xAdjustedFiles = [];

                    foreach (var xEntry in xAnalysisData)
                    {
                        using var xImage = Image.Load <Rgba32> (xEntry.FilePath);
                        Utility.ApplyLookupTable (xImage, xLookupTable);

                        string xAdjustedFileName = Path.GetFileNameWithoutExtension (xEntry.FilePath) + "-Adjusted" + Path.GetExtension (xEntry.FilePath),
                               xAdjustedFilePath = Path.Join (Path.GetDirectoryName (xEntry.FilePath), xAdjustedFileName);

                        xImage.Save (xAdjustedFilePath);
                        Console.WriteLine ($"Adjusted image saved to: {xAdjustedFilePath}");

                        string xNewFilePath = Path.Join (Path.GetDirectoryName (xEntry.FilePath), "Adjusted", Path.GetFileName (xEntry.FilePath));
                        Directory.CreateDirectory (Path.GetDirectoryName (xNewFilePath)!); // Just in case.
                        File.Move (xEntry.FilePath, xNewFilePath);
                        Console.WriteLine ($"Original image moved to: {xNewFilePath}");

                        xAdjustedFiles.Add ((xEntry.FilePath, xAdjustedFilePath));
                    }

                    ConsoleKeyInfo xKey = default;

                    while (true)
                    {
                        Console.Write ("Press 'F' to finish or 'R' to revert: ");
                        xKey = Console.ReadKey (true);

                        if (xKey.Key == ConsoleKey.F)
                        {
                            Console.WriteLine ("F");

                            string xAdjustedFileName = "Adjusted-" + DateTime.UtcNow.ToString ("yyyyMMdd'T'HHmmss'Z'") + ".txt",
                                   xAdjustedFilePath = Path.Join (Path.GetDirectoryName (args [0]), "Adjusted", xAdjustedFileName);

                            File.WriteAllText (xAdjustedFilePath, $"Min Value: {xMinValue}{Environment.NewLine}Max Value: {xMaxValue}{Environment.NewLine}{Environment.NewLine}", Encoding.UTF8);
                            File.AppendAllLines (xAdjustedFilePath, xAdjustedFiles.Select (x => $"{x.FilePath} => {x.AdjustedFilePath}"), Encoding.UTF8);
                            Console.WriteLine ($"Adjustment info saved to: {xAdjustedFilePath}");

                            break;
                        }

                        if (xKey.Key == ConsoleKey.R)
                        {
                            Console.WriteLine ("R");

                            foreach (var xAdjustedFile in xAdjustedFiles)
                            {
                                File.Delete (xAdjustedFile.AdjustedFilePath);
                                Console.WriteLine ($"Adjusted image deleted: {xAdjustedFile.AdjustedFilePath}");

                                string xNewFilePath = Path.Join (Path.GetDirectoryName (xAdjustedFile.FilePath), "Adjusted", Path.GetFileName (xAdjustedFile.FilePath));
                                File.Move (xNewFilePath, xAdjustedFile.FilePath);
                                Console.WriteLine ($"Original image restored: {xAdjustedFile.FilePath}");
                            }

                            break;
                        }
                    }

                    if (xKey.Key == ConsoleKey.F)
                        break;
                }
            }

            catch (Exception xException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine (xException.ToString ());
                Console.ResetColor ();
            }

            finally
            {
                Console.Write ("Press any key to exit: ");
                Console.ReadKey (true);
                Console.WriteLine ();
            }
        }
    }
}
