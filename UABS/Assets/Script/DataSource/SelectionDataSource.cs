using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.DataSource
{
    public class SelectionDataSource : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        private long _lastPathID;
        private List<long> _currBunPathIDs;
        private HashSet<long> _selections;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _selections = new();
        }

        public void OnEvent(AppEvent e)
        {
            if (e is AssetSelectionEvent ase)
            {
                if (!ase.IsHoldingShift)
                {
                    _selections = new();
                }
                long currPathID = ase.PathID;
                if (_selections.Contains(currPathID))
                {
                    _selections.Remove(currPathID);
                }
                else
                {
                    _selections.Add(currPathID);
                }
                _lastPathID = currPathID;
                AppEnvironment.Dispatcher.Dispatch(new AssetMultiSelectionEvent(_selections));
            }
            else if (e is GoBundleViewEvent gbve)
            {
                _currBunPathIDs = gbve.EntryInfos.Select(x => x.assetEntryInfo.pathID).ToList();
            }
        }

        public void Prev()
        {
            if (_currBunPathIDs.Count == 0)
            {
                Debug.Log("This bundle has no item.");
                return;
            }
            int index = StayInRange(FindIndexOfLastPathID() - 1);
            AppEnvironment.Dispatcher.Dispatch(new AssetSelectionEvent(_currBunPathIDs[index], index, _currBunPathIDs.Count, true));
        }

        public void Next()
        {
            if (_currBunPathIDs.Count == 0)
            {
                Debug.Log("This bundle has no item.");
                return;
            }
            int index = StayInRange(FindIndexOfLastPathID() + 1);
            AppEnvironment.Dispatcher.Dispatch(new AssetSelectionEvent(_currBunPathIDs[index], index, _currBunPathIDs.Count, true));
        }

        private int FindIndexOfLastPathID()
        {
            if (_currBunPathIDs == null)
            {
                return -1;
            }
            for (int i = 0; i < _currBunPathIDs.Count; i++)
            {
                if (_currBunPathIDs[i] == _lastPathID)
                {
                    return i;
                }
            }
            return -1;
        }

        private int StayInRange(int input)
        {
            if (input < 0)
                return _currBunPathIDs.Count - 1;
            else if (input >= _currBunPathIDs.Count)
                return 0;
            return input;
        }
    }
}