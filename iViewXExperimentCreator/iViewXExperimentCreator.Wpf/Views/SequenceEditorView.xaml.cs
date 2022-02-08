using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using iViewXExperimentCreator.Core.Models;
using iViewXExperimentCreator.Core.Subroutines;
using iViewXExperimentCreator.Core.ViewModels;
using Microsoft.Win32;

namespace iViewXExperimentCreator.Wpf.Views
{
    /// <summary>
    /// Interaktionslogik für SequenceEditorView.xaml
    /// </summary>
    public partial class SequenceEditorView
    {
        /// <summary>
        /// Konstruktor.
        /// </summary>
        public SequenceEditorView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Nimmt das StimulusModel als DragDrop-Element beim Gedrückthalten der linken Maustaste über dem
        /// Element in der Stimulus-Liste auf.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StimulusListItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StimulusModel stimulus = (sender as FrameworkElement).DataContext as StimulusModel;
            DataObject data = new(stimulus);
            DragDrop.DoDragDrop(e.OriginalSource as Image, data, DragDropEffects.Move);
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
            foreach (string ext in StimulusListUpdater.SUPPORTED_IMAGE_EXTENSIONS)
            {
                filter += "*" + ext + ";";
            }
            dialog.Filter = filter;
            if (dialog.ShowDialog() == true)
            {
                //Logger.Debug("Importiere: " + dialog.FileName);
                SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
                string[] paths = dialog.FileNames;
                foreach (string path in paths)
                {
                    Task addStimuliTask = new(() =>
                    {
                        vm.ImportStimulusCommand.Execute(path);
                    });
                    addStimuliTask.Start();
                    //stimList.Items.Refresh();
                }
            }
        }

        /// <summary>
        /// Führt den Command zum Löschen eines Slots in der Sequenz aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteSlot_Click(object sender, RoutedEventArgs e)
        {
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            MessageBoxResult result = MessageBox.Show("Möchten Sie diesen Slot wirklich löschen?", "Slot löschen", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                vm.DeleteSlotCommand.Execute(slot);
            }
        }

        /// <summary>
        /// Führt den Command zum Duplizieren eines Slots in der Sequenz aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DuplicateSlot_Click(object sender, RoutedEventArgs e)
        {
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            vm.DuplicateSlotCommand.Execute(slot);
        }

        /// <summary>
        /// Führt den Command zum Löschen des Reizes eines Slots in der Sequenz aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveStimulusFromSlot_Click(object sender, RoutedEventArgs e)
        {
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            vm.RemoveStimulusFromSlotCommand.Execute(slot);
        }



        /// <summary>
        /// Fügt bei Loslassen der linken Maustaste dem Slot das StimulusModel hinzu, welches als 
        /// DragDrop-Element gehalten wird. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stimulus_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(StimulusModel)) is StimulusModel dropped)
            {
                //SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
                //SlotModel target = DataContext as SlotModel;
                //target.Stimulus = dropped;


                SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
                SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
                vm.ChangeStimulusCommand.Execute((slot, dropped));
            }
        }

        /// <summary>
        /// Nimmt das SlotModel als DragDrop-Element beim Gedrückthalten der linken Maustaste über dem
        /// Slot auf.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SlotListItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            DataObject data = new(slot);
            DragDrop.DoDragDrop(e.OriginalSource as Image, data, DragDropEffects.Move);
        }

        /// <summary>
        /// Führt den Command zur Erhöhung des Layers des Slots aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IncrementSlotLayer_Click(object sender, RoutedEventArgs e)
        {
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            vm.IncrementSlotLayerCommand.Execute(slot);
        }

        /// <summary>
        /// Führt den Command zur Verringerung des Layers des Slots aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DecrementSlotLayer_Click(object sender, RoutedEventArgs e)
        {
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            vm.DecrementSlotLayerCommand.Execute(slot);
        }

        /// <summary>
        /// Führt den Command zur Anpassung der Slot-Skalierung an die Experimentauflösung aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StretchSlot_Click(object sender, RoutedEventArgs e)
        {
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            vm.StretchSlotCommand.Execute(slot);
        }

        /// <summary>
        /// Führt den Command zur Zentrierung des Slots aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CenterSlot_Click(object sender, RoutedEventArgs e)
        {
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            vm.CenterSlotCommand.Execute(slot);
        }


        /// <summary>
        /// Führt den Command zur Erhöhung des Layers des Slots aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IncrementSlotStartTime_Click(object sender, RoutedEventArgs e)
        {
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            vm.IncrementSlotStartTimeCommand.Execute(slot);
        }

        /// <summary>
        /// Führt den Command zur Verringerung des Layers des Slots aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DecrementSlotStartTime_Click(object sender, RoutedEventArgs e)
        {
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            vm.DecrementSlotStartTimeCommand.Execute(slot);
        }

        /// <summary>
        /// Führt den Command zur Erhöhung des Layers des Slots aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IncrementSlotDuration_Click(object sender, RoutedEventArgs e)
        {
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            vm.IncrementSlotDurationCommand.Execute(slot);
        }

        /// <summary>
        /// Führt den Command zur Verringerung des Layers des Slots aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DecrementSlotDuration_Click(object sender, RoutedEventArgs e)
        {
            SequenceEditorViewModel vm = DataContext as SequenceEditorViewModel;
            SlotModel slot = (sender as FrameworkElement).DataContext as SlotModel;
            vm.DecrementSlotDurationCommand.Execute(slot);
        }
    }
}
