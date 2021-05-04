using AcademyAPI.Data;
using AcademyAPI.Helpers;
using AcademyAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AcademyEnums.Roles;
using AcademyModels.IdentityModels;
using AcademyViewModels.UserVMs;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AcademyAPI.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppSettings _appSettings;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IOptions<AppSettings> appSettings,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _appSettings = appSettings.Value;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                List<string> errorList = new List<string>();

                var user = new AppUser
                {
                    Email = model.email,
                    UserName = model.username,
                    //SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await _userManager.CreateAsync(user, model.password);

                if (result.Succeeded)
                {
                    if (model.userRole == (int)RoleEnums.Teacher)
                    {
                        await _userManager.AddToRoleAsync(user, "Teacher");
                    }
                    else if (model.userRole == (int)RoleEnums.Student)
                    {
                        await _userManager.AddToRoleAsync(user, "Student");
                    }
                    else if (model.userRole == (int)RoleEnums.Administrator)
                    {
                        await _userManager.AddToRoleAsync(user, "Administrator");
                    }
                    else if (model.userRole == (int)RoleEnums.SuperAdmin)
                    {
                        await _userManager.AddToRoleAsync(user, "SuperAdmin");
                    }
                    else
                    {
                        throw new Exception("User must be mapped to a role");
                    }

                    // Sending Confirmation Email

                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { UserId = user.Id, Code = code }, protocol: HttpContext.Request.Scheme);

                    //await _emailsender.SendEmailAsync(user.Email, "Techhowdy.com - Confirm Your Email", "Please confirm your e-mail by clicking this link: <a href=\"" + callbackUrl + "\">click here</a>");
                    transaction.Commit();
                    transaction.Dispose();

                    return Ok(new { username = user.UserName, email = user.Email, status = 1, message = "Registration Successful" });

                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        errorList.Add(error.Description);
                    }
                }
                transaction.Rollback();
                return BadRequest(new JsonResult(errorList));
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = await _userManager.FindByNameAsync(model.username);

                var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSettings.Secret));

                double tokenExpiryTime = Convert.ToDouble(_appSettings.ExpireTime);

                if (user != null && await _userManager.CheckPasswordAsync(user, model.password))
                {

                    // THen Check If Email Is confirmed
                    //if (!await _userManager.IsEmailConfirmedAsync(user))
                    //{
                    //    ModelState.AddModelError(string.Empty, "User Has not Confirmed Email.");

                    //    return Unauthorized(new { LoginError = "We sent you an Confirmation Email. Please Confirm Your Registration With Techhowdy.com To Log in." });
                    //}

                    // get user Role
                    var roles = await _userManager.GetRolesAsync(user);

                    var tokenHandler = new JwtSecurityTokenHandler();

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                        new Claim(JwtRegisteredClaimNames.Sub, model.username),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                        new Claim("LoggedOn", DateTime.Now.ToString()),

                         }),

                        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                        Issuer = _appSettings.Site,
                        Audience = _appSettings.Audience,
                        Expires = DateTime.UtcNow.AddMinutes(tokenExpiryTime)
                    };

                    // Generate Token

                    var token = tokenHandler.CreateToken(tokenDescriptor);

                    transaction.Commit();
                    transaction.Dispose();

                    return Ok(new { token = tokenHandler.WriteToken(token), expiration = token.ValidTo, username = user.UserName, userRole = roles.FirstOrDefault() });

                }

                // return error
                ModelState.AddModelError("", "Username/Password was not Found");
                transaction.Rollback();
                return Unauthorized(new { LoginError = "Please Check the Login Credentials - Ivalid Username/Password was entered" });
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                transaction.Rollback();
                return new JsonResult(message);
            }

        }

        //[HttpGet("[action]")]
        //[AllowAnonymous]
        //public async Task<IActionResult> ConfirmEmail(string userId, string code)
        //{
        //    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        //    {
        //        ModelState.AddModelError("", "User Id and Code are required");
        //        return BadRequest(ModelState);

        //    }

        //    var user = await _userManager.FindByIdAsync(userId);

        //    if (user == null)
        //    {
        //        return new JsonResult("ERROR");
        //    }

        //    if (user.EmailConfirmed)
        //    {
        //        return Redirect("/login");
        //    }

        //    var result = await _userManager.ConfirmEmailAsync(user, code);

        //    if (result.Succeeded)
        //    {

        //        return RedirectToAction("EmailConfirmed", "Notifications", new { userId, code });

        //    }
        //    else
        //    {
        //        List<string> errors = new List<string>();
        //        foreach (var error in result.Errors)
        //        {
        //            errors.Add(error.ToString());
        //        }
        //        return new JsonResult(errors);
        //    }

        //}
    }
}
