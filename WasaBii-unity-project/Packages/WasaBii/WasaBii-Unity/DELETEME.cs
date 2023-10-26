using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BII.WasaBii.Unity
{
    
    public class DELETEME
    {
        [MenuItem("DELETEME/printLocation")]
        public static void PRINT() {
            Debug.Log(typeof(Debug).Assembly.Location);
        }
        
    }
}