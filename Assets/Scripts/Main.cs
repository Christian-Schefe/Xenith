using UnityEngine;

public class Main : MonoBehaviour
{
    public string SongPath { get; set; }
    public int OpenTrack { get; set; }

    public SerializedSong? OpenSong { get; set; }

    private void Awake()
    {
        if (!Globals<Main>.RegisterOrDestroy(this))
        {
            return;
        }
        DontDestroyOnLoad(this);
    }
}
