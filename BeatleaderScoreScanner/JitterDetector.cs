using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ReplayDecoder;

namespace BeatleaderScoreScanner
{
    internal static class JitterDetector
    {
        private const float MovementDirectionThreshold = 3f;
        private const float MovementDistanceThreshold  = 0.2f;
        private const float RotationDirectionThreshold = 45f;

        public static void PrintJitter(Replay replay)
        {
            string identifier = $"{replay.info.hmd}, {replay.info.trackingSystem}, {replay.info.gameVersion}, {replay.info.version}";

            List<Frame> dirJitter  = MovementDirectionJitter(replay, true);
            List<Frame> distJitter = MovementDistanceJitter (replay, true);
        }

        public static List<Frame> MovementDirectionJitter(Replay replay, bool output = false)
        {
            List<Frame> frames = new();
            
            int      frameNum       = 0;
            Frame?   lastFrame       = null;
            float[]? lastDirections  = null;
            foreach (Frame frame in replay.frames.Skip(2))
            {
                if (lastFrame != null)
                {
                    float[] directions =
                    {
                        Vector3.Angle(lastFrame.leftHand.position , frame.leftHand.position),
                        Vector3.Angle(lastFrame.rightHand.position, frame.rightHand.position),
                        Vector3.Angle(lastFrame.head.position     , frame.head.position),
                    };

                    if (lastDirections != null)
                    {
                        float[] difference =
                        {
                            directions[0] - lastDirections[0],
                            directions[1] - lastDirections[1],
                            directions[2] - lastDirections[2],
                        };

                        if(difference.Any((diff) => Math.Abs(diff) > MovementDirectionThreshold))
                        {
                            if(output)
                            {
                                Console.WriteLine($"Large move dir change: {frameNum} ({FormatSeconds(frame.time)}), {FloatArrayToString(difference)}");
                            }
                            frames.Add(frame);
                        }
                    }

                    lastDirections = directions;
                }

                lastFrame = frame;
                frameNum++;
            }

            return frames;
        }

        public static List<Frame> MovementDistanceJitter(Replay replay, bool output = false)
        {
            List<Frame> frames = new();

            int      frameNum       = 0;
            Frame?   lastFrame      = null;
            float[]? lastDistances  = null;
            foreach (Frame frame in replay.frames.Skip(2))
            {
                if (lastFrame != null)
                {
                    float[] distances =
                    {
                        Vector3.Magnitude(lastFrame.leftHand.position  - frame.leftHand.position),
                        Vector3.Magnitude(lastFrame.rightHand.position - frame.rightHand.position),
                        Vector3.Magnitude(lastFrame.head.position      - frame.head.position),
                    };

                    if (lastDistances != null)
                    {
                        float[] difference =
                        {
                            distances[0] - lastDistances[0],
                            distances[1] - lastDistances[1],
                            distances[2] - lastDistances[2],
                        };

                        if(difference.Any((diff) => Math.Abs(diff) > MovementDistanceThreshold))
                        {
                            if (output)
                            {
                                Console.WriteLine($"Large move dist change: {frameNum} ({FormatSeconds(frame.time)}), {FloatArrayToString(difference)}");
                            }
                            frames.Add(frame);
                        }
                    }

                    lastDistances = distances;
                }

                lastFrame = frame;
                frameNum++;
            }

            return frames;
        }

        public static List<Frame> RotationDirectionJitter(Replay replay)
        {
            List<Frame> frames = new();



            return frames;
        }

        private static string FloatArrayToString(float[] values)
        {
            string ret = "[ ";

            foreach (float f in values)
            {
                ret += $"{f,10:F5} ";
            }

            ret += "]";
            return ret;
        }

        private static string FormatSeconds(float seconds)
        {
            int   min = (int)(seconds / 60);
            float sec = seconds % 60;
            return $"{min}:{sec:00.000}";
        }
    }
}
