using Microsoft.EntityFrameworkCore;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class StudentRepository : Repository<Student>, IStudentRepository
{
    public StudentRepository(HomeschoolDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Student>> GetByGradeLevelAsync(string gradeLevel)
    {
        return await _dbSet
            .Where(s => s.GradeLevel == gradeLevel)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Student>> GetActiveStudentsAsync()
    {
        return await _dbSet
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();
    }

    public async Task<Student?> GetWithCoursesAsync(int id)
    {
        return await _dbSet
            .Include(s => s.Courses)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Student?> GetWithAssignmentsAsync(int id)
    {
        return await _dbSet
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Course)
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Grades)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}
