using ReplayDecoder;

namespace BeatLeaderScoreScanner.ReplayAnalyses.FrameComparators;

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

internal abstract class FrameComparator
{
    protected abstract CircularBuffer<Frame> FrameBuffer { get; set; }
    protected abstract Tracker Detected(SaberOffsets? saberOffsets = null);

    public Tracker Compare(Frame frame, SaberOffsets? saberOffsets = null)
    {
        FrameBuffer.Add(frame);
        if (!BufferFull()) { return Tracker.None; }

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
