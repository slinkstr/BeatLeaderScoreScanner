using BeatLeaderScoreScanner.ReplayAnalyses.FrameComparators;
using ReplayDecoder;

namespace BeatLeaderScoreScanner.ReplayAnalyses;

public class Jitter
{
    public List<JitterEvent> Events { get; set; }

    private const int DebounceDurationTicks = 5;
    private const float NoteWindow          = 0.125f;

    public Jitter(Replay replay)
    {
        Events = new();
        FrameComparator comparator = new TripleDirectionComparator();
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
                Events.Add(new JitterEvent(frame, tracker));
                debounceSkipTo = i + DebounceDurationTicks;
            }
        }
    }

    public List<JitterEvent> CausedScoreLoss(NoteEvent[] underswingNotes)
    {
        List<JitterEvent> underswingJitters = [];

        int jitterIndex = 0;
        int noteIndex = 0;
        while (jitterIndex < Events.Count && noteIndex < underswingNotes.Length)
        {
            var note = underswingNotes[noteIndex];
            var jitter = Events[jitterIndex];

            if (note.eventTime + NoteWindow < jitter.Frame.time)
            {
                noteIndex++;
                continue;
            }
            if (note.eventTime - NoteWindow > jitter.Frame.time)
            {
                jitterIndex++;
                continue;
            }

            if (note.eventTime < jitter.Frame.time)
            {
                if (note.noteCutInfo.afterCutRating < 1)
                {
                    underswingJitters.Add(jitter);
                }
            }
            else
            {
                if (note.noteCutInfo.beforeCutRating < 1)
                {
                    underswingJitters.Add(jitter);
                }
            }
            jitterIndex++;
        }

        return underswingJitters;
    }
}

public class JitterEvent
{
    public Frame   Frame   { get; private set; }
    public Tracker Tracker { get; private set; }

    public JitterEvent(Frame frame, Tracker tracker)
    {
        Frame = frame;
        Tracker = tracker;
    }
}
