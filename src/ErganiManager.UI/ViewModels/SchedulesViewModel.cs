using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErganiManager.Core.Interfaces;
using ErganiManager.Core.Models;
using ErganiManager.UI.Services;

namespace ErganiManager.UI.ViewModels;

/// <summary>One visual cell in the month grid. Empty cells (padding before the
/// 1st / after the last day) have IsInMonth = false and no Day.</summary>
public partial class CalendarCellViewModel : ViewModelBase
{
    public bool IsInMonth { get; init; }
    public DateOnly? Date { get; init; }

    [ObservableProperty] private AppWorkType? _workType;
    [ObservableProperty] private string _timeRangeText = string.Empty;
    [ObservableProperty] private bool _hasSchedule;
    [ObservableProperty] private bool _isToday;

    public string DayNumberText => Date?.Day.ToString() ?? string.Empty;

    public string WorkTypeIcon => WorkType switch
    {
        AppWorkType.Office => "🏢",
        AppWorkType.Home => "🏠",
        AppWorkType.Rest => "💤",
        AppWorkType.Absent => "❌",
        _ => ""
    };
}

public partial class SchedulesViewModel : ViewModelBase, IAdminSectionViewModel
{
    private readonly IScheduleService _scheduleService;
    private readonly IEmployeeService _employeeService;
    private readonly IBranchService _branchService;

    private UserSession? _session;

    public ObservableCollection<EmployeeDto> AvailableEmployees { get; } = new();
    [ObservableProperty] private EmployeeDto? _selectedEmployee;

    [ObservableProperty] private int _year = DateTime.Today.Year;
    [ObservableProperty] private int _month = DateTime.Today.Month;
    public string MonthLabel => new DateOnly(Year, Month, 1).ToString("MMMM yyyy");

    public ObservableCollection<CalendarCellViewModel> Cells { get; } = new();

    [ObservableProperty] private bool _hasActiveCompany;
    [ObservableProperty] private string _noCompanyMessage = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // Day-edit dialog state
    [ObservableProperty] private bool _isDayDialogOpen;
    [ObservableProperty] private DateOnly _editingDate;
    [ObservableProperty] private int _editingScheduleId;
    [ObservableProperty] private AppWorkType _editingWorkType = AppWorkType.Office;
    [ObservableProperty] private TimeSpan? _editingStartTime = new(9, 0, 0);
    [ObservableProperty] private TimeSpan? _editingEndTime = new(17, 0, 0);
    [ObservableProperty] private string _editingComments = string.Empty;
    [ObservableProperty] private string _editingActualText = string.Empty;
    [ObservableProperty] private string _editingSubmissionText = string.Empty;
    [ObservableProperty] private BranchDto? _editingBranch;
    public ObservableCollection<BranchDto> AvailableBranches { get; } = new();
    public ObservableCollection<AppWorkType> AvailableWorkTypes { get; } = new(Enum.GetValues<AppWorkType>());

    public bool ShowTimeFields => EditingWorkType is AppWorkType.Office or AppWorkType.Home;
    public bool IsEditingExisting => EditingScheduleId != 0;

    public SchedulesViewModel(IScheduleService scheduleService, IEmployeeService employeeService, IBranchService branchService)
    {
        _scheduleService = scheduleService;
        _employeeService = employeeService;
        _branchService = branchService;
    }

    public void Initialize(UserSession session)
    {
        _session = session;
        HasActiveCompany = session.CompanyId.HasValue;
        NoCompanyMessage = session.CompanyId.HasValue
            ? string.Empty
            : "Select a company first (Super Admin: pick a company from the Companies tab).";

        if (HasActiveCompany)
            _ = LoadEmployeesAsync();
    }

    private async Task LoadEmployeesAsync()
    {
        if (_session?.CompanyId is not int companyId)
            return;

        var employees = await _employeeService.GetByCompanyAsync(companyId, activeOnly: true);
        AvailableEmployees.Clear();
        foreach (var e in employees)
            AvailableEmployees.Add(e);

        var branches = await _branchService.GetByCompanyAsync(companyId);
        AvailableBranches.Clear();
        foreach (var b in branches)
            AvailableBranches.Add(b);

        if (AvailableEmployees.Count > 0)
            SelectedEmployee = AvailableEmployees[0];
    }

    partial void OnSelectedEmployeeChanged(EmployeeDto? value) => _ = LoadMonthAsync();
    partial void OnYearChanged(int value) { OnPropertyChanged(nameof(MonthLabel)); _ = LoadMonthAsync(); }
    partial void OnMonthChanged(int value) { OnPropertyChanged(nameof(MonthLabel)); _ = LoadMonthAsync(); }
    partial void OnEditingWorkTypeChanged(AppWorkType value) => OnPropertyChanged(nameof(ShowTimeFields));
    partial void OnEditingScheduleIdChanged(int value) => OnPropertyChanged(nameof(IsEditingExisting));

    [RelayCommand]
    private void PreviousMonth()
    {
        var d = new DateOnly(Year, Month, 1).AddMonths(-1);
        Year = d.Year;
        Month = d.Month;
    }

    [RelayCommand]
    private void NextMonth()
    {
        var d = new DateOnly(Year, Month, 1).AddMonths(1);
        Year = d.Year;
        Month = d.Month;
    }

    private async Task LoadMonthAsync()
    {
        Cells.Clear();
        if (SelectedEmployee == null)
            return;

        var schedules = await _scheduleService.GetMonthAsync(SelectedEmployee.Id, Year, Month);
        var scheduleByDate = schedules.ToDictionary(s => s.ScheduleDate);

        var firstOfMonth = new DateOnly(Year, Month, 1);
        var daysInMonth = DateTime.DaysInMonth(Year, Month);

        // Monday-first grid: ISO DayOfWeek has Sunday = 0, so shift so Monday = 0.
        int leadingBlanks = ((int)firstOfMonth.DayOfWeek + 6) % 7;

        for (int i = 0; i < leadingBlanks; i++)
            Cells.Add(new CalendarCellViewModel { IsInMonth = false });

        var today = DateOnly.FromDateTime(DateTime.Today);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(Year, Month, day);
            scheduleByDate.TryGetValue(date, out var sched);

            var cell = new CalendarCellViewModel
            {
                IsInMonth = true,
                Date = date,
                IsToday = date == today,
                HasSchedule = sched != null,
                WorkType = sched?.WorkType,
                TimeRangeText = sched is { StartTime: not null, EndTime: not null }
                    ? $"{sched.StartTime:HH:mm}-{sched.EndTime:HH:mm}"
                    : string.Empty
            };
            Cells.Add(cell);
        }
    }

    [RelayCommand]
    private async Task OpenDayAsync(CalendarCellViewModel cell)
    {
        if (!cell.IsInMonth || cell.Date == null || SelectedEmployee == null)
            return;

        EditingDate = cell.Date.Value;
        StatusMessage = string.Empty;

        var existing = await _scheduleService.GetByDateAsync(SelectedEmployee.Id, EditingDate);

        if (existing != null && existing.Id != 0)
        {
            EditingScheduleId = existing.Id;
            EditingWorkType = existing.WorkType;
            EditingStartTime = existing.StartTime?.ToTimeSpan() ?? new TimeSpan(9, 0, 0);
            EditingEndTime = existing.EndTime?.ToTimeSpan() ?? new TimeSpan(17, 0, 0);
            EditingComments = existing.Comments ?? string.Empty;
            EditingBranch = AvailableBranches.FirstOrDefault(b => b.Id == existing.BranchId) ?? AvailableBranches.FirstOrDefault();
            EditingSubmissionText = existing.SubmittedToErgani
                ? $"✅ Submitted to Ergani — Protocol: {existing.Protocol}"
                : "⏳ Not yet submitted to Ergani";
        }
        else
        {
            EditingScheduleId = 0;
            EditingWorkType = AppWorkType.Office;
            EditingStartTime = new TimeSpan(9, 0, 0);
            EditingEndTime = new TimeSpan(17, 0, 0);
            EditingComments = string.Empty;
            EditingBranch = AvailableBranches.FirstOrDefault(b => b.Id == SelectedEmployee.BranchId) ?? AvailableBranches.FirstOrDefault();
            EditingSubmissionText = "No schedule on file for this day.";
        }

        EditingActualText = existing is { ActualArrival: not null } or { ActualDeparture: not null }
            ? $"Actual: {existing?.ActualArrival:HH:mm} → {existing?.ActualDeparture:HH:mm}"
            : string.Empty;

        IsDayDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDayDialog() => IsDayDialogOpen = false;

    [RelayCommand]
    private async Task SaveDayAsync()
    {
        if (SelectedEmployee == null || EditingBranch == null)
        {
            StatusMessage = "Please select a branch.";
            return;
        }

        try
        {
            await _scheduleService.UpsertDayAsync(new ScheduleDayDto
            {
                Id = EditingScheduleId,
                EmployeeId = SelectedEmployee.Id,
                BranchId = EditingBranch.Id,
                ScheduleDate = EditingDate,
                WorkType = EditingWorkType,
                StartTime = ShowTimeFields && EditingStartTime.HasValue
                    ? TimeOnly.FromTimeSpan(EditingStartTime.Value) : null,
                EndTime = ShowTimeFields && EditingEndTime.HasValue
                    ? TimeOnly.FromTimeSpan(EditingEndTime.Value) : null,
                Comments = EditingComments
            });

            IsDayDialogOpen = false;
            await LoadMonthAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportMonth()
    {
        if (SelectedEmployee == null)
        {
            StatusMessage = "Select an employee first.";
            return;
        }

        try
        {
            var schedules = _scheduleService.GetMonthAsync(SelectedEmployee.Id, Year, Month).GetAwaiter().GetResult();
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = ExcelImportExportService.ExportMonthSchedule(
                SelectedEmployee.FullName, Year, Month, schedules, folder);
            StatusMessage = $"✅ Exported to Desktop: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteDayAsync()
    {
        if (EditingScheduleId == 0)
        {
            IsDayDialogOpen = false;
            return;
        }

        await _scheduleService.DeleteDayAsync(EditingScheduleId);
        IsDayDialogOpen = false;
        await LoadMonthAsync();
    }
}
