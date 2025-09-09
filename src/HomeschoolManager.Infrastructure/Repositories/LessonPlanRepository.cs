using Microsoft.EntityFrameworkCore;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class LessonPlanRepository : Repository<LessonPlan>, ILessonPlanRepository
{
    public LessonPlanRepository(HomeschoolDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<LessonPlan>> GetByCourseIdAsync(int courseId)
    {
        return await _dbSet
            .Where(lp => lp.CourseId == courseId)
            .OrderBy(lp => lp.WeekNumber)
            .ThenBy(lp => lp.DayNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<LessonPlan>> GetByWeekNumberAsync(int courseId, int weekNumber)
    {
        return await _dbSet
            .Where(lp => lp.CourseId == courseId && lp.WeekNumber == weekNumber)
            .OrderBy(lp => lp.DayNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<LessonPlan>> GetBySubjectAsync(string subject)
    {
        return await _dbSet
            .Include(lp => lp.Course)
            .Where(lp => lp.Course.Subject == subject)
            .OrderBy(lp => lp.WeekNumber)
            .ThenBy(lp => lp.DayNumber)
            .ToListAsync();
    }
}
