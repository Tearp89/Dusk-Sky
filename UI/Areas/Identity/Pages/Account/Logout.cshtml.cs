using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

    public class LogoutSessionModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutSessionModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync("Cookies");
            Response.Cookies.Delete("DuskSkyToken");
            UserSessionManager.Instance.ClearSession();


            HttpContext.Session.Clear(); // si usas sesiones
            return RedirectToPage("/");

        }

    }

