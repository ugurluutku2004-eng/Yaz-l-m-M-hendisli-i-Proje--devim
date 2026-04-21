using System.ComponentModel.DataAnnotations;

namespace KlinikYonetimSistemi.Web.Models;

public class Appointment
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    [Range(10, 120)]
    public int DurationMinutes { get; set; } = 30;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(1000)]
    public string? DoctorNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
