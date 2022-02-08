using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using iViewXExperimentCreator.Core;
using iViewXExperimentCreator.Core.Models;
using iViewXExperimentCreator.Core.ViewModels;
using System.Windows;
using Microsoft.Win32;
using iViewXExperimentCreator.Core.Subroutines;
using System.Threading.Tasks;

namespace iViewXExperimentCreator.Wpf.Views
{

    /// <summary>
    /// Interaktionslogik für VideoEditorView.xaml
    /// </summary>
    public partial class VideoEditorView
    {
        /// <summary>
        /// Konstruktor.
        /// </summary>
        public VideoEditorView()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Öffnet den Windows-File-Dialog und übergibt den ausgewählte Bildreiz-Dateipfad per Command an das
        /// ViewModel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_OpenStimulusImportFileDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();
            dialog.Multiselect = true;
            string filter = "Reize|";
            foreach (string ext in StimulusListUpdater.SUPPORTED_VIDEO_EXTENSIONS)
            {
                filter += "*" + ext + ";";
            }
            dialog.Filter = filter;
            if (dialog.ShowDialog() == true)
            {
                //Logger.Debug("Importiere: " + dialog.FileName);
                VideoEditorViewModel vm = DataContext as VideoEditorViewModel;
                string[] paths = dialog.FileNames;
                foreach (string path in paths)
                {
                    Task addStimuliTask = new(() =>
                    {
                        vm.ImportStimulusCommand.Execute(path);
                    });
                    addStimuliTask.Start();
                }
            }
        }
    }
}
