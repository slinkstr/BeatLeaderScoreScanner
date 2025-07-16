using ReplayDecoder;

namespace BeatLeaderScoreScanner.ReplayAnalyses.FrameComparators;

internal class TripleDirectionComparator : FrameComparator
{
    private const float _angleThreshold     = 100f;
    private const float _magnitudeThreshold = 0.001f;

    protected override CircularBuffer<Frame> FrameBuffer { get; set; } = new(4);

    protected override Tracker Detected(SaberOffsets? saberOffsets = null)
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

        var tracker = Tracker.None;
        for (int i = 0; i < 3; i++)
        {
            if (angleDiff[1][i] < _angleThreshold)             { continue; }
            if (angleDiff[0][i] < _angleThreshold)             { continue; }
            // First (oldest) tick is usually the most violent (highest magnitude)
            if (vecs[2][i].sqrMagnitude < _magnitudeThreshold) { continue; }

            tracker |= Util.IndexToTracker(i);
        }

        return tracker;
    }
}
