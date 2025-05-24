using System.Collections.Generic;
using FileCollector.Models;

namespace FileCollector.Services
{
    public class NavigationStateService
    {
        private NavigationState? _persistedNavigationState;

        public void SetPersistedState(string? rootPath, List<string> selectedFilePaths)
        {
            if (!string.IsNullOrEmpty(rootPath))
            {
                _persistedNavigationState = new NavigationState
                {
                    RootPath = rootPath,
                    SelectedFilePaths = new List<string>(selectedFilePaths)
                };
            }
            else
            {
                _persistedNavigationState = null;
            }
        }

        public NavigationState? ConsumePersistedState()
        {
            var stateToReturn = _persistedNavigationState;
            _persistedNavigationState = null;
            return stateToReturn;
        }
    }
}