using CommunityToolkit.Mvvm.ComponentModel;

namespace Catchlogr.Mobile.ViewModels;

/// <summary>
/// Base class for all ViewModels in the Catchlogr mobile app.
/// <para>
/// Uses CommunityToolkit.Mvvm source generators — <c>[ObservableProperty]</c> on a
/// private field automatically generates a public property with INotifyPropertyChanged.
/// </para>
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    /// <summary>
    /// True while an async operation is running.
    /// Bind to activity indicators and use IsNotBusy to enable/disable buttons.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Page or section title. Bind to the shell title bar.
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    /// <summary>
    /// Convenience inverse of <see cref="IsBusy"/> for button IsEnabled bindings.
    /// </summary>
    public bool IsNotBusy => !IsBusy;
    
    protected BaseViewModel()
    {
    }
}
