using KlinikYonetimSistemi.Web.Data;
using KlinikYonetimSistemi.Web.Models;
using KlinikYonetimSistemi.Web.Services;
using KlinikYonetimSistemi.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KlinikYonetimSistemi.Web.Controllers;

[Authorize]
public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAppointmentService _appointments;
    private readonly UserManager<ApplicationUser> _users;

    public AppointmentsController(
        ApplicationDbContext db,
        IAppointmentService appointments,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _appointments = appointments;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _users.GetUserId(User)!;
        IQueryable<Appointment> query = _db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Include(a => a.Patient).ThenInclude(p => p!.User)
            .AsNoTracking();

        if (User.IsInRole(Roles.Admin))
        {
            // admin sees all
        }
        else if (User.IsInRole(Roles.Doctor))
        {
            query = query.Where(a => a.Doctor!.UserId == userId);
        }
        else
        {
            query = query.Where(a => a.Patient!.UserId == userId);
        }

        var list = await query.OrderByDescending(a => a.ScheduledAt).ToListAsync();
        return View(list);
    }

    [Authorize(Roles = Roles.Patient)]
    public async Task<IActionResult> Create()
    {
        await PopulateDoctors();
        return View(new AppointmentCreateVm { ScheduledAt = DateTime.Now.AddDays(1).Date.AddHours(10) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Patient)]
    public async Task<IActionResult> Create(AppointmentCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDoctors();
            return View(vm);
        }

        var userId = _users.GetUserId(User)!;
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient is null)
        {
            ModelState.AddModelError(string.Empty, "Hasta profiliniz henüz oluşturulmamış. Lütfen yönetici ile iletişime geçin.");
            await PopulateDoctors();
            return View(vm);
        }

        var (ok, error, _) = await _appointments.BookAsync(
            patient.Id, vm.DoctorId, vm.ScheduledAt.ToUniversalTime(),
            vm.DurationMinutes, vm.Reason);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error!);
            await PopulateDoctors();
            return View(vm);
        }

        TempData["Success"] = "Randevunuz başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var appt = await _db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Include(a => a.Patient).ThenInclude(p => p!.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appt is null) return NotFound();

        var userId = _users.GetUserId(User)!;
        var allowed = User.IsInRole(Roles.Admin)
                      || appt.Doctor?.UserId == userId
                      || appt.Patient?.UserId == userId;
        if (!allowed) return Forbid();

        return View(appt);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _users.GetUserId(User)!;
        var ok = await _appointments.CancelAsync(id, userId, User.IsInRole(Roles.Admin));
        TempData[ok ? "Success" : "Error"] =
            ok ? "Randevu iptal edildi." : "Randevu iptal edilemedi.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Doctor + "," + Roles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, AppointmentStatus status, string? doctorNotes)
    {
        var ok = await _appointments.UpdateStatusAsync(id, status, doctorNotes);
        TempData[ok ? "Success" : "Error"] =
            ok ? "Randevu durumu güncellendi." : "Güncelleme başarısız.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateDoctors()
    {
        var doctors = await _db.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialty)
            .AsNoTracking()
            .ToListAsync();
        ViewBag.Doctors = doctors.Select(d => new SelectListItem
        {
            Value = d.Id.ToString(),
            Text = $"{d.User!.FullName} — {d.Specialty!.Name}"
        }).ToList();
    }
}
