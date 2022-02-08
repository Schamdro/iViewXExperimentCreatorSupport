using MvvmCross.Commands;
using iViewXExperimentCreator.Core.Models;
using iViewXExperimentCreator.Core.Subroutines;
using System.Threading.Tasks;
using System;
using System.Threading;
using iViewXExperimentCreator.Core.Enums;
using iViewXExperimentCreator.Core.Util;

namespace iViewXExperimentCreator.Core.ViewModels
{
    // Commands wurden für bessere Übersicht in einer Extradatei aufgelistet. Das ist nur eine partielle Klasse,
    // es macht keinen funktionalen Unterschied, ob alles in einer Datei oder mehreren gespeichert ist.
    public partial class MainViewModel
    {
        /// <summary>
        /// Wrappt Methoden, die dem View zugänglich gemacht werden sollen, in Command-Eigenschaften.
        /// </summary>
        private void InitCommands()
        {
            OpenNewExperimentWindowCommand = new MvxAsyncCommand(OpenNewExperimentWindow);
            OpenExperimentSettingsWindowCommand = new MvxAsyncCommand(OpenExperimentSettingsWindow);
            OpenLoadExperimentWindowCommand = new MvxAsyncCommand(OpenLoadExperimentWindow);
            EditSequenceCommand = new MvxAsyncCommand(EditSequence);
            NewSequenceCommand = new MvxAsyncCommand(NewSequence);
            NewVideoCommand = new MvxAsyncCommand(NewVideo);
            IncrementSequencePositionCommand = new MvxCommand(IncrementSequencePosition);
            DecrementSequencePositionCommand = new MvxCommand(DecrementSequencePosition);
            DeleteSequenceCommand = new MvxCommand(DeleteSequence);
            StartCalibrationCommand = new MvxCommand(StartCalibration);
            RightKeyPressedCommand = new MvxCommand(RightKeyPressed);
            LeftKeyPressedCommand = new MvxCommand(LeftKeyPressed);
            UpKeyPressedCommand = new MvxCommand(UpKeyPressed);
            DownKeyPressedCommand = new MvxCommand(DownKeyPressed);
            EscapeKeyPressedCommand = new MvxCommand(EscapeKeyPressed);
            RunExperimentCommand = new MvxCommand(RunExperiment);
            DuplicateComponentCommand = new MvxCommand(DuplicateComponent);
            //PreviewImageDropCommand = new MvxCommand<SlotModel>(PreviewImageDrop);
            ImportStimulusCommand = new MvxCommand<string>(ImportStimulus);
        }

        /// <summary>
        /// Commandwrapper für DuplicateComponent.
        /// </summary>
        public IMvxCommand DuplicateComponentCommand { get; private set; }
        /// <summary>
        /// Dupliziert SelectedExperimentComponent in der Liste der Komponenten.
        /// </summary>
        private void DuplicateComponent()
        {
            ExperimentFileManagerModel.CurrentExperiment?.DuplicateComponent(SelectedExperimentComponent);
        }

        /// <summary>
        /// Commandwrapper für ImportStimulus.
        /// </summary>
        public IMvxCommand<string> ImportStimulusCommand { get; private set; }
        /// <summary>
        /// Fügt den im Parameter übergebenen Stimulus-Dateipfad über den StimulusListUpdater dem Stimulus-Verzeichnis und somit der
        /// List der Stimuli in der Applikation hinzu.
        /// </summary>
        /// <param name="stimulusPath"></param>
        private void ImportStimulus(string stimulusPath)
        {
            StimulusListUpdater.ImportNewStimulus(stimulusPath);
        }

        /// <summary>
        /// Commandwrapper für NewVideoCommand.
        /// </summary>
        public IMvxCommand NewVideoCommand { get; private set; }
        /// <summary>
        /// Erstellt ein neues VideoModel und fügt dieses der Liste an Komponenten des derzeitigen Experiments hinzu. Öffnet im Anschluss den Videoeditor
        /// mit dem neu erstellen VideoModel als Parameter.
        /// </summary>
        /// <returns>Task</returns>
        private async Task NewVideo()
        {
            if (ExperimentFileManagerModel.CurrentExperiment == null) return;
            VideoModel video = new("Default-Video");
            ExperimentFileManagerModel.CurrentExperiment.Components.Add(video);
            SelectedExperimentComponent = video;
            await EditSequence();
        }

        /// <summary>
        /// Commandwrapper für IncrementSequencePosition.
        /// </summary>
        public IMvxCommand IncrementSequencePositionCommand { get; private set; }
        /// <summary>
        /// Bewegt die SelectedExperimentComponent-Experimentkomponente in der Liste der Experimentkomponenten in der Reihenfolge nach vorne. 
        /// </summary>
        private void IncrementSequencePosition()
        {
            int prevIdx = ExperimentComponents.IndexOf(SelectedExperimentComponent) - 1;
            if (prevIdx >= 0)
                ExperimentComponents.Swap(SelectedExperimentComponent, ExperimentComponents[prevIdx]);
        }

        /// <summary>
        /// Commandwrapper für DecrementSequencePosition.
        /// </summary>
        public IMvxCommand DecrementSequencePositionCommand { get; private set; }
        /// <summary>
        /// Bewegt die SelectedExperimentComponent-Experimentkomponente in der Liste der Experimentkomponenten in der Reihenfolge nach hinten. 
        /// </summary>
        private void DecrementSequencePosition()
        {
            int pastIdx = ExperimentComponents.IndexOf(SelectedExperimentComponent) + 1;
            if (pastIdx < ExperimentComponents.Count)
                ExperimentComponents.Swap(SelectedExperimentComponent, ExperimentComponents[pastIdx]);
        }

        ///// <summary>
        ///// Commandwrapper für PreviewImageDrop.
        ///// </summary>
        //public IMvxCommand<SlotModel> PreviewImageDropCommand { get; private set; }
        ///// <summary>
        ///// Handhabt die Drop-Interaktion auf dem Vorschaubild. Nimmt ein SlotModel entgegen, welches per Parameter vom View übergeben wird.
        ///// </summary>
        ///// <param name="slot"></param>
        //private void PreviewImageDrop(SlotModel slot)
        //{
        //    Logger.Debug("Yoink1");
        //    //SetPreviewToFirstSlotOccurence(slot);
        //}


        /// <summary>
        /// Commandwrapper für StartCalibration.
        /// </summary>
        public IMvxCommand StartCalibrationCommand { get; private set; }
        /// <summary>
        /// Setzt die Kalibrierung ingang.
        /// </summary>
        private void StartCalibration()
        {
            Thread t = new(new ThreadStart(Calibrate));
            t.IsBackground = true;
            t.Start();
        }

        /// <summary>
        /// Commandwrapper für RunExperiment.
        /// </summary>
        public IMvxCommand RunExperimentCommand { get; private set; }
        /// <summary>
        /// Setzt die Ausführung des Experiments ingang.
        /// </summary>
        private void RunExperiment()
        {
            Task t = new(ConductExperiment);
            //t.IsBackground = true;
            t.Start();
        }

        /// <summary>
        /// Commandwrapper für LeftKeyPressed.
        /// </summary>
        public IMvxCommand LeftKeyPressedCommand { get; private set; }
        /// <summary>
        /// Registriert Eingabe der linken Pfeiltaste.
        /// </summary>
        private void LeftKeyPressed()
        {
            if (ExperimentFileManagerModel.CurrentExperiment is null) return;
            if (!ExperimentFileManagerModel.CurrentExperiment.LeftKeyInputPossible) return;
            _presentation?.EnterInput(PressedKey.Links);
        }

        /// <summary>
        /// Commandwrapper für RightKeyPressed.
        /// </summary>
        public IMvxCommand RightKeyPressedCommand { get; private set; }
        /// <summary>
        /// Registriert Eingabe der rechten Pfeiltaste.
        /// </summary>
        private void RightKeyPressed()
        {
            if (ExperimentFileManagerModel.CurrentExperiment is null) return;
            if (!ExperimentFileManagerModel.CurrentExperiment.RightKeyInputPossible) return;
            _presentation?.EnterInput(PressedKey.Rechts);
        }

        /// <summary>
        /// Commandwrapper für UpKeyPressed.
        /// </summary>
        public IMvxCommand UpKeyPressedCommand { get; private set; }
        /// <summary>
        /// Registriert Eingabe der obere Pfeiltaste.
        /// </summary>
        private void UpKeyPressed()
        {
            if (ExperimentFileManagerModel.CurrentExperiment is null) return;
            if (!ExperimentFileManagerModel.CurrentExperiment.UpKeyInputPossible) return;
            _presentation?.EnterInput(PressedKey.Hoch);
        }

        /// <summary>
        /// Commandwrapper für DownKeyPressed.
        /// </summary>
        public IMvxCommand DownKeyPressedCommand { get; private set; }
        /// <summary>
        /// Registriert Eingabe der untere Pfeiltaste.
        /// </summary>
        private void DownKeyPressed()
        {
            if (ExperimentFileManagerModel.CurrentExperiment is null) return;
            if (!ExperimentFileManagerModel.CurrentExperiment.DownKeyInputPossible) return;
            _presentation?.EnterInput(PressedKey.Runter);
        }

        /// <summary>
        /// Commandwrapper für EscapeKeyPressed.
        /// </summary>
        public IMvxCommand EscapeKeyPressedCommand { get; private set; }
        /// <summary>
        /// Registriert Eingabe der Escapetaste.
        /// </summary>
        private void EscapeKeyPressed()
        {
            if (ExperimentFileManagerModel.CurrentExperiment is null) return;
            if (ExperimentRunning)
            {
                _presentation?.Interrupt();
            }
            if (Calibrating)
            {
                _calibration?.Interrupt();
            }
        }


        /// <summary>
        /// Commandwrapper für OpenNewExperimentWindow.
        /// </summary>
        public IMvxAsyncCommand OpenNewExperimentWindowCommand { get; private set; }
        /// <summary>
        /// Öffnet eine Instanz des Neues-Experiment-Erstellen-ViewModels.
        /// </summary>
        /// <returns></returns>
        public async Task OpenNewExperimentWindow()
        {
            try
            {
                await _navigationService.Navigate<NewExperimentViewModel>();
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Navigationsfehler von MainViewModel zu NewExperimentViewModel aufgetreten.");
            }
        }

        /// <summary>
        /// Commandwrapper für OpenExperimentSettingsWindow.
        /// </summary>
        public IMvxAsyncCommand OpenExperimentSettingsWindowCommand { get; private set; }
        /// <summary>
        /// Öffnet eine Instanz des Neues-Experiment-Erstellen-ViewModels.
        /// </summary>
        /// <returns></returns>
        public async Task OpenExperimentSettingsWindow()
        {
            try
            {
                if (ExperimentSettingsViewModel.Instance == null)
                    await _navigationService.Navigate<ExperimentSettingsViewModel>();
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Navigationsfehler von MainViewModel zu ExperimentSettingsViewModel aufgetreten.");
            }
        }

        /// <summary>
        /// Commandwrapper für OpenLoadExperimentWindow.
        /// </summary>
        public IMvxAsyncCommand OpenLoadExperimentWindowCommand { get; private set; }
        /// <summary>
        /// Öffnet eine Instanz des Experiment-Laden-ViewsModels.
        /// </summary>
        /// <returns></returns>
        private async Task OpenLoadExperimentWindow()
        {
            try
            {
                await _navigationService.Navigate<LoadExperimentViewModel>();
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Navigationsfehler von MainViewModel zu LoadExperimentViewModel aufgetreten.");
            }
        }

        /// <summary>
        /// Commandwrapper für NewSequence.
        /// </summary>
        public IMvxAsyncCommand NewSequenceCommand { get; private set; }
        /// <summary>
        /// Erstellt eine neue Sequenz, fügt diese der Liste der Experimentkomponenten hinzu und öffnet den Sequenz-Editor mit dieser als Übergabeparamater. 
        /// </summary>
        /// <returns></returns>
        private async Task NewSequence()
        {
            if (ExperimentFileManagerModel.CurrentExperiment is null) return;
            SequenceModel sequence = new("Default-Sequenz");
            ExperimentFileManagerModel.CurrentExperiment.Components.Add(sequence);
            SelectedExperimentComponent = sequence;
            await EditSequence();
        }

        /// <summary>
        /// Commandwrapper für DeleteSequence.
        /// </summary>
        public IMvxCommand DeleteSequenceCommand { get; private set; }
        /// <summary>
        /// Löscht die ausgewählte Sequenz.
        /// </summary>
        private void DeleteSequence()
        {
            if (ExperimentFileManagerModel.CurrentExperiment == null || SelectedExperimentComponent == null) return;
            ExperimentComponents.Remove(SelectedExperimentComponent);
        }

        /// <summary>
        /// Commandwrapper für EditSequence.
        /// </summary>
        public IMvxAsyncCommand EditSequenceCommand { get; private set; }
        /// <summary>
        /// Öffnet eine Instanz des Sequenzeditors, welcher die übergebene Sequenz enthält.
        /// </summary>
        /// <returns></returns>
        private async Task EditSequence()
        {
            if (ExperimentFileManagerModel.CurrentExperiment != null && SelectedExperimentComponent != null)
            {
                try
                {
                    if (SelectedExperimentComponent is SequenceModel sequence && !SequenceEditorViewModel.IsSequenceAlreadyOpen(sequence))
                        await _navigationService.Navigate<SequenceEditorViewModel, SequenceModel>(sequence);
                    else if (SelectedExperimentComponent is VideoModel video && !VideoEditorViewModel.IsVideoAlreadyOpen(video))
                        await _navigationService.Navigate<VideoEditorViewModel, VideoModel>(video);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Navigationsfehler von MainViewModel zu SequenceEditorViewModel/VideoSelectionViewModel aufgetreten.");
                }
            }
        }
    }
}
