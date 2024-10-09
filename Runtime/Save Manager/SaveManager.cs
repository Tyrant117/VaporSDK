using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.IO;
using Vapor.Inspector;

namespace Vapor.SaveManager
{
    public class SaveManager
    {
        enum FileOperationType
        {
            Save,
            Load,
            Delete,
            Init
        }

        readonly struct FileOperation
        {
            public readonly FileOperationType Type;
            public readonly string[] Filenames;

            public FileOperation(FileOperationType operationType, string[] filenames)
            {
                Type = operationType;
                Filenames = filenames;
            }
        }

        [Serializable]
        public class SaveableObject
        {
            public string Key;
            public object Data;
        }

        private static CancellationToken s_CancellationToken;

        private static string s_PersistantPath;
        private static readonly Dictionary<string, ISaveData> s_Saveables = new();
        private static readonly List<SaveableObject> s_LoadedSaveables = new();
        private static readonly Queue<FileOperation> s_FileOperationQueue = new();
        private static readonly HashSet<string> s_Files = new();

        private static bool s_IsInitialized;

        private static readonly JsonSerializerSettings s_JsonSerializerSettings = new()
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_IsInitialized = false;
        }

        private static void Initialize()
        {
            if (s_IsInitialized)
            {
                return;
            }

            s_CancellationToken = new CancellationToken();
            s_Saveables.Clear();
            s_LoadedSaveables.Clear();
            s_FileOperationQueue.Clear();
            s_Files.Clear();
            s_IsInitialized = false;

            s_PersistantPath = Application.persistentDataPath;

            s_IsInitialized = true;
        }

        #region SaveAsync API

        /// <summary>
        /// Boolean indicating whether or not a file operation is in progress.
        /// </summary>
        public static bool IsBusy { get; private set; }

        public static void SetSaveDirectory(string directoryName)
        {
            Initialize();
            s_PersistantPath = Path.Combine(s_PersistantPath, directoryName).Replace("\\", "/");
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(SaveManager), nameof(SetSaveDirectory))} - {s_PersistantPath}");
            if (!Directory.Exists(s_PersistantPath))
            {
                // Create the directory if it doesn't exist
                Directory.CreateDirectory(s_PersistantPath);
            }
        }

        /// <summary>
        /// Registers an ISaveable and its file for saving and loading.
        /// </summary>
        /// <param name="saveable">The ISaveable to register for saving and loading.</param>
        public static void RegisterSaveable(ISaveData saveable)
        {
            Initialize();
            if (s_Saveables.TryAdd(saveable.Key, saveable))
            {
                s_Files.Add(saveable.Filename);
            }
            else
            {
                Debug.LogError($"Saveable with Key {saveable.Key} already exists! {saveable.Filename}");
            }
        }

        /// <summary>
        /// Saves the files at the given paths or filenames.
        /// <code>
        /// File example: "MyFile.dat"
        /// Path example: "MyFolder/MyFile.dat"
        /// </code>
        /// </summary>
        /// <param name="filenames">The array of paths or filenames to save.</param>
        public static async Awaitable Save(params string[] filenames) => await DoFileOperation(FileOperationType.Save, filenames);

        /// <summary>
        /// Loads the files at the given paths or filenames.
        /// <code>
        /// File example: "MyFile.dat"
        /// Path example: "MyFolder/MyFile.dat"
        /// </code>
        /// </summary>
        /// <param name="filenames">The array of paths or filenames to load.</param>
        public static async Awaitable Load(params string[] filenames) => await DoFileOperation(FileOperationType.Load, filenames);

        /// <summary>
        /// Deletes the files at the given paths or filenames. Each file will be removed from disk.
        /// Use <see cref="Init(string[])"/> to fill each file with an empty string without removing it from disk.
        /// <code>
        /// File example: "MyFile.dat"
        /// Path example: "MyFolder/MyFile.dat"
        /// </code>
        /// </summary>
        /// <param name="filenames">The array of paths or filenames to delete.</param>
        public static async Awaitable Delete(string[] filenames) => await DoFileOperation(FileOperationType.Delete, filenames);

        /// <summary>
        /// Erases the files at the given paths or filenames. Each file will still exist on disk, but it will be empty.
        /// Use <see cref="Delete(string[])"/> to remove the files from disk.
        /// <code>
        /// File example: "MyFile.dat"
        /// Path example: "MyFolder/MyFile.dat"
        /// </code>
        /// </summary>
        /// <param name="filenames">The array of paths or filenames to erase.</param>
        public static async Awaitable Init(params string[] filenames) => await DoFileOperation(FileOperationType.Init, filenames);

        /// <summary>
        /// Sets the given Guid byte array to a new Guid byte array if it is null, empty, or an empty Guid.
        /// This method can be useful for creating unique keys for ISaveables.
        /// </summary>
        /// <param name="guidBytes">The byte array (passed by reference) that you would like to fill with a serializable guid.</param>
        /// <returns>The same byte array that contains the serializable guid, but returned from the method.</returns>
        public static byte[] GetSerializableGuid(ref byte[] guidBytes)
        {
            // If the byte array is null, return a new Guid byte array.
            if (guidBytes == null)
            {
                Debug.LogWarning("Guid byte array is null. Generating a new Guid.");
                guidBytes = Guid.NewGuid().ToByteArray();
            }

            // If the byte array is empty, return a new Guid byte array.
            if (guidBytes.Length == 0)
            {
                Debug.LogWarning("Guid byte array is empty. Generating a new Guid.");
                guidBytes = Guid.NewGuid().ToByteArray();
            }

            // If the byte array is not empty, but is not 16 bytes long, throw an exception.
            if (guidBytes.Length != 16)
                throw new ArgumentException("Guid byte array must be 16 bytes long.");

            // If the byte array is not an empty Guid, return a new Guid byte array.
            // Otherwise, return the given Guid byte array.
            Guid guidObj = new Guid(guidBytes);

            if (guidObj == Guid.Empty)
            {
                Debug.LogWarning("Guid is empty. Generating a new Guid.");
                guidBytes = Guid.NewGuid().ToByteArray();
            }

            return guidBytes;
        }

        #endregion

        #region - Operations -
        static async Awaitable DoFileOperation(FileOperationType operationType, string[] filenames)
        {
            Initialize();

            // If the cancellation token has been requested at any point, return
            while (!s_CancellationToken.IsCancellationRequested)
            {
                // Create the file operation struct and queue it
                s_FileOperationQueue.Enqueue(new FileOperation(operationType, filenames));

                // If we are already doing file I/O, return
                if (IsBusy)
                {
                    return;
                }

                // Prevent duplicate file operations from processing the queue
                IsBusy = true;

                // Switch to a background thread to process the queue
                await Awaitable.BackgroundThreadAsync();

                while (s_FileOperationQueue.Count > 0)
                {
                    s_FileOperationQueue.TryDequeue(out FileOperation fileOperation);
                    switch (fileOperation.Type)
                    {
                        case FileOperationType.Save:
                            await SaveFileOperationAsync(fileOperation.Filenames);
                            break;
                        case FileOperationType.Load:
                            await LoadFileOperationAsync(fileOperation.Filenames);
                            break;
                        case FileOperationType.Delete:
                            await DeleteFileOperationAsync(fileOperation.Filenames);
                            break;
                        case FileOperationType.Init:
                            await InitFileOperationAsync(fileOperation.Filenames);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // Switch back to the main thread before accessing Unity objects and setting IsBusy to false
                await Awaitable.MainThreadAsync();

                // If anything was populated in the loadedDataList, restore state
                // This is done here because it's better to process the whole queue before switching back to the main thread.
                if (s_LoadedSaveables.Count > 0)
                {
                    // Restore state for each ISaveable
                    foreach (SaveableObject wrappedData in s_LoadedSaveables)
                    {
                        if (wrappedData.Key == null)
                        {
                            Debug.LogError("The key for an ISaveable is null. JSON data may be malformed. " +
                                           "The data will not be restored. ");
                            continue;
                        }

                        // Try to get the ISaveable from the dictionary
                        if (s_Saveables.ContainsKey(wrappedData.Key) == false)
                        {
                            Debug.LogError("The ISaveable with the key " + wrappedData.Key + " was not found in the saveables dictionary. " +
                                           "The data will not be restored. This could mean that the string Key for the matching object has " +
                                           "changed since the save data was created.");
                            continue;
                        }

                        // Get the ISaveable from the dictionary
                        var saveable = s_Saveables[wrappedData.Key];

                        // If the ISaveable is null, log an error and continue to the next iteration
                        if (saveable == null)
                        {
                            Debug.LogError("The ISaveable with the key " + wrappedData.Key + " is null. "
                                           + "The data will not be restored.");
                            continue;
                        }

                        // Restore the state of the ISaveable
                        saveable.RestoreState(wrappedData.Data);
                    }
                }

                // Clear the list before the next iteration
                s_LoadedSaveables.Clear();

                IsBusy = false;

                // Return, otherwise we will loop forever
                return;
            }
        }

        private static async Awaitable InitFileOperationAsync(string[] filenames)
        {
            // Initialize Default Files
            foreach (string filename in filenames)
            {
                if (Exists(filename))
                {
                    //Debug.Log($"[SaveManager] InitFileOperationAsync - File at: {GetPath(filename)} already exists");
                    continue;
                }

                var json = InitJson();
                await WriteFile(filename, json, s_CancellationToken);
            }
        }

        private static async Awaitable SaveFileOperationAsync(string[] filenames)
        {
            // Get the ISaveables that correspond to the files, convert them to JSON, and save them
            foreach (string filename in filenames)
            {
                // Gather all of the saveables that correspond to the file
                List<ISaveData> saveablesToSave = new(s_Saveables.Values.Where(saveable => saveable.Filename == filename));
                Debug.Log($"Saving...{filename} [{saveablesToSave.Count}]");
                string json = SaveablesToJson(saveablesToSave);
                await WriteFile(filename, json, s_CancellationToken);
            }
        }

        private static async Awaitable LoadFileOperationAsync(string[] filenames)
        {
            // Load the files
            foreach (string filename in filenames)
            {
                string fileContent = await ReadFile(filename, s_CancellationToken);

                // If the file is empty, skip it
                if (string.IsNullOrEmpty(fileContent))
                {
                    continue;
                }

                // Deserialize the JSON data to List of SaveableDataWrapper
                List<SaveableObject> jsonObjects = null;
                try
                {
                    jsonObjects = JsonConvert.DeserializeObject<List<SaveableObject>>(fileContent, s_JsonSerializerSettings);
                    Debug.Log($"Found {jsonObjects.Count} SavebleObjects at {GetPath(filename)}");
                }
                catch (Exception e)
                {
                    Debug.LogError("Error deserializing JSON data. JSON data may be malformed. Exception message: " + e.Message);
                    continue;
                }

                if (jsonObjects != null)
                {
                    s_LoadedSaveables.AddRange(jsonObjects);
                }
            }
        }

        private static async Awaitable DeleteFileOperationAsync(string[] filenames)
        {
            // Delete the files from disk
            foreach (string filename in filenames)
            {
                await Erase(filename, s_CancellationToken);
            }
        }

        private static string InitJson()
        {
            List<SaveableObject> wrappedSaveables = new();
            return JsonConvert.SerializeObject(wrappedSaveables, s_JsonSerializerSettings);
        }

        private static string SaveablesToJson(List<ISaveData> saveables)
        {
            if (saveables == null)
            {
                throw new ArgumentNullException(nameof(saveables));
            }

            SaveableObject[] wrappedSaveables = new SaveableObject[saveables.Count];

            for (var i = 0; i < saveables.Count; i++)
            {
                var s = saveables[i];
                var data = s.CaptureState();

                wrappedSaveables[i] = new SaveableObject
                {
                    Key = s.Key.ToString(),
                    Data = data
                };
            }

            return JsonConvert.SerializeObject(wrappedSaveables, s_JsonSerializerSettings);
        }
        #endregion

        #region - File I/O -
        /// <summary>
        /// Returns the full path to a file in the persistent data path using the given path or filename.
        /// <code>
        /// File example: "MyFile.dat"
        /// Path example: "MyFolder/MyFile.dat"
        /// </code>
        /// </summary>
        /// <param name="pathOrFilename">The path or filename of the file that will be combined with the persistent data path.</param>
        private static string GetPath(string pathOrFilename) => Path.Combine(s_PersistantPath, pathOrFilename);

        /// <summary>
        /// Returns true if a file exists at the given path or filename.
        /// <code>
        /// File example: "MyFile.dat"
        /// Path example: "MyFolder/MyFile.dat"
        /// </code>
        /// </summary>
        /// <param name="pathOrFilename">The path or filename of the file to check.</param>
        /// <param name="cancellationToken">The cancellation token should be the same one from the calling MonoBehaviour.</param>
        public static bool Exists(string pathOrFilename) => File.Exists(GetPath(pathOrFilename));

        /// <summary>
        /// Writes the given content to a file at the given path or filename.
        /// <code>
        /// File example: "MyFile.dat"
        /// Path example: "MyFolder/MyFile.dat"
        /// </code>
        /// </summary>
        /// <param name="pathOrFilename">The path or filename of the file to write.</param>
        /// <param name="content">The string to write to the file.</param>
        /// <param name="cancellationToken">The cancellation token should be the same one from the calling MonoBehaviour.</param>
        public static async Awaitable WriteFile(string pathOrFilename, string content, CancellationToken cancellationToken) => await File.WriteAllTextAsync(GetPath(pathOrFilename), content, cancellationToken);

        /// <summary>
        /// Returns the contents of a file at the given path or filename.
        /// <code>
        /// File example: "MyFile.dat"
        /// Path example: "MyFolder/MyFile.dat"
        /// </code>
        /// </summary>
        /// <param name="pathOrFilename">The path or filename of the file to read.</param>
        /// <param name="cancellationToken">The cancellation token should be the same one from the calling MonoBehaviour.</param>
        public static async Awaitable<string> ReadFile(string pathOrFilename, CancellationToken cancellationToken)
        {
            // If the file does not exist, return an empty string and log a warning.
            bool exists = Exists(pathOrFilename);

            if (!exists)
            {
                Debug.LogWarning($"FileHandler: File does not exist at path or filename: {GetPath(pathOrFilename)}" +
                                 $"\nReturning empty string and no data will be loaded.");
                return string.Empty;
            }

            string fileContent = await File.ReadAllTextAsync(GetPath(pathOrFilename), cancellationToken);

            // If the file is empty, return an empty string and log a warning.
            if (string.IsNullOrEmpty(fileContent))
            {
                Debug.LogWarning($"FileHandler: Attempted to load {pathOrFilename} but the file was empty.");
                return string.Empty;
            }

            return fileContent;
        }

        /// <summary>
        /// Erases a file at the given path or filename. The file will still exist on disk, but it will be empty.
        /// Use <see cref="Delete(string)"/> to remove the file from disk.
        /// <code>
        /// File example: "MyFile.dat"
        /// Path example: "MyFolder/MyFile.dat"
        /// </code>
        /// </summary>
        /// <param name="pathOrFilename">The path or filename of the file to erase.</param>
        /// <param name="cancellationToken">The cancellation token should be the same one from the calling MonoBehaviour.</param>
        public static async Awaitable Erase(string pathOrFilename, CancellationToken cancellationToken) => await WriteFile(pathOrFilename, string.Empty, cancellationToken);
        #endregion
    }
}