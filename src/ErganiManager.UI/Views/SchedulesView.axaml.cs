using Avalonia.Controls;
using Avalonia.Interactivity;
using ErganiManager.UI.ViewModels;

namespace ErganiManager.UI.Views;

public partial class SchedulesView : UserControl
{
    public SchedulesView()
    {
        InitializeComponent();
    }

    private void OnCellClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CalendarCellViewModel cell } && DataContext is SchedulesViewModel vm)
            vm.OpenDayCommand.Execute(cell);
    }
}
