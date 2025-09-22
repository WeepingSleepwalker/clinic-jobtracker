using System.Net;
using System.Net.Http.Json;
using JobTracker.Domain.Entities;
using System;
using System.Threading.Tasks;
using Xunit;


namespace JobTracker.Api.Tests;

public class AppointmentsCreateTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public AppointmentsCreateTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateAppointment_HappyPath_Returns201()
    {
        var client = _factory.CreateClient();
        var (patientId, doctorId) = await _factory.SeedBasicAsync();

        var dto = new TestHelpers.CreateApptDto(patientId, doctorId, DateTime.UtcNow.AddMinutes(20), 30);

        var resp = await client.PostAsJsonAsync("/api/v1/appointments", dto);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var appt = await resp.Content.ReadFromJsonAsync<Appointment>();
        Assert.NotNull(appt);
        Assert.Equal(doctorId, appt!.DoctorId);
        Assert.Equal(patientId, appt.PatientId);
        Assert.Equal(ApptStatus.Scheduled, appt.Status);
    }

    [Fact]
    public async Task CreateAppointment_PastTime_Returns422()
    {
        var client = _factory.CreateClient();
        var (patientId, doctorId) = await _factory.SeedBasicAsync();

        var dto = new TestHelpers.CreateApptDto(patientId, doctorId, DateTime.UtcNow.AddMinutes(-5), 30);
        var resp = await client.PostAsJsonAsync("/api/v1/appointments", dto);

        Assert.Equal((HttpStatusCode)422, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_DoubleBookDoctor_Returns409()
    {
        var client = _factory.CreateClient();
        var (patientId, doctorId) = await _factory.SeedBasicAsync();

        var t0 = DateTime.UtcNow.AddMinutes(25);
        var dto1 = new TestHelpers.CreateApptDto(patientId, doctorId, t0, 30);
        var r1 = await client.PostAsJsonAsync("/api/v1/appointments", dto1);
        r1.EnsureSuccessStatusCode();

        var dto2 = new TestHelpers.CreateApptDto(patientId, doctorId, t0.AddMinutes(10), 30);
        var r2 = await client.PostAsJsonAsync("/api/v1/appointments", dto2);

        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_UnknownPatient_Returns422()
    {
        var client = _factory.CreateClient();
        var (_, doctorId) = await _factory.SeedBasicAsync();

        var dto = new TestHelpers.CreateApptDto(Guid.NewGuid(), doctorId, DateTime.UtcNow.AddMinutes(15), 30);
        var resp = await client.PostAsJsonAsync("/api/v1/appointments", dto);

        Assert.Equal((HttpStatusCode)422, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_UnknownDoctor_Returns422()
    {
        var client = _factory.CreateClient();
        var (patientId, _) = await _factory.SeedBasicAsync();

        var dto = new TestHelpers.CreateApptDto(patientId, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(15), 30);
        var resp = await client.PostAsJsonAsync("/api/v1/appointments", dto);

        Assert.Equal((HttpStatusCode)422, resp.StatusCode);
    }
}
