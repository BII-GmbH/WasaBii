﻿using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace // unity likes event classes to be without namespace

// Note: This is in a managed DLL so that this is always executed,
//        even if the containing unity project does not compile.
public class AddUnitDefinitionsToCsProj : AssetPostprocessor {
    
    private const string UnitDefFileEnding = ".units.json";
    
    /// <summary>
    /// When a file is added, removed or moved, and that file happens to be a unit definition file,
    ///  then maintains the `csc.rsp` file next to the unit definition in order to automatically register it.
    /// </summary>
    private static void OnPostprocessAllAssets(
        string[] importedAssets, 
        string[] deletedAssets, 
        string[] movedAssets, 
        string[] movedFromAssetPaths
    ) {
        static string FixPath(string path) {
            if (path.StartsWith("Packages/")) {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
                var basePath = packageInfo?.resolvedPath ?? "Failed to resolve package path: " + path;
                var fixedPath = basePath 
                                + Path.DirectorySeparatorChar
                                + string.Join(Path.DirectorySeparatorChar, path.Split("/").Skip(2));
                
                var unityProjectRoot = Directory.GetCurrentDirectory();
                var projectRootUri = new Uri(unityProjectRoot).ToString();
                var fixedPathUri = new Uri(fixedPath).ToString();
                // + 1 to include the path separator. We don't want the relative path to start with a /
                var relativeFixedPath = fixedPathUri.Substring(projectRootUri.Length + 1);
                
                Debug.Log($"adjusted '{path}' to '{relativeFixedPath}'");
                return relativeFixedPath;
            } else return path;
        }
        
        var addedDefs = importedAssets.Where(a => a.EndsWith(UnitDefFileEnding)).Select(FixPath).ToList();
        var removedDefs = deletedAssets.Where(a => a.EndsWith(UnitDefFileEnding)).Select(FixPath).ToList();
        
        for (var i = 0; i < movedAssets.Length; ++i) {
            if (movedAssets[i].EndsWith(UnitDefFileEnding)) {
                addedDefs.Add(FixPath(movedAssets[i]));
                removedDefs.Add(FixPath(movedFromAssetPaths[i]));
            }
        }
        
        // create/update csc.rsp files next to the assets

        foreach (var added in addedDefs) {
            var path = Path.GetDirectoryName(added)!;
            var cscRspPath = Path.Combine(path, "csc.rsp");

            if (!File.Exists(cscRspPath)) {
                File.WriteAllText(cscRspPath, "# This file has been generated by WasaBii.\n" +
                                              "# It registers the .units.json file so that your types are generated properly.\n" +
                                              "# Your own changes to this file will be kept.\n");
            }

            var allText = File.ReadAllText(cscRspPath);
            if (!allText.Contains(path)) {
                var pre = allText.EndsWith("\n") ? "" : Environment.NewLine;
                File.AppendAllText(cscRspPath, pre + CommandFor(added) + Environment.NewLine);
                Debug.Log("Found new unit definition file. Adjusting csc.rsp to automatically register it. At: " + added);
            }
        }
        
        foreach (var removed in removedDefs) {
            var path = Path.GetDirectoryName(removed)!;
            var cscRspPath = Path.Combine(path, "csc.rsp");
            
            if (File.Exists(cscRspPath)) {
                var lines = File.ReadAllLines(cscRspPath).ToList();
                lines.RemoveAll(line => line.Trim().Equals(CommandFor(removed), StringComparison.OrdinalIgnoreCase));

                if (lines.Count == 0 || lines.All(l => string.IsNullOrWhiteSpace(l) || l.StartsWith("#"))) 
                    File.Delete(cscRspPath);
                else
                    File.WriteAllLines(cscRspPath, lines);
                
                Debug.Log("Found deleted unit definition file. Adjusting csc.rsp and cleaning up. At: " + removed);
            }
        }

        static string CommandFor(string assetPath) => $"-additionalfile:\"{assetPath}\"";
    }
    
    // TODO: replace the above with this once Roslyn actually works as intended in Unity...
    // /// <summary>
    // /// Collects all files ending in `.units.json` and adds them to the appropriate .csproj file.
    // /// </summary>
    // public static string OnGeneratedCSProject(string path, string content) {
    //     // Load the .csproj file as an XDocument
    //     var doc = XDocument.Parse(content);
    //
    //     // Define the namespace for easier reference
    //     XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
    //
    //     // Find all .units.json files in the related assembly
    //     var files = doc.Descendants(ns + "None")
    //         .Select(x => (string)x.Attribute("Include"))
    //         .Where(x => x?.EndsWith(".units.json") == true)
    //         .Select(x => x.Replace('\\', '/'))
    //         .ToArray();
    //     
    //     if (files.Length == 0) return content;
    //
    //     // Locate the ItemGroup where you want to insert the additional files
    //     var newItemGroup = new XElement(ns + "ItemGroup");
    //
    //     // Add each .units.json file as an additional file
    //     foreach (var relFile in files) {
    //         var file = Path.GetFullPath(relFile);
    //         
    //         var additionalFile = new XElement(ns + "AdditionalFiles", new XAttribute("Include", file));
    //         newItemGroup.Add(additionalFile);
    //
    //         Debug.Log("Found WasaBii unit definition file at '" + file + "' for project: " +
    //                   path.Split('/').Last().Split('\\').Last());
    //     }
    //     
    //     // Add the new ItemGroup right after the first one
    //     if (doc.Root?.Elements(ns + "ItemGroup").FirstOrDefault() is { } firstItemGroup) {
    //         firstItemGroup.AddAfterSelf(newItemGroup);
    //     } else {
    //         // If there's no existing ItemGroup, add it to the root
    //         doc.Root!.Add(newItemGroup);
    //     }
    //     
    //     return doc.ToString(SaveOptions.None);
    // }
}