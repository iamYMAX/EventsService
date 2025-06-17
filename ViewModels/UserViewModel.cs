using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Имя пользователя")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Роли")]
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
