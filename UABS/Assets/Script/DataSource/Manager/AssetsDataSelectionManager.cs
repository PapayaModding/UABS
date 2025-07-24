using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;

namespace UABS.Assets.Script.DataSource.Manager
{
    public class AssetsDataSelectionManager : IAppEventListener
    {
        public Func<List<ParsedAssetAndEntry>> GetEntryInfosCallBack;

        private List<ParsedAssetAndEntry> EntryInfos => GetEntryInfosCallBack != null ? GetEntryInfosCallBack() : new();

        private readonly EventDispatcher _dispatcher;

        private int _lastIndex;

        public AssetsDataSelectionManager(EventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is AssetSelectionEvent ase)
            {
                ParsedAssetAndEntry entryInfo = FindEntryInfo(ase.PathID);
                _lastIndex = ase.Index;
                if (ase.IsHoldingShift)
                {
                    ShiftOperation(ase.PathID);
                }
                else if (ase.IsHoldingCtrl)
                {
                    entryInfo.isHighlighted = !entryInfo.isHighlighted;
                }
                else // Not holding neither
                {
                    UnhighlightAllEntryInfos();
                    entryInfo.isHighlighted = !entryInfo.isHighlighted;
                }
                if (ase.UseJump)
                {
                    _dispatcher.Dispatch(new AssetsRenderEvent(EntryInfos, ase.Index));
                }
                else
                {
                    _dispatcher.Dispatch(new AssetsRenderEvent(EntryInfos, -1));
                }
            }
            else if (e is GoBundleViewEvent)
            {
                _lastIndex = 0;
            }
        }

        public void Prev()
        {
            if (EntryInfos.Count == 0)
            {
                Debug.Log("This bundle has no item.");
                return;
            }
            int index = StayInRange(_lastIndex - 1);
            _dispatcher.Dispatch(new AssetSelectionEvent(EntryInfos[index].assetEntryInfo.pathID, index, EntryInfos.Count, false, false, true));
        }

        public void Next()
        {
            if (EntryInfos.Count == 0)
            {
                Debug.Log("This bundle has no item.");
                return;
            }
            int index = StayInRange(_lastIndex + 1);
            _dispatcher.Dispatch(new AssetSelectionEvent(EntryInfos[index].assetEntryInfo.pathID, index, EntryInfos.Count, false, false, true));
        }

        private ParsedAssetAndEntry FindEntryInfo(long pathID)
        {
            return EntryInfos[FindIndexByPathID(pathID)];
        }

        private void UnhighlightAllEntryInfos()
        {
            foreach (ParsedAssetAndEntry entryInfo in EntryInfos)
            {
                entryInfo.isHighlighted = false;
            }
        }

        private void ShiftOperation(long pathID)
        {
            int currIndex = FindIndexByPathID(pathID);
            List<bool> highlights = EntryInfos.Select(x => x.isHighlighted).ToList();
            int closestTrueOffset = FindClosestTrueOffset(highlights, currIndex);

            if (closestTrueOffset == int.MaxValue)
            {
                EntryInfos[currIndex].isHighlighted = !EntryInfos[currIndex].isHighlighted;
                return;
            }

            int start = Mathf.Min(currIndex, currIndex + closestTrueOffset);
            int end = Mathf.Max(currIndex, currIndex + closestTrueOffset);

            for (int i = start; i <= end; i++)
            {
                EntryInfos[i].isHighlighted = true;
            }
        }

        private int FindClosestTrueOffset(List<bool> boolList, int targetIndex)
        {
            int n = boolList.Count;
            if (n == 0 || targetIndex < 0 || targetIndex >= n)
                return int.MaxValue;  // or throw an exception

            int offset = 0;
            while (offset < n)
            {
                int left = targetIndex - offset;
                int right = targetIndex + offset;

                if (left >= 0 && boolList[left]) return left - targetIndex;   // negative or zero
                if (right < n && boolList[right]) return right - targetIndex; // positive or zero

                offset++;
            }

            return int.MaxValue;  // No true found
        }

        private int FindIndexByPathID(long pathID)
        {
            for (int i = 0; i < EntryInfos.Count; i++)
            {
                if (EntryInfos[i].assetEntryInfo.pathID == pathID)
                {
                    return i;
                }
            }
            Debug.LogError($"No asset with path id {pathID} found!");
            return -1;
        }

        private int StayInRange(int input)
        {
            if (input < 0)
                return EntryInfos.Count - 1;
            else if (input >= EntryInfos.Count)
                return 0;
            return input;
        }
    }
}