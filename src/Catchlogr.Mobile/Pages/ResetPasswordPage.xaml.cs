using Catchlogr.Mobile.Core.ViewModels;

namespace Catchlogr.Mobile.Pages;

/// <summary>Displays the code-based password reset form.</summary>
public partial class ResetPasswordPage : ContentPage, IQueryAttributable
{
    /// <summary>Initializes a new reset-password page.</summary>
    public ResetPasswordPage(ResetPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    /// <summary>Forwards Shell navigation values to the portable ViewModel.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ((ResetPasswordViewModel)BindingContext).ApplyQueryAttributes(query);
    }
}
