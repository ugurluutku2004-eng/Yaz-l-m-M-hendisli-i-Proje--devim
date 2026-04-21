using KlinikYonetimSistemi.Web.Data;
using KlinikYonetimSistemi.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace KlinikYonetimSistemi.Web.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _db;

    public AppointmentService(ApplicationDbContext db) => _db = db;

    public async Task<(bool ok, string? error, Appointment? appointment)> BookAsync(
        int patientId, int doctorId, DateTime scheduledAt, int durationMinutes, string? reason)
    {
        if (scheduledAt < DateTime.UtcNow.AddMinutes(-1))
            return (false, "Geçmiş bir tarihe randevu alınamaz.", null);

        if (durationMinutes is < 10 or > 120)
            return (false, "Randevu süresi 10 ile 120 dakika arasında olmalıdır.", null);

        var doctor = await _db.Doctors.FindAsync(doctorId);
        if (doctor is null)
            return (false, "Doktor bulunamadı.", null);

        var patient = await _db.Patients.FindAsync(patientId);
        if (patient is null)
            return (false, "Hasta bulunamadı.", null);

        var windowStart = scheduledAt.AddHours(-4);
        var windowEnd = scheduledAt.AddHours(4);
        var existing = await _db.Appointments
            .Where(a => a.DoctorId == doctorId
                        && a.Status != AppointmentStatus.Cancelled
                        && a.ScheduledAt >= windowStart
                        && a.ScheduledAt <= windowEnd)
            .ToListAsync();

        if (HasConflict(existing, scheduledAt, durationMinutes))
            return (false, "Bu saat aralığında doktorun başka bir randevusu bulunmaktadır.", null);

        var appt = new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            ScheduledAt = scheduledAt,
            DurationMinutes = durationMinutes,
            Reason = reason,
            Status = AppointmentStatus.Scheduled
        };
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();
        return (true, null, appt);
    }

    public async Task<bool> CancelAsync(int appointmentId, string actorUserId, bool isAdmin)
    {
        var appt = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appt is null) return false;

        var isOwner = isAdmin
                      || appt.Patient?.UserId == actorUserId
                      || appt.Doctor?.UserId == actorUserId;
        if (!isOwner) return false;

        if (appt.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            return false;

        appt.Status = AppointmentStatus.Cancelled;
        appt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int appointmentId, AppointmentStatus status, string? doctorNotes)
    {
        var appt = await _db.Appointments.FindAsync(appointmentId);
        if (appt is null) return false;
        appt.Status = status;
        if (!string.IsNullOrWhiteSpace(doctorNotes))
            appt.DoctorNotes = doctorNotes;
        appt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public bool HasConflict(IEnumerable<Appointment> doctorAppointments, DateTime start, int durationMinutes, int? ignoreId = null)
    {
        var end = start.AddMinutes(durationMinutes);
        foreach (var a in doctorAppointments)
        {
            if (ignoreId.HasValue && a.Id == ignoreId.Value) continue;
            if (a.Status == AppointmentStatus.Cancelled) continue;
            var aEnd = a.ScheduledAt.AddMinutes(a.DurationMinutes);
            if (start < aEnd && a.ScheduledAt < end) return true;
        }
        return false;
    }
}
