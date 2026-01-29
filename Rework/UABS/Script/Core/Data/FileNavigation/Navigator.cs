using System.Collections.Generic;
using UABS.Util;

namespace UABS.Data
{
    public sealed class Navigator
    {
        private readonly List<Location> _history = new();
        private int _currentIndex = -1;

        public Location? Current =>
            _currentIndex >= 0 ? _history[_currentIndex] : null;

        public void Push(Location location)
        {
            // If user navigates after Back → discard forward history
            if (_currentIndex < _history.Count - 1)
            {
                _history.RemoveRange(
                    _currentIndex + 1,
                    _history.Count - _currentIndex - 1);
            }

            _history.Add(location);
            _currentIndex = _history.Count - 1;
        }

        public void Back()
        {
            if (_currentIndex > 0)
                _currentIndex--;
        }

        public void PrintStack()
        {
            Log.Info("Navigation Stack:");
            for (int i = 0; i < _history.Count; i++)
            {
                var marker = i == _currentIndex ? "->" : "  ";
                Log.Info($"{marker} {_history[i].Node} ({_history[i].Kind})");
            }
            Log.Info("");
        }
    }
}