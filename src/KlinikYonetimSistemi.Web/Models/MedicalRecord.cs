using System.ComponentModel.DataAnnotations;

namespace KlinikYonetimSistemi.Web.Models;

public class MedicalRecord
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    [Required, StringLength(200)]
    public string Diagnosis { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Treatment { get; set; }

    [StringLength(1000)]
    public string? Prescription { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
