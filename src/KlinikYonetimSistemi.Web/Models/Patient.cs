using System.ComponentModel.DataAnnotations;

namespace KlinikYonetimSistemi.Web.Models;

public class Patient
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(10)]
    public string? BloodType { get; set; }

    [StringLength(500)]
    public string? Allergies { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}
