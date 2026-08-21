using Aspose.Email.Foss.Msg;
using PDFiumSharp;
using PDFiumSharp.Types;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Linq;

namespace PaperCounter.Utility
{
    public static class DocumentLoader
    {
        public class RawPages
        {
            public double Width;
            public double Height;
            public int Count;

            public RawPages(double width, double height) {
                Width = width;
                Height = height;
                Count = 1;
            }
        }
        public class RawDocument
        {
            public readonly List<string> Path;
            public readonly bool IsError;
            public readonly List<RawPages> Pages;

            public RawDocument(List<string> path, bool isError=false)
            {
                Path = path;
                IsError = isError;
                Pages = new List<RawPages>();
            }
        }

        public static readonly string[] extArch = { ".rar", ".zip", ".7z", ".tar", ".gz", ".bz2", ".lz", ".xz", ".tgz", ".tbz2", ".txz", ".zst", ".lzw", ".ace", ".arc", ".arj" };
        public static readonly string[] extMail = { ".msg", ".eml" };
        public static bool LoadArchives { get; set; } = true;
        public static bool LoadEmails { get; set; } = true;
        private static List<RawDocument> _loaded;

        public static bool CanLoad(string path)
        {
            var ext = Path.GetExtension(path).ToLower();

            return ext == ".pdf"
                || LoadArchives && extArch.Contains(ext)
                || LoadEmails && extMail.Contains(ext);
        }

        public static List<RawDocument> Load(string path)
        {
            _loaded = new();

            using (Stream stream = File.OpenRead(path))
                LoadFromStream(stream,new List<string>() { path });

            fileread = null;
            return _loaded;
        }

        private static void LoadFromStream(Stream stream, List<string> path)
        {
            var ext = Path.GetExtension(path.Last()).ToLower();
            if (ext==".pdf")
                LoadPdf(stream, path);
            else if (LoadEmails&&ext==".msg")
                LoadMsg(stream, path);
            else if (LoadEmails && ext == ".eml")
                LoadEml(stream, path);
            else if (LoadArchives && extArch.Contains(ext))
                LoadArchive(stream, path);
        }

        private static FPDF_FILEREAD fileread;
        private static void LoadPdf(Stream stream, List<string> path)
        {
            try
            {
                fileread=FPDF_FILEREAD.FromStream(stream);
                using (PdfDocument pdf = new PdfDocument(stream, fileread))
                {
                    RawDocument doc = new(path);
                    foreach (var page in pdf.Pages)
                    {
                        var w = Math.Round(page.Width * 3.528) / 10;
                        var h = Math.Round(page.Height * 3.528) / 10;
                        var pg = doc.Pages.Find(p => p.Width == w && p.Height == h);
                        if (pg == null)
                            doc.Pages.Add(new RawPages(w, h));
                        else
                            pg.Count++;
                    }
                    pdf.Close();
                    _loaded.Add(doc);
                }
            }
            catch
            {
                _loaded.Add(new RawDocument(path, true));
            }
        }

        private static void LoadArchive(Stream stream, List<string> path)
        {
            using (var archive = ArchiveFactory.OpenArchive(stream))
                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory&&CanLoad(e.Key)))
                        using (var es = entry.OpenEntryStream())
                        using (var ms = new MemoryStream())
                        {
                            es.CopyTo(ms);
                            ms.Position = 0;
                            LoadFromStream(ms, new List<string> (path){ entry.Key });
                        }
        }

        private static void LoadMsg(Stream stream, List<string> path)
        {
            using (var message = MapiMessage.FromStream(stream))
                foreach (var att in message.Attachments)
                    if (CanLoad(att.Filename))
                    {
                        LoadFromStream(att.OpenRead(), new List<string>(path){ att.Filename});
                    }
        }

        private static void LoadEml(Stream stream, List<string> path)
        {
            using (var message = MapiMessage.LoadFromEml(stream))
                foreach (var att in message.Attachments)
                    if (CanLoad(att.Filename))
                    {
                        LoadFromStream(att.OpenRead(), new List<string>(path) { att.Filename });
                    }
        }
    }
}
