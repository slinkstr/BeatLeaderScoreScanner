using System.Collections;

namespace BeatLeaderScoreScanner;

internal class CircularBuffer<T> : IEnumerable<T>
{
    private List<T> _values;
    private int _index;

    public CircularBuffer(int size)
    {
        _values = new List<T>(size);
        _index = 0;
    }

    public void Add(T item)
    {
        if (Count() < Capacity())
        {
            _values.Add(item);
        }
        else
        {
            _values[_index] = item;
        }
        _index = NextIndex();
    }

    public int Capacity()
    {
        return _values.Capacity;
    }

    public void Clear()
    {
        _values.Clear();
        _index = 0;
    }

    public int Count()
    {
        return _values.Count;
    }

    // ********************************************************************************
    // Enumerable/index
    // ********************************************************************************

    public T this[int targetIndex]
    {
        get => ValueAt(targetIndex);
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _values.Capacity; i++)
        {
            yield return ValueAt(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // ********************************************************************************
    // Util
    // ********************************************************************************

    private T ValueAt(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex > _values.Capacity)
        {
            throw new IndexOutOfRangeException();
        }

        var lastIndex = _index - 1;
        var valueIndex = lastIndex - targetIndex;
        while (valueIndex < 0)
        {
            valueIndex += _values.Capacity;
        }

        return _values.ElementAt(valueIndex);
    }

    private int NextIndex()
    {
        _index++;
        if (_index >= _values.Capacity)
        {
            _index = 0;
        }
        return _index;
    }
}
