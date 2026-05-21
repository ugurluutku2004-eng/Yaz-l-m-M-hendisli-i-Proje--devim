using System.ComponentModel.DataAnnotations;
using KlinikYonetimSistemi.Web.Data;
using KlinikYonetimSistemi.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KlinikYonetimSistemi.Web.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(11, MinimumLength = 11, ErrorMessage = "T.C. Kimlik No 11 haneli olmalıdır.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "T.C. Kimlik No yalnızca rakamlardan oluşmalıdır.")]
        [Display(Name = "T.C. Kimlik No (opsiyonel)")]
        public string? NationalId { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, ErrorMessage = "Şifre en az {2}, en fazla {1} karakter olmalıdır.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Şifre (Tekrar)")]
        [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FullName = Input.FullName,
            NationalId = string.IsNullOrWhiteSpace(Input.NationalId) ? null : Input.NationalId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("Yeni kullanıcı kaydı oluşturuldu.");

            // Yeni kayıtlar varsayılan olarak hasta rolüne atanır.
            await _userManager.AddToRoleAsync(user, Roles.Patient);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, TranslateIdentityError(error));
        }

        return Page();
    }

    private static string TranslateIdentityError(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Bu e-posta adresi zaten kayıtlı.",
        "PasswordTooShort" => "Şifre en az 8 karakter olmalıdır.",
        "PasswordRequiresNonAlphanumeric" => "Şifre en az bir özel karakter içermelidir.",
        "PasswordRequiresDigit" => "Şifre en az bir rakam içermelidir.",
        "PasswordRequiresUpper" => "Şifre en az bir büyük harf içermelidir.",
        _ => error.Description
    };
}
