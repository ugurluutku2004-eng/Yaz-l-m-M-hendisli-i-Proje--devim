using System.ComponentModel.DataAnnotations;

namespace KlinikYonetimSistemi.Web.Models;

public class Specialty
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
