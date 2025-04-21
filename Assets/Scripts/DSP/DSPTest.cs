using System.Collections.Generic;
using UnityEngine;

namespace DSP
{
    public class DPSTest : MonoBehaviour
    {
        private FastRandom random = new(1234);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var noteEditor = Globals<PianoRoll.NoteEditor>.Instance;
                var dsp = Globals<DSP>.Instance;
                var main = Globals<Main>.Instance;
                if (noteEditor.isPlaying)
                {
                    noteEditor.StopPlaying();
                    dsp.ResetDSP();
                    return;
                }
                noteEditor.StartPlaying();

                var startTime = noteEditor.GetPlayStartTime();
                var song = main.OpenSong.Value;
                var node = song.BuildAudioNode(startTime);

                dsp.Initialize(node);
            }
        }

        public struct FastRandom
        {
            private uint state;

            public FastRandom(uint seed)
            {
                state = seed != 0 ? seed : 1;
            }

            public float NextFloat()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;

                return (state & 0xFFFFFF) / (float)(1 << 24);
            }
        }
    }
}