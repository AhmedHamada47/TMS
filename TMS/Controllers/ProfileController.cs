using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TMS.Data;
using TMS.Models;

namespace TMS.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.Include(u => u.Tasks).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        return View(user);
    }

    public async Task<IActionResult> Edit()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(User model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (userId != model.Id) return NotFound();

        ModelState.Remove("Password");

        if (ModelState.IsValid)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.Name = model.Name;
            user.Email = model.Email;
            user.AvatarUrl = model.AvatarUrl;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }
}
