using MvvmCross.ViewModels;

namespace iViewXExperimentCreator.Core.Util
{
    /// <summary>
    /// Wird von allen Experimentkomponenten implementiert.
    /// </summary>
    public interface IExperimentComponent : IMvxNotifyPropertyChanged, IHasName
    {
        public bool Active { get; set; }
        public float Duration { get; }
    }
}
