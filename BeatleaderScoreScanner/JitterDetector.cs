using ReplayDecoder;

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
                //new DistanceComparator(),
                //new RotationComparator(),
            };

            return JitterTicks(replay, comparators);
        }

        public static List<Frame> JitterTicks(Replay replay, List<FrameComparator> comparators)
        {
            List<Frame> frames = new();

            Frame? lastFrame = null;
            int skipBefore = 0;
            // skip first few frames because position is erratic
            for (int i = 5; i < replay.frames.Count; i++)
            {
                Frame frame = replay.frames[i];

                if (lastFrame != null && i >= skipBefore)
                {
                    foreach (var comparator in comparators)
                    {
                        if (comparator.Compare(frame, lastFrame))
                        {
                            frames.Add(frame);
                            skipBefore = i + DebounceDurationTicks;
                            break;
                        }
                    }
                }

                lastFrame = frame;
            }

            return frames;
        }

        private static string FormatSeconds(float seconds)
        {
            int   min = (int)(seconds / 60);
            float sec = seconds % 60;
            return $"{min}:{sec:00.000}";
        }
    }
}

abstract class FrameComparator
{
    protected abstract float    threshold      { get; set; }
    protected abstract float[]? previousValues { get; set; }
    public abstract bool Compare(Frame lastFrame, Frame frame);

    protected bool MeetsThreshold(float[] values)
    {
        if (previousValues == null)
        {
            return false;
        }

        float[] difference = new float[3];
        for (int i = 0; i < values.Length; i++)
        {
            difference[i] = values[i] - previousValues[i];
        }

        if (difference.Any(diff => Math.Abs(diff) > threshold))
        {
            return true;
        }

        return false;
    }

    protected static string FloatArrayToString(float[] values)
    {
        string ret = "[ ";

        foreach (float f in values)
        {
            ret += $"{f,10:F5} ";
        }

        ret += "]";
        return ret;
    }
}

class DirectionComparator : FrameComparator
{
    protected override float    threshold      { get; set; } = 3f;
    protected override float[]? previousValues { get; set; } = null;

    public override bool Compare(Frame lastFrame, Frame frame)
    {
        float[] values =
        [
            Vector3.Angle(lastFrame.leftHand.position , frame.leftHand.position),
            Vector3.Angle(lastFrame.rightHand.position, frame.rightHand.position),
            Vector3.Angle(lastFrame.head.position     , frame.head.position),
        ];

        bool ret = MeetsThreshold(values);

        //if (ret)
        //{
        //    Console.WriteLine($"Detected at {frame.time,4:F3}: " + FloatArrayToString(values) + " " + FloatArrayToString(previousValues!));
        //}

        previousValues = values;
        return ret;
    }
}

class DistanceComparator : FrameComparator
{
    protected override float    threshold      { get; set; } = 0.2f;
    protected override float[]? previousValues { get; set; } = null;

    public override bool Compare(Frame lastFrame, Frame frame)
    {
        float[] values =
        [
            Vector3.Magnitude(lastFrame.leftHand.position  - frame.leftHand.position),
            Vector3.Magnitude(lastFrame.rightHand.position - frame.rightHand.position),
            Vector3.Magnitude(lastFrame.head.position      - frame.head.position),
        ];

        bool ret = MeetsThreshold(values);
        previousValues = values;
        return ret;
    }
}

class RotationComparator : FrameComparator
{
    protected override float    threshold      { get; set; } = 45f;
    protected override float[]? previousValues { get; set; } = null;

    public override bool Compare(Frame lastFrame, Frame frame)
    {
        throw new NotImplementedException();
        float[] values =
        [
            // todo
        ];

        bool ret = MeetsThreshold(values);
        previousValues = values;
        return ret;
    }
}
