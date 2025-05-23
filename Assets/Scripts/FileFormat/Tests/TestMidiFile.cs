using FileFormat;
using NUnit.Framework;
using UnityEngine;

public class TestMidiFile
{
    [Test]
    public void TestParseMidiFiles()
    {
        var path = "Assets/Resources/Midi";
        var midiPaths = System.IO.Directory.GetFiles(path, "*.mid", System.IO.SearchOption.AllDirectories);
        Debug.Log($"Found {midiPaths.Length} midi files in Resources/Midi");
        foreach (var midi in midiPaths)
        {
            Debug.Log($"Parsing {midi}");
            var bytes = System.IO.File.ReadAllBytes(midi);
            var midiFile = MidiFile.Decode(bytes);

            var simpleMidi = SimpleMidi.FromMidiFile(midiFile);
            foreach (var track in simpleMidi.tracks)
            {
                Debug.Log($"Track");
                foreach (var note in track.notes)
                {
                    Debug.Log($"Note: {note.pitch}, {note.velocity}, {note.start}, {note.duration}");
                }
            }
        }
    }
    [Test]
    public void TestParseMeditation()
    {
        var path = "Assets/Resources/Midi/meditation.mid";
        var bytes = System.IO.File.ReadAllBytes(path);
        var midiFile = MidiFile.Decode(bytes);

        Debug.Log($"{midiFile}");
        /*

        var simpleMidi = SimpleMidi.FromMidiFile(midiFile);
        foreach (var track in simpleMidi.tracks)
        {
            Debug.Log($"Track");
            foreach (var note in track.notes)
            {
                Debug.Log($"Note: {note.pitch}, {note.velocity}, {note.start}, {note.duration}");
            }
        }*/
    }
}
