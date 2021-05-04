using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcademyAPI.Controllers
{
    [Authorize]
    public class UserManagementController : Controller
    {
        //private readonly IUserManangementRepository _userManangementRepository;
        public UserManagementController()
        {
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
