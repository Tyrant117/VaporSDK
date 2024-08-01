using System;
using System.Collections.Generic;
using Vapor.Inspector;

namespace Vapor.GameplayTag
{
    public interface IGameplayTagContainer
    {
        IEnumerable<int> GetTags();
        bool HasAll(GameplayTagContainer tagContainer);
        bool HasAll(IEnumerable<int> tags);
        bool HasAll(params int[] tags);
        bool HasAllExact(GameplayTagContainer tagContainer);
        bool HasAllExact(IEnumerable<int> tags);
        bool HasAllExact(params int[] tags);
        bool HasAny(GameplayTagContainer tagContainer);
        bool HasAny(IEnumerable<int> tags);
        bool HasAny(params int[] tags);
        bool HasAnyExact(GameplayTagContainer tagContainer);
        bool HasAnyExact(IEnumerable<int> tags);
        bool HasAnyExact(params int[] tags);
        bool HasTag(int tagId);
        bool HasTagExact(int tagId);
    }
}