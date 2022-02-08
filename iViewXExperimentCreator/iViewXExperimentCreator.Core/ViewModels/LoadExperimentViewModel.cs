using iViewXExperimentCreator.Core.Models;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iViewXExperimentCreator.Core.ViewModels
{
    /// <summary>
    /// ViewModel für die GUI zum Laden eines Experiments.
    /// </summary>
    public class LoadExperimentViewModel : MvxViewModel
    {
        private static List<LoadExperimentViewModel> _instances = new();
        /// <summary>
        /// Eine Liste aller offener Instanzen dieses ViewModels.
        /// </summary>
        public static List<LoadExperimentViewModel> Instances { get => _instances; }

        private MvxObservableCollection<string> _experimentFolders;
        /// <summary>
        /// Liste der verfügbaren Experimente.
        /// </summary>
        public MvxObservableCollection<string> ExperimentFolders { get => _experimentFolders; set => SetProperty(ref _experimentFolders, value); }

        private string _selectedExpName;
        /// <summary>
        /// Name des ausgewählten Experiments.
        /// </summary>
        public string SelectedExpName { get => _selectedExpName; set => SetProperty(ref _selectedExpName, value); }

        private IMvxNavigationService _navigationService;
        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="navigationService"></param>
        public LoadExperimentViewModel(IMvxNavigationService navigationService)
        {
            _navigationService = navigationService;
            UpdateExperimentFolders();
        }

        /// <summary>
        /// Initialisiert das ViewModel und seine zugehörigen Commands.
        /// </summary>
        /// <returns></returns>
        public override async Task Initialize()
        {
            await base.Initialize();

            LoadExperimentCommand = new MvxCommand(LoadExperiment);
            DeleteExperimentCommand = new MvxCommand(DeleteExperiment);

            Instances.Add(this);
        }

        /// <summary>
        /// Erstellt die Liste der existierenden Experimente neu.
        /// </summary>
        private void UpdateExperimentFolders()
        {
            string expDir = AppContext.BaseDirectory + @$"\Experiments";
            if (!Directory.Exists(expDir)) return;
            string[] directories = Directory.GetDirectories(expDir);

            ExperimentFolders = new();

            foreach(string dir in directories)
            {
                string expName = dir[(dir.LastIndexOf(@"\") + 1)..];
                ExperimentFolders.Add(expName);
            }
        }

        /// <summary>
        /// Commandwrapper für LoadExperiment.
        /// </summary>
        public IMvxCommand LoadExperimentCommand { get; private set; }
        /// <summary>
        /// Initiiert das Laden des ausgewählten Experiments.
        /// </summary>
        private void LoadExperiment()
        {
            ExperimentFileManagerModel.LoadExperiment(SelectedExpName);

            _navigationService.Close(this);
        }

        /// <summary>
        /// Schließt das ViewModel.
        /// </summary>
        public void Close()
        {
            _navigationService.Close(this);
        }

        /// <summary>
        /// Wird aufgerufen, wenn der View geschlossen wurde.
        /// </summary>
        public override void ViewDisappeared()
        {
            base.ViewDisappeared();
            Instances.Remove(this);
        }

        /// <summary>
        /// Commandwrapper für DeleteExperiment.
        /// </summary>
        public IMvxCommand DeleteExperimentCommand { get; private set; }
        /// <summary>
        /// Initiiert die Löschung des ausgewählten Experiments.
        /// </summary>
        private void DeleteExperiment()
        {
            ExperimentFileManagerModel.DeleteExperiment(SelectedExpName);
            UpdateExperimentFolders();
        }
    }
}
