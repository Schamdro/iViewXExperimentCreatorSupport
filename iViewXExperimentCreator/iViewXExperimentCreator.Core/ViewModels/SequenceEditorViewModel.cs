using iViewXExperimentCreator.Core.Models;
using iViewXExperimentCreator.Core.Subroutines;
using iViewXExperimentCreator.Core.Util;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iViewXExperimentCreator.Core.ViewModels
{

    /// <summary>
    /// ViewModel für die GUI zur Bearbeitung eine Sequenz.
    /// </summary>
    public class SequenceEditorViewModel : MvxViewModel<SequenceModel>
    {
        private readonly float TIME_STEPS = 0.1f;

        private static List<SequenceEditorViewModel> _instances = new();
        /// <summary>
        /// Die offenen Instanzen dieses ViewModels.
        /// </summary>
        public static List<SequenceEditorViewModel> Instances
        {
            get => _instances;
        }

        /// <summary>
        /// Referenz zur Liste der Bildreize im MainViewModel.
        /// </summary>
        public static MvxObservableCollection<StimulusModel> Stimuli { get => ExperimentFileManagerModel.CurrentExperiment.ImageStimuli; }
        private IMvxNavigationService _navigationService;

        private static StimulusModel _selectedStimulus;
        /// <summary>
        /// Der ausgewählte Stimulus.
        /// </summary>
        public static StimulusModel SelectedStimulus
        {
            get { return _selectedStimulus; }
            set { _selectedStimulus = value; }
        }

        private SequenceModel _selectedSequence;
        /// <summary>
        /// Die in diesem Editor editierte Sequenz.
        /// </summary>
        public SequenceModel SelectedSequence
        {
            get { return _selectedSequence; }
            set { SetProperty(ref _selectedSequence, value); }
        }
        /// <summary>
        /// Der Konstruktor.
        /// </summary>
        /// <param name="navigationService"></param>
        public SequenceEditorViewModel(IMvxNavigationService navigationService)
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

            AddNewSlotCommand = new MvxCommand(AddNewSlot);
            ImportStimulusCommand = new MvxCommand<string>(ImportStimulus);
            DeleteSlotCommand = new MvxCommand<SlotModel>(DeleteSlot);
            DuplicateSlotCommand = new MvxCommand<SlotModel>(DuplicateSlot);
            RemoveStimulusFromSlotCommand = new MvxCommand<SlotModel>(RemoveStimulusFromSlot);
            IncrementSlotLayerCommand = new MvxCommand<SlotModel>(IncrementSlotLayer);
            DecrementSlotLayerCommand = new MvxCommand<SlotModel>(DecrementSlotLayer);
            CenterSlotCommand = new MvxCommand<SlotModel>(CenterSlot);
            StretchSlotCommand = new MvxCommand<SlotModel>(StretchSlot);
            ChangeStimulusCommand = new MvxCommand<(SlotModel, StimulusModel)>(ChangeStimulus);
            IncrementSlotStartTimeCommand = new MvxCommand<SlotModel>(IncrementSlotStartTime);
            DecrementSlotStartTimeCommand = new MvxCommand<SlotModel>(DecrementSlotStartTime);
            IncrementSlotDurationCommand = new MvxCommand<SlotModel>(IncrementSlotDuration);
            DecrementSlotDurationCommand = new MvxCommand<SlotModel>(DecrementSlotDuration);

            Instances.Add(this);
        }

        /// <summary>
        /// Nimmt den Übergabeparameter des Navigationsservices entgegen.
        /// </summary>
        /// <param name="parameter"></param>
        public override void Prepare(SequenceModel parameter)
        {
            SelectedSequence = parameter;
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
        public static bool IsSequenceAlreadyOpen(SequenceModel seq)
        {
            foreach(SequenceEditorViewModel vm in Instances)
            {
                if (vm.SelectedSequence == seq) return true;
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
        /// Commandwrapper für AddNewSlot.
        /// </summary>
        public IMvxCommand AddNewSlotCommand { get; private set; }
        /// <summary>
        /// Fügt der Sequenz einen neuen Slot hinzu.
        /// </summary>
        private void AddNewSlot() => SelectedSequence.Slots.Add(new());

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
        /// Commandwrapper für DeleteSlot.
        /// </summary>
        public IMvxCommand<SlotModel> DeleteSlotCommand { get; private set; }
        /// <summary>
        /// Löscht den übergebenen Slot aus der Liste der Slots.
        /// </summary>
        /// <param name="slot"></param>
        private void DeleteSlot(SlotModel slot)
        {
            SelectedSequence.Slots.Remove(slot);
        }

        /// <summary>
        /// Commandwrapper für DuplicateSlot.
        /// </summary>
        public IMvxCommand<SlotModel> DuplicateSlotCommand { get; private set; }
        /// <summary>
        /// Verdoppelt den übergebenen Slot.
        /// </summary>
        /// <param name="slot"></param>
        private void DuplicateSlot(SlotModel slot)
        {
            SelectedSequence.Slots.Add(slot.DeepCopy());
        }

        /// <summary>
        /// Commandwarapper für RemoveStimulusFromSlot.
        /// </summary>
        public IMvxCommand<SlotModel> RemoveStimulusFromSlotCommand { get; private set; }
        /// <summary>
        /// Entfernt den im Slot gehaltenen Stimulus. 
        /// </summary>
        /// <param name="slot"></param>
        private void RemoveStimulusFromSlot(SlotModel slot)
        {
            slot.Stimulus = null;
        }

        /// <summary>
        /// Commandwrapper für IncrementSlotLayer.
        /// </summary>
        public IMvxCommand<SlotModel> IncrementSlotLayerCommand { get; private set; }
        /// <summary>
        /// Erhöht den Layer des übergebenen Slots um 1.
        /// </summary>
        /// <param name="slot"></param>
        private void IncrementSlotLayer(SlotModel slot)
        {
            slot.Layer += 1;
        }

        /// <summary>
        /// Commandwrapper für DecrementSlotLayer.
        /// </summary>
        public IMvxCommand<SlotModel> DecrementSlotLayerCommand { get; private set; }
        /// <summary>
        /// Verringert den Layer des übergebenen Slots um 1.
        /// </summary>
        /// <param name="slot"></param>
        private void DecrementSlotLayer(SlotModel slot)
        {
            slot.Layer -= 1;
        }

        /// <summary>
        /// Commandwrapper für IncrementSlotStartTime.
        /// </summary>
        public IMvxCommand<SlotModel> IncrementSlotStartTimeCommand { get; private set; }

        /// <summary>
        /// Erhöht die Anfangszeit des übergebenen Slots um 0.1.
        /// </summary>
        /// <param name="slot"></param>
        private void IncrementSlotStartTime(SlotModel slot)
        {
            slot.StartTime += TIME_STEPS;
        }

        /// <summary>
        /// Commandwrapper für DecrementSlotStartTime.
        /// </summary>
        public IMvxCommand<SlotModel> DecrementSlotStartTimeCommand { get; private set; }
        /// <summary>
        /// Verringert die Anfangszeit des übergebenen Slots um 0.1.
        /// </summary>
        /// <param name="slot"></param>
        private void DecrementSlotStartTime(SlotModel slot)
        {
            slot.StartTime -= TIME_STEPS;
        }

        /// <summary>
        /// Commandwrapper für IncrementSlotDuration.
        /// </summary>
        public IMvxCommand<SlotModel> IncrementSlotDurationCommand { get; private set; }
        /// <summary>
        /// Erhöht die Dauer des übergebenen Slots um 0.1
        /// </summary>
        /// <param name="slot"></param>
        private void IncrementSlotDuration(SlotModel slot)
        {
            slot.Duration += TIME_STEPS;
        }

        /// <summary>
        /// Commandwrapper für DecrementSlotDuration.
        /// </summary>
        public IMvxCommand<SlotModel> DecrementSlotDurationCommand { get; private set; }
        /// <summary>
        /// Verringert die Dauer des übergebenen Slots um 0.1.
        /// </summary>
        /// <param name="slot"></param>
        private void DecrementSlotDuration(SlotModel slot)
        {
            slot.Duration -= TIME_STEPS;
        }

        /// <summary>
        /// Commandwrapper für CenterSlot.
        /// </summary>
        public IMvxCommand<SlotModel> CenterSlotCommand { get; private set; }
        /// <summary>
        /// Änder die Position des übergebenen Slots so, dass diese im Snapshot zentriert ist.
        /// </summary>
        /// <param name="slot"></param>
        private void CenterSlot(SlotModel slot)
        {
            slot.CenterStimulus();
        }

        /// <summary>
        /// Wrapper für StretchSlot.
        /// </summary>
        public IMvxCommand<SlotModel> StretchSlotCommand { get; private set; }
        /// <summary>
        /// Ändert die Skalierung des übergebenen Slots so, dass diese an ihrer längsten Seite nicht größer als die Auflösung ist.
        /// </summary>
        /// <param name="slot"></param>
        private void StretchSlot(SlotModel slot)
        {
            slot.StretchStimulus();
        }

        /// <summary>
        /// Commandwrapper für ChangeStimulus.
        /// </summary>
        public IMvxCommand<(SlotModel, StimulusModel)> ChangeStimulusCommand { get; private set; }
        /// <summary>
        /// Ändert den Stimulus des im Tupel übergebenen Slots zum im Tupel übergebenen Stimulus.
        /// </summary>
        /// <param name="values"></param>
        private void ChangeStimulus((SlotModel slot, StimulusModel sm) values)
        {
            values.slot.Stimulus = values.sm;
        }
    }
}
