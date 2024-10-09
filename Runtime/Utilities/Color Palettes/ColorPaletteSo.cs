using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor
{
    [System.Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public struct ColorPaletteData
    {
        [HideInInspector]
        public string Name;
        [Label("@Name")]
        public Color Color;
    }

    [CreateAssetMenu(fileName = "ColorPaletteSo", menuName = "Scriptable Objects/ColorPaletteSo")]
    public class ColorPaletteSo : NamedKeySo
    {
        public string CsvImport;

        public List<ColorPaletteData> Colors;

        [Button(rebuildTree: true)]
        private void Import()
        {
            string[] lines = CsvImport.Split(new[] { '\n', '\r', '>' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                // Split by comma
                string[] parts = line.Split(',');
                if (parts.Length == 2)
                {
                    string hexColor = parts[0].Trim();
                    string colorName = parts[1].Trim();

                    // Convert hex string to Color
                    if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
                    {
                        ColorPaletteData colorData = new ColorPaletteData
                        {
                            Name = colorName,
                            Color = color
                        };
                        Colors.Add(colorData);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid color format: {hexColor}");
                    }
                }
            }
            RuntimeEditorUtility.DirtyAndSave(this);
        }

        public IEnumerable GetColors()
        {
            foreach (var c in Colors)
            {
                yield return new ValueTuple<string, Color>(c.Name, c.Color);
            }
        }
    }
}
