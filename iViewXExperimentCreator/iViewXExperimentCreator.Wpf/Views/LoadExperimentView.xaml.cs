using iViewXExperimentCreator.Core.ViewModels;
using System.Windows;

namespace iViewXExperimentCreator.Wpf.Views
{
    /// <summary>
    /// Interaction logic for LoadExperimentView.xaml
    /// </summary>
    public partial class LoadExperimentView
    {
        /// <summary>
        /// Konstruktor.
        /// </summary>
        public LoadExperimentView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Führt das Kommando zum Laden des Experiments im ViewModel aus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadExperiment_Click(object sender, RoutedEventArgs e)
        {
            LoadExperimentViewModel vm = DataContext as LoadExperimentViewModel;
            vm.LoadExperimentCommand.Execute();
        }

        /// <summary>
        /// Führt das Kommando zum Löschen des Experiments im ViewModel aus. Fragt Nutzer zunächst, ob dieser sicher ist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteExperiment_Click(object sender, RoutedEventArgs e)
        {
            LoadExperimentViewModel vm = DataContext as LoadExperimentViewModel;
            MessageBoxResult result = MessageBox.Show("Möchten Sie dieses Experiment wirklich löschen?", "Experiment löschen", MessageBoxButton.YesNo);
            if(result == MessageBoxResult.Yes)
            {
                vm.DeleteExperimentCommand.Execute();
            }
        }
    }
}
