using ReplayDecoder;

namespace BeatLeaderScoreScanner;

public static class Util
{
    public static Tracker ArrayToTracker<T>(T[] array, Func<T, bool> selector)
    {
        var tracker = Tracker.None;
        for (int i = 0; i < array.Length; i++)
        {
            if (selector(array[i]))
            {
                tracker |= IndexToTracker(i);
            }
        }
        return tracker;
    }

    public static Tracker IndexToTracker(int index)
    {
        return index switch
        {
            0 => Tracker.LeftHand,
            1 => Tracker.RightHand,
            2 => Tracker.Head,
            _ => throw new Exception("Unrecognized tracker index")
        };
    }

    public static Vector3 ApplyOffsets(Vector3 position, Vector3? offset)
    {
        if (offset == null) { return position; }
        return new Vector3(position.x - offset.Value.x, position.y - offset.Value.y, position.z - offset.Value.z);
    }
}
