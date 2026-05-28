using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public static class AttendanceStatusDisplay
{
    public static string GetLabel(AttendanceStatus status)
    {
        return status switch
        {
            AttendanceStatus.Present => "Present",
            AttendanceStatus.Absent => "Absent",
            AttendanceStatus.Sick => "Sick",
            AttendanceStatus.FieldTrip => "Field Trip",
            AttendanceStatus.Holiday => "Holiday",
            AttendanceStatus.Partial => "Partial",
            _ => status.ToString()
        };
    }

    public static string GetBadgeColor(AttendanceStatus status)
    {
        return status switch
        {
            AttendanceStatus.Present => "success",
            AttendanceStatus.Absent => "danger",
            AttendanceStatus.Sick => "warning",
            AttendanceStatus.FieldTrip => "info",
            AttendanceStatus.Holiday => "secondary",
            AttendanceStatus.Partial => "primary",
            _ => "secondary"
        };
    }

    public static string GetShortLabel(AttendanceStatus status)
    {
        return status switch
        {
            AttendanceStatus.Present => "P",
            AttendanceStatus.Absent => "A",
            AttendanceStatus.Sick => "S",
            AttendanceStatus.FieldTrip => "FT",
            AttendanceStatus.Holiday => "H",
            AttendanceStatus.Partial => "PA",
            _ => "?"
        };
    }
}
