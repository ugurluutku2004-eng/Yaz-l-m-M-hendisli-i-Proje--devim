using KlinikYonetimSistemi.Web.Data;
using KlinikYonetimSistemi.Web.Models;
using KlinikYonetimSistemi.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace KlinikYonetimSistemi.Tests;

public class AppointmentServiceTests
{
    private static ApplicationDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static async Task<(ApplicationDbContext, int doctorId, int patientId)> SeedAsync()
    {
        var db = NewDb();
        var specialty = new Specialty { Name = "Dahiliye" };
        db.Specialties.Add(specialty);
        await db.SaveChangesAsync();

        var doctorUser = new ApplicationUser { Id = "doc1", UserName = "d@x", Email = "d@x", FullName = "Dr" };
        var patientUser = new ApplicationUser { Id = "pat1", UserName = "p@x", Email = "p@x", FullName = "P" };
        db.Users.Add(doctorUser);
        db.Users.Add(patientUser);
        var doctor = new Doctor { UserId = "doc1", SpecialtyId = specialty.Id, LicenseNumber = "L1", YearsOfExperience = 3 };
        var patient = new Patient { UserId = "pat1" };
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return (db, doctor.Id, patient.Id);
    }

    [Fact]
    public void HasConflict_ReturnsTrue_WhenOverlapping()
    {
        var svc = new AppointmentService(NewDb());
        var existing = new[]
        {
            new Appointment { Id = 1, ScheduledAt = new DateTime(2030, 1, 1, 10, 0, 0), DurationMinutes = 30, Status = AppointmentStatus.Scheduled }
        };
        var hasConflict = svc.HasConflict(existing, new DateTime(2030, 1, 1, 10, 15, 0), 30);
        Assert.True(hasConflict);
    }

    [Fact]
    public void HasConflict_ReturnsFalse_WhenBackToBack()
    {
        var svc = new AppointmentService(NewDb());
        var existing = new[]
        {
            new Appointment { Id = 1, ScheduledAt = new DateTime(2030, 1, 1, 10, 0, 0), DurationMinutes = 30, Status = AppointmentStatus.Scheduled }
        };
        Assert.False(svc.HasConflict(existing, new DateTime(2030, 1, 1, 10, 30, 0), 30));
    }

    [Fact]
    public void HasConflict_IgnoresCancelled()
    {
        var svc = new AppointmentService(NewDb());
        var existing = new[]
        {
            new Appointment { Id = 1, ScheduledAt = new DateTime(2030, 1, 1, 10, 0, 0), DurationMinutes = 30, Status = AppointmentStatus.Cancelled }
        };
        Assert.False(svc.HasConflict(existing, new DateTime(2030, 1, 1, 10, 15, 0), 30));
    }

    [Fact]
    public async Task Book_Rejects_PastDate()
    {
        var (db, docId, patId) = await SeedAsync();
        var svc = new AppointmentService(db);
        var (ok, error, _) = await svc.BookAsync(patId, docId, DateTime.UtcNow.AddDays(-1), 30, "test");
        Assert.False(ok);
        Assert.Contains("Geçmiş", error);
    }

    [Fact]
    public async Task Book_Rejects_InvalidDuration()
    {
        var (db, docId, patId) = await SeedAsync();
        var svc = new AppointmentService(db);
        var (ok, _, _) = await svc.BookAsync(patId, docId, DateTime.UtcNow.AddDays(1), 5, "test");
        Assert.False(ok);
    }

    [Fact]
    public async Task Book_Succeeds_WhenValid()
    {
        var (db, docId, patId) = await SeedAsync();
        var svc = new AppointmentService(db);
        var when = DateTime.UtcNow.AddDays(2);
        var (ok, _, appt) = await svc.BookAsync(patId, docId, when, 30, "muayene");
        Assert.True(ok);
        Assert.NotNull(appt);
        Assert.Equal(AppointmentStatus.Scheduled, appt!.Status);
    }

    [Fact]
    public async Task Book_Rejects_Conflict()
    {
        var (db, docId, patId) = await SeedAsync();
        var svc = new AppointmentService(db);
        var when = DateTime.UtcNow.AddDays(2);
        await svc.BookAsync(patId, docId, when, 30, "ilk");
        var (ok, error, _) = await svc.BookAsync(patId, docId, when.AddMinutes(10), 30, "çakışan");
        Assert.False(ok);
        Assert.Contains("başka bir randevusu", error);
    }

    [Fact]
    public async Task Cancel_ChangesStatus()
    {
        var (db, docId, patId) = await SeedAsync();
        var svc = new AppointmentService(db);
        var (_, _, appt) = await svc.BookAsync(patId, docId, DateTime.UtcNow.AddDays(3), 30, null);
        var ok = await svc.CancelAsync(appt!.Id, "pat1", isAdmin: false);
        Assert.True(ok);
        var reloaded = await db.Appointments.FindAsync(appt.Id);
        Assert.Equal(AppointmentStatus.Cancelled, reloaded!.Status);
    }

    [Fact]
    public async Task Cancel_Rejects_UnauthorizedUser()
    {
        var (db, docId, patId) = await SeedAsync();
        var svc = new AppointmentService(db);
        var (_, _, appt) = await svc.BookAsync(patId, docId, DateTime.UtcNow.AddDays(3), 30, null);
        var ok = await svc.CancelAsync(appt!.Id, "otherUser", isAdmin: false);
        Assert.False(ok);
    }
}
