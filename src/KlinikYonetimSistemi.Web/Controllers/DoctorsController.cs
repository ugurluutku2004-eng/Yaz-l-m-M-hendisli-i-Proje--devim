using KlinikYonetimSistemi.Web.Data;
using KlinikYonetimSistemi.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KlinikYonetimSistemi.Web.Controllers;

[Authorize]
public class DoctorsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public DoctorsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index(int? specialtyId)
    {
        var q = _db.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialty)
            .AsNoTracking();
        if (specialtyId.HasValue) q = q.Where(d => d.SpecialtyId == specialtyId.Value);

        ViewBag.Specialties = await _db.Specialties.AsNoTracking()
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name, Selected = s.Id == specialtyId })
            .ToListAsync();

        return View(await q.OrderBy(d => d.User!.FullName).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var doc = await _db.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialty)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();
        return View(doc);
    }

    [Authorize(Roles = Roles.Doctor + "," + Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var doc = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();
        var userId = _users.GetUserId(User)!;
        if (!User.IsInRole(Roles.Admin) && doc.UserId != userId) return Forbid();

        ViewBag.Specialties = await _db.Specialties.AsNoTracking()
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name, Selected = s.Id == doc.SpecialtyId })
            .ToListAsync();
        return View(doc);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Doctor + "," + Roles.Admin)]
    public async Task<IActionResult> Edit(int id, Doctor input)
    {
        var doc = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();
        var userId = _users.GetUserId(User)!;
        if (!User.IsInRole(Roles.Admin) && doc.UserId != userId) return Forbid();

        doc.SpecialtyId = input.SpecialtyId;
        doc.LicenseNumber = input.LicenseNumber;
        doc.Bio = input.Bio;
        doc.YearsOfExperience = input.YearsOfExperience;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Doktor profili güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
