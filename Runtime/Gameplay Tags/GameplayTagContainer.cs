using System.Collections.Generic;
using UnityEngine;
using Vapor.GameplayTag;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.GameplayTag
{
    [System.Serializable, DrawWithVapor(UIGroupType.Foldout)]
    public class GameplayTagContainer : IGameplayTagContainer
    {
        [SerializeField, ValueDropdown("$GetAllTagValues", searchable: true)]
        private List<KeyDropdownValue> _tags;

        private bool _isInit;
        private HashSet<int> _validTags;

        public bool HasTag(int tagId)
        {
            Init();

            return _validTags.Contains(tagId) || RecurseParentForValidTag(tagId);
        }

        public bool HasTagExact(int tagId)
        {
            Init();

            return _validTags.Contains(tagId);
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
        public bool HasAllExact(GameplayTagContainer tagContainer)
        {
            return HasAllExact(tagContainer.GetTags());
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

        public static List<(string, KeyDropdownValue)> GetAllTagValues()
        {
            return GameplayTagUtility.GetAllGameplayTagsKeyValues();
        }
    }
}
