using Persistence;
using ReactiveData.App;
using System.Collections.Generic;
using UnityEngine;
using Yeast;

namespace DTO
{
    public class SongController : MonoBehaviour
    {
        private readonly Dictionary<SongID, ReactiveSong> songs = new();
        private int unsavedSongIndex = 0;

        public ReactiveSong GetSong(SongID id)
        {
            return songs[id];
        }

        public SongID AddSong()
        {
            var id = new UnsavedSongID(unsavedSongIndex);
            unsavedSongIndex++;
            var song = ReactiveSong.Default;
            songs.Add(id, song);
            return id;
        }

        public void UnloadSong(SongID id)
        {
            if (songs.ContainsKey(id))
            {
                songs.Remove(id);
            }
            else
            {
                Debug.LogError($"Failed to unload song {id.GetName()}");
            }
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
                songs.Add(id, DTOConverter.Deserialize(song));
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
            FilePersistence.SaveFullPath(path, DTOConverter.Serialize(song).ToJson());
            return newId != id;
        }

        public bool HasUnsavedChanges(SongID id)
        {
            return songs.ContainsKey(id) && !songs[id].IsEmpty();
        }
    }
}
