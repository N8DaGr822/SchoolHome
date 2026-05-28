namespace HomeschoolManager.Application.Services;

public sealed record LearningTimeReport(
    DateTime StartDate,
    DateTime EndDate,
    int TotalMinutes,
    IReadOnlyList<LearningTimeStudentTotal> ByStudent,
    IReadOnlyList<LearningTimeSubjectTotal> BySubject,
    IReadOnlyList<LearningTimeDateTotal> ByDate)
{
    public double TotalHours => Math.Round(TotalMinutes / 60d, 2);
}

public sealed record LearningTimeStudentTotal(
    int StudentId,
    string StudentName,
    int Minutes)
{
    public double Hours => Math.Round(Minutes / 60d, 2);
}

public sealed record LearningTimeSubjectTotal(
    int SubjectId,
    string Subject,
    int Minutes)
{
    public double Hours => Math.Round(Minutes / 60d, 2);
}

public sealed record LearningTimeDateTotal(
    DateTime Date,
    int Minutes)
{
    public double Hours => Math.Round(Minutes / 60d, 2);
}
