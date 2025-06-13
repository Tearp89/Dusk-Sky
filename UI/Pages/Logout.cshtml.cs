using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LogoutModel : PageModel
{
    public IActionResult OnPost()
    {
        Response.Cookies.Delete("DuskSkyToken");
        return RedirectToPage("/StartPage"); // O donde sea tu página inicial
    }
}
