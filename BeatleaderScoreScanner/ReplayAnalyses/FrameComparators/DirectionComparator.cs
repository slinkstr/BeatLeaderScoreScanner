using ReplayDecoder;

namespace BeatLeaderScoreScanner.ReplayAnalyses.FrameComparators;

class DirectionComparator : FrameComparator
{
    private const float _threshold = 2f;

    protected override CircularBuffer<Frame> FrameBuffer { get; set; } = new(3);

    protected override Tracker Detected(SaberOffsets? saberOffsets = null)
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
        var tracker = Util.ArrayToTracker(diffdiffs, x => x > _threshold);
        return tracker;
    }
}
