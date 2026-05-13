using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;

namespace InventoryMS.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            HttpContext.Session?.Clear();
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password, string role)
        {
            try
            {
                if (email == "admin@inventory.com" && password == "admin123" && role == "admin")
                {
                    SetSession(1, "Admin", "John Admin", email);  // UserId = 1
                    return Json(new { success = true, redirectUrl = "/Account/Dashboard" });
                }
                else if (email == "manager@inventory.com" && password == "manager123" && role == "manager")
                {
                    SetSession(2, "Manager", "Sarah Manager", email);  // UserId = 2
                    return Json(new { success = true, redirectUrl = "/Account/Dashboard" });
                }
                else if (email == "staff@inventory.com" && password == "staff123" && role == "staff")
                {
                    SetSession(3, "Staff", "Mike Staff", email);  // UserId = 3
                    return Json(new { success = true, redirectUrl = "/Account/Dashboard" });
                }

                return Json(new { success = false, message = "Invalid credentials!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // UPDATED: SetSession with UserId parameter
        private void SetSession(int userId, string role, string name, string email)
        {
            HttpContext.Session.SetInt32("UserId", userId);  // ✅ IMPORTANT: Set UserId
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("UserName", name);
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("IsLoggedIn", "true");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Json(new { success = true, redirectUrl = "/Account/Login" });
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // ========== SINGLE DASHBOARD FOR ALL ROLES ==========

        public IActionResult Dashboard()
        {
            // Check login
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login");

            string userRole = HttpContext.Session.GetString("UserRole");

            // Agar role null ya invalid hai to login par bhejo
            if (string.IsNullOrEmpty(userRole))
                return RedirectToAction("Login");

            // Set common ViewBag values
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = userRole;
            ViewData["Title"] = $"{userRole} Dashboard";

            // Set permissions based on role
            switch (userRole)
            {
                case "Admin":
                    ViewBag.CanManageUsers = true;
                    ViewBag.CanManageCategories = true;
                    ViewBag.CanManageSuppliers = true;
                    ViewBag.CanManageProducts = true;
                    ViewBag.CanManageStock = true;
                    ViewBag.CanViewReports = true;
                    break;

                case "Manager":
                    ViewBag.CanManageUsers = false;
                    ViewBag.CanManageCategories = true;
                    ViewBag.CanManageSuppliers = true;
                    ViewBag.CanManageProducts = true;
                    ViewBag.CanManageStock = true;
                    ViewBag.CanViewReports = true;
                    break;

                case "Staff":
                    ViewBag.CanManageUsers = false;
                    ViewBag.CanManageCategories = false;
                    ViewBag.CanManageSuppliers = false;
                    ViewBag.CanManageProducts = false;
                    ViewBag.CanManageStock = true;
                    ViewBag.CanViewReports = true;
                    break;

                default:
                    return RedirectToAction("Login");
            }

            ViewBag.DashboardAction = "Dashboard";

            return View("Dashboard");
        }

        // ========== GET CURRENT USER ==========

        [HttpGet]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var userName = HttpContext.Session.GetString("UserName");
                var userRole = HttpContext.Session.GetString("UserRole");
                var userEmail = HttpContext.Session.GetString("UserEmail");

                if (!userId.HasValue || HttpContext.Session.GetString("IsLoggedIn") != "true")
                {
                    return Unauthorized(new { success = false, message = "Not logged in" });
                }

                return Ok(new
                {
                    success = true,
                    userId = userId.Value,
                    userName = userName ?? "Unknown",
                    userRole = userRole ?? "Unknown",
                    userEmail = userEmail ?? "",
                    isLoggedIn = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // ========== TEST SESSION ENDPOINT ==========

        [HttpGet]
        public IActionResult TestSession()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");
            var isLoggedIn = HttpContext.Session.GetString("IsLoggedIn");

            return Json(new
            {
                userId = userId,
                userName = userName,
                userRole = userRole,
                isLoggedIn = isLoggedIn,
                sessionId = HttpContext.Session.Id
            });
        }

        // ========== HELPER METHODS ==========

        private void SetDashboardViewBag(string role, string title, string icon)
        {
            ViewBag.UserRole = role;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewData["Title"] = title;
            ViewBag.PageIcon = icon;
        }

        private void SetPermissions(bool canManageUsers, bool canManageCategories,
                                     bool canManageSuppliers, bool canManageProducts,
                                     bool canManageStock, bool canViewReports)
        {
            ViewBag.CanManageUsers = canManageUsers;
            ViewBag.CanManageCategories = canManageCategories;
            ViewBag.CanManageSuppliers = canManageSuppliers;
            ViewBag.CanManageProducts = canManageProducts;
            ViewBag.CanManageStock = canManageStock;
            ViewBag.CanViewReports = canViewReports;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetString("IsLoggedIn") == "true";
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        private bool IsManager()
        {
            return HttpContext.Session.GetString("UserRole") == "Manager";
        }

        private bool IsStaff()
        {
            return HttpContext.Session.GetString("UserRole") == "Staff";
        }
    }
}