using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonParentNoteRepository : IParentNoteRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonParentNoteRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<ParentNote?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var note = data.ParentNotes.FirstOrDefault(n => n.Id == id);
        return note == null ? null : RepositoryProjection.HydrateParentNote(data, note);
    }

    public async Task<IEnumerable<ParentNote>> GetAllAsync()
    {
        return await GetFilteredAsync(new ParentNoteFilter());
    }

    public async Task<ParentNote> AddAsync(ParentNote entity)
    {
        var saved = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            ValidateReferences(data, saved);
            saved.Id = saved.Id == 0 ? NextId(data.ParentNotes.Select(n => n.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.ParentNotes.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(ParentNote entity)
    {
        var updated = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            var index = data.ParentNotes.FindIndex(n => n.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Parent note {updated.Id} was not found.");
            }

            ValidateReferences(data, updated);
            updated.CreatedAt = updated.CreatedAt == default ? data.ParentNotes[index].CreatedAt : updated.CreatedAt;
            data.ParentNotes[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data => data.ParentNotes.RemoveAll(n => n.Id == id));
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.ParentNotes.Any(n => n.Id == id);
    }

    public async Task<IEnumerable<ParentNote>> GetFilteredAsync(ParentNoteFilter filter)
    {
        var data = await _store.ReadAsync();
        var query = data.ParentNotes.AsEnumerable();

        if (filter.StudentId.HasValue && filter.StudentId.Value > 0)
        {
            query = query.Where(n => n.StudentId == filter.StudentId.Value);
        }

        if (filter.SubjectId.HasValue && filter.SubjectId.Value > 0)
        {
            query = query.Where(n => n.SubjectId == filter.SubjectId.Value);
        }

        if (filter.AssignmentId.HasValue && filter.AssignmentId.Value > 0)
        {
            query = query.Where(n => n.AssignmentId == filter.AssignmentId.Value);
        }

        if (filter.LessonPlanId.HasValue && filter.LessonPlanId.Value > 0)
        {
            query = query.Where(n => n.LessonPlanId == filter.LessonPlanId.Value);
        }

        if (filter.Category.HasValue)
        {
            query = query.Where(n => n.Category == filter.Category.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(n => n.NoteDate.Date >= filter.StartDate.Value.Date);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(n => n.NoteDate.Date <= filter.EndDate.Value.Date);
        }

        return query
            .OrderByDescending(n => n.NoteDate)
            .ThenByDescending(n => n.CreatedAt)
            .Select(n => RepositoryProjection.HydrateParentNote(data, n))
            .ToList();
    }

    public async Task<IEnumerable<ParentNote>> GetByStudentIdAsync(int studentId)
    {
        return await GetFilteredAsync(new ParentNoteFilter(StudentId: studentId));
    }

    private static ParentNote Normalize(ParentNote note)
    {
        note.Title = note.Title?.Trim() ?? string.Empty;
        note.Content = note.Content?.Trim() ?? string.Empty;
        note.NoteDate = note.NoteDate.Date;
        return note;
    }

    private static void ValidateReferences(HomeschoolData data, ParentNote note)
    {
        if (note.StudentId <= 0 || !data.Students.Any(s => s.Id == note.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (note.SubjectId.HasValue && !data.Courses.Any(c => c.Id == note.SubjectId.Value))
        {
            throw new InvalidOperationException("The linked subject was not found.");
        }

        if (note.AssignmentId.HasValue && !data.Assignments.Any(a => a.Id == note.AssignmentId.Value))
        {
            throw new InvalidOperationException("The linked assignment was not found.");
        }

        if (note.AssignmentId.HasValue && data.Assignments.First(a => a.Id == note.AssignmentId.Value).StudentId != note.StudentId)
        {
            throw new InvalidOperationException("The linked assignment belongs to a different student.");
        }

        if (note.LessonPlanId.HasValue && !data.LessonPlans.Any(lp => lp.Id == note.LessonPlanId.Value))
        {
            throw new InvalidOperationException("The linked lesson plan was not found.");
        }

        if (note.LessonPlanId.HasValue && data.LessonPlans.First(lp => lp.Id == note.LessonPlanId.Value).StudentId != note.StudentId)
        {
            throw new InvalidOperationException("The linked lesson plan belongs to a different student.");
        }
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
