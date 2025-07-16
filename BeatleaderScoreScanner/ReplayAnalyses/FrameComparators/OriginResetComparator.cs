using ReplayDecoder;

namespace BeatLeaderScoreScanner.ReplayAnalyses.FrameComparators;

internal class OriginResetComparator : FrameComparator
{
    private const float _threshold = 0.01f;

    protected override CircularBuffer<Frame> FrameBuffer { get; set; } = new(1);

    protected override Tracker Detected(SaberOffsets? saberOffsets = null)
    {
        var frame = FrameBuffer[0];
        Vector3[] positions =
        [
            frame.leftHand.position,
            frame.rightHand.position,
        ];

        positions[0] = Util.ApplyOffsets(positions[0], saberOffsets?.leftSaberLocalPosition);
        positions[1] = Util.ApplyOffsets(positions[1], saberOffsets?.rightSaberLocalPosition);

        var tracker = Util.ArrayToTracker(positions, vec => Math.Abs(vec.x) < _threshold && Math.Abs(vec.y) < _threshold && Math.Abs(vec.z) < _threshold);
        return tracker;
    }
}
