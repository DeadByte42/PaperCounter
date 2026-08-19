using DB42.Lib.WPF;
using PaperCounter.Utility;
using System.Collections.ObjectModel;
using static System.Windows.Forms.DataFormats;

namespace PaperCounter
{
    public class Document : VMBase
    {

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }


        private string _path;
        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPropertyChanged();
            }
        }

        public bool IsError { get; private set; }
        public bool IsTotal { get; private set; }

        public List<PageFormat> RawPages = new();
        //public Dictionary<string, PageSet> Pages { get; private set; } = new();
        public ObservableCollection<PageSet> Pages { get; private set; } = new();


        //public int Count => Pages.Sum(a => a.Value.Count);
        //public double CountA4 => Pages.Sum(a => a.Value.CountA4);
        public int Count => Pages.Sum(a => a.Count);
        public double CountA4 => Pages.Sum(a => a.CountA4);

        public Document(List<string> path, bool isError = false)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path.Last());
            Path = path[0];
            for (int i=1;i<path.Count;i++)
            Path += "\n"+new string(' ',(i-1)*4)+" ↳ " + path[i];
            if (isError)
            {
                IsError = true;
                Path = "Ошибка открытия:\n" + Path;
            }
        }
        public Document(int count) {
            Name = $"Все документы ({count})";
            IsTotal = true;
        }

        public void Update()
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(CountA4));
        }
    }
}
