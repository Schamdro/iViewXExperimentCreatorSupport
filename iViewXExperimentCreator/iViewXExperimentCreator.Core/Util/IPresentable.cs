using MvvmCross.ViewModels;

namespace iViewXExperimentCreator.Core.Util
{
    /// <summary>
    /// Wird von allen Präsentationselementen implementiert.
    /// </summary>
    public interface IPresentable : IMvxNotifyPropertyChanged, IHasName
    {
        public float Duration { get; set; }
        public bool Pauses { get; set; }
    }
}
