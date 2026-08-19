using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static System.Windows.Forms.DataFormats;

namespace PaperCounter
{
    public class PageDefinition
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public double ShortSide { get; private set; }
        public double LongSide { get; private set; }
        public double SizeMult { get; private set; }
        public FormatType FormatType { get; private set; }

        public static List<PageDefinition> CommonFormats => new() {
            new("A4",210,297,1,FormatType.Primary),
            new("A4х3",297,630,3, FormatType.Secondary),
            new("A4х4",297,841,4, FormatType.Secondary),
            new("A4х5",297,1051,5,FormatType.Secondary),
            new("A4х6",297,1261,6,FormatType.Secondary),
            new("A4х7",297,1471,7,FormatType.Secondary),
            new("A4х8",297,1682,8,FormatType.Secondary),
            new("A4х9",297,1892,9,FormatType.Secondary),

            new("A3",297,420,2,FormatType.Primary),
            new("A3х3",420,891,6,   FormatType.Secondary),
            new("A3х4",420,1189,8,  FormatType.Secondary),
            new("A3х5",420,1486,10, FormatType.Secondary),
            new("A3х6",420,1783,12, FormatType.Secondary),
            new("A3х7",420,2080,14, FormatType.Secondary),

            new("A2",420,594,4,FormatType.Primary),
            new("A2х3",594,1261,12, FormatType.Secondary),
            new("A2х4",594,1682,16, FormatType.Secondary),
            new("A2х5",594,2102,20, FormatType.Secondary),

            new("A1",594,841,8,FormatType.Primary),
            new("A1х3",841,1783,24, FormatType.Secondary),
            new("A1х4",841,2378,32, FormatType.Secondary),

            new("A0",841,1189,16,FormatType.Primary),
            new("A0х2",1189,1682,32, FormatType.Secondary),
            new("A0х3",1189,2523,48, FormatType.Secondary)
        };

        private PageDefinition(string name, double side1, double side2, double sizeMult, FormatType type)
        {
            Name = name;
            ShortSide = Math.Min(side1, side2);
            LongSide = Math.Max(side1, side2);

            SizeMult = sizeMult;
            FormatType = type;
            Description = @$"{(type == FormatType.Primary ? "Основной" : "Дополнительный")} формат по ГОСТ 2.301-68, размер {ShortSide}x{LongSide}, площадь эквивалентна {SizeMult:0} л.А4";
        }


        public bool IsMatch(double width, double height)
        {
            if (Math.Abs(Math.Max(width, height) - LongSide) > (LongSide <= 150 ? 1.5 : (LongSide <= 600 ? 2 : 3))) return false;
            if (Math.Abs(Math.Min(width, height) - ShortSide) > (ShortSide <= 150 ? 1.5 : (LongSide <= 600 ? 2 : 3))) return false;
            return true;
        }

    }
}
