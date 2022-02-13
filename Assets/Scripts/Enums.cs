using System;
public enum Dir
{
    Up,
    Right,
    Down,
    Left,
}

[Flags]
public enum RGBColor
{
    Black = 0,
    
    Blue = 1,
    Green = 2,
    Red = 4,

    Cyan = Blue | Green,
    Magenta = Red | Blue,
    Yellow = Red | Green,

    White = Red | Green | Blue
}