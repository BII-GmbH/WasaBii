#if UNITY_EDITOR

using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace BII.WasaBii.Units
{
    public class AddUnitDefinitionsToCsProj : AssetPostprocessor
    {
        /// <summary> Collects all files ending in `.units.json` and adds them to the .csproj file. </summary>
        public static string OnGeneratedCSProject(string path, string content) {
            // Load the .csproj file as an XDocument
            var doc = XDocument.Parse(content);

            // Define the namespace for easier reference
            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

            // Locate the ItemGroup where you want to insert the additional files
            var itemGroup = doc.Root!.Element(ns + "ItemGroup");

            // Find all .units.json files in the related assembly
            var files = doc.Descendants(ns + "None")
                .Select(x => (string)x.Attribute("Include"))
                .Where(x => x?.EndsWith(".units.json") == true)
                .ToArray();

            // Add each .units.json file as an additional file
            foreach (var file in files) {
                var additionalFile = new XElement(ns + "AdditionalFiles", new XAttribute("Include", file));
                itemGroup!.Add(additionalFile);
                Debug.Log("Found WasaBii unit definition file at: " + file);
            }

            return doc.ToString(SaveOptions.None);
        }
    }
}

#endif