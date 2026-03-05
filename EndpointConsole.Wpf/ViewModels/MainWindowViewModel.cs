using CommunityToolkit.Mvvm.ComponentModel;

namespace EndpointConsole.Wpf.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IReadOnlyDictionary<AppPage, ViewModelBase> _pages;

    [ObservableProperty]
    private NavigationItem? selectedNavigationItem;

    [ObservableProperty]
    private ViewModelBase? currentPageViewModel;

    public MainWindowViewModel(
        DashboardViewModel dashboardViewModel,
        ServicesViewModel servicesViewModel,
        DiagnosticsViewModel diagnosticsViewModel)
    {
        NavigationItems =
        [
            new NavigationItem(AppPage.Dashboard, "Dashboard", "Machine summary and session activity"),
            new NavigationItem(AppPage.Services, "Services", "Managed service state and control surface"),
            new NavigationItem(AppPage.Diagnostics, "Diagnostics", "Support bundle workflow and logs")
        ];

        _pages = new Dictionary<AppPage, ViewModelBase>
        {
            [AppPage.Dashboard] = dashboardViewModel,
            [AppPage.Services] = servicesViewModel,
            [AppPage.Diagnostics] = diagnosticsViewModel
        };

        SelectedNavigationItem = NavigationItems[0];
    }

    public string Title => "EndpointConsole";

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (value is null)
        {
            return;
        }

        CurrentPageViewModel = _pages[value.Page];
    }
}
