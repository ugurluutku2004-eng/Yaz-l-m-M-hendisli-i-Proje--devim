using KlinikYonetimSistemi.Web.Data;
using KlinikYonetimSistemi.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KlinikYonetimSistemi.Web.Controllers;

[Authorize]
public class PatientsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public PatientsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [Authorize(Roles = Roles.Admin + "," + Roles.Doctor)]
    public async Task<IActionResult> Index()
    {
        var list = await _db.Patients
            .Include(p => p.User)
            .AsNoTracking()
            .OrderBy(p => p.User!.FullName)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var patient = await _db.Patients
            .Include(p => p.User)
            .Include(p => p.MedicalRecords)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (patient is null) return NotFound();

        var userId = _users.GetUserId(User)!;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Doctor) && patient.UserId != userId)
            return Forbid();

        return View(patient);
    }

    [Authorize(Roles = Roles.Doctor + "," + Roles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMedicalRecord(int patientId, int? appointmentId, string diagnosis, string? treatment, string? prescription)
    {
        if (string.IsNullOrWhiteSpace(diagnosis))
        {
            TempData["Error"] = "Tanı alanı zorunludur.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        _db.MedicalRecords.Add(new MedicalRecord
        {
            PatientId = patientId,
            AppointmentId = appointmentId,
            Diagnosis = diagnosis,
            Treatment = treatment,
            Prescription = prescription
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Tıbbi kayıt eklendi.";
        return RedirectToAction(nameof(Details), new { id = patientId });
    }
}
