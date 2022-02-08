using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;

namespace iViewXExperimentCreator.Wpf
{
    /// <summary>
    /// Main Window acts as a shell for user controls/views e.g. MainView
    /// </summary>
    public partial class MainWindow : MvxWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void MaximizeWindow()
        {
            WindowStyle = System.Windows.WindowStyle.None;
            WindowState = System.Windows.WindowState.Maximized;
        }

        public void NormalizeWindow() 
        {
            WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
            WindowState = System.Windows.WindowState.Normal;
        }
    }
}
