using System.IO;
using UnityEngine;
using TMPro;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc.AppCore;

namespace UABS.Assets.Script.View
{
    public class SortScrollViewButton : MonoBehaviour, IAppEnvironment, IAppEventListener
    {
        [SerializeField]
        private SortByType _sortByType;

        [SerializeField]
        private TextMeshProUGUI _text;

        private SortOrder? _currentOrder = null;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void ClickButton()
        {
            SortOrder nextOrder = GetNextOrder();
            AppEnvironment.Dispatcher.Dispatch(new SortScrollViewEvent(new()
            {
                sortByType = _sortByType,
                sortOrder = nextOrder
            }));
            _text.text = GetNextText(nextOrder);
            _currentOrder = nextOrder;
        }

        private string GetNextText(SortOrder nextOrder)
        {
            // Type + next
            string nextOrderString = "";
            if (nextOrder == SortOrder.Up)
                nextOrderString = "▼";
            else if (nextOrder == SortOrder.Down)
                nextOrderString = "▲";
            return $"{_sortByType} {nextOrderString}";
        }

        private SortOrder GetNextOrder()
        {
            if (_currentOrder == null)
            {
                return SortOrder.Down;
            }
            SortOrder currOrdrer = (SortOrder)_currentOrder;
            if (currOrdrer == SortOrder.Up)
            {
                return SortOrder.Down;
            }
            else if (currOrdrer == SortOrder.Down)
            {
                return SortOrder.Up;
            }
            return SortOrder.Down;
        }

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is SortScrollViewEvent ssve)
            {
                if (_sortByType != ssve.SortProp.sortByType)
                {
                    _text.text = GetNextText(SortOrder.None);
                }
            }
            else if (e is FolderReadEvent fre)
            {
                if (Directory.Exists(fre.FolderPath))
                {
                    _text.text = GetNextText(SortOrder.None);
                }
            }
        }
    }
}