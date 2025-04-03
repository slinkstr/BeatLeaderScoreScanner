using System.Collections;
using ReplayDecoder;

/*
# Beat saber axes

| Axis | Direction        | Reference points                                      |
| ---- | ---------------- | ----------------------------------------------------- |
| X    | Left/right       | 0: center, 1: almost right edge, -1: almost left edge |
| Y    | Up/down          | 0: ground, 1: waist, 1.5: head, 2: above head         |
| Z    | Forward/backward | 0: center, 1: front edge, -1: back edge               |

# Debugging replays:
```js
let primaryHand = document.getElementById("rightHand");
let primaryPosition = primaryHand.getAttribute("position");
// primaryPosition.x += 0.5 ...
```
Note that for some reason when you set position Z the value is inverted

# Get replay cur time:
```js
document.getElementsByTagName("a-scene")[0].components.song.getCurrentTime();
```
or just skip to Frame.time with &time=<time * 1000>
*/

namespace BeatLeaderScoreScanner
{
    internal static class JitterDetector
    {
        private const int   DebounceDurationTicks = 5;
        private const float NoteWindow = 0.1f;

        public static List<Frame> Jitters(Replay replay, bool requireScoreLoss)
        {
            return MatchingFrames(replay, [ new TwoFrameDirectionComparator() ], [ new OriginResetComparator() ], requireScoreLoss);
        }

        public static List<Frame> OriginResets(Replay replay)
        {
            return MatchingFrames(replay, [ new OriginResetComparator() ]);
        }

        public static List<Frame> MatchingFrames(Replay replay, List<FrameComparator> comparators, List<FrameComparator>? ignoreComparators = null, bool requireScoreLoss = false)
        {
            List<Frame> ret = [];
            int debounceSkipTo = 0;

            List<NoteEvent> underswingNotes = replay.notes.Where(x =>
            {
                if (x.eventType != NoteEventType.good)
                {
                    return false;
                }

                if (x.noteCutInfo.afterCutRating < 1 || x.noteCutInfo.beforeCutRating < 1)
                {
                    return true;
                }

                return false;
            }).ToList();

            int curNoteIndex = 0;

            // skip first frames because position is erratic
            for (int i = 10; i < replay.frames.Count; i++)
            {
                Frame frame = replay.frames[i];

                if (i < debounceSkipTo)
                {
                    comparators.ForEach(c => c.Reset());
                    continue;
                }

                if (requireScoreLoss)
                {
                    if (curNoteIndex >= underswingNotes.Count)
                    {
                        return ret;
                    }

                    NoteEvent note = underswingNotes[curNoteIndex];

                    // skip jitters that are too far away from the note
                    if (note.eventTime + NoteWindow < frame.time)
                    {
                        if (curNoteIndex == underswingNotes.Count - 1)
                        {
                            return ret;
                        }

                        note = underswingNotes[++curNoteIndex];
                    }
                    if (note.eventTime - NoteWindow > frame.time)
                    {
                        comparators.ForEach(c => c.Reset());
                        continue;
                    }

                    // skip jitters before note if preswing is OK, vice versa for postswing
                    if (note.eventTime > frame.time && note.noteCutInfo.beforeCutRating >= 1)
                    {
                        continue;
                    }
                    if (note.eventTime < frame.time && note.noteCutInfo.afterCutRating >= 1)
                    {
                        continue;
                    }
                }

                foreach (var comparator in ignoreComparators ?? [])
                {
                    if (comparator.Compare(frame, replay.saberOffsets))
                    {
                        debounceSkipTo = i + DebounceDurationTicks;

                    }
                }

                foreach (var comparator in comparators)
                {
                    if (comparator.Compare(frame, replay.saberOffsets))
                    {
                        ret.Add(frame);
                        debounceSkipTo = i + DebounceDurationTicks;
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
            valueIndex = valueIndex + _values.Capacity;
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

abstract class FrameComparator
{
    protected abstract CircularBuffer<Frame> FrameBuffer { get; set; }
    protected abstract bool Detected(SaberOffsets? saberOffsets = null);

    public bool Compare(Frame frame, SaberOffsets? saberOffsets = null)
    {
        FrameBuffer.Add(frame);
        if (!BufferFull()) { return false; }

        return Detected(saberOffsets);
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

class TwoFrameDirectionComparator : FrameComparator
{
    private const float _angleThreshold     = 100f;
    private const float _magnitudeThreshold = 0.001f;

    protected override CircularBuffer<Frame> FrameBuffer { get; set; } = new(4);

    protected override bool Detected(SaberOffsets? saberOffsets = null)
    {
        Vector3[][] vecs = new Vector3[FrameBuffer.Capacity() - 1][];
        for (int i = 0; i < vecs.Length; i++)
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

        float[][] angleDiff = new float[vecs.Length - 1][];
        for (int i = 0; i < angleDiff.Length; i++)
        {
            angleDiff[i] =
            [
                Vector3.Angle(vecs[i][0], vecs[i + 1][0]),
                Vector3.Angle(vecs[i][1], vecs[i + 1][1]),
                Vector3.Angle(vecs[i][2], vecs[i + 1][2]),
            ];
        }

        for (int i = 0; i < 3; i++)
        {
            if (angleDiff[1][i] < _angleThreshold)             { continue; }
            if (angleDiff[0][i] < _angleThreshold)             { continue; }
            // First (oldest) tick is usually the most violent (highest magnitude)
            if (vecs[2][i].sqrMagnitude < _magnitudeThreshold) { continue; }

            return true;
        }

        return false;
    }
}

class DirectionComparator : FrameComparator
{
    private const float _threshold = 2f;

    protected override CircularBuffer<Frame> FrameBuffer { get; set; } = new(3);

    protected override bool Detected(SaberOffsets? saberOffsets = null)
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

        if (diffdiffs.Any(x => x > _threshold))
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

    protected override bool Detected(SaberOffsets? saberOffsets = null)
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

class OriginResetComparator : FrameComparator
{
    private const float _threshold = 0.01f;

    protected override CircularBuffer<Frame> FrameBuffer { get; set; } = new(1);

    protected override bool Detected(SaberOffsets? saberOffsets = null)
    {
        var frame = FrameBuffer[0];
        Vector3[] positions =
        [
            frame.leftHand.position,
            frame.rightHand.position,
        ];

        if (saberOffsets != null)
        {
            positions[0].x -= saberOffsets.leftSaberLocalPosition.x;
            positions[0].y -= saberOffsets.leftSaberLocalPosition.y;
            positions[0].z -= saberOffsets.leftSaberLocalPosition.z;
            positions[1].x -= saberOffsets.rightSaberLocalPosition.x;
            positions[1].y -= saberOffsets.rightSaberLocalPosition.y;
            positions[1].z -= saberOffsets.rightSaberLocalPosition.z;
        }

        // Quaternion[] rotations =
        // [
        //     frame.leftHand.rotation,
        //     frame.rightHand.rotation,
        // ];

        if (positions.Any(vec => Math.Abs(vec.x) > _threshold || Math.Abs(vec.y) > _threshold || Math.Abs(vec.z) > _threshold))
        {
            return false;
        }

        return true;
    }
}
