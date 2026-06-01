using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class ParentNoteService : IParentNoteService
{
    private readonly IParentNoteRepository _noteRepository;

    public ParentNoteService(IParentNoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    public async Task<ParentNote?> GetNoteByIdAsync(int id)
    {
        return await _noteRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<ParentNote>> GetNotesAsync(ParentNoteFilter filter)
    {
        return await _noteRepository.GetFilteredAsync(filter);
    }

    public async Task<ParentNote> CreateNoteAsync(ParentNote note)
    {
        Normalize(note);
        note.CreatedAt = DateTime.UtcNow;
        return await _noteRepository.AddAsync(note);
    }

    public async Task<ParentNote> UpdateNoteAsync(ParentNote note)
    {
        Normalize(note);
        var existing = await _noteRepository.GetByIdAsync(note.Id)
            ?? throw new InvalidOperationException($"Parent note {note.Id} was not found.");

        note.CreatedAt = existing.CreatedAt;
        note.UpdatedAt = DateTime.UtcNow;
        await _noteRepository.UpdateAsync(note);
        return note;
    }

    public async Task DeleteNoteAsync(int id)
    {
        await _noteRepository.DeleteAsync(id);
    }

    private static void Normalize(ParentNote note)
    {
        note.Title = note.Title?.Trim() ?? string.Empty;
        note.Content = note.Content?.Trim() ?? string.Empty;
        note.NoteDate = note.NoteDate.Date;

        if (string.IsNullOrWhiteSpace(note.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(note.Content))
        {
            throw new InvalidOperationException("Note content is required.");
        }
    }
}
