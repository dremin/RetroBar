using ManagedShell.WindowsTasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    internal class ProgramNameTaskbarSorter : IComparer
    {
        private List<string> programs = null;
        private bool alphabeticSorting = Settings.Instance.SortTaskbarByProgramName;

        public void setSortingList(String list)
        {
            if (!String.IsNullOrEmpty(list) && list.Length > 3 && list.Contains(","))
            {
                string[] parts = list.Split(',');
                parts = parts.Select(p => p.Trim()).ToArray();

                programs = new List<string>(parts);
            }
        }

        public int Compare(object x, object y)
        {
            var winA = x as ApplicationWindow;
            var winB = y as ApplicationWindow;
            if (winA == null || winB == null) return 0;

            if (programs != null)
            {
                int wantedIndexA = programs.FindIndex(p => winA.WinFileName != null && p != null && winA.WinFileName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
                int wantedIndexB = programs.FindIndex(p => winB.WinFileName != null && p != null && winB.WinFileName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);

                if (wantedIndexA == -1 && wantedIndexB == -1) { }
                else if (wantedIndexA == -1) { return 1; }
                else if (wantedIndexB == -1) { return -1; }
                else if (wantedIndexA < wantedIndexB) { return -1; }
                else if (wantedIndexA > wantedIndexB) { return 1; }
            }

            if (!alphabeticSorting) { return 1; }

            // Use WinFileName as a substitute for ProcessName
            int cmp = string.Compare(winA.WinFileName, winB.WinFileName, StringComparison.OrdinalIgnoreCase);
            System.Diagnostics.Debug.WriteLine("wina: " + winA.WinFileName + "winb: " + winB.WinFileName + "ret cmp: " + cmp);
            if (cmp != 0) return cmp;

            // Fallback: sort by window title
            return string.Compare(winA.Title, winB.Title, StringComparison.OrdinalIgnoreCase);
        }
    }
}
