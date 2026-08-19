using DB42.Lib.WPF;

namespace PaperCounter
{
    public class PageSet:VMBase
    {
        public static bool IsCountMiscA4 { get; set; }

        public PageSetInfo Info { get; private set; }
        private List<PageFormat> _formats = new();

        private int _count;
        public int Count
        {
            get => _count;
            private set
            {
                _count = value;
                OnPropertyChanged();
            }
        }

        private double _countA4;
        public double CountA4
        {
            get => _countA4;
            private set
            {
                _countA4 = value;
                OnPropertyChanged();
            }
        }

        public PageSet(PageSetInfo info) {
            Info = info;
        }

        public void AddFormat(PageFormat format)
        {
            _formats.Add(format);
        }


        public void Update()
        {
            Count = _formats.Sum(a => a.Count);
            CountA4 = _formats.Sum(a => (!IsCountMiscA4 && a.Info.FormatType == FormatType.Custom) ? 0 : a.CountA4);
        }
    }
}
