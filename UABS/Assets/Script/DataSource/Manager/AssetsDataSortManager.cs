using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;

namespace UABS.Assets.Script.DataSource.Manager
{
    public class AssetsDataSortManager : IAppEventListener
    {
        public Func<List<ParsedAssetAndEntry>> GetEntryInfosCallBack;

        private List<ParsedAssetAndEntry> EntryInfos => GetEntryInfosCallBack != null ? GetEntryInfosCallBack() : new();

        public Action<List<ParsedAssetAndEntry>> SetEntryInfosCallBack;

        public void OnEvent(AppEvent e)
        {
            if (e is SortScrollViewEvent ssve)
            {
                SortByType sortByType = ssve.SortProp.sortByType;
                SortOrder sortOrder = ssve.SortProp.sortOrder;
                SetEntryInfosCallBack(SortedEntryInfos(sortByType, sortOrder));
            }
        }

        private List<ParsedAssetAndEntry> SortedEntryInfos(SortByType sortByType, SortOrder sortOrder)
        {
            List<ParsedAssetAndEntry> copy = new(EntryInfos);
            if (sortByType == SortByType.Name)
            {
                if (sortOrder == SortOrder.Down)
                {
                    copy.Sort((a, b) => NaturalCompare(a.assetEntryInfo.name, b.assetEntryInfo.name));
                    return copy;
                }
                else if (sortOrder == SortOrder.Up)
                {
                    copy.Sort((b, a) => NaturalCompare(a.assetEntryInfo.name, b.assetEntryInfo.name));
                    return copy;
                }
            }
            else if (sortByType == SortByType.Type)
            {
                if (sortOrder == SortOrder.Down)
                {
                    return copy.OrderBy(x => x.assetEntryInfo.classID).ToList();
                }
                else if (sortOrder == SortOrder.Up)
                {
                    return copy.OrderByDescending(x => x.assetEntryInfo.classID).ToList();
                }
            }
            else if (sortByType == SortByType.PathID)
            {
                if (sortOrder == SortOrder.Down)
                {
                    return copy.OrderBy(x => x.assetEntryInfo.pathID).ToList();
                }
                else if (sortOrder == SortOrder.Up)
                {
                    return copy.OrderByDescending(x => x.assetEntryInfo.pathID).ToList();
                }
            }
            return copy;
        }

        private static int NaturalCompare(string a, string b)
        {
            var regex = new Regex(@"\d+|\D+");
            var matchesA = regex.Matches(a);
            var matchesB = regex.Matches(b);
            int i = 0;

            while (i < matchesA.Count && i < matchesB.Count)
            {
                var chunkA = matchesA[i].Value;
                var chunkB = matchesB[i].Value;

                if (int.TryParse(chunkA, out int numA) && int.TryParse(chunkB, out int numB))
                {
                    int cmp = numA.CompareTo(numB);
                    if (cmp != 0) return cmp;
                }
                else
                {
                    int cmp = string.Compare(chunkA, chunkB, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0) return cmp;
                }

                i++;
            }

            return matchesA.Count.CompareTo(matchesB.Count);
        }
    }
}