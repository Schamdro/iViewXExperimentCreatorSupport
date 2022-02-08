using iViewXExperimentCreator.Core.Models;
using iViewXExperimentCreator.Core.Subroutines;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iViewXExperimentCreator.Core.ViewModels
{
    /// <summary>
    /// ViewModel für die GUI zur Bearbeitung eines Videos.
    /// </summary>
    public class VideoEditorViewModel : MvxViewModel<VideoModel>
    {
        private readonly float TIME_STEPS = 0.1f;
        private static List<VideoEditorViewModel> _instances = new();
        /// <summary>
        /// Die offenen Instanzen dieses ViewModels.
        /// </summary>
        public static List<VideoEditorViewModel> Instances
        {
            get => _instances;
        }

        /// <summary>
        /// Referenz zur Liste der Videoreize im MainViewModel.
        /// </summary>
        public static MvxObservableCollection<StimulusModel> Stimuli { get => ExperimentFileManagerModel.CurrentExperiment.VideoStimuli; }
        private IMvxNavigationService _navigationService;

        private VideoModel _selectedVideo;
        /// <summary>
        /// Das ausgewählte Video.
        /// </summary>
        public VideoModel SelectedVideo
        {
            get { return _selectedVideo; }
            set { SetProperty(ref _selectedVideo, value); }
        }
        /// <summary>
        /// Der Konstruktor.
        /// </summary>
        /// <param name="navigationService"></param>
        public VideoEditorViewModel(IMvxNavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        /// <summary>
        /// Initialisiert das ViewModel und seine zugehörigen Commands.
        /// </summary>
        /// <returns></returns>
        public override async Task Initialize()
        {
            await base.Initialize();

            Instances.Add(this);

            ImportStimulusCommand = new MvxCommand<string>(ImportStimulus);
            IncrementVideoDurationCommand = new MvxCommand(IncrementVideoDuration);
            DecrementVideoDurationCommand = new MvxCommand(DecrementVideoDuration);
            IncrementVideoStartTimeCommand = new MvxCommand(IncrementVideoStartTime);
            DecrementVideoStartTimeCommand = new MvxCommand(DecrementVideoStartTime); 
        }

        /// <summary>
        /// Nimmt den Übergabeparameter des Navigationsservices entgegen.
        /// </summary>
        /// <param name="parameter"></param>
        public override void Prepare(VideoModel parameter)
        {
            SelectedVideo = parameter;
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
        /// Überprüft, ob ein ViewModel mit der übergebenen Sequenz bereits geöffnet ist.
        /// </summary>
        /// <param name="seq"></param>
        /// <returns></returns>
        public static bool IsVideoAlreadyOpen(VideoModel video)
        {
            foreach (VideoEditorViewModel vm in Instances)
            {
                if (vm.SelectedVideo == video) return true;
            }
            return false;
        }

        /// <summary>
        /// Schließt das ViewModel.
        /// </summary>
        public void Close()
        {
            _navigationService.Close(this);
        }

        /// <summary>
        /// Commandwarapper für ImportStimulus.
        /// </summary>
        public IMvxCommand<string> ImportStimulusCommand { get; private set; }
        /// <summary>
        /// Importiert einen neuen Stimulus. 
        /// </summary>
        /// <param name="path"></param>
        private void ImportStimulus(string path)
        {
            StimulusListUpdater.ImportNewStimulus(path);
        }


        /// <summary>
        /// Commandwarapper für IncrementVideoDuration.
        /// </summary>
        public IMvxCommand IncrementVideoDurationCommand { get; private set; }
        /// <summary>
        /// Erhöht die Dauer des generierten Videosnippets.
        /// </summary>
        private void IncrementVideoDuration()
        {
            SelectedVideo.Duration += TIME_STEPS;
        }

        /// <summary>
        /// Commandwarapper für DecrementVideoDuration.
        /// </summary>
        public IMvxCommand DecrementVideoDurationCommand { get; private set; }
        /// <summary>
        /// Verringert die Dauer des generierten Videosnippets.
        /// </summary>
        private void DecrementVideoDuration()
        {
            SelectedVideo.Duration -= TIME_STEPS;
        }

        /// <summary>
        /// Commandwarapper für IncrementVideoDuration.
        /// </summary>
        public IMvxCommand IncrementVideoStartTimeCommand { get; private set; }
        /// <summary>
        /// Erhöht die Dauer des generierten Videosnippets.
        /// </summary>
        private void IncrementVideoStartTime()
        {
            SelectedVideo.Timestamp += TIME_STEPS;
        }

        /// <summary>
        /// Commandwarapper für DecrementVideoDuration.
        /// </summary>
        public IMvxCommand DecrementVideoStartTimeCommand { get; private set; }
        /// <summary>
        /// Verringert die Dauer des generierten Videosnippets.
        /// </summary>
        private void DecrementVideoStartTime()
        {
            SelectedVideo.Timestamp -= TIME_STEPS;
        }
    }


}
