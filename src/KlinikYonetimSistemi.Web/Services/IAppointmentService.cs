using KlinikYonetimSistemi.Web.Models;

namespace KlinikYonetimSistemi.Web.Services;

public interface IAppointmentService
{
    Task<(bool ok, string? error, Appointment? appointment)> BookAsync(
        int patientId, int doctorId, DateTime scheduledAt, int durationMinutes, string? reason);

    Task<bool> CancelAsync(int appointmentId, string actorUserId, bool isAdmin);

    Task<bool> UpdateStatusAsync(int appointmentId, AppointmentStatus status, string? doctorNotes);

    bool HasConflict(IEnumerable<Appointment> doctorAppointments, DateTime start, int durationMinutes, int? ignoreId = null);
}
