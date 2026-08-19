using DB42.Lib.WPF;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using PaperCounter.Utility;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace PaperCounter
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class VM : VMBase
    {
        //=====================
        //  Init
        //=====================
        #region Init
        public VM()
        {
            CommonFormats = PageDefinition.CommonFormats;
            Documents = new();
            TotalDoc = new Document(0);

            SelectFilesCommand = new RelayCommand((a) => AddFilesDialog());
            SelectFoldersCommand = new RelayCommand((a) => AddFoldersDialog());

            Queue = new(ProcessDocument, AddDocuments);
            CancelProcessingCommand = new RelayCommand((a) => Queue.RequestCancel());


            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                _shellRegistrationManager = new Utility.ShellRegistrationManager(Environment.ProcessPath);
                _shellRegistrationManager.ForceServerRegistration();
                switch (_shellRegistrationManager.IsRegisteredForExts(".pdf"))
                {
                    case Utility.RegState.Corrupted: IsContextMenuPDF = true; break;
                    case Utility.RegState.Enabled: _isContextMenuPDF = true; break;
                }
                switch (_shellRegistrationManager.IsRegisteredForExts(DocumentLoader.extMail))
                {
                    case Utility.RegState.Corrupted: IsContextMenuMail = true; break;
                    case Utility.RegState.Enabled: _isContextMenuMail = true; break;
                }
                switch (_shellRegistrationManager.IsRegisteredForExts(DocumentLoader.extArch))
                {
                    case Utility.RegState.Corrupted: IsContextMenuArch = true; break;
                    case Utility.RegState.Enabled: _isContextMenuArch = true; break;
                }
                switch (_shellRegistrationManager.IsRegisteredForFolders)
                {
                    case Utility.RegState.Corrupted: IsContextMenuFolders = true; break;
                    case Utility.RegState.Enabled: _isContextMenuFolders = true; break;
                }
            }
        }
        #endregion



        //=====================
        //  Queue
        //=====================
        #region Queue
        public QueueManager<string, List<DocumentLoader.RawDocument>> Queue { get; private set; }
        public ICommand CancelProcessingCommand { get; }

        public async Task<List<DocumentLoader.RawDocument>> ProcessDocument(string path) 
            => DocumentLoader.Load(path);

        public void EnqueueDocuments(string[] filedrop)
        {
            Queue.AddItems(Utility.Utility.GetFilesRecursive(filedrop).Where(a => DocumentLoader.CanLoad(a)));
        }
        #endregion



        //=====================
        //  Actions
        //=====================
        #region Actions
        public ICommand SelectFilesCommand { get; }
        public ICommand SelectFoldersCommand { get; }

        private void AddFilesDialog()
        {
            var dlg = new CommonOpenFileDialog("Добавить файлы")
            {
                Multiselect = true
            };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                EnqueueDocuments(dlg.FileNames.ToArray());
        }

        private void AddFoldersDialog()
        {
            var dlg = new CommonOpenFileDialog("Добавить папки")
            {
                Multiselect = true,
                IsFolderPicker = true
            };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                EnqueueDocuments(dlg.FileNames.ToArray());
        }

        public void CombineDocs(List<Document> documents)
        {
            documents.Remove(TotalDoc);

            if (documents == null || documents.Count < 2)
                return;

            CombineDocsData(documents[0].Name, documents);
        }
        private void CombineDocsData(string name, List<Document> documents)
        {
            var combo = documents[0];
            documents.Remove(combo);
            combo.Name = name;
            combo.Path = String.Join("\n", documents.Select(a => a.Path));

            documents.ForEach(a => Documents.Remove(a));
            foreach (var key in DBFormats.Keys.ToList())
            {
                for (int i = DBFormats[key].Count-1; i >=0 ; i--)
                {
                    Document srcDoc = DBFormats[key][i];
                    if (!documents.Contains(srcDoc))
                        continue;
                    var srcFmt = srcDoc.RawPages.FirstOrDefault(a => a.Info == key);
                    var trgFmt = combo.RawPages.FirstOrDefault(a => a.Info == key);
                    if (trgFmt == null)
                    {
                        combo.RawPages.Add(srcFmt);
                        DBFormats[key].Add(combo);
                    }
                    else {
                        trgFmt.Count += srcFmt.Count;
                    }
                    DBFormats[key].Remove(srcDoc);
                }
            }
            ResetDocument(combo);
            FormatsChanged?.Invoke(DBSets.Keys.OrderBy(p => p.FormatType).ThenBy(p => p.Name, new NaturalStringComparer()).ToList());
        }

        public void DeleteDocs(List<Document> documents)
        {
            if (documents.Contains(TotalDoc)||documents.Count==Documents.Count-1)
            {
                Documents.Clear();
                TotalDoc.RawPages.Clear();
                TotalDoc.Pages.Clear();
            }
            else
            {
                documents.ForEach(a =>Documents.Remove(a));
                foreach (var key in DBFormats.Keys.ToList()) {
                    DBFormats[key].RemoveAll(a => documents.Contains(a));
                    if (DBFormats[key].Count == 0)
                    {
                        TotalDoc.RawPages.RemoveAll(pg => pg.Info == key);
                        DBFormats.Remove(key);
                    }
                    else
                    {
                        var sub = TotalDoc.RawPages.FirstOrDefault(a => a.Info == key);
                        foreach (var doc in documents)
                            sub.Count -= doc.RawPages.FirstOrDefault(a => a.Info == key)?.Count ?? 0;
                    }
                }

                TotalDoc.Name = "Все документы (" + (Documents.Count() - 1) + ")";
            }
            ResetDocuments();
        }
        #endregion



        //============
        //  Settings
        //============
        #region Settings
        private ShellRegistrationManager _shellRegistrationManager;

        [JsonProperty]
        public bool IsLoadArchives
        {
            get => DocumentLoader.LoadArchives;
            set
            {
                DocumentLoader.LoadArchives = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty]
        public bool IsLoadMail
        {
            get => DocumentLoader.LoadEmails;
            set
            {
                DocumentLoader.LoadEmails = value;
                OnPropertyChanged();
            }
        }

        private bool _isCountMisc=true;
        [JsonProperty]
        public bool IsCountMisc
        {
            get => _isCountMisc;
            set
            {
                _isCountMisc = value;
                OnPropertyChanged();
                ResetDocuments();
            }
        }

        private bool _isCountMiscA4 = true;
        [JsonProperty]
        public bool IsCountMiscA4
        {
            get => _isCountMiscA4;
            set
            {
                _isCountMiscA4 = value;
                OnPropertyChanged();
                ResetDocuments();
            }
        }

        private bool _isCountMiscExact;
        [JsonProperty]
        public bool IsCountMiscExact
        {
            get => _isCountMiscExact;
            set
            {
                _isCountMiscExact = value;
                OnPropertyChanged();
                ResetDocuments();
            }
        }

        private bool _isCountMiscOrientation;
        [JsonProperty]
        public bool IsCountMiscOrientation
        {
            get => _isCountMiscOrientation;
            set
            {
                _isCountMiscOrientation = value;
                OnPropertyChanged();
                ResetDocuments();
            }
        }

        private bool _isContextMenuPDF;
        public bool IsContextMenuPDF
        {
            get => _isContextMenuPDF;
            set
            {
                _isContextMenuPDF = value;
                _shellRegistrationManager.RegisterForExts(_isContextMenuPDF,".pdf");
                OnPropertyChanged();
            }
        }

        private bool _isContextMenuMail;
        public bool IsContextMenuMail
        {
            get => _isContextMenuMail;
            set
            {
                _isContextMenuMail = value;
                _shellRegistrationManager.RegisterForExts(_isContextMenuMail, DocumentLoader.extMail);
                OnPropertyChanged();
            }
        }

        private bool _isContextMenuArch;
        public bool IsContextMenuArch
        {
            get => _isContextMenuArch;
            set
            {
                _isContextMenuArch = value;
                _shellRegistrationManager.RegisterForExts(_isContextMenuArch, DocumentLoader.extArch);
                OnPropertyChanged();
            }
        }

        private bool _isContextMenuFolders;
        public bool IsContextMenuFolders
        {
            get => _isContextMenuFolders;
            set
            {
                _isContextMenuFolders = value;
                _shellRegistrationManager.RegisterForFolders(_isContextMenuFolders);
                OnPropertyChanged();
            }
        }
        #endregion



        private readonly List<PageDefinition> CommonFormats;
        private readonly Dictionary<PageFormatInfo, List<Document>> DBFormats=new();
        private readonly Dictionary<PageSetInfo,List<PageFormatInfo> > DBSets=new();
        public ObservableCollection<Document> Documents { get; private set; }
        public ICollectionView DisplayCollection { get; private set; }

        private Document selectedDoc;
        public Document SelectedDoc
        {

        	get => selectedDoc;
        	set
        	{
        		selectedDoc = value;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
        		OnPropertyChanged();
        	}
        }

        private Document totalDoc;
        public Document TotalDoc
        {

            get => totalDoc;
            set
            {
                totalDoc = value;
                OnPropertyChanged();
            }
        }



        private void AddDocuments(List<DocumentLoader.RawDocument> docs)
        {
            Documents.Remove(TotalDoc);

            foreach (DocumentLoader.RawDocument rdoc in docs) {
                var doc = new Document(rdoc.Path, rdoc.IsError);

                foreach (var page in rdoc.Pages)
                {
                    PageFormatInfo size = DBFormats.Keys.FirstOrDefault(a => a.IsMatch(page.Width, page.Height));
                    if (size == null)
                    {
                        var def = CommonFormats.FirstOrDefault(a => a.IsMatch(page.Width, page.Height));
                        if (def != null)
                            size = new PageFormatInfo(def, page.Height > page.Width);
                        else
                            size = new PageFormatInfo(page.Width, page.Height);
                    }

                    if (!DBFormats.ContainsKey(size))
                        DBFormats.Add(size, new ());
                    if (!DBFormats[size].Contains(doc))
                        DBFormats[size].Add(doc);

                    var format = doc.RawPages.FirstOrDefault(a => a.Info == size);
                    if (format == null)
                    {
                        format = new PageFormat(size, page.Count);
                        doc.RawPages.Add(format);
                    }
                    else
                        format.Count += page.Count;

                    format = TotalDoc.RawPages.FirstOrDefault(a => a.Info == size);
                    if (format == null)
                    {
                        format = new PageFormat(size, page.Count);
                        TotalDoc.RawPages.Add(format);
                    }
                    else
                        format.Count += page.Count;
                }
                Documents.Add(doc);
                ResetDocument(doc);
                ResetDocument(TotalDoc);
                TotalDoc.Pages.Sort((a, b) => a.Info.FormatType < b.Info.FormatType ? -1 : a.Info.FormatType > b.Info.FormatType ? 1 : new NaturalStringComparer().Compare(a.Info.Name, b.Info.Name));

            }
            if (Documents.Count == 0)
                return;

            TotalDoc.Name="Все документы (" + Documents.Count + ")";
            Documents.Add(TotalDoc);
            FormatsChanged?.Invoke(DBSets.Keys.OrderBy(p => p.FormatType).ThenBy(p => p.Name, new NaturalStringComparer()).ToList());
        }

        private void ResetDocuments()
        {
            PageSet.IsCountMiscA4 = IsCountMiscA4;
            PageSetInfo.IsCountMiscExact = IsCountMiscExact;
            PageSetInfo.IsCountMiscOrientation = IsCountMiscOrientation;
            DBSets.Clear();

            foreach (var doc in Documents)
                ResetDocument(doc);


            if (SelectedDoc != null)
            {
                var sel = SelectedDoc;
                SelectedDoc = null;
                ResetDocument(sel);
                sel.Pages.Sort((a, b) => a.Info.FormatType < b.Info.FormatType ? -1 : a.Info.FormatType > b.Info.FormatType ? 1 : new NaturalStringComparer().Compare(a.Info.Name, b.Info.Name));
                SelectedDoc = sel;
            }
            FormatsChanged?.Invoke(DBSets.Keys.OrderBy(p => p.FormatType)    .ThenBy(p => p.Name, new NaturalStringComparer())    .ToList());
            TotalDoc.Pages.Sort((a, b) => a.Info.FormatType < b.Info.FormatType ? -1 : a.Info.FormatType > b.Info.FormatType ? 1 : new NaturalStringComparer().Compare(a.Info.Name, b.Info.Name));
        }
        private void ResetDocument(Document doc)
        {
            doc.Pages.Clear();

            foreach (var fm in doc.RawPages)
            {
                if (!IsCountMisc && fm.Info.FormatType == FormatType.Custom)
                    continue;

                var psi = DBSets.Keys.FirstOrDefault(a => a.IsMatch(fm.Info));
                if (psi == null)
                {
                    psi = new PageSetInfo(fm.Info);
                    DBSets.Add(psi, new List<PageFormatInfo>() );
                }
                DBSets[psi].Add(fm.Info);

                var pset = doc.Pages.FirstOrDefault(a => a.Info == psi);
                if (pset == null) {
                    pset = new PageSet(psi);
                    doc.Pages.Add(pset);
                }

                pset.AddFormat(fm);

                foreach (var ps in doc.Pages)
                    pset.Update();
            }

            doc.Update();
        }

        public void SelectDocuments(List<Document> docs)
        {
            if (docs.Contains(TotalDoc))
                SelectedDoc = TotalDoc;
            else
            {
                var selectedDoc = new Document(0);
                foreach (var doc in docs)
                    foreach (var f in doc.RawPages)
                    {
                        var fmt = selectedDoc.RawPages.FirstOrDefault(a => a.Info == f.Info);
                        if (fmt != null)
                            fmt.Count += f.Count;
                        else
                        {
                            fmt = new PageFormat(f.Info, f.Count);
                            selectedDoc.RawPages.Add(fmt);
                        }
                    }
                ResetDocument(selectedDoc);
                selectedDoc.Pages.Sort((a,b)=>a.Info.FormatType<b.Info.FormatType?-1: a.Info.FormatType > b.Info.FormatType?1: new NaturalStringComparer().Compare(a.Info.Name, b.Info.Name));
                SelectedDoc = selectedDoc;
            }
        }


        //============
        //  Info
        //============
        #region Info

        private bool _isAboutShown;
		public bool IsAboutShown
		{
			get => _isAboutShown;
			set
			{
				_isAboutShown = value;
				OnPropertyChanged();
			}
		}


        #endregion



        //============
        //  Info
        //============
        #region Saving

        // Сохранение при закрытии
        public void SaveState()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText("PaperCounter.cfg", json);
        }

        // Загрузка при открытии (обновление только нужных свойств)
        public void LoadState()
        {
            if (!File.Exists("PaperCounter.cfg"))
                return;

            string json = File.ReadAllText("PaperCounter.cfg");
            var settings = JsonConvert.DeserializeObject<VM>(json);
            if (settings == null)
                return;

            // Вручную присваиваем значения, не трогая остальные свойства VM
            this.IsLoadArchives = settings.IsLoadArchives;
            this.IsLoadMail = settings.IsLoadMail;
            this.IsCountMisc = settings.IsCountMisc;
            this.IsCountMiscA4 = settings.IsCountMiscA4;
            this.IsCountMiscExact = settings.IsCountMiscExact;
            this.IsCountMiscOrientation = settings.IsCountMiscOrientation;
        }
        #endregion

        public delegate void FormatsChangedHandler(List<PageSetInfo> names);
		public event FormatsChangedHandler FormatsChanged;


    }
}
