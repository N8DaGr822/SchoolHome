using Microsoft.EntityFrameworkCore;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
{
    public AssignmentRepository(HomeschoolDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Assignment>> GetByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Where(a => a.StudentId == studentId)
            .Include(a => a.Course)
            .Include(a => a.Grades)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Assignment>> GetByCourseIdAsync(int courseId)
    {
        return await _dbSet
            .Where(a => a.CourseId == courseId)
            .Include(a => a.Student)
            .Include(a => a.Grades)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Assignment>> GetByStatusAsync(AssignmentStatus status)
    {
        return await _dbSet
            .Where(a => a.Status == status)
            .Include(a => a.Student)
            .Include(a => a.Course)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _dbSet
            .Where(a => a.DueDate < today && a.Status != AssignmentStatus.Completed)
            .Include(a => a.Student)
            .Include(a => a.Course)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Assignment>> GetDueSoonAsync(int daysAhead = 7)
    {
        var today = DateTime.UtcNow.Date;
        var futureDate = today.AddDays(daysAhead);
        
        return await _dbSet
            .Where(a => a.DueDate >= today && a.DueDate <= futureDate && a.Status != AssignmentStatus.Completed)
            .Include(a => a.Student)
            .Include(a => a.Course)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<Assignment?> GetWithGradesAsync(int id)
    {
        return await _dbSet
            .Include(a => a.Grades)
            .Include(a => a.Student)
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}
