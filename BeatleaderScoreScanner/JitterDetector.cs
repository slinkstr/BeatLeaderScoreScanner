﻿using System.Collections;
using ReplayDecoder;

/*
# Beat saber axes

| Axis | Direction        | Reference points                                      |
| ---- | ---------------- | ----------------------------------------------------- |
| X    | Left/right       | 0: center, 1: almost right edge, -1: almost left edge |
| Y    | Up/down          | 0: ground, 1: waist, 1.5: head, 2: above head         |
| Z    | Forward/backward | 0: center, 1: back edge, -1: front edge               |

# Debugging replays:
```js
let primaryHand = document.getElementById("rightHand");
let primaryPosition = primaryHand.getAttribute('position');
// primaryPosition.x += 0.5 ...
```

# Get replay cur time:
```js
document.getElementsByTagName("a-scene")[0].components.song.getCurrentTime();
```
or just skip to Frame.time with &time=<time * 1000>
*/

namespace BeatleaderScoreScanner
{
    internal static class JitterDetector
    {
        private const int DebounceDurationTicks = 30;

        public static List<Frame> JitterTicks(Replay replay)
        {
            List<FrameComparator> comparators = new()
            {
                new DirectionComparator(),
                //new DirectionComparatorPlus(),
                //new DistanceComparator(),
            };

            return JitterTicks(replay, comparators);
        }

        public static List<Frame> JitterTicks(Replay replay, List<FrameComparator> comparators)
        {
            List<Frame> ret = new();
            int skipBefore = 0;

            // skip first frames because position is erratic
            for (int i = 10; i < replay.frames.Count; i++)
            {
                Frame frame = replay.frames[i];

                foreach (var comparator in comparators)
                {
                    if (i < skipBefore)
                    {
                        comparator.Reset();
                        continue;
                    }

                    if (comparator.Compare(frame))
                    {
                        ret.Add(frame);
                        skipBefore = i + DebounceDurationTicks;
                        break;
                    }
                }
            }

            return ret;
        }
    }
}

class CircularBuffer<T> : IEnumerable<T>
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
        for(int i = 0; i < _values.Capacity; i++)
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
            valueIndex = valueIndex + _values.Capacity;
        }

        return _values.ElementAt(valueIndex);
    }

    private int NextIndex()
    {
        _index++;
        if(_index >= _values.Capacity)
        {
            _index = 0;
        }
        return _index;
    }
}

abstract class FrameComparator
{
    protected abstract CircularBuffer<Frame> FrameBuffer { get; set; }
    protected abstract bool Detected();

    public bool Compare(Frame frame)
    {
        FrameBuffer.Add(frame);
        if (!BufferFull()) { return false; }

        return Detected();
    }

    public string DetectionAlert()
    {
        return $"[{this.GetType().Name}] Detected jitter at {FrameBuffer[0].time}";
    }

    public void Reset()
    {
        FrameBuffer.Clear();
    }

    protected bool BufferFull()
    {
        return FrameBuffer.Count() == FrameBuffer.Capacity();
    }
}

class DirectionComparatorPlus : FrameComparator
{
    private const float _threshold = 2f;

    protected override CircularBuffer<Frame> FrameBuffer { get; set; } = new(4);

    protected override bool Detected()
    {
        Vector3[][] vecs = new Vector3[FrameBuffer.Capacity() - 1][];
        for (int i = 0; i < FrameBuffer.Capacity() - 1; i++)
        {
            var frame = FrameBuffer[i];
            var lastFrame = FrameBuffer[i + 1];

            vecs[i] =
            [
                frame.leftHand.position  - lastFrame.leftHand.position,
                frame.rightHand.position - lastFrame.rightHand.position,
                frame.head.position      - lastFrame.head.position,
            ];
        }

        Console.WriteLine($"Left: {vecs[0][0].x} {vecs[0][0].y} {vecs[0][0].z}\n" +
                          $"Right: {vecs[0][1].x} {vecs[0][1].y} {vecs[0][1].z}\n" +
                          $"Head: {vecs[0][2].x} {vecs[0][2].y} {vecs[0][2].z}");

        throw new NotImplementedException();
    }
}

class DirectionComparator : FrameComparator
{
    private const float _threshold = 2f;

    protected override CircularBuffer<Frame> FrameBuffer { get; set; } = new(3);

    protected override bool Detected()
    {
        float[][] diffs = new float[FrameBuffer.Capacity() - 1][];
        for (int i = 0; i < FrameBuffer.Capacity() - 1; i++)
        {
            var frame     = FrameBuffer[i];
            var lastFrame = FrameBuffer[i + 1];

            diffs[i] = [
                Vector3.Angle(lastFrame.leftHand.position , frame.leftHand.position),
                Vector3.Angle(lastFrame.rightHand.position, frame.rightHand.position),
                Vector3.Angle(lastFrame.head.position     , frame.head.position),
                ];
        }

        float[] diffdiffs = diffs[1].Zip(diffs[0], (x, y) => x - y).ToArray();

        if(diffdiffs.Any(x => x > _threshold))
        {
            return true;
        }

        return false;
    }
}

class DistanceComparator : FrameComparator
{
    private const float _threshold = 0.1f;

    protected override CircularBuffer<Frame> FrameBuffer { get; set; } = new(3);

    protected override bool Detected()
    {
        float[][] diffs = new float[FrameBuffer.Capacity() - 1][];
        for (int i = 0; i < FrameBuffer.Capacity() - 1; i++)
        {
            var frame = FrameBuffer[i];
            var lastFrame = FrameBuffer[i + 1];

            diffs[i] = [
                Vector3.Magnitude(lastFrame.leftHand.position  - frame.leftHand.position),
                Vector3.Magnitude(lastFrame.rightHand.position - frame.rightHand.position),
                Vector3.Magnitude(lastFrame.head.position      - frame.head.position),
                ];
        }

        float[] diffdiffs = diffs[1].Zip(diffs[0], (x, y) => x - y).ToArray();

        if (diffdiffs.Any(x => x > _threshold))
        {
            return true;
        }

        return false;
    }
}
