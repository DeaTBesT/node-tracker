using Microsoft.EntityFrameworkCore;
using NodeTracker.Models;

namespace NodeTracker.Services
{
    public class NoteService
    {
        private readonly string _dbPath;

        public NoteService(string dbPath)
        {
            _dbPath = dbPath;
            using var context = new DatabaseContext(_dbPath);
            context.Database.EnsureCreated();
        }

        public async Task<List<Note>> GetNotesAsync(int? userId = null)
        {
            using var context = new DatabaseContext(_dbPath);
            var query = context.Notes.AsQueryable();
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);

            return await query.OrderByDescending(n => n.CreatedDate).ToListAsync();
        }

        public async Task<Note> AddOrUpdateNoteAsync(Note note, int? userId = null)
        {
            using var context = new DatabaseContext(_dbPath);

            if (note.Id == 0)
            {
                note.CreatedDate = DateTime.Now;
                if (userId.HasValue)
                    note.UserId = userId.Value;
                context.Notes.Add(note);
            }
            else
            {
                var existingNote = await context.Notes.FirstOrDefaultAsync(n => n.Id == note.Id);
                if (existingNote != null)
                {
                    existingNote.Title = note.Title;
                    existingNote.Content = note.Content;
                    existingNote.Tags = note.Tags;
                    existingNote.IsFavorite = note.IsFavorite;
                }
            }

            await context.SaveChangesAsync();
            return note;
        }

        public async Task DeleteNoteAsync(Note note)
        {
            using var context = new DatabaseContext(_dbPath);
            context.Notes.Remove(note);
            await context.SaveChangesAsync();
        }
    }
}
