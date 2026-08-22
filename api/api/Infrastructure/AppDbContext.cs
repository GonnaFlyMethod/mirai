using MedScans.Histories;
using MedScans.Patients;
using MedScans.Scans;
using Microsoft.EntityFrameworkCore;

namespace MedScans.Infrastructure;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<BrainScan> BrainScans => Set<BrainScan>();

    public DbSet<HistoryRecord> Histories => Set<HistoryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(patient => patient.Id);
            entity.Property(patient => patient.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(patient => patient.LastName).HasMaxLength(100).IsRequired();
            entity.Property(patient => patient.Gender).HasMaxLength(40);
            entity.Property(patient => patient.Email).HasMaxLength(256);
            entity.Property(patient => patient.PhoneNumber).HasMaxLength(40);
            entity.Property(patient => patient.Address).HasMaxLength(500);
            entity.Property(patient => patient.CreatedAt).IsRequired();
            entity.Property(patient => patient.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<BrainScan>(entity =>
        {
            entity.ToTable("BrainScans");
            entity.HasKey(scan => scan.Id);
            entity.Property(scan => scan.OriginalFileName).HasMaxLength(260).IsRequired();
            entity.Property(scan => scan.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(scan => scan.ImageBytes).IsRequired();
            entity.Property(scan => scan.PredictedLabel).HasMaxLength(80).IsRequired();
            entity.Property(scan => scan.AnalysisStatus).HasMaxLength(80).IsRequired();
            entity.Property(scan => scan.AnalyzerVersion).HasMaxLength(160).IsRequired();
            entity.Property(scan => scan.ProbabilitiesJson).IsRequired();
            entity.Property(scan => scan.ErrorMessage).HasMaxLength(1000);
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(scan => scan.PatientId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HistoryRecord>(entity =>
        {
            entity.ToTable("Histories");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Datetime).IsRequired();
            entity.Property(record => record.Title).HasMaxLength(1500).IsRequired();
            entity.Property(record => record.Description).IsRequired();
            entity.HasOne<Patient>()
                .WithMany()
                .HasForeignKey(record => record.PatientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
