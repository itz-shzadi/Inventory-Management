using Inventory.Models;
using Inventory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Inventory.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============ VIEW ============

        [HttpGet]
        public IActionResult Users()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ============ GET ALL ACTIVE USERS ============

        [HttpGet]
        public async Task<IActionResult> GetAllUsers(string searchTerm = "", string role = "")
        {
            try
            {
                var query = _context.Users.Where(u => u.isDelete == false && u.isActive == true);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm));
                }

                if (!string.IsNullOrEmpty(role) && role != "All Roles")
                {
                    query = query.Where(u => u.Role == role);
                }

                var users = await query
                    .Select(u => new {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.Role,
                        u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ GET DELETED USERS ============

        [HttpGet]
        public async Task<IActionResult> GetDeletedUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.isDelete == true)
                    .Select(u => new {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.Role,
                        u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ GET COUNT ============

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            try
            {
                var count = await _context.Users.CountAsync(u => u.isDelete == false && u.isActive == true);
                return Ok(new { count = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ GET USER BY ID ============

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && u.isDelete == false && u.isActive == true)
                    .Select(u => new {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.Role
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ CREATE USER ============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] User user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.UserName))
                    return BadRequest(new { success = false, message = "Username is required" });

                if (string.IsNullOrWhiteSpace(user.Email))
                    return BadRequest(new { success = false, message = "Email is required" });

                if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 4)
                    return BadRequest(new { success = false, message = "Password must be at least 4 characters" });

                if (string.IsNullOrWhiteSpace(user.Role))
                    return BadRequest(new { success = false, message = "Role is required" });

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email || u.UserName == user.UserName);

                if (existingUser != null)
                {
                    return BadRequest(new { success = false, message = "User with this email or username already exists" });
                }

                var newUser = new User
                {
                    UserName = user.UserName.Trim(),
                    Email = user.Email.Trim().ToLower(),
                    Password = user.Password,
                    Role = user.Role,
                    CreatedAt = DateTime.Now,
                    isActive = true,
                    isDelete = false
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ UPDATE USER ============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] User user)
        {
            try
            {
                if (user.Id <= 0)
                    return BadRequest(new { success = false, message = "Invalid user ID" });

                var existingUser = await _context.Users.FindAsync(user.Id);
                if (existingUser == null || existingUser.isDelete == true)
                    return NotFound(new { success = false, message = "User not found" });

                existingUser.UserName = user.UserName.Trim();
                existingUser.Email = user.Email.Trim().ToLower();
                existingUser.Role = user.Role;

                if (!string.IsNullOrWhiteSpace(user.Password) && user.Password.Length >= 4)
                {
                    existingUser.Password = user.Password;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ DELETE USER (FIXED - ADD THIS) ============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Soft delete - just mark as deleted
                user.isDelete = true;
                user.isActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ SOFT DELETE ============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                user.isDelete = true;
                user.isActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User soft deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ RESTORE USER ============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                user.isDelete = false;
                user.isActive = true;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User restored successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ PERMANENT DELETE (Hard Delete) ============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User permanently deleted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        // ============ DELETE ALL USERS ============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                _context.Users.RemoveRange(users);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "All users deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}