using JobTracker.Domain.Entities;
using JobTracker.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;


namespace JobTracker.Api.Tests;

public static class TestHelpers
{
    public static async Task<(Guid patientId, Guid doctorId)> SeedBasicAsync(this TestWebAppFactory f)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var doc = new Doctor { FirstName = "Test", LastName = "Doc", Specialty = "Gen" };
        var pat = new Patient { FirstName = "Test", LastName = "Pat", Dob = new DateOnly(1990,1,1) };

        db.Doctors.Add(doc);
        db.Patients.Add(pat);
        await db.SaveChangesAsync();

        return (pat.Id, doc.Id);
    }

    public record CreateApptDto(Guid PatientId, Guid DoctorId, DateTime ScheduledAt, int DurationMin);
}
