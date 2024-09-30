namespace _imageGamma
{
    public static class Utility
    {
        // https://en.wikipedia.org/wiki/Relative_luminance
        public static double GetLuminance (byte red, byte green, byte blue) => 0.2126 * red + 0.7152 * green + 0.0722 * blue;
    }
}
