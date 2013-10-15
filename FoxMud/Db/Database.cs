using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using FoxMud.Db;

namespace FoxMud
{
    class Database : IDisposable
    {
        private readonly string storagePath;
        private readonly ReaderWriterLockSlim readWriteLock;
        public Dictionary<Type, Dictionary<string, object>> data;

        public Database(string storagePath)
        {
            this.storagePath = storagePath;
            this.readWriteLock = new ReaderWriterLockSlim();
            this.data = new Dictionary<Type, Dictionary<string, object>>();
        }

        private string GetStorageLocationFor(Type type, string key)
        {
            return Path.Combine(storagePath, type.Name, key + ".db");
        }

        private string GetTypeRoot(Type type)
        {
            return Path.Combine(storagePath, type.Name);
        }

        private Dictionary<string, object> GetOrCreateItemStore(Type itemType)
        {
            if (!data.ContainsKey(itemType))
                data[itemType] = new Dictionary<string, object>();

            return data[itemType];
        }

        public void Put<T>(T item)
            where T : class, Storable
        {
            readWriteLock.EnterReadLock();
            try
            {
                var keyToItemMap = GetOrCreateItemStore(typeof(T));

                keyToItemMap[item.Key] = item;
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        private T LoadFile<T>(string fileName)
        {
            string contents = File.ReadAllText(fileName);

            return JsonConvert.DeserializeObject<T>(contents);
        }

        private T LoadFromDisk<T>(string key)
                    where T : class, Storable
        {
            readWriteLock.EnterWriteLock();
            try
            {
                var keyToItemMap = GetOrCreateItemStore(typeof(T));

                var storagePath = GetStorageLocationFor(typeof(T), key);

                if (!File.Exists(storagePath))
                {
                    return null;
                }

                var loaded = LoadFile<T>(storagePath);

                keyToItemMap[key] = loaded;

                return loaded;
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        public T Get<T>(string key)
            where T : class, Storable
        {
            if (key == null)
                key = "(null)";

            readWriteLock.EnterUpgradeableReadLock();
            try
            {
                var keyToItemMap = GetOrCreateItemStore(typeof(T));

                if (keyToItemMap.ContainsKey(key))                    
                    return (T)keyToItemMap[key];
                
                return LoadFromDisk<T>(key);                
            }
            finally
            {
                readWriteLock.ExitUpgradeableReadLock();
            }
        }

        public void Delete<T>(string key)
            where T : class, Storable
        {
            readWriteLock.EnterWriteLock();

            try
            {
                var keyToItemMap = GetOrCreateItemStore(typeof(T));

                if (keyToItemMap.ContainsKey(key))
                    keyToItemMap.Remove(key);

                var storageLocation = GetStorageLocationFor(typeof(T), key);

                if (File.Exists(storageLocation))
                    File.Delete(storageLocation);
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        public IEnumerable<T> GetAll<T>()
            where T : class, Storable
        {
            string typeRoot = GetTypeRoot(typeof(T));

            if (!Directory.Exists(typeRoot))
                yield break;

            foreach (var file in Directory.EnumerateFiles(typeRoot, "*.db", SearchOption.AllDirectories))
            {
                var item = LoadFile<T>(file);

                yield return Get<T>(item.Key);
            }
        }

        private void SaveItemToFile(Type type, string key, object item)
        {
            string filePath = GetStorageLocationFor(type, key);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Write the entire database to disk
        /// </summary>
        public void SaveDatabase()
        {
            readWriteLock.ExitReadLock();
            try
            {
                lock (data)
                {
                    foreach (var typeToDataMap in data)
                    {
                        var keyToItemMap = data[typeToDataMap.Key];

                        foreach (var item in keyToItemMap)
                        {
                            SaveItemToFile(typeToDataMap.Key, item.Key, item.Value);
                        }
                    }
                }
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Put an item into the database, and write to disk.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        public void Save<T>(T item)
            where T : class, Storable
        {
            Put(item);
            SaveItemToFile(typeof(T), item.Key, item);
        }

        public void Dispose()
        {
            readWriteLock.Dispose();
        }

        public bool Exists<T>(string key)
            where T : class, Storable
        {
            return Get<T>(key) != null;
        }
    }
}
