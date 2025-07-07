using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Dispatcher;
using System;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.DataSource.Manager
{
    public class AssetsDataSelectionManager : IAppEventListener
    {
        // private long _lastPathID;
        // private List<long> _currBunPathIDs;
        // private HashSet<long> _selections;

        public Func<List<ParsedAssetAndEntry>> GetEntryInfosCallBack;

        private List<ParsedAssetAndEntry> EntryInfos => GetEntryInfosCallBack != null ? GetEntryInfosCallBack() : new();

        private EventDispatcher _dispatcher;

        public AssetsDataSelectionManager(EventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is AssetSelectionEvent ase)
            {
                // Debug.Log($"shift: {ase.IsHoldingShift}, control: {ase.IsHoldingCtrl}");
                ParsedAssetAndEntry entryInfo = FindEntryInfo(ase.PathID);
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
                _dispatcher.Dispatch(new AssetsRenderEvent(EntryInfos, -1));
            }
            // if (e is AssetSelectionEvent ase)
            // {
            //     if (!ase.IsHoldingShift)
            //     {
            //         _selections = new();
            //     }
            //     long currPathID = ase.PathID;
            //     if (_selections.Contains(currPathID))
            //     {
            //         _selections.Remove(currPathID);
            //     }
            //     else
            //     {
            //         _selections.Add(currPathID);
            //     }
            //     _lastPathID = currPathID;
            //     AppEnvironment.Dispatcher.Dispatch(new AssetMultiSelectionEvent(_selections, FindIndexOfLastPathID()));
            // }
            // else if (e is GoBundleViewEvent gbve)
            // {
            //     _currBunPathIDs = gbve.EntryInfos.Select(x => x.assetEntryInfo.pathID).ToList();
            //     Debug.Log(_currBunPathIDs.Count);
            // }
            // else if (e is OnAssetsDataChangeEvent dce)
            // {
            //     _currBunPathIDs = dce.RenderEntryInfos.Select(x => x.assetEntryInfo.pathID).ToList();
            //     Debug.Log(_currBunPathIDs.Count);
            // }

        }

        public void Prev()
        {

        }

        public void Next()
        {

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

        // public void Prev()
        // {
        //     if (_currBunPathIDs.Count == 0)
        //     {
        //         Debug.Log("This bundle has no item.");
        //         return;
        //     }
        //     int indexOfLastPathID = FindIndexOfLastPathID();
        //     if (indexOfLastPathID == -1)
        //     {
        //         Debug.LogWarning("No index of last path id found");
        //     }
        //     int index = StayInRange(indexOfLastPathID - 1);
        //     Debug.Log($"Prev index: {index}, id: {_currBunPathIDs[index]}");
        //     // AppEnvironment.Dispatcher.Dispatch(new AssetSelectionEvent(_currBunPathIDs[index], index, _currBunPathIDs.Count, true));
        // }

        // public void Next()
        // {
        //     if (_currBunPathIDs.Count == 0)
        //     {
        //         Debug.Log("This bundle has no item.");
        //         return;
        //     }
        //     int indexOfLastPathID = FindIndexOfLastPathID();
        //     if (indexOfLastPathID == -1)
        //     {
        //         Debug.LogWarning("No index of last path id found");
        //     }
        //     int index = StayInRange(indexOfLastPathID + 1);
        //     Debug.Log($"Next index: {index}, id: {_currBunPathIDs[index]}");
        //     // AppEnvironment.Dispatcher.Dispatch(new AssetSelectionEvent(_currBunPathIDs[index], index, _currBunPathIDs.Count, true));
        // }

        // private int FindIndexOfLastPathID()
        // {
        //     if (_currBunPathIDs == null)
        //     {
        //         return -1;
        //     }
        //     for (int i = 0; i < _currBunPathIDs.Count; i++)
        //     {
        //         if (_currBunPathIDs[i] == _lastPathID)
        //         {
        //             return i;
        //         }
        //     }
        //     return -1;
        // }

        // private int StayInRange(int input)
        // {
        //     if (input < 0)
        //         return _currBunPathIDs.Count - 1;
        //     else if (input >= _currBunPathIDs.Count)
        //         return 0;
        //     return input;
        // }
    }
}