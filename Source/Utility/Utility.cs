using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;

namespace PaperCounter.Utility
{
    public class NaturalStringComparer : IComparer<string>
    {
        // Native Windows API for logical string comparison
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string x, string y);

        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            return StrCmpLogicalW(x, y);
        }
    }

    public static class Utility
	{
		public static int OrderedPasteIndex<T>(T item, IList<T> insertList, IList<T> orderList) where T : class
		{
			var order = orderList.IndexOf(item);
			for (int i = 0; i < insertList.Count; i++)
			{
				if (insertList[i] == orderList[order - 1])
					return i + 1;
			}
			return insertList.Count;
		}
		public static int OrderedPasteIndex<T, T1>(T item, IList<T1> insertList, IList<T> orderList, Func<T1, T> selector) where T : class
		{
			if (insertList.Count == 0) return 0;
			var order = orderList.IndexOf(item);
			for (int i = 0; i < insertList.Count; i++)
			{
				var idx = orderList.IndexOf(selector(insertList[i]));
				if (idx > order)
					return i;
			}
			return insertList.Count;
		}
		public static void FilesRecursive(string[] paths, Action<string> action)
		{
			foreach (var path in paths)
			{
				if (File.Exists(path)) action?.Invoke(path);
				else if (Directory.Exists(path)) FilesRecursive(Directory.GetFileSystemEntries(path), action);
			}
		}
        public static int GetInsertionIndex<T>(T item, IList<T> insertList, IList<T> orderList) where T : class
        {
            if (insertList == null || orderList == null || item == null)
                return 0;

            // Находим позицию вставляемого элемента в orderList
            int itemIndexInOrder = orderList.IndexOf(item);
            if (itemIndexInOrder == -1)
                return insertList.Count; // Если элемента нет в orderList, вставляем в конец

            // Ищем в insertList ближайший предыдущий элемент из orderList
            int targetIndex = 0;
            int bestOrderIndex = -1;

            for (int i = 0; i < insertList.Count; i++)
            {
                var existingItem = insertList[i];
                int existingOrderIndex = orderList.IndexOf(existingItem);

                if (existingOrderIndex != -1 && existingOrderIndex < itemIndexInOrder)
                {
                    // Нашли элемент, который должен быть перед вставляемым
                    if (existingOrderIndex > bestOrderIndex)
                    {
                        bestOrderIndex = existingOrderIndex;
                        targetIndex = i + 1;
                    }
                }
            }

            return targetIndex;
        }
        public static int GetInsertionIndex<T, T1>(T item, IList<T1> insertList, IList<T> orderList, Func<T1, T> selector) where T : class
        {
            if (insertList == null || orderList == null || item == null)
                return 0;

            // Находим позицию вставляемого элемента в orderList
            int itemIndexInOrder = orderList.IndexOf(item);
            if (itemIndexInOrder == -1)
                return insertList.Count; // Если элемента нет в orderList, вставляем в конец

            // Ищем в insertList ближайший предыдущий элемент из orderList
            int targetIndex = 0;
            int bestOrderIndex = -1;

            for (int i = 0; i < insertList.Count; i++)
            {
                var existingItem = insertList[i];
                int existingOrderIndex = orderList.IndexOf(selector(existingItem));

                if (existingOrderIndex != -1 && existingOrderIndex < itemIndexInOrder)
                {
                    // Нашли элемент, который должен быть перед вставляемым
                    if (existingOrderIndex > bestOrderIndex)
                    {
                        bestOrderIndex = existingOrderIndex;
                        targetIndex = i + 1;
                    }
                }
            }

            return targetIndex;
        }

        public static string GetCommonPrefix(IEnumerable<string> strings)
        {
            if (strings == null || !strings.Any()) return string.Empty;

            // Use the first string as a baseline
            string first = strings.First();
            int minLength = strings.Min(s => s.Length);

            for (int i = 0; i < minLength; i++)
            {
                // Check if all strings have the same character at current index
                if (strings.Any(s => s[i] != first[i]))
                {
                    return first.Substring(0, i);
                }
            }

            return first.Substring(0, minLength);
        }

        public static IEnumerable<string> GetFilesRecursive(string[] paths, string pattern="*")
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    yield return path;
                }
                else if (Directory.Exists(path))
                {
                    foreach (var file in Directory.EnumerateFiles(path,"*",SearchOption.AllDirectories))
                    {
                        yield return file;
                    }
                }
            }
        }

        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            // Guard check for safe execution
            if (collection == null || comparison == null) return;

            // Copy elements to a sortable temporary list
            List<T> sortedList = new List<T>(collection);

            // Use the native optimized List sort mechanism
            sortedList.Sort(comparison);

            // Rearrange items in place using Move to trigger correct CollectionChanged notifications
            for (int newIndex = 0; newIndex < sortedList.Count; newIndex++)
            {
                int oldIndex = collection.IndexOf(sortedList[newIndex]);

                if (oldIndex != newIndex)
                {
                    collection.Move(oldIndex, newIndex);
                }
            }
        }
    }
}
