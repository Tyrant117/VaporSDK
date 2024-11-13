using UnityEngine;
using UnityEngine.UIElements;

namespace Vapor.ModelViewController
{
    public abstract class View : VisualElement
    {
        protected void ConstructFromStyleSheet(string uxmlPath)
        {
            var uxml = Resources.Load<VisualTreeAsset>(uxmlPath);
            var ss = Resources.Load<StyleSheet>(uxmlPath);
            styleSheets.Add(ss);
            uxml.CloneTree(this);
        }
        public abstract void InitializeView();
    }
}
