
namespace VaporEditor.VisualScripting
{
    public readonly struct EditorLabelVisualData
    {
        public readonly string IconPath;
        public readonly string StyleSheet;
        public readonly string BorderName;
        public readonly string ClassName;

        public EditorLabelVisualData(string iconPath, string styleSheet, string borderName, string className)
        {
            IconPath = iconPath;
            StyleSheet = styleSheet;
            BorderName = borderName;
            ClassName = className;
        }
    }
}
