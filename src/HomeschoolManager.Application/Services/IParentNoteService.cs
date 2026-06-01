using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public interface IParentNoteService
{
    Task<ParentNote?> GetNoteByIdAsync(int id);
    Task<IEnumerable<ParentNote>> GetNotesAsync(ParentNoteFilter filter);
    Task<ParentNote> CreateNoteAsync(ParentNote note);
    Task<ParentNote> UpdateNoteAsync(ParentNote note);
    Task DeleteNoteAsync(int id);
}
