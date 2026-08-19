namespace PaperCounter
{
    public class PageFormat
    {
        public PageFormatInfo Info { get; private set; }
        public int Count { get; set; } = 0;
        public double CountA4 => Count * Info.SizeMult;

        public PageFormat(PageFormatInfo format, int count =0)
        {
            Info = format;
            Count = count;
        }
    }
}
