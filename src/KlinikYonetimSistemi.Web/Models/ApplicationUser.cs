using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace KlinikYonetimSistemi.Web.Models;

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(11, MinimumLength = 11)]
    public string? NationalId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Doctor? DoctorProfile { get; set; }
    public Patient? PatientProfile { get; set; }
}
