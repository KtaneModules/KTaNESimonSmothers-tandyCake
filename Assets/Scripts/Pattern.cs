using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pattern : IEnumerable<Coordinate>, IEquatable<Pattern>
{

    private List<Coordinate> _coords = new List<Coordinate>();
    public static readonly Pattern center = new Pattern { _coords = new List<Coordinate> { new Coordinate(0, 0, 0) } };

    public Pattern() { }
    public Pattern(IEnumerable<Coordinate> coords)
    {
        _coords = coords.ToList();
    }

    public void Add(Coordinate c)
    {
        bool alreadyHas = _coords.Contains(c);
        _coords.Add(c);
        if (alreadyHas)
            _CheckXOR();
    }
    public void Add(IEnumerable<Coordinate> coords)
    {
        _coords.AddRange(coords);
        _CheckXOR();
    }
    public bool AddInDir(Dir direction, int newValue)
    {
        Coordinate c = _coords.Last().ApplyMovement(direction);
        c.tileValue = newValue;
        if (_coords.Contains(c))
            return false;
        else
        {
            _coords.Add(c);
            return true;
        }
    }
    public bool AddInDir(Dir direction)
    {
        return AddInDir(direction, _coords.Last().tileValue);
    }


    public void Reset()
    {
        _coords.Clear();
    }
    private void _CheckXOR()
    {
        HashSet<Coordinate> placed = new HashSet<Coordinate>();
        foreach (Coordinate coord in _coords)
        {
            if (!placed.Add(coord))
                placed.Single(x => x.Equals(coord)).tileValue ^= coord.tileValue;
        }
        _coords = new List<Coordinate>(placed);
    }

    private Pattern _NormalizePosition(Pattern p)
    {
        if (_coords.Count == 0)
            return this;
        Pattern output = new Pattern();
        int minX = p._coords.Min(coord => coord.x);
        int minY = p._coords.Min(coord => coord.y);

        foreach (Coordinate coord in p)
        {
            Coordinate newCoord = coord.Copy();
            newCoord.x -= minX;
            newCoord.y -= minY;
            output.Add(newCoord);
        }
        IEnumerable<Coordinate> coords = output.OrderBy(coord => coord.x).ThenBy(coord => coord.y);
        return new Pattern(coords);
    }
    private Pattern _NormalizeColors(Pattern p)
    {
        if (_coords.Count == 0)
            return this;
        int[] allNums = p._coords.Select(x => x.tileValue)
                               .Distinct().OrderBy(x => x).ToArray();
        Dictionary<int, int> tableLookup = Enumerable.Range(0, allNums.Count()).ToDictionary(x => allNums[x]);

        Pattern output = new Pattern();
        foreach (Coordinate coord in p)
            output.Add(new Coordinate(coord.x, coord.y, tableLookup[coord.tileValue]));
        return output;
    }

    public Pattern Normalize()
    {
        return _NormalizeColors(_NormalizePosition(this));
    }
    private char _GetRGBChar(int value)
    {
        return value > 7 ? ((char)(value - 8 + '0')) : ((RGBColor)value).ToString()[0];
    }

    public IEnumerable<string> GetLoggingPattern(bool usingColors)
    {
        if (_coords.Count == 0)
            yield break;
        Pattern p = _NormalizePosition(this);
        int width = p.Max(crd => crd.x) + 1;
        int height = p.Max(crd => crd.y) + 1;

        for (int lineIx = 0; lineIx < height; lineIx++)
        {
            string loggingLine = "";
            Coordinate[] line = p.Where(crd => crd.y == lineIx).ToArray();
            for (int x = 0; x < width; x++)
            {
                if (line.Any(crd => crd.x == x))
                    loggingLine += usingColors ?
                                     _GetRGBChar(line.First(crd => crd.x == x).tileValue) :
                                     ((char)(line.First(crd => crd.x == x).tileValue + 'A'));
                else loggingLine += '.';
            }
            yield return loggingLine;
        }
    }

    public bool IsPaintable()
    {
        Dictionary<int, List<Coordinate>> groups = new Dictionary<int, List<Coordinate>>();
        foreach (Coordinate coord in this)
        {
            if (!groups.ContainsKey(coord.tileValue))
                groups.Add(coord.tileValue, new List<Coordinate>() { coord });
            else groups[coord.tileValue].Add(coord);
        }
        return this.Any(x => _IsPaintableFromCoord(x, groups));
    }
    private bool _IsPaintableFromCoord(Coordinate position, Dictionary<int, List<Coordinate>> groups)
    {
        if (!this.Contains(position))
            throw new ArgumentException();
        Coordinate current = position.Copy();
        List<Coordinate> coordsWeCanGoTo = _GetAdjacentsInPattern(current).Where(x => x.tileValue == position.tileValue).ToList();
        do
        {
            int currentPaint = position.tileValue;
            groups[currentPaint].Remove(position);
            List<Coordinate> adjacents = _GetAdjacentsInPattern(current).ToList();
            
        } while (true);
    }

    private IEnumerable<Coordinate> _GetAdjacentsInPattern(Coordinate c)
    {
        return c.GetAdjacents().Where(x => this.Contains(x));
    }
    public override string ToString()
    {
        return this.Join(", ");
    }
    public override bool Equals(object obj)
    {
        return obj is Pattern && Equals(obj as Pattern);
    }
    public bool Equals(Pattern other)
    {
        return this.Count() == other.Count() && this.Normalize().SequenceEqual(other.Normalize());
    }
    public override int GetHashCode()
    {
        int output = 0;
        foreach (Coordinate coord in this)
            output ^= coord.GetHashCode();
        return output;
    }
    public IEnumerator<Coordinate> GetEnumerator()
        { return _coords.GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

}
