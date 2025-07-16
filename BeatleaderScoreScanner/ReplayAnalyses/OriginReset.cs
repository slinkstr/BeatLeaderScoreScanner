using BeatLeaderScoreScanner.ReplayAnalyses.FrameComparators;
using ReplayDecoder;

namespace BeatLeaderScoreScanner.ReplayAnalyses;

public class OriginReset
{
    public List<OriginResetEvent> Events { get; set; }

    private const int DebounceDurationTicks = 2;

    public OriginReset(Replay replay)
    {
        Events = new();
        FrameComparator comparator = new OriginResetComparator();
        int debounceSkipTo = 0;

        // skip first frames because position is erratic
        for (int i = 10; i < replay.frames.Count; i++)
        {
            if (i < debounceSkipTo)
            {
                comparator.Reset();
                continue;
            }

            Frame frame = replay.frames[i];
            Tracker tracker = comparator.Compare(frame, replay.saberOffsets);
            if (tracker != Tracker.None)
            {
                Events.Add(new OriginResetEvent(frame, tracker));
                debounceSkipTo = i + DebounceDurationTicks;
            }
        }
    }
}

public class OriginResetEvent
{
    public Frame   Frame   { get; private set; }
    public Tracker Tracker { get; private set; }

    public OriginResetEvent(Frame frame, Tracker tracker)
    {
        Frame = frame;
        Tracker = tracker;
    }
}
