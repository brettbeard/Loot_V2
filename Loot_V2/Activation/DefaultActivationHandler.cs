using Loot_V2.Contracts.Services;
using Loot_V2.ViewModels;
using Loot_V2.Views;

using Microsoft.UI.Xaml;

namespace Loot_V2.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;

    public DefaultActivationHandler(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        _navigationService.NavigateTo(typeof(TransactionsViewModel).FullName!, args.Arguments);

        await Task.CompletedTask;
    }
}
