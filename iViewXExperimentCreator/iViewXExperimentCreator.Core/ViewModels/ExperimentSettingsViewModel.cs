using iViewXExperimentCreator.Core.Models;
using iViewXExperimentCreator.Core.Util;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System.Drawing;
using System.Threading.Tasks;

namespace iViewXExperimentCreator.Core.ViewModels
{
    /// <summary>
    /// ViewModel für die GUI zur Bearbeitung des aktuellen Experiments.
    /// </summary>
    public class ExperimentSettingsViewModel : MvxViewModel
    {
        private static ExperimentSettingsViewModel _instance;
        public static ExperimentSettingsViewModel Instance => _instance;

        private IMvxNavigationService _navigationService;

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="navigationService"></param>
        public ExperimentSettingsViewModel(IMvxNavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        /// <summary>
        /// Initialisiert das ViewModel.
        /// </summary>
        /// <returns></returns>
        public override async Task Initialize()
        {
            await base.Initialize();
            _instance = this;
        }

        /// <summary>
        /// Wird aufgerufen, wenn der View geschlossen wurde.
        /// </summary>
        public override void ViewDisappeared()
        {
            base.ViewDisappeared();
            _instance = null;
        }

        /// <summary>
        /// Schließt das ViewModel.
        /// </summary>
        public void Close()
        {
            _navigationService.Close(this);
        }

        /// <summary>
        /// Eigenschaft zur Veränderung des Experimentnamens.
        /// </summary>
        public string ExperimentName
        {
            get { return ExperimentFileManagerModel.CurrentExperiment.Name; }
            set { ExperimentFileManagerModel.CurrentExperiment.Name = value; }
        }

        /// <summary>
        /// Eigenschaft zur Veränderung der Hintergrundfarbe der Snapshots des Experiments.
        /// </summary>
        public Color Color
        {
            get => ExperimentFileManagerModel.CurrentExperiment.Background;
            set
            {
                ExperimentFileManagerModel.CurrentExperiment.Background = value;
                RaisePropertyChanged("Color");
            }
        }

        /// <summary>
        /// Eigenschaft zur Veränderung der Breite der Snapshots und Videos des Experiments.
        /// </summary>
        public string ResolutionX
        {
            get 
            {
                return ExperimentFileManagerModel.CurrentExperiment.ResolutionX.ToString(); 
            }
            set 
            {
                ExperimentFileManagerModel.CurrentExperiment.ResolutionX = 
                    value.ParseToPositiveInt(defaultValue: ExperimentFileManagerModel.CurrentExperiment.ResolutionX, maxValue: 10000);
            }
        }

        /// <summary>
        /// Eigenschaft zur Veränderung der Höhe der Snapshots und Videos des Experiments.
        /// </summary>
        public string ResolutionY
        {
            get 
            { 
                return ExperimentFileManagerModel.CurrentExperiment.ResolutionY.ToString(); 
            }
            set
            {
                ExperimentFileManagerModel.CurrentExperiment.ResolutionY = 
                    value.ParseToPositiveInt(defaultValue: ExperimentFileManagerModel.CurrentExperiment.ResolutionY, maxValue: 10000);
            }
        }

        /// <summary>
        /// Eigenschaft, welche die verfügbaren Kalibrierungspunkte zurückgibt.
        /// </summary>
        public int[] CalibrationPoints => ExperimentModel.CalibrationPoints;

        /// <summary>
        /// Eigenschaft zur Veränderung der ausgewählten Anzahl der Kalibrierungspunkte des Experiments.
        /// </summary>
        public int SelectedCalibrationPoints
        {
            get { return ExperimentFileManagerModel.CurrentExperiment.SelectedCalibrationPoints; }
            set { ExperimentFileManagerModel.CurrentExperiment.SelectedCalibrationPoints = value; }
        }

        /// <summary>
        /// Die der linken Pfeiltaste zugewiesene Bedeutung. Wird in der Versuchslog hinterlegt.
        /// </summary>
        public string LeftKeyMeaning { get => ExperimentFileManagerModel.CurrentExperiment.LeftKeyMeaning; set => ExperimentFileManagerModel.CurrentExperiment.LeftKeyMeaning = value; }
        /// <summary>
        /// Die der rechten Pfeiltaste zugewiesene Bedeutung. Wird in der Versuchslog hinterlegt.
        /// </summary>
        public string RightKeyMeaning { get => ExperimentFileManagerModel.CurrentExperiment.RightKeyMeaning; set => ExperimentFileManagerModel.CurrentExperiment.RightKeyMeaning = value; }
        /// <summary>
        /// Die der oberen Pfeiltaste zugewiesene Bedeutung. Wird in der Versuchslog hinterlegt.
        /// </summary>
        public string UpKeyMeaning { get => ExperimentFileManagerModel.CurrentExperiment.UpKeyMeaning; set => ExperimentFileManagerModel.CurrentExperiment.UpKeyMeaning = value; }
        /// <summary>
        /// Die der unteren Pfeiltaste zugewiesene Bedeutung. Wird in der Versuchslog hinterlegt.
        /// </summary>
        public string DownKeyMeaning { get => ExperimentFileManagerModel.CurrentExperiment.DownKeyMeaning; set => ExperimentFileManagerModel.CurrentExperiment.DownKeyMeaning = value; }

        /// <summary>
        /// Wahr, wenn Eingaben der linken Pfeiltaste während der Präsentation erlaubt sind.
        /// </summary>
        public bool LeftKeyInputPossible 
        { 
            get => ExperimentFileManagerModel.CurrentExperiment.LeftKeyInputPossible;
            set
            {
                ExperimentFileManagerModel.CurrentExperiment.LeftKeyInputPossible = value;
                RaisePropertyChanged("LeftKeyInputPossible");
            }
        }
        /// <summary>
        /// Wahr, wenn Eingaben der rechten Pfeiltaste während der Präsentation erlaubt sind.
        /// </summary>
        public bool RightKeyInputPossible 
        { 
            get => ExperimentFileManagerModel.CurrentExperiment.RightKeyInputPossible;
            set
            {
                ExperimentFileManagerModel.CurrentExperiment.RightKeyInputPossible = value;
                RaisePropertyChanged("RightKeyInputPossible");
            }
        }
        /// <summary>
        /// Wahr, wenn Eingaben der oberen Pfeiltaste während der Präsentation erlaubt sind.
        /// </summary>
        public bool UpKeyInputPossible
        { 
            get => ExperimentFileManagerModel.CurrentExperiment.UpKeyInputPossible;
            set
            {
                ExperimentFileManagerModel.CurrentExperiment.UpKeyInputPossible = value;
                RaisePropertyChanged("UpKeyInputPossible");
            }
        }
        /// <summary>
        /// Wahr, wenn Eingaben der unteren Pfeiltaste während der Präsentation erlaubt sind.
        /// </summary>
        public bool DownKeyInputPossible 
        { 
            get => ExperimentFileManagerModel.CurrentExperiment.DownKeyInputPossible;
            set
            {
                ExperimentFileManagerModel.CurrentExperiment.DownKeyInputPossible = value;
                RaisePropertyChanged("DownKeyInputPossible");
            }
        }

    }
}
