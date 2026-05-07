namespace Inventory.Dtos
{
    public class UserDto
    {
        public int Id { get; set; } 
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }    
        public bool isActive { get; set; }
        public bool isDelete { get; set; }  


    }
}
