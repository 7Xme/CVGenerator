using System.Text.Json;
using CVGenerator.Data;
using CVGenerator.Models;
using CVGenerator.Templates;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CVGenerator.Services;

public class DraftPersistenceService
{
    private readonly string _dbPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public DraftPersistenceService(string? dbPath = null)
    {
        _dbPath = dbPath;
        EnsureDatabase();
    }

    private void EnsureDatabase()
    {
        try
        {
            using var db = new AppDbContext(_dbPath);
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize SQLite database");
        }
    }

    public bool HasDraft()
    {
        try
        {
            using var db = new AppDbContext(_dbPath);
            return db.Drafts.Any();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check for draft");
            return false;
        }
    }

    public void SaveDraft(CVData data, string? templateKey = null)
    {
        try
        {
            using var db = new AppDbContext(_dbPath);
            var entity = db.Drafts.FirstOrDefault();
            var json = JsonSerializer.Serialize(data, _jsonOptions);

            if (entity == null)
            {
                db.Drafts.Add(new DraftEntity
                {
                    CvDataJson = json,
                    UpdatedAt = DateTime.Now,
                    TemplateKey = templateKey ?? TemplateCatalog.Default.Key
                });
            }
            else
            {
                entity.CvDataJson = json;
                entity.UpdatedAt = DateTime.Now;
                if (!string.IsNullOrEmpty(templateKey))
                    entity.TemplateKey = templateKey;
            }

            db.SaveChanges();
            Log.Information("Draft saved at {Time}", DateTime.Now);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save draft");
        }
    }

    public (CVData? Data, string? TemplateKey) LoadDraft()
    {
        try
        {
            using var db = new AppDbContext(_dbPath);
            var entity = db.Drafts.FirstOrDefault();
            if (entity == null)
                return (null, null);

            var data = JsonSerializer.Deserialize<CVData>(entity.CvDataJson);
            return (data, entity.TemplateKey);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load draft");
            return (null, null);
        }
    }

    public void ClearDraft()
    {
        try
        {
            using var db = new AppDbContext(_dbPath);
            var entity = db.Drafts.FirstOrDefault();
            if (entity != null)
            {
                db.Drafts.Remove(entity);
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clear draft");
        }
    }
}
