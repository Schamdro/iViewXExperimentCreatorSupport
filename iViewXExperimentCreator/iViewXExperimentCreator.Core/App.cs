using MvvmCross.ViewModels;
using iViewXExperimentCreator.Core.ViewModels;
namespace iViewXExperimentCreator.Core
{
    /// <summary>
    /// Schnittstelle für MVVMCross. Bei MVVM wird die Applikation über das View-Projekt gestartet.
    /// MVVMCross verwendet als Einstiegspunkt in das Core-Projekt die App-Klasse. 
    /// </summary>
    public class App : MvxApplication
    {
        /// <summary>
        /// Startet das MainViewModel.
        /// </summary>
        public override void Initialize()
        {
            RegisterAppStart<MainViewModel>();
            Logger.Message("Programm gestartet.");
            Logger.Message("Erstellen oder laden Sie ein Experiment mit den Kontrollelementen am Bildschirmrand links oben.");
        }
    }
}
