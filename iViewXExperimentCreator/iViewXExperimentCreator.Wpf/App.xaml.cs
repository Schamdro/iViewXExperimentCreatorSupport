using MvvmCross.Core;
using MvvmCross.Platforms.Wpf.Core;
using MvvmCross.Platforms.Wpf.Views;

namespace iViewXExperimentCreator.Wpf
{
    public partial class App : MvxApplication
    {
        /// <summary>
        /// Starts the MvxStarter.Core.App.cs which launches the Application. 
        /// This connects the view project (MvxStarter.Wpf) to the core class library (MvxStarter.Core).
        /// </summary>
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<MvxWpfSetup<Core.App>>();
        }
    }
}
