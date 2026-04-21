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

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!await db.Specialties.AnyAsync())
        {
            db.Specialties.AddRange(
                new Specialty { Name = "Dahiliye", Description = "İç hastalıkları ana bilim dalı" },
                new Specialty { Name = "Kardiyoloji", Description = "Kalp ve damar hastalıkları" },
                new Specialty { Name = "Pediatri", Description = "Çocuk sağlığı ve hastalıkları" },
                new Specialty { Name = "Ortopedi", Description = "Kas ve iskelet sistemi hastalıkları" },
                new Specialty { Name = "Dermatoloji", Description = "Cilt hastalıkları" }
            );
            await db.SaveChangesAsync();
        }

        var adminEmail = "admin@klinik.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Sistem Yöneticisi",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin!234");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        var doctorEmail = "doktor@klinik.local";
        if (await userManager.FindByEmailAsync(doctorEmail) is null)
        {
            var doctor = new ApplicationUser
            {
                UserName = doctorEmail,
                Email = doctorEmail,
                FullName = "Dr. Ayşe Yılmaz",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(doctor, "Doktor!234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(doctor, Roles.Doctor);
                var kardiyoloji = await db.Specialties.FirstAsync(s => s.Name == "Kardiyoloji");
                db.Doctors.Add(new Doctor
                {
                    UserId = doctor.Id,
                    SpecialtyId = kardiyoloji.Id,
                    LicenseNumber = "TR-DOK-0001",
                    YearsOfExperience = 8,
                    Bio = "Kardiyoloji uzmanı, 8 yıllık klinik deneyim."
                });
                await db.SaveChangesAsync();
            }
        }

        var patientEmail = "hasta@klinik.local";
        if (await userManager.FindByEmailAsync(patientEmail) is null)
        {
            var patient = new ApplicationUser
            {
                UserName = patientEmail,
                Email = patientEmail,
                FullName = "Mehmet Demir",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(patient, "Hasta!234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(patient, Roles.Patient);
                db.Patients.Add(new Patient
                {
                    UserId = patient.Id,
                    DateOfBirth = new DateTime(1990, 5, 14),
                    BloodType = "A+",
                    Allergies = "Penisilin"
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
