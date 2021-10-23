using System.IO;
using System.IO.Compression;
using UnityEditor;

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

        [MenuItem("Assets/Extract", false, 0)]
        public static void Extract()
        {
            // Prompt the user to delete the zip after extraction.
            // Start extracting the contents.
            Extract(EditorUtility.DisplayDialog("Extraction", "Would you like to delete this file after extraction?", "Yes", "No"));
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

            // Entries cache.
            int entries = 0;
        
            // Open a stream and copy all the file data into it. (avoid file lock)
            using (Stream fileStream = new MemoryStream(File.ReadAllBytes(zipLocation)))
            {
                // Extract the contents to this new directory. (Wrap in using to dipose when finished)
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read, false))
                {
                    // Precache locals.
                    bool isFile;
                    string extractionPath;
                    entries = archive.Entries.Count;

                    bool replaceAll = false;
                    bool overWriteSingleEntry = false;

                    if (entries == 0)
                    {
                        // Notify the user the extraction can't continue.
                        EditorUtility.DisplayDialog($"Extraction Error!", "Can't extract an empty archive.", "Ok");
                        return;
                    }

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // Is this entry a file or directory?
                        isFile = !entry.FullName.EndsWith("/");

                        // Obtain the full extraction path.
                        extractionPath = Path.GetFullPath(Path.Combine(newDirectory, entry.FullName));

                        // Get the entry directory name.
                        string dirName = Path.GetDirectoryName(extractionPath);

                        // Overwrite check.
                        if ((File.Exists(extractionPath) || Directory.Exists(extractionPath)) && !replaceAll)
                        {
                            // Get the results.
                            int result = PromptToOverwrite(dirName);
                            replaceAll = result == 2;
                            overWriteSingleEntry = result == 0;
                        }

                        // Does the directory exist?
                        // No need to prompt to overwrite these.
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
                        }
                        else
                        {
                            // This entry is a file, extract the file and its contents.
                            entry.ExtractToFile(extractionPath, overWriteSingleEntry || replaceAll);
                        }

                        // Reset flag.
                        overWriteSingleEntry = false;
                    }
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
            EditorUtility.DisplayDialog($"Complete!", $"Extracted {entries} entries to: {newDirectory}!", "Ok");
        }

        /// <summary>
        /// Is the given input a valid zip file?
        /// </summary>
        private static bool IsZipFile(string input)
        {
            // Get the file extension.
            input = Path.HasExtension(input) ? Path.GetExtension(input).Remove(0, 1) : string.Empty;

            // Is it supported?
            switch (input.ToLowerInvariant())
            {
                // Supported formats.
                case "zipx":
                case "7z":
                case "zip":
                    return true;


                default:
                    return false;
            }
        }

        /// <summary>
        /// Ask for permission to overwire an existing file.
        /// </summary>
        private static int PromptToOverwrite(string existingName)
        {
            return EditorUtility.DisplayDialogComplex("Overwrite", $"{existingName} already exists, would you like to overwrite it?", "Yes", "No", "Replace All");
        }
    }
}