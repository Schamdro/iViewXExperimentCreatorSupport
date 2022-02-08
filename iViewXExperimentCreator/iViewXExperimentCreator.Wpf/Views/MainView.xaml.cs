using MvvmCross.Platforms.Wpf.Views;
using System.Windows;
using System.Windows.Controls;
using iViewXExperimentCreator.Core.ViewModels;
using iViewXExperimentCreator.Core.Models;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System;
using iViewXExperimentCreator.Core.Subroutines;
using System.Threading.Tasks;
using iViewXExperimentCreator.Core;
using MvvmCross.Base;
using MvvmCross;

namespace iViewXExperimentCreator.Wpf.Views
{
    /// <summary>
    /// Interaktionslogik für MainView.xaml
    /// </summary>
    public partial class MainView : MvxWpfView
    {
        private MainWindow _shell;

        /// <summary>
        /// Konstruktor.
        /// </summary>
        public MainView()
        {
            InitializeComponent();
            Unloaded += (_,_) => Application.Current.Shutdown();
        }

        /// <summary>
        /// Dieses Event feuert immer dann, wenn das Image, das als Vollbildanzeige für das Experiment dient, seine Sichtbarkeit
        /// verändert. Recht unintuitiv wird hier aber ein Boolean und kein Visibility-Enum von WPF übergeben. Das sollte aber nicht 
        /// weiter stören, weil hier sowieso nur Visible und Collapsed von Interesse sind, Hidden sollte das Image sowieso nicht sein.
        /// Für den Fall, dass das trotzdem passiert, funktioniert aber dennoch alles gleich.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FullscreenExperimentView_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            //Im Konstruktor lässt sich die Referenz an die Shell nicht übergeben,
            //weil MainWindow MainView erfordert und deshalb noch nicht existiert.
            if(_shell is null) 
                _shell = (MainWindow)Parent;

            //Sichtbar? -> Maximieren, Unsichtbar? -> Fenster geht wieder in den normalen Fenstermodus
            if ((bool)e.NewValue) 
                _shell.MaximizeWindow();
            else 
                _shell.NormalizeWindow();
        }

        /// <summary>
        /// Wird beim Droppen eines DragDrop-Elements auf der Preview-Fläche aufgerufen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewImage_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(SlotModel)) is SlotModel dropped)
            {
                double scalingFactor = ((BitmapImage)PreviewImage.Source).PixelWidth / PreviewImage.ActualWidth;
                dropped.XCoordinate = (int)(e.GetPosition(PreviewImage).X * scalingFactor);
                dropped.YCoordinate = (int)(e.GetPosition(PreviewImage).Y * scalingFactor);
                //MainViewModel vm = DataContext as MainViewModel;
                //vm.PreviewImageDropCommand.Execute(dropped);
            }

        }

        /// <summary>
        /// Wird jedes Mal dann aufgerufen, wenn die Eigenschaft, an welches das Log-Element gebunden ist, sich ändert.
        /// Scrollt den Text nach unten.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Log_TextChanged(object sender, TextChangedEventArgs e)
        {
            logScrollViewer.ScrollToBottom();
        }

        /// <summary>
        /// Öffnet den Windows-File-Dialog und gibt den Dateipfad der ausgewählten Bild-/Videodatei an das per Command weiter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_OpenStimulusImportFileDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();
            dialog.Multiselect = true;
            string filter = "Reize|";
            foreach (string ext in StimulusListUpdater.SUPPORTED_EXTENSIONS)
            {
                filter += "*"+ext+";";
            }
            dialog.Filter = filter;
            if (dialog.ShowDialog() == true)
            {
                MainViewModel vm = DataContext as MainViewModel;
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

        /// <summary>
        /// Jedes Mal, wenn eine Videodatei im Preview-Modus geladen wird, wird diese an die korrekte Position gesetzt.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewWindowMedia_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (MainViewModel.Instance.PreviewVideo is not null)
            {
                PreviewWindowMedia.Position = TimeSpan.FromSeconds(MainViewModel.Instance.PreviewVideo.Timestamp);
                if (MainViewModel.Instance.PreviewVideo.Pauses && MainViewModel.Instance.ExperimentRunning)
                {
                    PreviewWindowMedia.LoadedBehavior = MediaState.Pause;
                    if(MainViewModel.Instance.Presentation != null)
                    {
                        void Unpause()
                        {
                            var dispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
                            dispatcher.ExecuteOnMainThreadAsync(() =>
                            {
                                MainViewModel.Instance.Presentation.Unpaused -= Unpause;
                                PreviewWindowMedia.LoadedBehavior = MediaState.Play;
                            });
                        };
                        MainViewModel.Instance.Presentation.Unpaused += Unpause;
                    }
                }
                else
                {
                    PreviewWindowMedia.LoadedBehavior = MediaState.Play;
                }
            }
        }

        /// <summary>
        /// Jedes Mal, wenn eine Videodatei im Fullscreen-Modus geladen wird, wird diese an die korrekte Position gesetzt.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FullscreenWindowMedia_MediaOpened(object sender, RoutedEventArgs e)
        {
            if(MainViewModel.Instance.PreviewVideo is not null)
            {
                FullscreenWindowMedia.Position = TimeSpan.FromSeconds(MainViewModel.Instance.PreviewVideo.Timestamp);
                if (MainViewModel.Instance.PreviewVideo.Pauses && MainViewModel.Instance.ExperimentRunning)
                {
                    FullscreenWindowMedia.LoadedBehavior = MediaState.Pause;
                    if (MainViewModel.Instance.Presentation != null)
                    {
                        void Unpause()
                        {
                            var dispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
                            dispatcher.ExecuteOnMainThreadAsync(() =>
                            {
                                MainViewModel.Instance.Presentation.Unpaused -= Unpause;
                                FullscreenWindowMedia.LoadedBehavior = MediaState.Play;
                            });
                        };
                        MainViewModel.Instance.Presentation.Unpaused += Unpause;
                    }
                }
                else
                {
                    FullscreenWindowMedia.LoadedBehavior = MediaState.Play;
                }
            }
        }


        /// <summary>
        /// Führt das Kommando zum Löschen des Experiments im ViewModel aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteSequence_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel vm = DataContext as MainViewModel;
            MessageBoxResult result = MessageBox.Show("Möchten Sie diese Sequenz wirklich löschen?", "Sequenz löschen", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                vm.DeleteSequenceCommand.Execute();
            }
        }
    }
}
