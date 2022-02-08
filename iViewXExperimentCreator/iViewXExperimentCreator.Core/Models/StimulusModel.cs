using MvvmCross.ViewModels;
using System.Drawing;
using System.IO;
using iViewXExperimentCreator.Core.Util;
using iViewXExperimentCreator.Core.Enums;
using MediaToolkit;
using Newtonsoft.Json;
using System;
using System.Threading;

namespace iViewXExperimentCreator.Core.Models
{
    /// <summary>
    /// Repräsentiert einen Reiz. Enthält dessen Dateipfad und Namen.
    /// </summary>
    public class StimulusModel : MvxNotifyPropertyChanged, IHasName
    {
        private string _filePath;
        /// <summary>
        /// Dateipfad des Reizes.
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (value.StartsWith(@"file:///")) value = value.Remove(0, 8);
                SetProperty(ref _filePath, value);
            }
        }

        [JsonProperty]
        private int _resX = 0;
        /// <summary>
        /// Breite des Reizes (momentan nur Bildreize unterstützt).
        /// </summary>
        [JsonIgnore]
        public int ResX => _resX;

        [JsonProperty]
        private int _rexY = 0;
        /// <summary>
        /// Höhe des Reizes (momentan nur Bildreize unterstützt).
        /// </summary>
        [JsonIgnore]
        public int ResY => _rexY;

        private string _name;
        /// <summary>
        /// Name des Reizes. Dies ist der Dateiname mit Extension.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set 
            {
                _name = value; 
            }
        }

        /// <summary>
        /// Konstruktor des StimulusModel.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="loaded"></param>
        public StimulusModel(string filePath, ExtensionType ext)
        {
            _name = Path.GetFileName(filePath);
            FilePath = filePath;

            int tries = 3;

            for(int i = 0; i < tries; i++)
            {
                if (SetResolutionFromFile(ext, i == tries - 1)) break;
                if(i < tries - 1) Thread.Sleep(100); //Warten, falls File evtl noch nicht 100% erstellt ist
            }

            Logger.Message($"Stimulus geladen: '{ Name }'");
        }

        /// <summary>
        /// Öffnet die Bilddatei und lädt die Auflösung der Datei.
        /// </summary>
        /// <param name="ext"></param>
        public bool SetResolutionFromFile(ExtensionType ext, bool showError)
        {
            bool success = false;
            if (ext == ExtensionType.Image)
            {
                try
                {
                    Image img = Image.FromFile(FilePath);
                    _resX = img.Width;
                    _rexY = img.Height;
                    img.Dispose();
                    success = true;
                } 
                catch (Exception e)
                {
                    if(showError) Logger.Error(e, "Fehler beim Öffnen des Reizes " + FilePath);
                    success = false;
                }
            }
            if (ext == ExtensionType.Video)
            {
                using (Engine engine = new())
                {
                    //falls Res bei einem Video-Stimulus mal gebraucht wird
                }
            }

            return success;
        }

        /// <summary>
        /// Falls bei einer Änderung des Experimentverzeichnisses (z.B. durch Umbenennung) eine Änderung
        /// des Pfades erfordert wird, kann dies hiermit bewirkt werden.
        /// </summary>
        /// <param name="stimDir"></param>
        public void UpdateFilePath(string stimDir)
        {
            SetProperty(ref _filePath, Path.Combine(stimDir, Name));
            RaisePropertyChanged("FilePath");
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
