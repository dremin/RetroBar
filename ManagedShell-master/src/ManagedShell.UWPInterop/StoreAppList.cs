using System.Collections;
using System.Collections.Generic;

namespace ManagedShell.UWPInterop
{
    public class StoreAppList : IEnumerable<StoreApp>
    {
        private List<StoreApp> _appList = new List<StoreApp>();

        public void FetchApps()
        {
            _appList.Clear();
            _appList.AddRange(StoreAppHelper.GetStoreApps());
        }

        public StoreApp GetAppByAumid(string appUserModelId)
        {
            // first attempt to get an app in our list already
            foreach (var storeApp in _appList)
            {
                if (storeApp.AppUserModelId == appUserModelId)
                {
                    return storeApp;
                }
            }

            // not in list, get from StoreAppHelper
            StoreApp app = StoreAppHelper.GetStoreApp(appUserModelId);

            if (app != null)
            {
                _appList.Add(app);
                return app;
            }

            // no app found for given AUMID
            return null;
        }

        public IEnumerator<StoreApp> GetEnumerator()
        {
            return ((IEnumerable<StoreApp>)_appList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_appList).GetEnumerator();
        }
    }
}
