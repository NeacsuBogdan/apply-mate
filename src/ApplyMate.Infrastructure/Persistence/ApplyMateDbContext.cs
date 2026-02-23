using ApplyMate.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApplyMate.Infrastructure.Persistence;

public sealed class ApplyMateDbContext : DbContext
{
    public ApplyMateDbContext(DbContextOptions<ApplyMateDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var app = modelBuilder.Entity<JobApplication>();

        app.ToTable("JobApplications");
        app.HasKey(x => x.Id);

        app.Property(x => x.JobName)
            .IsRequired()
            .HasMaxLength(JobApplication.JobNameMaxLength);

        app.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(JobApplication.CompanyNameMaxLength);

        app.Property(x => x.JobSummary)
            .HasMaxLength(JobApplication.JobSummaryMaxLength);

        app.Property(x => x.JobUrl)
            .HasMaxLength(JobApplication.JobUrlMaxLength);

        app.Property(x => x.AppliedOn)
            .IsRequired();

        app.Property(x => x.LastStatusChangedOn)
            .IsRequired();

        app.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        app.Property(x => x.CvStoredPath)
            .HasMaxLength(2048);

        app.Property(x => x.CvOriginalFileName)
            .HasMaxLength(255);

        app.HasIndex(x => x.Status);
        app.HasIndex(x => x.AppliedOn);
        app.HasIndex(x => x.InterviewAt);
    }
}
