using KlinikYonetimSistemi.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KlinikYonetimSistemi.Web.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        // --- Roller ---
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // --- Uzmanlıklar (eksik olanlar tek tek eklenir) ---
        var specialtySeeds = new (string Name, string Description)[]
        {
            ("Dahiliye", "İç hastalıkları ana bilim dalı"),
            ("Kardiyoloji", "Kalp ve damar hastalıkları"),
            ("Pediatri", "Çocuk sağlığı ve hastalıkları"),
            ("Ortopedi", "Kas ve iskelet sistemi hastalıkları"),
            ("Dermatoloji", "Cilt hastalıkları"),
            ("Nöroloji", "Sinir sistemi hastalıkları"),
            ("Göz Hastalıkları", "Oftalmoloji ana bilim dalı"),
        };
        foreach (var s in specialtySeeds)
        {
            if (!await db.Specialties.AnyAsync(x => x.Name == s.Name))
                db.Specialties.Add(new Specialty { Name = s.Name, Description = s.Description });
        }
        await db.SaveChangesAsync();

        // --- Yönetici ---
        await EnsureUserAsync(userManager, "admin@klinik.local", "Sistem Yöneticisi", "Admin!234", Roles.Admin);

        // --- Doktorlar ---
        var doctorSeeds = new[]
        {
            new DoctorSeed("doktor@klinik.local",  "Dr. Ayşe Yılmaz",  "Doktor!234", "Kardiyoloji", "TR-DOK-0001", 8,  "Kardiyoloji uzmanı, 8 yıllık klinik deneyim."),
            new DoctorSeed("m.kaya@klinik.local",  "Dr. Mehmet Kaya",  "Doktor!234", "Dahiliye",    "TR-DOK-0002", 12, "Dahiliye uzmanı, kronik hastalık takibi."),
            new DoctorSeed("z.sahin@klinik.local", "Dr. Zeynep Şahin", "Doktor!234", "Pediatri",    "TR-DOK-0003", 6,  "Çocuk sağlığı ve hastalıkları uzmanı."),
            new DoctorSeed("c.ozturk@klinik.local","Dr. Can Öztürk",   "Doktor!234", "Ortopedi",    "TR-DOK-0004", 15, "Ortopedi ve travmatoloji uzmanı."),
            new DoctorSeed("e.aydin@klinik.local", "Dr. Elif Aydın",   "Doktor!234", "Dermatoloji", "TR-DOK-0005", 5,  "Dermatoloji uzmanı, estetik dermatoloji ilgi alanı."),
            new DoctorSeed("s.demir@klinik.local", "Dr. Selin Demir",  "Doktor!234", "Nöroloji",    "TR-DOK-0006", 10, "Nöroloji uzmanı, baş ağrısı ve epilepsi takibi."),
        };

        foreach (var d in doctorSeeds)
        {
            var user = await EnsureUserAsync(userManager, d.Email, d.FullName, d.Password, Roles.Doctor);
            if (user is not null && !await db.Doctors.AnyAsync(x => x.UserId == user.Id))
            {
                var specialty = await db.Specialties.FirstOrDefaultAsync(s => s.Name == d.SpecialtyName);
                if (specialty is null)
                    continue;
                db.Doctors.Add(new Doctor
                {
                    UserId = user.Id,
                    SpecialtyId = specialty.Id,
                    LicenseNumber = d.LicenseNumber,
                    YearsOfExperience = d.Years,
                    Bio = d.Bio
                });
            }
        }
        await db.SaveChangesAsync();

        // --- Hastalar ---
        var patientSeeds = new[]
        {
            new PatientSeed("hasta@klinik.local",   "Mehmet Demir",   "Hasta!234", new DateTime(1990, 5, 14),  "A+",  "Penisilin"),
            new PatientSeed("a.yildiz@klinik.local","Ahmet Yıldız",   "Hasta!234", new DateTime(1985, 11, 2),  "0+",  null),
            new PatientSeed("f.celik@klinik.local", "Fatma Çelik",    "Hasta!234", new DateTime(1998, 3, 27),  "B-",  "Polen alerjisi"),
            new PatientSeed("h.arslan@klinik.local","Hüseyin Arslan", "Hasta!234", new DateTime(1972, 8, 9),   "AB+", "Aspirin"),
            new PatientSeed("g.koc@klinik.local",   "Gül Koç",        "Hasta!234", new DateTime(2005, 1, 19),  "A-",  null),
        };

        foreach (var p in patientSeeds)
        {
            var user = await EnsureUserAsync(userManager, p.Email, p.FullName, p.Password, Roles.Patient);
            if (user is not null && !await db.Patients.AnyAsync(x => x.UserId == user.Id))
            {
                db.Patients.Add(new Patient
                {
                    UserId = user.Id,
                    DateOfBirth = p.DateOfBirth,
                    BloodType = p.BloodType,
                    Allergies = p.Allergies
                });
            }
        }
        await db.SaveChangesAsync();

        // --- Demo randevular ---
        if (!await db.Appointments.AnyAsync())
        {
            var docs = await db.Doctors.OrderBy(d => d.Id).ToListAsync();
            var pats = await db.Patients.OrderBy(p => p.Id).ToListAsync();
            if (docs.Count > 0 && pats.Count > 0)
            {
                var now = DateTime.UtcNow;
                db.Appointments.AddRange(
                    new Appointment { DoctorId = docs[0].Id, PatientId = pats[0].Id,
                        ScheduledAt = now.AddDays(2).Date.AddHours(9), DurationMinutes = 30,
                        Status = AppointmentStatus.Scheduled, Reason = "Genel kontrol" },
                    new Appointment { DoctorId = docs[0].Id, PatientId = pats[1 % pats.Count].Id,
                        ScheduledAt = now.AddDays(-5).Date.AddHours(14), DurationMinutes = 45,
                        Status = AppointmentStatus.Completed, Reason = "Göğüs ağrısı",
                        DoctorNotes = "EKG normal sınırlarda, üç ay sonra kontrol önerildi." },
                    new Appointment { DoctorId = docs[1 % docs.Count].Id, PatientId = pats[2 % pats.Count].Id,
                        ScheduledAt = now.AddDays(1).Date.AddHours(11), DurationMinutes = 30,
                        Status = AppointmentStatus.Confirmed, Reason = "Tansiyon takibi" },
                    new Appointment { DoctorId = docs[2 % docs.Count].Id, PatientId = pats[3 % pats.Count].Id,
                        ScheduledAt = now.AddDays(-2).Date.AddHours(10), DurationMinutes = 30,
                        Status = AppointmentStatus.Cancelled, Reason = "Çocuk aşı randevusu" }
                );
                await db.SaveChangesAsync();
            }
        }

        // --- Demo tıbbi kayıt (tamamlanmış randevuya bağlı) ---
        if (!await db.MedicalRecords.AnyAsync())
        {
            var completed = await db.Appointments
                .FirstOrDefaultAsync(a => a.Status == AppointmentStatus.Completed);
            if (completed is not null)
            {
                db.MedicalRecords.Add(new MedicalRecord
                {
                    PatientId = completed.PatientId,
                    AppointmentId = completed.Id,
                    Diagnosis = "Hafif hipertansiyon",
                    Treatment = "Tuz kısıtlaması ve düzenli egzersiz önerildi.",
                    Prescription = "Günde 1x5 mg amlodipin"
                });
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string fullName, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return null;
            await userManager.AddToRoleAsync(user, role);
        }
        return user;
    }

    private record DoctorSeed(
        string Email, string FullName, string Password,
        string SpecialtyName, string LicenseNumber, int Years, string Bio);

    private record PatientSeed(
        string Email, string FullName, string Password,
        DateTime DateOfBirth, string BloodType, string? Allergies);
}
