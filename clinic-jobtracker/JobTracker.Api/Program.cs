using JobTracker.Domain.Entities;
using JobTracker.Infrastructure.Data;
using JobTracker.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using JobTracker.Application.Services;
using JobTracker.Infrastructure.Services;
using JobTracker.Infrastructure.Jobs;
using JobTracker.Infrastructure.Data.Entities;
using JobTracker.Api.Workers;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Hosting;


using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Use file-based SQLite (simple demo DB)
builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
// OR uncomment this to use in-memory SQLite with shared cache
// var keepAlive = new SqliteConnection("Data Source=:memory:;Cache=Shared");
// keepAlive.Open();
// builder.Services.AddDbContext<AppDb>(o => o.UseSqlite(keepAlive));
builder.Services.AddSingleton<InMemoryJobQueue>();
builder.Services.AddHostedService<JobWorker>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));


// Application port -> Infrastructure adapter
builder.Services.AddScoped<IBillingBatchProcessor, BillingBatchProcessor>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors();
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Request-Id"] = ctx.TraceIdentifier;
    await next();
});

// ---- Create DB + seed ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();

    if (!db.Doctors.Any())
    {
        db.Doctors.AddRange(
            new Doctor { FirstName="Ava",  LastName="Ng",    Specialty="Internal Med" },
            new Doctor { FirstName="Noah", LastName="Kim",   Specialty="Pediatrics"   },
            new Doctor { FirstName="Liam", LastName="Patel", Specialty="Cardiology"   },
            new Doctor { FirstName="Mia",  LastName="Lopez", Specialty="Dermatology"  },
            new Doctor { FirstName="Eli",  LastName="Chen",  Specialty="Family Med"   }
        );
        db.Patients.AddRange(
            new Patient { FirstName="Jane", LastName="Doe", Dob=new DateOnly(1990,1,1), Email="jane@example.com" },
            new Patient { FirstName="Mark", LastName="Lee", Dob=new DateOnly(1988,5,5), Email="mark@example.com" }
        );
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/health", () => Results.Ok(new { ok = true }));

// ---- Doctors ----
app.MapGet("/api/v1/doctors", async (AppDb db, string? q) =>
{
    var query = db.Doctors.AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
    {
        var s = q.Trim().ToLower();
        query = query.Where(d =>
            (d.FirstName + " " + d.LastName).ToLower().Contains(s) ||
            (d.Specialty ?? "").ToLower().Contains(s));
    }

    var items = await query
        .OrderBy(d => d.LastName).ThenBy(d => d.FirstName)
        .Select(d => new { d.Id, name = d.FirstName + " " + d.LastName, d.Specialty })
        .ToListAsync();

    return Results.Ok(items);
});

// ---- Patients ----
app.MapPost("/api/v1/patients", async (Patient p, AppDb db) =>
{
    if (string.IsNullOrWhiteSpace(p.FirstName) || string.IsNullOrWhiteSpace(p.LastName))
        return Results.UnprocessableEntity(new { message = "first/last name required" });

    db.Patients.Add(p);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/patients/{p.Id}", p);
});

app.MapGet("/api/v1/patients", async (AppDb db) =>
{
    var items = await db.Patients
        .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
        .Select(p => new { p.Id, name = p.FirstName + " " + p.LastName })
        .ToListAsync();

    return Results.Ok(items);
});

// ---- Appointments ----
app.MapPost("/api/v1/appointments", async (CreateApptDto req, AppDb db) =>
{
    if (!await db.Patients.AnyAsync(p => p.Id == req.PatientId))
        return Results.UnprocessableEntity(new { message = "patient not found" });
    if (!await db.Doctors.AnyAsync(d => d.Id == req.DoctorId))
        return Results.UnprocessableEntity(new { message = "doctor not found" });
    if (req.ScheduledAt <= DateTime.UtcNow)
        return Results.UnprocessableEntity(new { message = "time must be in the future (UTC)" });
    if (req.DurationMin is < 5 or > 120)
        return Results.UnprocessableEntity(new { message = "duration must be 5..120" });

    var start = req.ScheduledAt;
    var end = req.ScheduledAt.AddMinutes(req.DurationMin);
    var clash = await db.Appointments.AnyAsync(a =>
        a.DoctorId == req.DoctorId && a.Status != ApptStatus.Canceled &&
        a.ScheduledAt < end && start < a.ScheduledAt.AddMinutes(a.DurationMin));
    if (clash) return Results.Conflict(new { message = "doctor unavailable at this time" });

    var appt = new Appointment
    {
        PatientId = req.PatientId,
        DoctorId = req.DoctorId,
        ScheduledAt = req.ScheduledAt,
        DurationMin = req.DurationMin
    };

    db.Appointments.Add(appt);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/appointments/{appt.Id}", appt);
});

//error handling
        app.Use(async (ctx, next) =>
        {
            try { await next(); }
            catch (Exception ex)
            {
                var traceId = ctx.TraceIdentifier;
                var problem = new
                {
                    type = "about:blank",
                    title = "Unexpected error",
                    status = StatusCodes.Status500InternalServerError,
                    detail = "An unexpected error occurred. See logs with traceId.",
                    traceId
                };
                ctx.Response.ContentType = "application/problem+json";
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await ctx.Response.WriteAsJsonAsync(problem);
                // Optionally log here using ILoggerFactory from ctx.RequestServices
            }
        });

app.MapGet("/api/v1/appointments/{id:guid}", async (Guid id, AppDb db) =>
{
    var a = await db.Appointments.FindAsync(id);
    return a is null ? Results.NotFound() : Results.Ok(a);
});

// ---- Doctor agenda (per day) ----
app.MapGet("/api/v1/doctors/{doctorId:guid}/agenda", async (Guid doctorId, DateOnly date, AppDb db) =>
{
    var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var dayEnd   = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

    var items = await db.Appointments
        .Where(a => a.DoctorId == doctorId && a.ScheduledAt >= dayStart && a.ScheduledAt <= dayEnd)
        .OrderBy(a => a.ScheduledAt)
        .Select(a => new
        {
            a.Id,
            a.ScheduledAt,
            End = a.ScheduledAt.AddMinutes(a.DurationMin),
            a.Status,
            Patient = db.Patients
                        .Where(p => p.Id == a.PatientId)
                        .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName })
                        .First()
        })
        .ToListAsync();

    return Results.Ok(new { doctorId, date, items });
});

// ---- Invoices ----
app.MapPost("/api/v1/invoices", async (CreateInvoiceDto req, AppDb db) =>
{
    var appt = await db.Appointments.FindAsync(req.AppointmentId);
    if (appt is null) return Results.UnprocessableEntity(new { message = "appointment not found" });
    if (req.AmountCents < 0) return Results.UnprocessableEntity(new { message = "amount must be >= 0" });

    var existing = await db.Invoices.SingleOrDefaultAsync(i => i.AppointmentId == req.AppointmentId);
    if (existing is not null) return Results.Ok(existing); // idempotent

    var inv = new Invoice { AppointmentId = req.AppointmentId, AmountCents = req.AmountCents, Status = InvoiceStatus.Pending };
    db.Invoices.Add(inv);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/invoices/{inv.Id}", inv);
});

app.MapGet("/api/v1/invoices/{id:guid}", async (Guid id, AppDb db) =>
{
    var inv = await db.Invoices.FindAsync(id);
    return inv is null ? Results.NotFound() : Results.Ok(inv);
});

// ---- Complete + invoice (transaction demo) ----
app.MapPost("/api/v1/appointments/{id:guid}/complete-and-invoice", async (Guid id, CompleteAndInvoiceDto req, AppDb db) =>
{
    await using var tx = await db.Database.BeginTransactionAsync();
    try
    {
        var appt = await db.Appointments.FindAsync(id);
        if (appt is null) return Results.NotFound();
        if (appt.Status != ApptStatus.Scheduled)
            return Results.UnprocessableEntity(new { message = "appointment must be Scheduled to complete" });

        appt.Status = ApptStatus.Completed;

        var invoice = await db.Invoices.SingleOrDefaultAsync(i => i.AppointmentId == id);
        if (invoice is null)
        {
            invoice = new Invoice { AppointmentId = id, AmountCents = req.AmountCents, Status = InvoiceStatus.Pending };
            db.Invoices.Add(invoice);
        }

        // Simulate billing gateway
        if (!req.SimulateBillingSuccess) throw new Exception("billing failed");

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Results.Ok(new { appointmentId = id, invoiceId = invoice.Id, invoiceStatus = invoice.Status });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        app.Logger.LogError(ex, "Rollback in complete-and-invoice");
        return Results.BadRequest(new { message = "rolled back", error = ex.Message });
    }
});
// Enqueue bulk billing job: expects JSON array of invoice IDs
app.MapPost("/api/v1/jobs/billing", async (
    InMemoryJobQueue q,
    AppDb db,
    List<Guid> invoiceIds,
    HttpRequest req,
    bool? forceFail) =>
{
    if (invoiceIds is null || invoiceIds.Count == 0)
        return Results.BadRequest(new { message = "invoiceIds (array) required" });

    // Accept key via header or query (?key=...)
    string? key = null;
    if (req.Headers.TryGetValue("Idempotency-Key", out var hdr) && !string.IsNullOrWhiteSpace(hdr))
        key = hdr.ToString();
    else if (req.Query.TryGetValue("key", out var qv) && !string.IsNullOrWhiteSpace(qv))
        key = qv.ToString();

    // If key exists and we’ve seen it, return its status (idempotent response)
    if (!string.IsNullOrWhiteSpace(key))
    {
        var existing = await db.JobExecutions.SingleOrDefaultAsync(j => j.Key == key);
        if (existing is not null)
        {
            return Results.Accepted($"/api/v1/jobs/{existing.Id}",
                new { jobId = existing.Id, idempotencyKey = existing.Key, status = existing.Status });
        }

        // First time we see this key: record Pending
        db.JobExecutions.Add(new JobExecution { Key = key, Status = "Pending" });
        await db.SaveChangesAsync();
    }

    var cmd = new BulkBillingCommand
    {
        InvoiceIds = invoiceIds,
        ForceFail = forceFail == true,
        IdempotencyKey = key
    };

    await q.EnqueueAsync(cmd);
    return Results.Accepted($"/api/v1/jobs/{cmd.JobId}",
        new { jobId = cmd.JobId, idempotencyKey = key, status = "Pending" });
});

app.MapGet("/api/v1/jobs/{jobId:guid}", (Guid jobId, InMemoryJobQueue q) =>
{
    var status = q.GetStatus(jobId);
    return status is null ? Results.NotFound() : Results.Ok(status);
});


app.Run();

public partial class Program { }
