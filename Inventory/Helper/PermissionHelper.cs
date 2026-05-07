using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace InventoryMS.Helpers
{
    public static class PermissionHelper
    {
        // Check if user is logged in
        public static bool IsLoggedIn(ISession session)
        {
            return session.GetString("IsLoggedIn") == "true";
        }

        // Get current user role
        public static string GetUserRole(ISession session)
        {
            return session.GetString("UserRole") ?? "guest";
        }

        // Check if user has specific role
        public static bool HasRole(ISession session, string role)
        {
            var userRole = GetUserRole(session);
            return userRole == role;
        }

        // Check if user is Admin
        public static bool IsAdmin(ISession session)
        {
            return HasRole(session, "admin");
        }

        // Check if user is Manager
        public static bool IsManager(ISession session)
        {
            return HasRole(session, "manager");
        }

        // Check if user is Staff
        public static bool IsStaff(ISession session)
        {
            return HasRole(session, "staff");
        }

        // ========== PERMISSION CHECKS ==========

        // Manage Users - Only Admin
        public static bool CanManageUsers(ISession session)
        {
            return IsAdmin(session);
        }

        // Manage Categories - Admin and Manager
        public static bool CanManageCategories(ISession session)
        {
            return IsAdmin(session) || IsManager(session);
        }

        // Manage Suppliers - Admin and Manager
        public static bool CanManageSuppliers(ISession session)
        {
            return IsAdmin(session) || IsManager(session);
        }

        // Manage Products - Admin and Manager
        public static bool CanManageProducts(ISession session)
        {
            return IsAdmin(session) || IsManager(session);
        }

        // Stock In/Out - All roles
        public static bool CanManageStock(ISession session)
        {
            return IsLoggedIn(session);
        }

        // View Reports - Admin and Manager (Staff Limited)
        public static bool CanViewReports(ISession session)
        {
            return IsAdmin(session) || IsManager(session);
        }

        // View Full Reports - Admin only
        public static bool CanViewFullReports(ISession session)
        {
            return IsAdmin(session);
        }

        // View Limited Reports - Staff
        public static bool CanViewLimitedReports(ISession session)
        {
            return IsStaff(session);
        }

        // View Dashboard - All roles
        public static bool CanViewDashboard(ISession session)
        {
            return IsLoggedIn(session);
        }
    }

    // Custom Authorization Filter
    public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public RoleAuthorizeAttribute(string roles)
        {
            _allowedRoles = roles.Split(',');
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var userRole = session.GetString("UserRole");

            if (string.IsNullOrEmpty(userRole))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (!_allowedRoles.Contains(userRole))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }
    }
}