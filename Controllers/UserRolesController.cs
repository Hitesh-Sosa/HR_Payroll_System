using HrPayrollSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HrPayrollSystem.Controllers;

[Authorize(Roles = "Admin")]
public class UserRolesController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserRolesController(UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public class UserRoleVm
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.OrderBy(u => u.Email).ToList();
        var list = new List<UserRoleVm>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            list.Add(new UserRoleVm { UserId = u.Id, Email = u.Email ?? "", Roles = roles.ToList() });
        }

        ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).OrderBy(r => r).ToList();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) { TempData["Error"] = "User not found."; return RedirectToAction(nameof(Index)); }

        // SAFETY 1: can't change your own role while logged in as that user
        if (user.UserName == User.Identity!.Name)
        {
            TempData["Error"] = "You cannot change your own role (safety lock).";
            return RedirectToAction(nameof(Index));
        }

        if (!await _roleManager.RoleExistsAsync(newRole))
        {
            TempData["Error"] = "Unknown role.";
            return RedirectToAction(nameof(Index));
        }

        // SAFETY 2: never demote the last remaining Admin
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        if (admins.Count == 1 && admins[0].Id == user.Id && newRole != "Admin")
        {
            TempData["Error"] = "Cannot demote the last remaining Admin.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);

        TempData["Success"] = $"{user.Email} is now a {newRole}.";
        return RedirectToAction(nameof(Index));
    }
}