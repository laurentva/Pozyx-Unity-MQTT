using System;

public struct PositionData
{
    public String ID;
    public int x, y, z;

    public PositionData(String _ID, int _x, int _y, int _z)
    {
        ID = _ID;
        x = _x;
        y = _y;
        z = _z;
    }
}