using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CoreBB.Web.Models;

namespace CoreBB.Web.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private CoreBBContext _dbContext;

        public UserController(CoreBBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet, Authorize(Roles = Roles.Administrator)]
        public IActionResult Index()
        {
            var users = _dbContext.User.ToList();
            return View(users);
        }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> Register()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View();
        }

        [AllowAnonymous, HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception("Invalid registration information.");
            }

            model.Name = model.Name.Trim();
            model.Password = model.Password.Trim();
            model.RepeatPassword = model.RepeatPassword.Trim();

            var targetUser = _dbContext.User
                .SingleOrDefault(u => u.Name.Equals(model.Name, StringComparison.CurrentCultureIgnoreCase));

            if (targetUser != null)
            {
                throw new Exception("User name already exists.");
            }

            if (!model.Password.Equals(model.RepeatPassword))
            {
                throw new Exception("Passwords are not identical.");
            }

            var hasher = new PasswordHasher<User>();
            targetUser = new User { Name = model.Name, RegisterDateTime = DateTime.Now, Description = model.Description };
            targetUser.PasswordHash = hasher.HashPassword(targetUser, model.Password);

            if (_dbContext.User.Count() == 0)
            {
                targetUser.IsAdministrator = true;
            }

            await _dbContext.User.AddAsync(targetUser);
            await _dbContext.SaveChangesAsync();

            await LogInUserAsync(targetUser);

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> LogIn()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View();
        }

        [AllowAnonymous, HttpPost]
        public async Task<IActionResult> LogIn(LogInViewModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception("Invalid user information.");
            }

            var targetUser = _dbContext.User.SingleOrDefault(u => u.Name.Equals(model.Name, StringComparison.CurrentCultureIgnoreCase));
            if (targetUser == null)
            {
                throw new Exception("User does not exist.");
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(targetUser, targetUser.PasswordHash, model.Password);
            if (result != PasswordVerificationResult.Success)
            {
                throw new Exception("The password is wrong.");
            }

            await LogInUserAsync(targetUser);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Detail(string name)
        {
            var user = _dbContext.User.SingleOrDefault(u => u.Name == name);
            if (user == null)
            {
                throw new Exception($"User '{name}' does not exist.");
            }

            return View(user);
        }

        [HttpGet]
        public IActionResult Edit(string name)
        {
            if (User.Identity.Name != name && !User.IsInRole(Roles.Administrator))
            {
                throw new Exception("Operation is denied.");
            }

            var user = _dbContext.User.SingleOrDefault(u => u.Name == name);
            if (user == null)
            {
                throw new Exception($"User '{name}' does not exist.");
            }

            var model = UserEditViewModel.FromUser(user);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception("Invalid user information.");
            }

            var user = _dbContext.User
                .SingleOrDefault(u => u.Name.Equals(model.Name, StringComparison.CurrentCultureIgnoreCase));

            if (user == null)
            {
                throw new Exception("User does not exist.");
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                model.Password = model.Password.Trim();
                model.RepeatPassword = model.RepeatPassword.Trim();
                if (!model.Password.Equals(model.RepeatPassword))
                {
                    throw new Exception("Passwords are not identical.");
                }

                var hasher = new PasswordHasher<User>();
                if (!User.IsInRole(Roles.Administrator))
                {
                    var vr = hasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword);
                    if (vr != PasswordVerificationResult.Success)
                    {
                        throw new Exception("Please provide correct current password.");
                    }
                }

                user.PasswordHash = hasher.HashPassword(user, model.Password);
            }

            user.Description = model.Description;

            if (User.IsInRole(Roles.Administrator))
            {
                user.IsAdministrator = model.IsAdministrator;
                user.IsLocked = model.IsLocked;
            }

            _dbContext.User.Update(user);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Detail", new { name = user.Name });
        }

        private async Task LogInUserAsync(User user)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.Name));
            if (user.IsAdministrator)
            {
                claims.Add(new Claim(ClaimTypes.Role, Roles.Administrator));
            }

            var claimsIndentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIndentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
            user.LastLogInDateTime = DateTime.Now;
            await _dbContext.SaveChangesAsync();
        }
    }
}