using ReplayDecoder;

namespace BeatLeaderScoreScanner.ReplayAnalyses;

public class Underswing
{
    public int    Score            { get; set; }
    public int    ScoreMax         { get; set; }
    public int    ScoreLost        { get; set; }
    public int    ScoreFullSwing   => Score + ScoreLost;
    public double Percent          => Score / (double)ScoreMax;
    public double PercentFullSwing => ScoreFullSwing / (double)ScoreMax;
    public double PercentLost      => PercentFullSwing - Percent;
    public List<UnderswingEvent> Events { get; set; }

    public Underswing(Replay replay)
    {
        Events = DetectUnderswing(replay);

        var processed = ReplayStatistic.ProcessReplay(replay);
        if (processed.Item2 != null) { throw new Exception(processed.Item2); }
        ScoreStatistic stats = processed.Item1!;

        Score          = replay.info.score;
        ScoreMax       = stats.winTracker.maxScore;
        ScoreLost      = Events.Aggregate(0, (acm, cur) => acm += cur.Lost);

        // TODO: extra verification for if detected underswing matches replay info
    }

    private static List<UnderswingEvent> DetectUnderswing(Replay replay)
    {
        List<UnderswingEvent> events = [];
        MultiplierCounter multiplierCounter = new();

        var comboEvents = replay.notes.Concat<object>(replay.walls).ToList().OrderBy(x =>
        {
            if (x is NoteEvent n) { return n.eventTime; }
            if (x is WallEvent w) { return w.time;      }
            throw new Exception("Encountered unknown type in comboEvents");
        });

        foreach (var comboEvent in comboEvents)
        {
            if (comboEvent is WallEvent)
            {
                multiplierCounter.Decrease();
                continue;
            }

            var note = comboEvent as NoteEvent ?? throw new Exception("Unable to cast comboEvent to NoteEvent");
            if (note.eventType != NoteEventType.good)
            {
                multiplierCounter.Decrease();
                continue;
            }

            multiplierCounter.Increase();

            int targetPre;
            int targetPost;

            switch(note.noteParams.scoringType)
            {
                case ScoringType.BurstSliderHead:
                    targetPre  = 70;
                    targetPost = 0;
                    break;
                case ScoringType.BurstSliderElement:
                    targetPre  = 0;
                    targetPost = 0;
                    break;
                default:
                    targetPre  = 70;
                    targetPost = 30;
                    break;
            }

            int underPre  = targetPre  - note.score.pre_score;
            int underPost = targetPost - note.score.post_score;
            if (underPre > 0 || underPost > 0)
            {
                events.Add(new UnderswingEvent(note, underPre, underPost, multiplierCounter.Multiplier));
            }
        }

        return events;
    }
}

public class UnderswingEvent
{
    public NoteEvent Note  { get; private set; }
    public int Multiplier  { get; private set; }
    public int RawLostPre  { get; private set; }
    public int RawLostPost { get; private set; }
    public int Lost => (RawLostPre + RawLostPost) * Multiplier;

    public UnderswingEvent(NoteEvent note, int rawLostPre, int rawLostPost, int multiplier)
    {
        Note = note;
        Multiplier = multiplier;
        RawLostPre = rawLostPre;
        RawLostPost = rawLostPost;
    }
}
