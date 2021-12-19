using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flash {

	public Dir direction;
	public RGBColor color;
	public Square associatedSquare;

    public Flash(Dir direction, RGBColor color, int associatedSquareLength)
    {
        this.direction = direction;
        this.color = color;
        this.associatedSquare = new Square(associatedSquareLength, this.color);
    }
    public override string ToString()
    {
        return string.Format("({0} {1})", direction, color);
    }
}
