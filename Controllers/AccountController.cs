using CustomIdentity.Models;
using CustomIdentity.Services;
using CustomIdentity.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CustomIdentity.Controllers;

public class AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager,IEmailService emailService) : Controller
{
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVM model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var user = await userManager.FindByNameAsync(model.Username);
            if (user == null || !await userManager.CheckPasswordAsync(user, model.Password))
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre yanlış.");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "E-posta adresinizi doğrulamadınız. Lütfen e-posta adresinizi kontrol edin ve doğrulama işlemini tamamlayın.");
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, false);
            if (result.Succeeded)
            {
                // Eğer e-posta doğrulaması yapılmışsa, kullanıcıyı belirtilen returnUrl'a yönlendir
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Hesabınızın girişine izin verilmiyor.");
            }
            else if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Hesabınız kilitlendi, lütfen daha sonra tekrar deneyin.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi");
            }
        }

        // Model geçerli değilse giriş formunu tekrar göster
        return View(model);
    }


    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVM model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            // Yeni kullanıcı oluştur
            AppUser user = new()
            {
                Name = model.Name,
                UserName = model.Email,
                Email = model.Email,
                Address = model.Address
            };

            var result = await userManager.CreateAsync(user, model.Password!);

            if (result.Succeeded)
            {
                // Kullanıcıyı email ile doğrula
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);
                await emailService.SendEmailAsync(user.Email, "Confirm your email",
     $"Please confirm your email address to complete your registration. Click <a href='{confirmationLink}'>here</a> to confirm your account. Thank you!");



               
                return RedirectToLocal(returnUrl); // Başarılı kayıt durumunda yönlendirme yap
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description); // Kayıt hatası varsa model hatası ekle
            }
        }
        return View(model); // Model geçerli değilse register view'ını tekrar göster
    }

    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (userId == null || token == null)
        {
            return RedirectToAction("Index", "Home"); // Hatalı istek durumunda ana sayfaya yönlendir
        }
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction("Index", "Home"); // Kullanıcı bulunamazsa ana sayfaya yönlendir
        }
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            // Email doğrulama başarılı, istediğiniz eylemi gerçekleştirin
            return View("EmailConfirmed"); // Email doğrulandı sayfasını göster
        }
        return RedirectToAction("Index", "Home"); // Email doğrulaması başarısızsa ana sayfaya yönlendir
    }


    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index","Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index","Home");
    }
}
