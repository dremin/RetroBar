using System;
using System.Collections.Generic;
using System.Windows;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Data;
using System.Threading.Tasks;
using ManagedShell.Common.Logging;
using ManagedShell.Common.Common;
using ManagedShell.Common.SupportingClasses;

namespace ManagedShell.Common.Helpers
{
    public class SearchHelper : DependencyObject
    {
        private static object searchLock = new object();
        private static int QueryNum;
        private static string SearchString;

        const int MAX_RESULT = 8;

        const string CONNECTION_STRING =
            "provider=Search.CollatorDSO.1;EXTENDED PROPERTIES=\"Application=Windows\"";

        static SearchHelper()
        {
            m_results = new ThreadSafeObservableCollection<SearchResult>();
            m_resultsReadOnly = new ReadOnlyObservableCollection<SearchResult>(m_results);
        }

        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register("SearchText",
            typeof(string), typeof(SearchHelper), new UIPropertyMetadata(default(string),
                new PropertyChangedCallback(OnSearchTextChanged)));

        static void OnSearchTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SearchString = e.NewValue.ToString();

            m_results.Clear();
            IncrementQueryNum();

            if (SearchString.Length > 0)
            {
                Task.Run(DoSearch);
            }
        }
        
        static void IncrementQueryNum()
        {
            if (QueryNum < int.MaxValue)
            {
                QueryNum++;
            }
            else
            {
                QueryNum = 0;
            }
        }

        static void DoSearch()
        {
            int localQueryNum = QueryNum;

            // check if user wants to show file extensions. always show on windows < 8 due to property missing
            string displayNameColumn = "System.ItemNameDisplayWithoutExtension";
            if (ShellHelper.GetFileExtensionsVisible() || !EnvironmentHelper.IsWindows8OrBetter)
                displayNameColumn = "System.ItemNameDisplay";

            string query;
            OleDbConnection cConnection;
            List<SearchResult> results = new List<SearchResult>();

            if (EnvironmentHelper.IsWindows81OrBetter)
            {
                // Some additional columns that we should use were added in later Windows versions
                query =
                    $@"SELECT TOP {MAX_RESULT} ""{displayNameColumn}"", ""System.ItemUrl"", ""System.ItemPathDisplay"", ""System.DateModified"", ""System.Search.Rank""
                    FROM ""SYSTEMINDEX""
                    WHERE WITH(System.ItemNameDisplay, System.ItemAuthors, System.Keywords, System.HighKeywords, System.MediumKeywords, System.Music.AlbumTitle, System.Title, System.Music.Genre, System.Message.FromName, System.Subject, System.Contact.FullName) AS #MRProps
                    (System.Shell.OmitFromView != 'TRUE'
                    AND ((System.ItemNameDisplayWithoutExtension = '{SearchString}' AND System.ItemType = '.lnk') RANK BY COERCION(ABSOLUTE, 1000)
                    OR (System.ItemNameDisplay LIKE '{SearchString}%' AND System.ItemType = '.lnk') RANK BY COERCION(ABSOLUTE, 999)
                    OR (System.ItemNameDisplay LIKE '%{SearchString}%' AND System.ItemType = '.lnk') RANK BY COERCION(ABSOLUTE, 997)
                    OR System.ItemNameDisplay = '{SearchString}' RANK BY COERCION(ABSOLUTE, 998)
                    OR System.ItemNameDisplay LIKE '{SearchString}%' RANK BY COERCION(ABSOLUTE, 996)
                    OR System.HighKeywords = SOME ARRAY['{SearchString}'] RANK BY COERCION(MULTIPLY, 0.9)
                    OR System.MediumKeywords = SOME ARRAY['{SearchString}'] RANK BY COERCION(MULTIPLY, 0.9)
                    OR System.Title = '{SearchString}' RANK BY COERCION(MULTIPLY, 0.9)
                    OR System.Subject = '{SearchString}' RANK BY COERCION(MULTIPLY, 0.9)
                    OR CONTAINS(#MRProps,'""{SearchString}""') RANK BY COERCION(MULTIPLY, 0.9)
                    OR CONTAINS(#MRProps,'""{SearchString}*""') RANK BY COERCION(MULTIPLY, 0.9)
                    OR CONTAINS(*, '""{SearchString}""') RANK BY COERCION(MULTIPLY, 0.9)
                    OR CONTAINS(*, '""{SearchString}*""') RANK BY COERCION(MULTIPLY, 0.9)
                    OR FREETEXT(#MRProps, '""{SearchString}""') RANK BY COERCION(MULTIPLY, 0.9)))
                    ORDER BY System.Search.Rank desc";
            }
            else
            {
                query = 
                    $@"SELECT TOP {MAX_RESULT} ""{displayNameColumn}"", ""System.ItemUrl"", ""System.ItemPathDisplay"", ""System.DateModified"", ""System.Search.Rank""
                    FROM ""SYSTEMINDEX""
                    WHERE WITH(System.ItemNameDisplay, System.ItemAuthors, System.Keywords, System.Music.AlbumTitle, System.Title, System.Music.Genre, System.Message.FromName, System.Subject, System.Contact.FullName) AS #MRProps
                    (System.Shell.OmitFromView != 'TRUE'
                    AND ((System.ItemNameDisplay LIKE '{SearchString}%' AND System.ItemType = '.lnk') RANK BY COERCION(ABSOLUTE, 999)
                    OR (System.ItemNameDisplay LIKE '%{SearchString}%' AND System.ItemType = '.lnk') RANK BY COERCION(ABSOLUTE, 997)
                    OR System.ItemNameDisplay = '{SearchString}' RANK BY COERCION(ABSOLUTE, 998)
                    OR System.ItemNameDisplay LIKE '{SearchString}%' RANK BY COERCION(ABSOLUTE, 996)
                    OR System.Title = '{SearchString}' RANK BY COERCION(MULTIPLY, 0.9)
                    OR System.Subject = '{SearchString}' RANK BY COERCION(MULTIPLY, 0.9)
                    OR CONTAINS(#MRProps,'""{SearchString}""') RANK BY COERCION(MULTIPLY, 0.9)
                    OR CONTAINS(#MRProps,'""{SearchString}*""') RANK BY COERCION(MULTIPLY, 0.9)
                    OR CONTAINS(*, '""{SearchString}""') RANK BY COERCION(MULTIPLY, 0.9)
                    OR CONTAINS(*, '""{SearchString}*""') RANK BY COERCION(MULTIPLY, 0.9)
                    OR FREETEXT(#MRProps, '""{SearchString}""') RANK BY COERCION(MULTIPLY, 0.9)))
                    ORDER BY System.Search.Rank desc";
            }

            try
            {
                using (cConnection = new OleDbConnection(
                    CONNECTION_STRING))
                {
                    cConnection.Open();
                    using (OleDbCommand cmd = new OleDbCommand(
                        query,
                        cConnection))
                    {
                        if (cConnection.State == ConnectionState.Open)
                        {
                            using (OleDbDataReader reader = cmd.ExecuteReader())
                            {
                                while (!reader.IsClosed && reader.Read() && QueryNum == localQueryNum)
                                {
                                    SearchResult result = BuildSearchResult(reader);
                                    results.Add(result);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShellLogger.Error("Error in doSearch.", ex);
            }

            lock (searchLock)
            {
                if (QueryNum == localQueryNum)
                {
                    m_results.Clear();

                    foreach (var result in results)
                    {
                        if (QueryNum != localQueryNum)
                        {
                            break;
                        }
                        
                        m_results.Add(result);
                    }

                    if (QueryNum != localQueryNum)
                    {
                        m_results.Clear();
                    }
                }
            }
        }

        static SearchResult BuildSearchResult(OleDbDataReader reader)
        {
            SearchResult result = new SearchResult
            {
                Name = reader[0].ToString(),
                Path = reader[1].ToString(),
                PathDisplay = reader[2].ToString(),
                DateModified = reader[3].ToString()
            };

            if (result.Name.EndsWith(".lnk"))
                result.Name =
                    result.Name.Substring(0,
                        result.Name.Length -
                        4); // Windows always hides this regardless of setting, so do it

            return result;
        }

        public string SearchText
        {
            get { return GetValue(SearchTextProperty).ToString(); }
            set { SetValue(SearchTextProperty, value); }
        }

        static ThreadSafeObservableCollection<SearchResult> m_results;
        static ReadOnlyObservableCollection<SearchResult> m_resultsReadOnly;

        public ReadOnlyObservableCollection<SearchResult> Results
        {
            get { return m_resultsReadOnly; }
        }
    }
}