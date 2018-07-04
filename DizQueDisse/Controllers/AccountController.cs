using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using DizQueDisse.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace DizQueDisse.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<MyUser> _userManager;
        private readonly SignInManager<MyUser> _signInManager;

        public AccountController(UserManager<MyUser> userManager, SignInManager<MyUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Returns login page.
        /// </summary>
        /// <param name="returnUrl">The url to which the user will be redirect upon successful login.</param>
        /// <returns></returns>
        [HttpGet("/Conta")]
        public async Task<IActionResult> Index(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl; //this will fill in the return url on form, so that if login is successful you are redirected there;
            return View();
        }

        /// <summary>
        /// Allows the manager to login from web page.
        /// </summary>
        /// <param name="model">The model containing login credentials.</param>
        /// <param name="returnUrl">The url to which redirect if login is successfull.</param>
        /// <returns></returns>
        [HttpPost("/account/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);
                if (result.Succeeded)
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return RedirectToAction("Index");
                }
            }

            // If we got this far, something failed, redisplay form
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Allows Azure Functions to login as manager.
        /// </summary>
        /// <param name="model"></param>
        [HttpPost("/account/loginazure")]
        public async Task<IActionResult> Login([FromBody] LoginAzure model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);

                if (result.Succeeded)
                    return Ok();
                else
                    return BadRequest();
            }

            // If we got this far, something failed, redisplay form
            return BadRequest();

        }

        /// <summary>
        /// Allows manager to logout.
        /// </summary>
        [Authorize]
        [HttpPost("/account/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
