using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

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
                    SetSession("Admin", "John Admin", email);
                    return Json(new { success = true, redirectUrl = "/Account/AdminDashboard" });
                }
                else if (email == "manager@inventory.com" && password == "manager123" && role == "manager")
                {
                    SetSession("Manager", "Sarah Manager", email);
                    return Json(new { success = true, redirectUrl = "/Account/ManagerDashboard" });
                }
                else if (email == "staff@inventory.com" && password == "staff123" && role == "staff")
                {
                    SetSession("Staff", "Mike Staff", email);
                    return Json(new { success = true, redirectUrl = "/Account/StaffDashboard" });
                }

                return Json(new { success = false, message = "Invalid credentials!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private void SetSession(string role, string name, string email)
        {
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

        // ========== DASHBOARDS WITH PERMISSIONS ==========

        public IActionResult StaffDashboard()
        {
            // Check login
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login");

            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login");

            // SET ALL ViewBag VALUES HERE
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = "Staff";  // IMPORTANT: This must be "Staff"
            ViewData["Title"] = "Staff Dashboard";

            // PERMISSIONS - Staff ke liye ALL false except stock
            ViewBag.CanManageUsers = false;
            ViewBag.CanManageCategories = false;
            ViewBag.CanManageSuppliers = false;
            ViewBag.CanManageProducts = false;
            ViewBag.CanManageStock = true;
            ViewBag.CanViewReports = true;
            ViewBag.DashboardAction = "StaffDashboard";

            return View();
        }

        public IActionResult ManagerDashboard()
        {
            // Check login
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login");

            if (HttpContext.Session.GetString("UserRole") != "Manager")
                return RedirectToAction("Login");

            // SET ALL ViewBag VALUES HERE
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = "Manager";  // IMPORTANT: This must be "Manager"
            ViewData["Title"] = "Manager Dashboard";

            // PERMISSIONS - Manager ke liye
            ViewBag.CanManageUsers = false;
            ViewBag.CanManageCategories = true;
            ViewBag.CanManageSuppliers = true;
            ViewBag.CanManageProducts = true;
            ViewBag.CanManageStock = true;
            ViewBag.CanViewReports = true;
            ViewBag.DashboardAction = "ManagerDashboard";

            return View();
        }

        public IActionResult AdminDashboard()
        {
            // Check login
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login");

            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login");

            // SET ALL ViewBag VALUES HERE
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = "Admin";  // IMPORTANT: This must be "Admin"
            ViewData["Title"] = "Admin Dashboard";

            // PERMISSIONS - Admin ke liye ALL true
            ViewBag.CanManageUsers = true;
            ViewBag.CanManageCategories = true;
            ViewBag.CanManageSuppliers = true;
            ViewBag.CanManageProducts = true;
            ViewBag.CanManageStock = true;
            ViewBag.CanViewReports = true;
            ViewBag.DashboardAction = "AdminDashboard";

            return View();
        }
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

        // ========== SIMPLIFIED DASHBOARD VIEWS ==========
        
        public IActionResult AdminDashboardSimple()
        {
            if (!IsLoggedIn() || !IsAdmin())
                return RedirectToAction("Login");
            
            SetDashboardViewBag("Admin", "Admin Dashboard", "fa-shield-alt");
            SetPermissions(true, true, true, true, true, true);
            ViewBag.DashboardAction = "AdminDashboardSimple";
            return View("AdminDashboard");
        }

        public IActionResult ManagerDashboardSimple()
        {
            if (!IsLoggedIn() || !IsManager())
                return RedirectToAction("Login");
            
            SetDashboardViewBag("Manager", "Manager Dashboard", "fa-user-tie");
            SetPermissions(false, true, true, true, true, true);
            ViewBag.DashboardAction = "ManagerDashboardSimple";
            return View("ManagerDashboard");
        }

       
    }
}