using Persistence;
using System.Collections.Generic;
using UnityEngine;
using Yeast;

namespace DTO
{
    public class SongController : MonoBehaviour
    {
        private readonly Dictionary<SongID, Song> songs = new();
        private int unsavedSongIndex = 0;

        public Song GetSong(SongID id)
        {
            return songs[id];
        }

        public SongID AddSong()
        {
            var id = new UnsavedSongID(unsavedSongIndex);
            unsavedSongIndex++;
            var song = Song.Default();
            songs.Add(id, song);
            return id;
        }

        public bool TryLoadSong(string path, out SongID id)
        {
            id = new SongID(path);
            if (songs.ContainsKey(id))
            {
                return false;
            }

            if (!FilePersistence.TryLoadFullPath(path, out var json))
            {
                Debug.LogError($"Failed to read file from {path}");
            }

            if (json.TryFromJson(out Song song))
            {
                songs.Add(id, song);
                return true;
            }
            Debug.LogError($"Failed to parse song from {path}");
            return false;
        }

        public bool SaveSong(SongID id, string path, out SongID newId)
        {
            var song = songs[id];
            newId = id;
            if (id.path != path)
            {
                songs.Remove(id);
                newId = new SongID(path);
                songs.Add(newId, song);
            }
            FilePersistence.SaveFullPath(path, song.ToJson());
            return newId != id;
        }

        public bool HasUnsavedChanges(SongID id)
        {
            return songs.ContainsKey(id) && !songs[id].IsEmpty();
        }
    }
}
