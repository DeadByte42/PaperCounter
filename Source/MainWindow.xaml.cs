using GongSolutions.Wpf.DragDrop;
using PaperCounter.Utility;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using static System.Windows.Forms.DataFormats;

namespace PaperCounter
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDropTarget
    {
        VM vm;
        PageSetStatConverter lfc = new();

        public MainWindow()
        {
            InitializeComponent();
            Environment.CurrentDirectory = Path.GetDirectoryName(Environment.ProcessPath);
            Loaded += (s, a) =>
            {
                vm = DataContext as VM;
                vm.LoadState();
                vm.FormatsChanged += DataGrid_UpdateColumns;

                var args = Environment.GetCommandLineArgs();
                vm.EnqueueDocuments(args);
            };
            this.Closing += (s, e) => vm.SaveState();
        }

        private void DataGrid_UpdateColumns(List<PageSetInfo> sets)
        {
            for (int i = DetailGrid.Columns.Count; i > 3; i--)
                DetailGrid.Columns.RemoveAt(i-3);

            foreach (var set in sets)
            {
                var text = new TextBlock() { Text = set.Name, ToolTip = set.Description };
                var col = new DataGridTextColumn()
                {
                    IsReadOnly = true,
                    Binding = new Binding($"Pages") { Mode = BindingMode.OneWay, ConverterParameter = set.Name, Converter = lfc },
                    Header = text
                };

                DetailGrid.Columns.Insert(DetailGrid.Columns.Count-2, col);
            }

        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            e.Effects = DragDropEffects.None;
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            var data = (string[])e.Data.GetData(DataFormats.FileDrop);
            vm.EnqueueDocuments(data);
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.SelectDocuments((sender as DataGrid).SelectedItems.Cast<Document>().ToList());
        }
        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                vm.DeleteDocs((sender as DataGrid).SelectedItems.Cast<Document>().ToList());
            }
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var dg = sender as DataGrid;
            if (dg.ItemsSource is DataView dt)
            {
                var col = dt.Table.Columns[e.PropertyName];
                e.Column.Header = col.Caption;
            }
        }

        private void InfoAbout_Click(object sender, RoutedEventArgs e)
        {
            vm.IsAboutShown = !vm.IsAboutShown;
        }

        private void ListDelete_Click(object sender, RoutedEventArgs e)
        {
            if (tabs.SelectedIndex == 0)
                vm.DeleteDocs(SummaryGrid.SelectedItems.Cast<Document>().ToList());
            else
                vm.DeleteDocs(DetailGrid.SelectedItems.Cast<Document>().ToList());
        }

        private void ListCombine_Click(object sender, RoutedEventArgs e)
        {
            if (tabs.SelectedIndex == 0)
                vm.CombineDocs(SummaryGrid.SelectedItems.Cast<Document>().ToList());
            else
                vm.CombineDocs(DetailGrid.SelectedItems.Cast<Document>().ToList());
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            var dataObject = dropInfo.Data as IDataObject;
            if (dataObject != null && dataObject.GetDataPresent(DataFormats.FileDrop))
                dropInfo.Effects = DragDropEffects.Copy;
            else if (dropInfo.KeyStates.HasFlag(DragDropKeyStates.AltKey))  
            {
                if ((dropInfo.Data is Document doc && doc != dropInfo.TargetItem || dropInfo.Data is IList docs && !docs.Contains(dropInfo.TargetItem)))
                {
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    dropInfo.Effects = DragDropEffects.Move;
                }
            }
            else
                GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.DragOver(dropInfo);
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var dataObject = dropInfo.Data as DataObject;
            if (dataObject != null && dataObject.GetDataPresent(DataFormats.FileDrop))
                vm.EnqueueDocuments((string[])dataObject.GetData(DataFormats.FileDrop));
            else if (dropInfo.KeyStates.HasFlag(DragDropKeyStates.AltKey))
            {
                List<Document> sources = new List<Document>();
                if (dropInfo.TargetItem is Document doc1 && dropInfo.Data is Document doc2 && doc1 != doc2)
                {
                    sources.Add(doc1);
                    sources.Add(doc2);
                }
                else if (dropInfo.TargetItem is Document doc && dropInfo.Data is IList lst && !lst.Contains(doc))
                {
                    sources.Add(doc);
                    sources.AddRange(lst.Cast<Document>());
                }
                vm.CombineDocs(sources);
            }
            else
            {
                GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.Drop(dropInfo);
                if (vm.Documents.Last() != vm.TotalDoc)
                {
                    vm.Documents.Remove(vm.TotalDoc);
                    vm.Documents.Add(vm.TotalDoc);
                }
            }
        }

        private void HyperlinkNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void InfoHelp_Click(object sender, RoutedEventArgs e)
        {
            new HelpWindow().Show();
        }

    }
}
