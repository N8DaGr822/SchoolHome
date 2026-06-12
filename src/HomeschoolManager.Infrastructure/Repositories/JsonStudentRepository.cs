using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonStudentRepository : JsonRepositoryBase<Student>, IStudentRepository
{
    public JsonStudentRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<Student> Items(HomeschoolData data) => data.Students;

    protected override string EntityLabel => "Student";

    private protected override Student Hydrate(HomeschoolData data, Student entity) =>
        RepositoryProjection.HydrateStudent(data, entity);

    private protected override IEnumerable<Student> Order(HomeschoolData data, IEnumerable<Student> items) =>
        items.OrderBy(s => s.LastName).ThenBy(s => s.FirstName);

    private protected override void OnDeleting(HomeschoolData data, int id)
    {
        data.Assignments.RemoveAll(a => a.StudentId == id);
        data.Grades.RemoveAll(g => g.StudentId == id);
        data.AttendanceRecords.RemoveAll(a => a.StudentId == id);
        data.LearningTimeEntries.RemoveAll(e => e.StudentId == id);
        data.PortfolioItems.RemoveAll(i => i.StudentId == id);
        data.StudentCurricula.RemoveAll(c => c.StudentId == id);
        data.ParentNotes.RemoveAll(n => n.StudentId == id);
    }

    public async Task<IEnumerable<Student>> GetByGradeLevelAsync(string gradeLevel)
    {
        var data = await Store.ReadAsync();
        return data.Students
            .Where(s => s.GradeLevel.Equals(gradeLevel, StringComparison.OrdinalIgnoreCase))
            .Select(s => RepositoryProjection.HydrateStudent(data, s))
            .ToList();
    }

    public async Task<IEnumerable<Student>> GetActiveStudentsAsync()
    {
        return await GetAllAsync();
    }

    public async Task<Student?> GetWithCoursesAsync(int id)
    {
        return await GetByIdAsync(id);
    }

    public async Task<Student?> GetWithAssignmentsAsync(int id)
    {
        return await GetByIdAsync(id);
    }

    public async Task<Student?> GetWithGradesAsync(int id)
    {
        return await GetByIdAsync(id);
    }
}
