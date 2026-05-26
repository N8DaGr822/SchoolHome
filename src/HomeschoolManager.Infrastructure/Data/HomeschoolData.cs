using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Infrastructure.Data;

internal class HomeschoolData
{
    public List<Student> Students { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<Assignment> Assignments { get; set; } = new();
    public List<Grade> Grades { get; set; } = new();
}
