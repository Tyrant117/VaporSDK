using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.GameplayTag
{
    public class GameplayTagComponent : VaporBehaviour, IGameplayTagContainer
    {
        [SerializeField]
        private GameplayTagContainer _tagContainer;
        public GameplayTagContainer TagContainer { get => _tagContainer; }

        public IEnumerable<int> GetTags()
        {
            return _tagContainer.GetTags();
        }

        public bool HasAll(GameplayTagContainer tagContainer)
        {
            return _tagContainer.HasAll(tagContainer);
        }

        public bool HasAll(IEnumerable<int> tags)
        {
            return _tagContainer.HasAll(tags);
        }

        public bool HasAll(params int[] tags)
        {
            return _tagContainer.HasAll(tags);
        }

        public bool HasAllExact(GameplayTagContainer tagContainer)
        {
            return _tagContainer.HasAllExact(tagContainer);
        }

        public bool HasAllExact(IEnumerable<int> tags)
        {
            return _tagContainer.HasAllExact(tags);
        }

        public bool HasAllExact(params int[] tags)
        {
            return _tagContainer.HasAllExact(tags);
        }

        public bool HasAny(GameplayTagContainer tagContainer)
        {
            return _tagContainer.HasAny(tagContainer);
        }

        public bool HasAny(IEnumerable<int> tags)
        {
            return _tagContainer.HasAny(tags);
        }

        public bool HasAny(params int[] tags)
        {
            return _tagContainer.HasAny(tags);
        }

        public bool HasAnyExact(GameplayTagContainer tagContainer)
        {
            return _tagContainer.HasAnyExact(tagContainer);
        }

        public bool HasAnyExact(IEnumerable<int> tags)
        {
            return _tagContainer.HasAnyExact(tags);
        }

        public bool HasAnyExact(params int[] tags)
        {
            return _tagContainer.HasAnyExact(tags);
        }

        public bool HasTag(int tagId)
        {
            return _tagContainer.HasTag(tagId);
        }

        public bool HasTagExact(int tagId)
        {
            return _tagContainer.HasTagExact(tagId);
        }
    }
}
