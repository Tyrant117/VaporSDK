using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VaporEditor.Inspector;

namespace VaporEditor.Blueprints
{
    public class BlueprintSearchWindowAdapter : SearcherAdapter
    {
        private readonly VisualTreeAsset _defaultItemTemplate;
        public override bool HasDetailsPanel => false;

        private readonly bool _allowMultiSelect;
        public override bool MultiSelectEnabled => _allowMultiSelect;

        public BlueprintSearchWindowAdapter(string title, bool allowMultiSelect) : base(title)
        {
            _defaultItemTemplate = Resources.Load<VisualTreeAsset>("SearcherItem");
            _allowMultiSelect = allowMultiSelect;
        }

        private SearcherItem GetFirstChildItem(SearcherItem item)
        {
            if (item.Children.Count == 0)
            {
                return item;
            }

            SearcherItem childIterator = null;
            // Discard searcher item for selection if it is a category, get next best child item from it instead
            // There is no utility in selecting category headers/titles, only the leaf entries
            childIterator = item.Children[0];
            while (childIterator != null && childIterator.Children.Count != 0)
            {
                childIterator = childIterator.Children[0];
            }

            item = childIterator;

            return item;
        }

        private int ComputeScoreForMatch(string[] queryTerms, SearcherItem matchItem)
        {
            // Scoring Criteria:
            // - Exact name match is most preferred.
            // - Partial name match is next.
            // - Exact synonym match is next.
            // - Partial synonym match is next.
            // - No match is last.
            int score = 0;

            // Split the entry name so that we can remove suffix that looks like "Clamp: In(4)"
            var nameSansSuffix = matchItem.Name;//.Split(':').First();

            int nameCharactersMatched = 0;

            foreach (var queryWord in queryTerms)
            {
                if (nameSansSuffix.Contains(queryWord, StringComparison.OrdinalIgnoreCase))
                {
                    score += 100000;
                    nameCharactersMatched += queryWord.Length;
                }

                // Check for synonym matches -- give a bonus to each
                if (matchItem.Synonyms != null)
                {
                    foreach (var syn in matchItem.Synonyms)
                    {
                        if (syn.Equals(queryWord, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 10000;
                        }
                        else if (syn.Contains(queryWord, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 1000;
                            score -= (syn.Length - queryWord.Length);
                        }
                    }
                }
            }

            if (nameCharactersMatched > 0)
            {
                int unmatchedCharacters = (nameSansSuffix.Length - nameCharactersMatched);
                score -= unmatchedCharacters;
            }

            return score;
        }

        public override SearcherItem OnSearchResultsFilter(IEnumerable<SearcherItem> searchResults, string searchQuery)
        {
            if (searchQuery.Length == 0)
            {
                return GetFirstChildItem(searchResults.FirstOrDefault());
            }

            // Sort results by length so that shorter length results are prioritized
            // prevents entries with short names getting stuck at end of list after entries with longer names when both contain the same word
            searchResults = searchResults.OrderBy(x => x.Name.Length).ToList();

            var bestMatch = GetFirstChildItem(searchResults.FirstOrDefault());
            int bestScore = 0;
            List<int> visitedItems = new();
            var queryTerms = searchQuery.Split(' ');
            foreach (var result in searchResults)
            {
                var currentItem = GetFirstChildItem(result);

                if (currentItem.Parent != null)
                {
                    SearcherItem parentItem = currentItem.Parent;
                    foreach (var matchItem in parentItem.Children)
                    {
                        if (visitedItems.Contains(matchItem.Id))
                        {
                            continue;
                        }

                        int currentScore = ComputeScoreForMatch(queryTerms, matchItem);
                        if (currentScore > bestScore)
                        {
                            bestScore = currentScore;
                            bestMatch = matchItem;
                        }

                        visitedItems.Add(matchItem.Id);
                    }
                }
            }

            return bestMatch;
        }
    }
}
