
namespace PaperCounter
{
    public class PageFormatInfo
    {
        public string Name { get; private set; }
        public string Size { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }
        public double SizeMult { get; private set; }
        public FormatType FormatType { get; private set; }
        public FormatOrientation Orientation => (Width > Height) ? FormatOrientation.Horizontal : (Height > Width) ? FormatOrientation.Vertical : FormatOrientation.Square;

        public PageFormatInfo(PageDefinition definition, bool vertical)
        {
            Width = vertical ? definition.ShortSide : definition.LongSide;
            Height = vertical ? definition.LongSide : definition.ShortSide;
            Name = definition.Name;
            Size = (int)Math.Max(Width, Height) + "x" + (int)Math.Min(Width, Height);
            SizeMult = definition.SizeMult;
            FormatType = definition.FormatType;
        }

        public PageFormatInfo(double width, double height)
        {
            Width = width;
            Height = height;
            Name = (int)Math.Max(Width, Height) + "x" + (int)Math.Min(Width, Height);
            Size = Name;
            SizeMult = width * height / 297 / 210;
            FormatType = FormatType.Custom;
        }

        public bool IsMatch(double width, double height)
        {
            if (Width == Height)
                return width == Width && height == Height;
            if (Math.Abs(width - Width) > (Width <= 150 ? 1.5 : (Width <= 600 ? 2 : 3)))
                return false;
            if (Math.Abs(height - Height) > (Height <= 150 ? 1.5 : (Height <= 600 ? 2 : 3)))
                return false;

            return true;
        }
    }
}
