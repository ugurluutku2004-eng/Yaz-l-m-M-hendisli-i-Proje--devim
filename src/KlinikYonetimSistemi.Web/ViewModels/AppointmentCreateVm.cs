using System.ComponentModel.DataAnnotations;

namespace KlinikYonetimSistemi.Web.ViewModels;

public class AppointmentCreateVm
{
    [Display(Name = "Doktor"), Required]
    public int DoctorId { get; set; }

    [Display(Name = "Randevu Tarihi ve Saati"), Required]
    [DataType(DataType.DateTime)]
    public DateTime ScheduledAt { get; set; }

    [Display(Name = "Süre (dakika)"), Range(10, 120)]
    public int DurationMinutes { get; set; } = 30;

    [Display(Name = "Başvuru Nedeni"), StringLength(500)]
    public string? Reason { get; set; }
}
