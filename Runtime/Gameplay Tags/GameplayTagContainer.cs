using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.GameplayTag;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.GameplayTag
{
    [Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public class GameplayTagContainer : IGameplayTagContainer
    {
        [SerializeField, ValueDropdown("@GetAllTagValues", searchable: true)]
        private List<KeyDropdownValue> _tags = new();

        [NonSerialized]
        private bool _isInit;
        [NonSerialized]
        private HashSet<int> _validTags;

        public bool HasTag(int tagId)
        {
            Init();

            return _validTags.Contains(tagId) || RecurseParentForValidTag(tagId);
        }
        public bool HasTag(string tagName)
        {
#if UNITY_EDITOR
            return _tags.Any(k => k.Guid == tagName) || RecurseParentForValidTag(tagName);
#else
            return HasTag(tagName.GetStableHashU16());
#endif
        }

        public bool HasTagExact(int tagId)
        {
            Init();

            return _validTags.Contains(tagId);
        }
        public bool HasTagExact(string tagName)
        {
#if UNITY_EDITOR
            return _tags.Any(k => k.Guid == tagName);
#else
            return HasTagExact(tagName.GetStableHashU16());
#endif
        }

        public bool HasAny(params int[] tags)
        {
            Init();

            foreach (var t in tags)
            {
                if (_validTags.Contains(t))
                {
                    return true;
                }
                else if (RecurseParentForValidTag(t))
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasAny(params string[] tagNames)
        {
#if UNITY_EDITOR
            foreach (var t in tagNames)
            {
                if (_tags.Any(k => k.Guid == t))
                {
                    return true;
                }
                else if (RecurseParentForValidTag(t))
                {
                    return true;
                }
            }
            return false;
#else
            int[] convert = new int[tagNames.Length];
            for (int i = 0; i < tagNames.Length; i++)
            {
                convert[i] = tagNames[i].GetStableHashU16();
            }
            return HasAny(convert);
#endif
        }
        public bool HasAny(IEnumerable<int> tags)
        {
            Init();

            foreach (var t in tags)
            {
                if (_validTags.Contains(t))
                {
                    return true;
                }
                else if (RecurseParentForValidTag(t))
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasAny(IEnumerable<string> tagNames)
        {
#if UNITY_EDITOR
            foreach (var t in tagNames)
            {
                if (_tags.Any(k => k.Guid == t))
                {
                    return true;
                }
                else if (RecurseParentForValidTag(t))
                {
                    return true;
                }
            }
            return false;
#else
            List<int> convert = new();
            foreach (var tag in tagNames)
            {
                convert.Add(tag.GetStableHashU16());
            }
            return HasAny(convert);
#endif
        }
        public bool HasAny(GameplayTagContainer tagContainer)
        {
            return HasAny(tagContainer.GetTags());
        }

        public bool HasAnyExact(params int[] tags)
        {
            Init();

            foreach (var t in tags)
            {
                if (_validTags.Contains(t))
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasAnyExact(params string[] tagNames)
        {
#if UNITY_EDITOR
            foreach (var t in tagNames)
            {
                if (_tags.Any(k => k.Guid == t))
                {
                    return true;
                }
            }
            return false;
#else
            int[] convert = new int[tagNames.Length];
            for (int i = 0; i < tagNames.Length; i++)
            {
                convert[i] = tagNames[i].GetStableHashU16();
            }
            return HasAnyExact(convert);
#endif
        }
        public bool HasAnyExact(IEnumerable<int> tags)
        {
            Init();

            foreach (var t in tags)
            {
                if (_validTags.Contains(t))
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasAnyExact(IEnumerable<string> tagNames)
        {
#if UNITY_EDITOR
            foreach (var t in tagNames)
            {
                if (_tags.Any(k => k.Guid == t))
                {
                    return true;
                }
            }
            return false;
#else
            List<int> convert = new();
            foreach (var tag in tagNames)
            {
                convert.Add(tag.GetStableHashU16());
            }
            return HasAnyExact(convert);
#endif
        }
        public bool HasAnyExact(GameplayTagContainer tagContainer)
        {
            return HasAnyExact(tagContainer.GetTags());
        }

        public bool HasAll(params int[] tags)
        {
            Init();

            foreach (var t in tags)
            {
                if (!_validTags.Contains(t) && !RecurseParentForValidTag(t))
                {
                    return false;
                }
            }
            return true;
        }
        public bool HasAll(params string[] tagNames)
        {
#if UNITY_EDITOR
            foreach (var t in tagNames)
            {
                if (!_tags.Any(k => k.Guid == t) && !RecurseParentForValidTag(t))
                {
                    return false;
                }
            }
            return true;
#else
            int[] convert = new int[tagNames.Length];
            for (int i = 0; i < tagNames.Length; i++)
            {
                convert[i] = tagNames[i].GetStableHashU16();
            }
            return HasAll(convert);
#endif
        }
        public bool HasAll(IEnumerable<int> tags)
        {
            Init();

            foreach (var t in tags)
            {
                if (!_validTags.Contains(t) && !RecurseParentForValidTag(t))
                {
                    return false;
                }
            }
            return true;
        }
        public bool HasAll(IEnumerable<string> tagNames)
        {
#if UNITY_EDITOR
            foreach (var t in tagNames)
            {
                if (!_tags.Any(k => k.Guid == t) && !RecurseParentForValidTag(t))
                {
                    return false;
                }
            }
            return true;
#else
            List<int> convert = new();
            foreach (var tag in tagNames)
            {
                convert.Add(tag.GetStableHashU16());
            }
            return HasAll(convert);
#endif
        }
        public bool HasAll(GameplayTagContainer tagContainer)
        {
            return HasAll(tagContainer.GetTags());
        }

        public bool HasAllExact(params int[] tags)
        {
            Init();

            foreach (var t in tags)
            {
                if (!_validTags.Contains(t))
                {
                    return false;
                }
            }
            return true;
        }
        public bool HasAllExact(params string[] tagNames)
        {
#if UNITY_EDITOR
            foreach (var t in tagNames)
            {
                if (!_tags.Any(k => k.Guid == t))
                {
                    return false;
                }
            }
            return true;
#else
            int[] convert = new int[tagNames.Length];
            for (int i = 0; i < tagNames.Length; i++)
            {
                convert[i] = tagNames[i].GetStableHashU16();
            }
            return HasAllExact(convert);
#endif
        }
        public bool HasAllExact(IEnumerable<int> tags)
        {
            Init();

            foreach (var t in tags)
            {
                if (!_validTags.Contains(t))
                {
                    return false;
                }
            }
            return true;
        }
        public bool HasAllExact(IEnumerable<string> tagNames)
        {
#if UNITY_EDITOR
            foreach (var t in tagNames)
            {
                if (!_tags.Any(k => k.Guid == t))
                {
                    return false;
                }
            }
            return true;
#else
            List<int> convert = new();
            foreach (var tag in tagNames)
            {
                convert.Add(tag.GetStableHashU16());
            }
            return HasAllExact(convert);
#endif
        }
        public bool HasAllExact(GameplayTagContainer tagContainer)
        {
            return HasAllExact(tagContainer.GetTags());
        }

        public bool AddTag(int tagId)
        {
            Init();
            return _validTags.Add(tagId);
        }
        public bool AddTag(string tagName)
        {
            return AddTag(tagName.GetStableHashU16());
        }

        public bool RemoveTag(int tagId)
        {
            Init();

            return _validTags.Remove(tagId);
        }
        public bool RemoveTag(string tagName)
        {
            return RemoveTag(tagName.GetStableHashU16());
        }

        public IEnumerable<int> GetTags()
        {
            foreach (var t in _tags)
            {
                yield return t;
            }
        }

        private void Init()
        {
            if (_isInit)
            {
                return;
            }

            _validTags = new HashSet<int>(_tags.Count);
            foreach (var t in _tags)
            {
                _validTags.Add(t);
            }
            _isInit = true;
        }

        private bool RecurseParentForValidTag(int tagId)
        {
            if (RuntimeDatabase<GameplayTagSo>.TryGet(tagId, out var tag))
            {
                int parentTagId = tag.Parent;
                while (parentTagId != 0)
                {
                    if (_validTags.Contains(parentTagId))
                    {
                        return true;
                    }
                    parentTagId = RuntimeDatabase<GameplayTagSo>.Get(tagId).Parent;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        private bool RecurseParentForValidTag(string childTag)
        {
            var split = childTag.Split('.');
            if (split.Length > 1)
            {
                for (int i = split.Length-1; i >= 0; i--)
                {
                    var last = split[i];
                    if (_tags.Any(k => k.Guid == last))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public static List<(string, KeyDropdownValue)> GetAllTagValues()
        {
            return GameplayTagUtility.GetAllGameplayTagsKeyValues();
        }
    }
}
