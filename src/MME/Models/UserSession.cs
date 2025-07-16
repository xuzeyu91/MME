using System.ComponentModel.DataAnnotations;

namespace MME.Models
{
    public class UserSession
    {
        public string UserName { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public DateTime LoginTime { get; set; }
        public List<object> Menus { get; set; } = new List<object>();
    }
} 