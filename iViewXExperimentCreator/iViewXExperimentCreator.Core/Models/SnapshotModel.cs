using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using MvvmCross.ViewModels;
using iViewXExperimentCreator.Core.Util;
using Newtonsoft.Json;

namespace iViewXExperimentCreator.Core.Models
{
    /// <summary>
    /// Hält einen Snapshot aus Slots, die zu einem Zeitpunkt aktiv sind, fest und wandelt 
    /// enthaltene Daten in ein zusammenhängendes Bild um, wenn dieses angefordert wird.
    /// </summary>
    public class SnapshotModel : MvxNotifyPropertyChanged, IPresentable
    {
        private float _duration;
        private string _name;
        private bool _pauses;

        /// <summary>
        /// Die Dauer des Snapshots. 
        /// </summary>
        public float Duration { get => _duration; set => SetProperty(ref _duration, value); }
        /// <summary>
        /// Der Name des Snapshots.
        /// </summary>
        public string Name { get => _name; set => SetProperty(ref _name, value); }

        private Image _snapshotImage;
        /// <summary>
        /// Das aus dem Snapshot generierte Bild. Dieses wird immer dann generiert, wenn es
        /// das ERSTE Mal angefordert wird. Wird bei der Speicherung verworfen und muss nach
        /// dem Laden neu generiert werden.
        /// </summary>
        [JsonIgnore]
        public Image SnapshotImage => _snapshotImage ?? CreateSnapshotImage();

        /// <summary>
        /// Bestimmt, ob die Präsentation pausiert, wenn der Snapshot an der Reihe ist.
        /// </summary>
        public bool Pauses { get => _pauses; set => SetProperty(ref _pauses, value); }
        public List<SlotModel> SlotModels { get; init; }
        //public (int x, int y) Resolution { get; init; }


        /// <summary>
        /// Ein Snapshot erfordert recht viele Eingabeparameter, daher 
        /// bietet sich für die Eingabe ein eigener Datentyp an. 
        /// </summary>
        public record SnapshotInputData(
            List<SlotModel> SlotModels,
            //(int x, int y) Resolution,
            float Duration,
            bool PausesSequence,
            string SnapshotName);

        /// <summary>
        /// Der Konstruktor. Nimmt eine SnapshotInputData-Record entgegen, welches alle
        /// relevanten Daten enthält.
        /// </summary>
        /// <param name="data"></param>
        public SnapshotModel(SnapshotInputData data)
        {
            //Für Deserialisierung, damit kein leerer Konstruktor nötig ist.
            if (data == null) return;
            SlotModels = data.SlotModels;
            //Resolution = data.Resolution;
            Duration = data.Duration;
            Pauses = data.PausesSequence;
            Name = data.SnapshotName;
        }

        /// <summary>
        /// Erstellt das Snapshot-Bild aus den einzelnen Slots, die aktiv sind.
        /// </summary>
        /// <returns></returns>
        private Image CreateSnapshotImage()
        {
            //if (MainViewModel.Instance.CurrentExperiment is null) return null;
            string time = DateTime.Now.ToString("dd-MM-yyyy--HH-mm-ss");

            Logger.Debug($"Creating Snapshot...");
            //var watch = System.Diagnostics.Stopwatch.StartNew();


            Bitmap bmp = new(ExperimentFileManagerModel.CurrentExperiment.ResolutionX,
                ExperimentFileManagerModel.CurrentExperiment.ResolutionY); //Erstellt ein leeres Bitmap
            Graphics graphic = Graphics.FromImage(bmp); //Zur Bearbeitung als Graphics-Objekt parsen

            Color color = Color.White;

            if (ExperimentFileManagerModel.CurrentExperiment is not null)
                color = ExperimentFileManagerModel.CurrentExperiment.Background;
            graphic.Clear(color);

            //Alle Slots durchgehen und die einzelnen aktiven Bilder zusammenfügen
            foreach (SlotModel sl in SlotModels.OrderBy(sl => sl.Layer).ToList())
            {
                Image stimulus = new Bitmap(1, 1);
                try
                {
                    stimulus = Image.FromFile(sl.StimulusPath);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Fehler beim Erstellen des Snapshots. Der Reiz '{ sl.Stimulus.Name }' wurde nicht in den Snapshot übernommen.");
                }

                Point location = new(sl.XCoordinate - (int)(stimulus.Width * sl.Scale / 2),
                                     sl.YCoordinate - (int)(stimulus.Height * sl.Scale / 2));
                SizeF size = new(stimulus.Width * sl.Scale, stimulus.Height * sl.Scale);
                RectangleF slotParams = new(location, size);

                graphic.DrawImage(stimulus, slotParams); 
                stimulus.Dispose();
            }

            graphic.Flush();
            //watch.Stop();
            //Logger.Debug($"Snapshot created in {watch.ElapsedMilliseconds} ms.");
            _snapshotImage = bmp;
            return bmp;
        }

        public void Reset()
        {
            _snapshotImage = null;
            CreateSnapshotImage();
        }
    }
}
