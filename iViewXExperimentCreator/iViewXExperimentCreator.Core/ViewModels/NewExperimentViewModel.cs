using iViewXExperimentCreator.Core.Models;
using iViewXExperimentCreator.Core.Util;
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
    /// ViewModel für die GUI zur Erstellung eines neuen Experiments.
    /// </summary>
    public class NewExperimentViewModel : MvxViewModel
    {
        private static List<NewExperimentViewModel> _instances = new();
        /// <summary>
        /// Eine Liste aller offener Instanzen dieses ViewModels.
        /// </summary>
        public static List<NewExperimentViewModel> Instances { get => _instances; }

        private IMvxNavigationService _navigationService;
        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="navigationService"></param>
        public NewExperimentViewModel(IMvxNavigationService navigationService)
        {
            _navigationService = navigationService;

            CreateExperimentCommand = new MvxCommand(CreateExperiment);
        }

        private string _experimentName = "Default";
        /// <summary>
        /// Eigenschaft zur Veränderung des Experimentnamens.
        /// </summary>
        public string ExperimentName
        {
            get { return _experimentName; }
            set { SetProperty(ref _experimentName, value); }
        }

        private int _resolutionX = 1920;
        private int _resolutionY = 1080;

        /// <summary>
        /// Eigenschaft zur Veränderung der Breite der Snapshots und Videos des Experiments.
        /// </summary>
        public string ResolutionX
        {
            get
            {
                return _resolutionX.ToString();
            }
            set
            {
                SetProperty(ref _resolutionX, value.ParseToPositiveInt(defaultValue: _resolutionX, maxValue: 10000));
            }
        }

        /// <summary>
        /// Eigenschaft zur Veränderung der Höhe der Snapshots und Videos des Experiments.
        /// </summary>
        public string ResolutionY
        {
            get
            {
                return _resolutionY.ToString();
            }
            set
            {
                SetProperty(ref _resolutionY, value.ParseToPositiveInt(defaultValue: _resolutionY, maxValue: 10000));
            }
        }


        /// <summary>
        /// Eigenschaft, welche die verfügbaren Kalibrierungspunkte zurückgibt.
        /// </summary>
        public int[] CalibrationPoints => ExperimentModel.CalibrationPoints;

        private int _selectedCalibrationPoints = 13;

        /// <summary>
        /// Eigenschaft zur Veränderung der ausgewählten Anzahl der Kalibrierungspunkte des Experiments.
        /// </summary>
        public int SelectedCalibrationPoints
        {
            get { return _selectedCalibrationPoints; }
            set { SetProperty(ref _selectedCalibrationPoints, value); }
        }
        /// <summary>
        /// Initialisiert das ViewModel.
        /// </summary>
        /// <returns></returns>
        public override async Task Initialize()
        {
            await base.Initialize();

            Instances.Add(this);
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
        /// Commandwrapper für CreateExperiment.
        /// </summary>
        public IMvxCommand CreateExperimentCommand { get; private set; }
        /// <summary>
        /// Erstellt ein neues Experiment und setzt es als das derzeitige Experiment.
        /// </summary>
        private void CreateExperiment()
        {
            if (Directory.Exists(AppContext.BaseDirectory + @$"\Experiments\{ExperimentName}"))
            {
                Logger.Message($"Experiment mit dem Namen {ExperimentName} existiert bereits.");
                return;
            }

            ExperimentFileManagerModel.CreateExperiment(ExperimentName, (_resolutionX, _resolutionY), SelectedCalibrationPoints);
            _navigationService.Close(this);
        }

    }
}
