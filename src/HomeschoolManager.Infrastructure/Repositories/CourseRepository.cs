using Microsoft.EntityFrameworkCore;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class CourseRepository : Repository<Course>, ICourseRepository
{
    public CourseRepository(HomeschoolDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Course>> GetBySubjectAsync(string subject)
    {
        return await _dbSet
            .Where(c => c.Subject == subject)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Course>> GetByGradeLevelAsync(string gradeLevel)
    {
        return await _dbSet
            .Where(c => c.GradeLevel == gradeLevel)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Course>> GetActiveCoursesAsync()
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Course?> GetWithLessonPlansAsync(int id)
    {
        return await _dbSet
            .Include(c => c.LessonPlans)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Course?> GetWithAssignmentsAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Assignments)
                .ThenInclude(a => a.Student)
            .Include(c => c.Assignments)
                .ThenInclude(a => a.Grades)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Course?> GetWithStudentsAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
