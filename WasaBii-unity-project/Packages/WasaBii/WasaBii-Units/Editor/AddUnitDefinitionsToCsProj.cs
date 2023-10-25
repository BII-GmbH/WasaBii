#if UNITY_EDITOR

using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace BII.WasaBii.UnitSystem
{
    public class AddUnitDefinitionsToCsProj : AssetPostprocessor
    {
        /// <summary> Collects all files ending in `.units.json` and adds them to the .csproj file. </summary>
        public static string OnGeneratedCSProject(string path, string content) {
            // Load the .csproj file as an XDocument
            var doc = XDocument.Parse(content);

            // Define the namespace for easier reference
            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

            // Find all .units.json files in the related assembly
            var files = doc.Descendants(ns + "None")
                .Select(x => (string)x.Attribute("Include"))
                .Where(x => x?.EndsWith(".units.json") == true)
                .Select(x => x.Replace('\\', '/'))
                .ToArray();
            
            if (files.Length == 0) return content;

            // Locate the ItemGroup where you want to insert the additional files
            var newItemGroup = new XElement(ns + "ItemGroup");

            // Add each .units.json file as an additional file
            foreach (var relFile in files) {

                var file = Path.GetFullPath(relFile);
                
                var additionalFile = new XElement(ns + "AdditionalFiles", new XAttribute("Include", file));
                newItemGroup.Add(additionalFile);
                
                Debug.Log("Found WasaBii unit definition file at: " + file + "\nFor project: " + path);
            }
            
            // Add the new ItemGroup right after the first one
            if (doc.Root?.Elements(ns + "ItemGroup").FirstOrDefault() is { } firstItemGroup) {
                firstItemGroup.AddAfterSelf(newItemGroup);
            } else {
                // If there's no existing ItemGroup, add it to the root
                doc.Root!.Add(newItemGroup);
            }
            
            return doc.ToString(SaveOptions.None);
        }
    }
}

#endif