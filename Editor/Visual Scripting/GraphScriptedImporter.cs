using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Vapor.VisualScripting;

namespace VaporEditor.VisualScripting
{
    [ScriptedImporter(5, Extension)]
    public class GraphScriptedImporter : ScriptedImporter
    {
        public const string Extension = "graph";

        public override void OnImportAsset(AssetImportContext ctx)
        {         
            TextAsset graphData = new (File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("main", graphData, Resources.Load<Texture2D>("chart-line"));
            ctx.SetMainObject(graphData);
        }
    }
}
