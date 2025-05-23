using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Persistence
{
    public static class FilePersistence
    {
        public static string BuildPersistencePath(string relativePath)
        {
            return Path.Combine(Application.persistentDataPath, relativePath);
        }

        public static void SaveFullPath(string fullPath, string data)
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(fullPath, data);
        }

        public static void Save(string path, string data)
        {
            var fullPath = BuildPersistencePath(path);
            SaveFullPath(fullPath, data);
        }

        public static bool TryLoadFullPath(string fullPath, out string data)
        {
            if (File.Exists(fullPath))
            {
                data = File.ReadAllText(fullPath);
                return true;
            }
            data = null;
            return false;
        }

        public static bool TryLoad(string path, out string data)
        {
            var fullPath = BuildPersistencePath(path);
            return TryLoadFullPath(fullPath, out data);
        }

        public static bool Delete(string path)
        {
            var fullPath = BuildPersistencePath(path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
            return false;
        }
    }
}
