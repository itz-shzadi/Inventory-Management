using Inventory.Models;
using System.Collections.Generic;

namespace Inventory.Data.IServices
{
    public interface IUserService
    {
        List<User> GetAllUsers();
        List<User> GetAllUsersIncludingDeleted();
        List<User> GetDeletedUsers();
        User? GetUserById(int id);
        bool CreateUser(User user);
        bool UpdateUser(User user);
        bool DeleteUser(int id);  // Soft delete
        bool PermanentDeleteUser(int id);  // Hard delete
        bool DeleteAllUsers();  // Soft delete all
        bool PermanentDeleteAllUsers();  // Hard delete all
        bool RestoreUser(int id);  // Restore soft deleted user
        List<User> SearchUsers(string searchTerm);
        List<User> SearchUsersIncludingDeleted(string searchTerm);
        List<User> GetUsersByRole(string role);
        int GetUserCount();
        int GetTotalUserCountIncludingDeleted();
        bool IsEmailUnique(string email, int excludeId = 0);
        bool IsUsernameUnique(string username, int excludeId = 0);
    }
}