using System.ComponentModel.DataAnnotations;

namespace KlinikYonetimSistemi.Web.Models;

public class Doctor
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    [Required, StringLength(20)]
    public string LicenseNumber { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Bio { get; set; }

    [Range(0, 50)]
    public int YearsOfExperience { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
