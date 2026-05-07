using Inventory.Data.IServices;
using Inventory.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Data.Services
{
    public class UserServices : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserServices(ApplicationDbContext context)
        {
            _context = context;
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Sirf active users (jo delete nahi hue) return karega
        public List<User> GetAllUsers()
        {
            return _context.Users
                .Where(u => !u.isDelete && u.isActive) // Soft delete filter
                .Select(u => new User
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = u.Role,
                    Password = null,
                    isActive = u.isActive,
                    isDelete = u.isDelete
                })
                .ToList();
        }

        // Sab users return karega (including deleted) - Admin ke liye
        public List<User> GetAllUsersIncludingDeleted()
        {
            return _context.Users
                .Select(u => new User
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = u.Role,
                    Password = null,
                    isActive = u.isActive,
                    isDelete = u.isDelete
                })
                .ToList();
        }

        // Sirf deleted users return karega
        public List<User> GetDeletedUsers()
        {
            return _context.Users
                .Where(u => u.isDelete)
                .Select(u => new User
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = u.Role,
                    Password = null,
                    isActive = u.isActive,
                    isDelete = u.isDelete
                })
                .ToList();
        }

        public User? GetUserById(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return null;

            return new User
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role,
                Password = null,
                isActive = user.isActive,
                isDelete = user.isDelete
            };
        }

        public bool CreateUser(User user)
        {
            try
            {
                // Check if user already exists
                if (_context.Users.Any(u => u.UserName == user.UserName || u.Email == user.Email))
                    return false;

                user.Password = HashPassword(user.Password);
                user.isActive = true;  // Default active
                user.isDelete = false; // Default not deleted

                _context.Users.Add(user);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateUser(User user)
        {
            try
            {
                var existingUser = _context.Users.Find(user.Id);
                if (existingUser == null) return false;

                // Check unique constraints
                if (_context.Users.Any(u => u.UserName == user.UserName && u.Id != user.Id && !u.isDelete))
                    return false;
                if (_context.Users.Any(u => u.Email == user.Email && u.Id != user.Id && !u.isDelete))
                    return false;

                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.Role = user.Role;
                existingUser.isActive = user.isActive;
                existingUser.isDelete = user.isDelete;

                if (!string.IsNullOrWhiteSpace(user.Password))
                {
                    existingUser.Password = HashPassword(user.Password);
                }

                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Soft Delete - Sirf isActive aur isDelete update karega
        public bool DeleteUser(int id)
        {
            try
            {
                var user = _context.Users.Find(id);
                if (user == null) return false;

                // Sirf yeh do columns update karenge, actual remove nahi karenge
                user.isActive = false;
                user.isDelete = true;

                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Permanent Delete - Actual database se remove karega
        public bool PermanentDeleteUser(int id)
        {
            try
            {
                var user = _context.Users.Find(id);
                if (user == null) return false;

                _context.Users.Remove(user);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Soft Delete All Active Users
        public bool DeleteAllUsers()
        {
            try
            {
                var activeUsers = _context.Users.Where(u => !u.isDelete).ToList();
                foreach (var user in activeUsers)
                {
                    user.isActive = false;
                    user.isDelete = true;
                }
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Permanent Delete All Users (including soft deleted)
        public bool PermanentDeleteAllUsers()
        {
            try
            {
                _context.Users.RemoveRange(_context.Users);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Restore Soft Deleted User
        public bool RestoreUser(int id)
        {
            try
            {
                var user = _context.Users.Find(id);
                if (user == null) return false;

                user.isActive = true;
                user.isDelete = false;

                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Search only in active users
        public List<User> SearchUsers(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAllUsers();

            return _context.Users
                .Where(u => !u.isDelete && // Only active users
                           (u.UserName.Contains(searchTerm) ||
                            u.Email.Contains(searchTerm) ||
                            u.Role.Contains(searchTerm)))
                .Select(u => new User
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = u.Role,
                    Password = null,
                    isActive = u.isActive,
                    isDelete = u.isDelete
                })
                .ToList();
        }

        // Search in all users (including deleted)
        public List<User> SearchUsersIncludingDeleted(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAllUsersIncludingDeleted();

            return _context.Users
                .Where(u => u.UserName.Contains(searchTerm) ||
                           u.Email.Contains(searchTerm) ||
                           u.Role.Contains(searchTerm))
                .Select(u => new User
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = u.Role,
                    Password = null,
                    isActive = u.isActive,
                    isDelete = u.isDelete
                })
                .ToList();
        }

        public List<User> GetUsersByRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return GetAllUsers();

            return _context.Users
                .Where(u => !u.isDelete && u.Role == role) // Only active users
                .Select(u => new User
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = u.Role,
                    Password = null,
                    isActive = u.isActive,
                    isDelete = u.isDelete
                })
                .ToList();
        }

        public int GetUserCount()
        {
            return _context.Users.Count(u => !u.isDelete); // Sirf active users ki count
        }

        public int GetTotalUserCountIncludingDeleted()
        {
            return _context.Users.Count(); // Sab users ki count
        }

        public bool IsEmailUnique(string email, int excludeId = 0)
        {
            return !_context.Users.Any(u => u.Email == email && u.Id != excludeId && !u.isDelete);
        }

        public bool IsUsernameUnique(string username, int excludeId = 0)
        {
            return !_context.Users.Any(u => u.UserName == username && u.Id != excludeId && !u.isDelete);
        }
    }
}