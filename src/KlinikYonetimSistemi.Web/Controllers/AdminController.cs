using KlinikYonetimSistemi.Web.Data;
using KlinikYonetimSistemi.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KlinikYonetimSistemi.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new
        {
            TotalUsers = await _db.Users.CountAsync(),
            TotalDoctors = await _db.Doctors.CountAsync(),
            TotalPatients = await _db.Patients.CountAsync(),
            TotalAppointments = await _db.Appointments.CountAsync(),
            UpcomingAppointments = await _db.Appointments
                .Where(a => a.ScheduledAt >= DateTime.UtcNow && a.Status != AppointmentStatus.Cancelled)
                .CountAsync(),
            CompletedAppointments = await _db.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed).CountAsync()
        };
        ViewBag.Stats = stats;

        var recent = await _db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Include(a => a.Patient).ThenInclude(p => p!.User)
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();
        return View(recent);
    }

    public async Task<IActionResult> Users()
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        var roles = new Dictionary<string, IList<string>>();
        foreach (var u in users)
            roles[u.Id] = await _users.GetRolesAsync(u);
        ViewBag.UserRoles = roles;
        return View(users);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string role)
    {
        if (!Roles.All.Contains(role))
        {
            TempData["Error"] = "Geçersiz rol.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _users.FindByIdAsync(userId);
        if (user is null)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Users));
        }

        var current = await _users.GetRolesAsync(user);
        await _users.RemoveFromRolesAsync(user, current);
        await _users.AddToRoleAsync(user, role);

        if (role == Roles.Doctor && !await _db.Doctors.AnyAsync(d => d.UserId == user.Id))
        {
            var anySpecialty = await _db.Specialties.FirstAsync();
            _db.Doctors.Add(new Doctor
            {
                UserId = user.Id,
                SpecialtyId = anySpecialty.Id,
                LicenseNumber = "PENDING",
                YearsOfExperience = 0
            });
            await _db.SaveChangesAsync();
        }
        else if (role == Roles.Patient && !await _db.Patients.AnyAsync(p => p.UserId == user.Id))
        {
            _db.Patients.Add(new Patient { UserId = user.Id });
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = $"{user.FullName} için rol '{role}' olarak güncellendi.";
        return RedirectToAction(nameof(Users));
    }
}
