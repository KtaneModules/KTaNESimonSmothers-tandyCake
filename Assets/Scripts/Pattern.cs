using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Pattern : IEnumerable<Coordinate>
{
    private List<Coordinate> _coords = new List<Coordinate>();
    private List<CoordinateDifference> _differences = new List<CoordinateDifference>();
    public static Pattern center { get { return new Pattern { _coords = new List<Coordinate> { new Coordinate(0, 0, 0) } }; } }

    public Pattern() { }
    public Pattern(IEnumerable<Coordinate> coords, List<CoordinateDifference> diffs = null)
    {
        _coords = coords.ToList();
        _differences = diffs ?? new List<CoordinateDifference>();
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
    public bool AddInDir(Dir direction, int newValue, bool diff)
    {
        Coordinate prev = _coords.Last();
        Coordinate next = prev.ApplyMovement(direction);
        next.tileValue = newValue;
        if (_coords.Contains(next))
            return false;
        else
        {
            _coords.Add(next);
            _differences.Add(new CoordinateDifference(prev, next, diff));
            return true;
        }
    }
    public bool AddInDir(Dir direction, bool diff)
    {
        return AddInDir(direction, _coords.Last().tileValue, diff);
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
    public Pattern Normalize()
    {
        if (_coords.Count == 0)
            return this;
        Pattern output = new Pattern();
        List<CoordinateDifference> diffs = new List<CoordinateDifference>();

        int minX = _coords.Min(coord => coord.x);
        int minY = _coords.Min(coord => coord.y);

        foreach (Coordinate coord in this)
        {
            Coordinate newCoord = coord.Copy();
            newCoord.x -= minX;
            newCoord.y -= minY;
            output.Add(newCoord);
        }
        foreach (CoordinateDifference d in _differences)
            diffs.Add(new CoordinateDifference(
                new Coordinate(d.a.x - minX, d.a.y - minY),
                new Coordinate(d.b.x - minX, d.b.y - minY),
                d.isDiff));
        IEnumerable<Coordinate> coords = output.OrderBy(coord => coord.x).ThenBy(coord => coord.y);
        return new Pattern(coords, diffs);
    }
    private char _GetRGBChar(int value)
    {
        RGBColor c = (RGBColor) value;
        return c == RGBColor.Black ? 'K' : c.ToString()[0];
    }

    public IEnumerable<string> GetLoggingPattern(bool usingColors)
    {
        if (_coords.Count == 0)
            yield break;
        Pattern p = Normalize();
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
                                     _GetRGBChar(line.First(crd => crd.x == x).tileValue) : 'x';
                else loggingLine += '.';
            }
            yield return loggingLine;
        }
    }
    public string GetLoggingDifferences()
    {
        Pattern norm = this.Normalize();
        return norm._differences.Where(d => d.isDiff).Select(d => string.Format("{0} != {1}", d.a, d.b)).Join(" / ");
    }

    public bool IsPaintable()
    {
        List<Coordinate> coords = this.ToList();
        foreach (Coordinate c in coords)
            if (_IsPaintableFrom(coords.ToList(), c))
                return true;
        return false;
    }
    private bool _IsPaintableFrom(List<Coordinate> coords, Coordinate start)
    {
        coords.Remove(start);
        if (coords.Count == 0)
            return true;
        List<Coordinate> next = new List<Coordinate>();
        for (int i = 0; i < 4; i++)
            if (coords.Contains(start.ApplyMovement((Dir) i)))
                next.Add(start.ApplyMovement((Dir) i));
        foreach (Coordinate coordinate in next)
            if (_IsPaintableFrom(coords.ToList(), coordinate))
                return true;
        return false;
    }

    public bool IsEquivalentPattern(Pattern other)
    {
        Pattern normThis = Normalize(), normOther = other.Normalize();
        if (this.Count() != other.Count())
            return false;
        foreach (Coordinate coordinate in normOther)
            if (!normOther.Contains(coordinate))
                return false;
        foreach (CoordinateDifference difference in normOther._differences)
        {
            if (!normThis.Contains(difference.a) || !normThis.Contains(difference.b))
                return false;
            int valA = normThis.First(x => x.Equals(difference.a)).tileValue;
            int valB = normThis.First(x => x.Equals(difference.b)).tileValue;
            if (difference.isDiff && valA == valB)
                return false;
            if (!difference.isDiff && valA != valB)
                return false;
        }
        return true;
    }

    private IEnumerable<Coordinate> _GetAdjacentsInPattern(Coordinate c)
    {
        return c.GetAdjacents().Where(x => this.Contains(x));
    }
    public override string ToString()
    {
        return this.Join(", ");
    }
    public IEnumerator<Coordinate> GetEnumerator()
    { return _coords.GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator()
    { return GetEnumerator(); }

}
