using MvvmCross.ViewModels;
using iViewXExperimentCreator.Core.ViewModels;
using System.Collections.Generic;
using System.Linq;
using iViewXExperimentCreator.Core.Util;
using System.ComponentModel;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace iViewXExperimentCreator.Core.Models
{
    /// <summary>
    /// Eine Sequenzkomponente. Aus Sequenzen können Reizabfolgen und -überlagerungen erstellt werden. Sie bestehen aus einer Anzahl an
    /// Slots, welche die relevanten Informationen zur Generierung von Snapshots enthalten.
    /// </summary>
    public class SequenceModel : MvxNotifyPropertyChanged, IExperimentComponent
    {
        private string _name;
        private bool _active;

        public string Name
        {
            get { return _name; }
            set 
            {
                value = value.GetFreeNameInCollection(MainViewModel.Instance.ExperimentComponents);
                //if(_name is not null) Logger.Message($"Sequenz '{ Name }' wurde in '{ value }' umbenannt.");
                SetProperty(ref _name, value); 
            }
        }
        public bool Active
        {
            get { return _active; }
            set { SetProperty(ref _active, value); }
        }
        public float Duration
        {
            get
            {
                if (Slots.Count == 0) return 0f;
                return Slots.ToList().Max(sl => sl.EndTime);
            }
        }

        private void HandleSlotChanged(object sender, PropertyChangedEventArgs e)
        {
            ProcessChanges();
            MainViewModel.Instance.SetPreviewToFirstSlotOccurence(sender as SlotModel);
        }

        private void HandleSlotListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Slots is null) return;
            RefreshSlotSubscribtions();
            ProcessChanges();
        }

        private void ProcessChanges()
        {
            UpdateSnapshots();
            RaisePropertyChanged("Slots");
            RaisePropertyChanged("Duration");
        }
        
        public void UpdateSnapshots()
        {
            if (Slots is null || ExperimentFileManagerModel.CurrentExperiment is null) return;
            Snapshots.Clear();
            foreach (var ss in CreateSnapshotPlaylist())
            {
                Snapshots.Add(ss);
            }
            //Logger.Debug(""+MainViewModel.Instance.ExperimentComponents.Count);
            MainViewModel.Instance.RaisePropertyChanged("ExperimentSnapshots");
            //MainViewModel.Instance.RefreshPreviewImage();
        }

        private void RefreshSlotSubscribtions()
        {
            UnsubscribeAllSlots();
            SubscribeAllSlots();
        }

        private void UnsubscribeAllSlots()
        {
            foreach(var sl in Slots)
            {
                sl.PropertyChanged -= HandleSlotChanged;
            }
        }

        private void SubscribeAllSlots()
        {
            foreach(var sl in Slots)
            {
                sl.PropertyChanged += HandleSlotChanged;
            }
        }

        /// <summary>
        /// Generiert eine Liste aus Snapshots für eine Sequenz.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private List<SnapshotModel> CreateSnapshotPlaylist()
        {
            List<float> relevantTimestamps = GetRelevantTimestampsInSlotList();
            List<SnapshotModel> snapshots = new();

            for (int i = 0; i < relevantTimestamps.Count; i++)
            {
                /* Der Snapshot soll nur pausieren, aufnehmen, oder die Aufnahme stoppen, wenn der Slot, der diese
                 * Eigenschaften hat, dort startet oder endet (entsprechend).*/

                bool PausesAtStart(SlotModel sl)
                    => relevantTimestamps[i] == sl.StartTime && sl.Pauses;

                bool pauses = Slots.Any(sl => PausesAtStart(sl));

                //Alle Slots, die bisher gestartet aber noch nicht geendet haben, werden hinzugefügt.
                List<SlotModel> activeSlots = Slots.Where(sl => relevantTimestamps[i] >= sl.StartTime &&
                                                                relevantTimestamps[i] < sl.EndTime).ToList();

                if(activeSlots.Count > 0) snapshots.Add(CreateSnapshot(activeSlots, i, relevantTimestamps, pauses));
            }
            return snapshots;
        }

        private List<float> GetRelevantTimestampsInSlotList()
        {
            List<float> relevantTimestamps = new();
            relevantTimestamps.Add(0f);
            foreach (SlotModel sl in Slots)
            {
                // Slots ohne Dauer ergeben keinen Sinn
                if (sl.Duration == 0) continue;

                //Falls ein Zeitstempel bereits vorhanden ist es nicht notwendig, dass dieser aus 
                //einer anderen Quelle erneut hinzugefügt wird.
                if (!relevantTimestamps.Contains(sl.StartTime)) relevantTimestamps.Add(sl.StartTime);
                if (!relevantTimestamps.Contains(sl.EndTime)) relevantTimestamps.Add(sl.EndTime);
            }

            relevantTimestamps.Sort();
            return relevantTimestamps;
        }

        private SnapshotModel CreateSnapshot(List<SlotModel> activeSlots, int i, List<float> relevantTimestamps, bool pauses)
        {
            if (ExperimentFileManagerModel.CurrentExperiment == null)
            {
                return null;
            }

            //Berechnung der Dauer eines Snapshots
            float duration = 0f;
            if (i < relevantTimestamps.Count - 1) //Wie lange dauert es bis zum nächsten Snapshot?
            {
                float timeToNextSnap = relevantTimestamps[i + 1] - relevantTimestamps[i];
                duration = timeToNextSnap;
            }
            else if (activeSlots.Count > 0) //Wie lange dauert es bis der Snapshot vorbei ist?
                duration = activeSlots.Max(sl => sl.EndTime) - relevantTimestamps[i];

            string snapshotName = $"{ExperimentFileManagerModel.CurrentExperiment.Components.IndexOf(this)}-{i}-{Name}";

            SnapshotModel.SnapshotInputData data = new(
                activeSlots,
                duration,
                pauses,
                snapshotName);

            return new(data);
        }

        private MvxObservableCollection<SlotModel> _slots;
        public MvxObservableCollection<SlotModel> Slots { get => _slots; init => SetProperty(ref _slots, value); }

        private MvxObservableCollection<SnapshotModel> _snapshots;
        public MvxObservableCollection<SnapshotModel> Snapshots { get => _snapshots; init => SetProperty(ref _snapshots, value); }

        /// <summary>
        /// Initialisiert die Sequenz.
        /// </summary>
        /// <param name="name">Sequenzname, Standardname ist "Default"</param>
        /// <param name="slots">Vordefinierte Slots</param>
        /// <param name="loaded">Wird die Sequenz von der Festplatte geladen oder neu erstellt?</param>
        public SequenceModel(string name)
        {
            Name = name;
            Slots = new();
            Active = true;
            Snapshots = new();

            HandleLoadUp();

            Logger.Message($"Neue Sequenz erstellt.");
        }

        [JsonConstructor]
        public SequenceModel(){}

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            HandleLoadUp();
            Logger.Message($"Sequenz geladen: '{Name}'");
        }

        public void HandleLoadUp()
        {
            Slots.CollectionChanged += HandleSlotListChanged;
            UnsubscribeAllSlots();
            SubscribeAllSlots();
            UpdateSnapshots();
        }
    }
}
