using System.Collections.Generic;
using UnityEngine;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    public class BlueprintSearcherItem : SearcherItem
    {
        public BlueprintSearcherItem(BlueprintSearchEntry entry,
            string help = " ", List<SearcherItem> children = null) : base(entry.CombinedMenuName, help: help, children: children)
        {
            UserData = (entry, Vector2.zero);
            Synonyms = entry.Synonyms;
        }
    }
}