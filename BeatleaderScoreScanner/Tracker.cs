namespace BeatLeaderScoreScanner;

[Flags]
public enum Tracker
{
    None        = 0,
    LeftHand    = 1 << 0,
    RightHand   = 1 << 1,
    Head        = 1 << 2
}
