using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yeast;

namespace Persistence
{
    public static class JsonPersistence
    {
        public static void Save<T>(string path, T data)
        {
            var json = data.ToJson();
            FilePersistence.Save(path, json);
        }

        public static bool TryLoad<T>(string path, out T data)
        {
            if (FilePersistence.TryLoad(path, out var json))
            {
                return json.TryFromJson(out data);
            }
            data = default;
            return false;
        }

        public static T LoadDefault<T>(string path, T defaultValue)
        {
            if (TryLoad(path, out T data))
            {
                return data;
            }
            return defaultValue;
        }

        public static bool Delete(string path)
        {
            return FilePersistence.Delete(path);
        }
    }
}
