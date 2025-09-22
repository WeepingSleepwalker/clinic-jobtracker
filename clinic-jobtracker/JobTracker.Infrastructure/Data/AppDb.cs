using JobTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using JobTracker.Infrastructure.Data.Entities;


namespace JobTracker.Infrastructure.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);  // <--
        b.Entity<Appointment>().Property(x => x.Status).HasConversion<string>();
        b.Entity<Invoice>().Property(x => x.Status).HasConversion<string>();
        b.Entity<Invoice>().HasIndex(i => i.AppointmentId).IsUnique();
        b.Entity<Appointment>().HasIndex(a => new { a.DoctorId, a.ScheduledAt });
        b.Entity<JobExecution>(e =>
{
    e.HasIndex(x => x.Key).IsUnique();
    e.Property(x => x.Key).IsRequired();
    e.Property(x => x.Status).IsRequired();
});
    }
}
