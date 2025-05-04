using System.Collections.Generic;
using UnityEngine;

namespace DSP
{
    public class DPSTest : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var noteEditor = Globals<PianoRoll.NoteEditor>.Instance;
                var dsp = Globals<DSP>.Instance;
                var trackEditor = Globals<TrackEditor>.Instance;
                if (noteEditor.isPlaying)
                {
                    noteEditor.StopPlaying();
                    dsp.ResetDSP();
                    return;
                }
                noteEditor.StartPlaying();

                var startTime = noteEditor.GetPlayStartTime();
                var song = trackEditor.Serialize();
                var node = song.BuildAudioNode(startTime);

                dsp.Initialize(node);
            }
        }
    }
}