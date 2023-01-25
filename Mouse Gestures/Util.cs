using System;
using System.Runtime.InteropServices;

namespace WMG.Core
{
     // Compatible with the winapi POINT struct
     // http://pinvoke.net/default.aspx/Structures.POINT
     // https://docs.microsoft.com/en-us/windows/desktop/api/windef/ns-windef-point
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct Point(int X, int Y)
    {
        public double SquareDistance(Point other) =>
            (X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y);

        public override string ToString() => $"({X}, {Y})";
    }

    // Compatible with the winapi RECT struct
    // https://pinvoke.net/default.aspx/Structures/RECT.html
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct Rect(int Left, int Top, int Right, int Bottom)
    {
        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public static Rect FromDimensions(int left, int top, int width, int height) => new(left, top, left + width, top + height);

        public override string ToString() => $"({Left}, {Top}, {Right}, {Bottom})";
    }
}