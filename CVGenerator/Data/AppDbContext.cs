using System.IO;
using Microsoft.EntityFrameworkCore;

namespace CVGenerator.Data;

public class DraftEntity
{
    public int Id { get; set; }
    public string CvDataJson { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? TemplateKey { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<DraftEntity> Drafts { get; set; } = null!;

    private readonly string _dbPath;

    public AppDbContext(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CVGenerator", "cvgenerator.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DraftEntity>()
            .ToTable("Drafts")
            .HasKey(d => d.Id);
    }
}
