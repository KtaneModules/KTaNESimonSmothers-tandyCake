using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Square : IEnumerable<Coordinate>
{
    private int _sideLength;
    public RGBColor color { get; private set; }
    public Coordinate[] encompassingSquares { get; private set; }

    public Square(int sideLength, RGBColor color)
    {
        _sideLength = sideLength;
        this.color = color;
        encompassingSquares = new Coordinate[sideLength * sideLength];
        for (int y = 0; y < sideLength; y++)
            for (int x = 0; x < sideLength; x++)
                encompassingSquares[y * sideLength + x] = new Coordinate(x, y, (int)color);
    }

    public void Move(Dir dir)
    {
        for (int i = 0; i < 9; i++)
            encompassingSquares[i] = encompassingSquares[i].ApplyMovement(dir);
    }
    public void Move(IEnumerable<Dir> dirs)
    {
        foreach (Dir d in dirs)
            Move(d);
    }

    public IEnumerator<Coordinate> GetEnumerator()
    { return encompassingSquares.AsEnumerable().GetEnumerator(); }

    IEnumerator IEnumerable.GetEnumerator()
    { return GetEnumerator(); }
}
