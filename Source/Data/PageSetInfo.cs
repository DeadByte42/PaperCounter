using System.Drawing;
using System.Windows.Media.Media3D;

namespace PaperCounter
{
    public class PageSetInfo
    {
        //❚↕⇕  ▬↔⇔
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string FormatName { get; private set; }
        public FormatOrientation Orientation { get; private set; }
        public FormatType FormatType { get; private set; }

        public static bool IsCountMiscOrientation { get; set; }
        public static bool IsCountMiscExact { get; set; }


        public PageSetInfo(PageFormatInfo format)
        {
            FormatType = format.FormatType;
            if (format.FormatType != FormatType.Custom || IsCountMiscExact)
                FormatName = format.Name;
            if (IsCountMiscOrientation)
                Orientation = format.Orientation;
            else Orientation = FormatOrientation.None;

            Name = (FormatName == null ? "*" : FormatName) + (Orientation == FormatOrientation.Vertical ? " (В)" : Orientation == FormatOrientation.Horizontal ? " (Г)" : "");
            Description = FormatType == FormatType.Primary ? "Основной формат по ГОСТ 2.301-68" : FormatType == FormatType.Secondary ? "Дополнительный формат по ГОСТ 2.301-68" : "Лист нестандартного размера";
            Description += ", " + (FormatType == FormatType.Custom && !IsCountMiscExact ? ", любые размеры" : format.Size);
            if (IsCountMiscOrientation)
                Description += Orientation == FormatOrientation.Vertical ? " (вертикальный)" : Orientation == FormatOrientation.Horizontal ? " (горизонтальный)" : "";
            if (FormatType != FormatType.Custom || IsCountMiscExact)
                Description += $", площадь эквивалентна {format.SizeMult:0.##} л.А4";
        }

        public bool IsMatch(PageFormatInfo format)
        {
            if (FormatType != format.FormatType)
                return false;
            if (Orientation!=FormatOrientation.None && Orientation!=format.Orientation)
                return false;
            if ((format.FormatType != FormatType.Custom || IsCountMiscExact)&& FormatName != format.Name)
                return false;

            return true;
        }
    }
}
