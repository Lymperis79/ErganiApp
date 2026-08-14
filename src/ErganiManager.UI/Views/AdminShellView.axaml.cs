using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ErganiManager.UI.ViewModels;

namespace ErganiManager.UI.Views;

public partial class AdminShellView : Window
{
    public AdminShellView()
    {
        InitializeComponent();
    }
}

/// <summary>
/// Maps each section ViewModel type to its corresponding UserControl view.
/// Used by AdminShellView's ContentControl so navigating sections just means
/// swapping CurrentSectionViewModel — no manual view instantiation needed.
/// </summary>
public class SectionViewModelTemplateSelector : IDataTemplate
{
    public Control? Build(object? data)
    {
        return data switch
        {
            CompaniesViewModel => new CompaniesView { DataContext = data },
            BranchesViewModel => new BranchesView { DataContext = data },
            EmployeesViewModel => new EmployeesView { DataContext = data },
            UsersViewModel => new UsersView { DataContext = data },
            SchedulesViewModel => new SchedulesView { DataContext = data },
            WorkCardHistoryViewModel => new WorkCardHistoryView { DataContext = data },
            OvertimeViewModel => new OvertimeView { DataContext = data },
            SubmissionLogViewModel => new SubmissionLogView { DataContext = data },
            _ => null
        };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
