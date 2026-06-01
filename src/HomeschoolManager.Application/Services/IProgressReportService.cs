namespace HomeschoolManager.Application.Services;

public interface IProgressReportService
{
    Task<ProgressReport> GenerateAsync(int studentId, DateTime startDate, DateTime endDate);
}
