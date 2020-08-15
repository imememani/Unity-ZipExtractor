using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace LateNightStudios.Extensions.ZipExtractor
{

    /// <summary>
    /// An extension class that allows you to extract zip files directly inside Unity!
    /// </summary>
    public static class UnityZipExtraction
    {
        [MenuItem("Assets/Extract", true)]
        public static bool ExtractionVerification()
        {
            // Obtain the actual asset and check if it's a zipped file.
            return IsZipFile(AssetDatabase.GetAssetPath(Selection.activeInstanceID));
        }

        [MenuItem("Assets/Extract And Delete", true)]
        public static bool ExtractionAndDeleteVerification()
        {
            // Obtain the actual asset and check if it's a zipped file.
            return IsZipFile(AssetDatabase.GetAssetPath(Selection.activeInstanceID));
        }

        [MenuItem("Assets/Extract", false, 0)]
        public static void Extract()
        {
            // Start extracting the contents.
            Extract(false);
        }

        [MenuItem("Assets/Extract And Delete", false, 0)]
        public static void ExtractAndDelete()
        {
            // Display confirmation dialog.
            if (EditorUtility.DisplayDialog("Delete Zip?", "Are you sure you want to delete this file after extraction?", "Yes", "No"))
            {
                // Start extracting the contents.
                Extract(true);
            }
        }

        /// <summary>
        /// Handles extraction of a given zip file.
        /// </summary>
        private static void Extract(bool deleteZipOnFinish)
        {
            // Get the full file path.
            string zipLocation = AssetDatabase.GetAssetPath(Selection.activeInstanceID);

            // Get the file name.
            string fileName = Selection.activeObject.name;

            // Get the parent directory path.
            string parentDirectory = Directory.GetParent(zipLocation).FullName;

            // Cache the new directory to extract into.
            string newDirectory = Path.Combine(parentDirectory, fileName);

            // Create a new folder within the parent folder with the file name.
            Directory.CreateDirectory(newDirectory);

            // Notify the user the extraction has begun.
            EditorUtility.DisplayDialog($"Extracting {fileName}", "Your zip file is extracting.", "Ok");

            // Extract the contents to this new directory. (Wrap in using to dipose when finished)
            using (ZipArchive archive = ZipFile.OpenRead(zipLocation))
            {
                // Precache locals.
                bool isFile;
                string extractionPath;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Is this entry a file or directory?
                    isFile = !entry.FullName.EndsWith("/");

                    // Obtain the full extraction path.
                    extractionPath = Path.GetFullPath(Path.Combine(newDirectory, entry.FullName));

                    // Get the entry directory name.
                    string dirName = Path.GetDirectoryName(extractionPath);

                    // Does the directory exist?
                    if (!Directory.Exists(dirName))
                    {
                        // No, create it so the entry can properly extract itself.
                        Directory.CreateDirectory(dirName);
                    }

                    // Is this entry not a file?
                    if (!isFile)
                    {
                        // It's a directory, create it.
                        Directory.CreateDirectory(extractionPath);

                        continue;
                    }
                    else
                    {
                        // This entry is a file, extract the file and its contents.
                        entry.ExtractToFile(extractionPath, true);
                    }
                }

                // The extraction process has finished, the file lock is lifted.
                // Do we want to remove the zip?
                if (deleteZipOnFinish)
                {
                    // Yes, delete the file.
                    File.Delete(zipLocation);
                }

                // Refresh the project.
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Notify the user the extraction is complete.
                EditorUtility.DisplayDialog($"Complete!", "Zip extracted.", "Ok");
            }
        }

        /// <summary>
        /// Is the given input a valid zip file?
        /// </summary>
        private static bool IsZipFile(string input) 
        {
          switch (input.ToLowerInvariant())
          {
              case "zip":
                  return true;
              case "zipx":
                  return true;
              case "7z":
                  return true;
                  
              default:
                  return false;
          }
        }
    }
}
