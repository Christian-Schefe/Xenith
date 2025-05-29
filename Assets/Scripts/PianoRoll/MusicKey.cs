using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PianoRoll
{
    public struct MusicKey
    {
        public int edo;
        public float basePitch;
        public List<int> pitches;

        public MusicKey(int edo, float basePitch, List<int> pitches)
        {
            this.edo = edo;
            this.basePitch = basePitch;
            this.pitches = pitches;
            this.pitches.Sort();
        }

        public static float HalfSteps(int edo, float baseFreq, float halfSteps)
        {
            return baseFreq * Mathf.Pow(2, halfSteps / edo);
        }

        public readonly float StepsToFreq(int steps)
        {
            return HalfSteps(edo, basePitch, steps);
        }

        public static MusicKey FromMidi(int sharps, bool isMajor)
        {
            var steps = isMajor
                ? new List<int> { 0, 2, 4, 5, 7, 9, 11 }
                : new List<int> { 0, 2, 3, 5, 7, 8, 10 };
            int stepOffset = ((sharps * 7 % 12) + 12) % 12;
            steps = steps.Select(s => (s + stepOffset) % 12).ToList();
            return new MusicKey(12, C0, steps);
        }

        public static float C0 = HalfSteps(12, 27.5f, -9);

        public static MusicKey CMajor = new(12, C0, new() { 0, 2, 4, 5, 7, 9, 11 });
        public static MusicKey FMinor = new(12, HalfSteps(12, C0, 5), new() { 0, 2, 3, 5, 7, 8, 10 });

        public static MusicKey DoubleLydian = new(31, C0, new() { 0, 4, 5, 9, 10, 14, 15, 17, 18, 22, 23, 27, 28, 30 });
    }
}
