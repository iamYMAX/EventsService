using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Имя пользователя обязательно.")]
        [Display(Name = "Имя пользователя")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен.")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя обязательно для заполнения.")]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна для заполнения.")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Должность обязательна для заполнения.")]
        [Display(Name = "Должность")]
        public string Position { get; set; } = string.Empty;

        // For managing roles - this is a common way to handle multi-select or checkbox list
        [Display(Name = "Роли")]
        public List<int> SelectedRoleIds { get; set; } = new List<int>();
        public MultiSelectList? AllRoles { get; set; } // All available roles to select from
        public IList<string>? CurrentRoles { get; set; } // Names of currently assigned roles for display

        // Optional: For password change, but often handled in a separate "ChangePassword" view
        // [StringLength(100, ErrorMessage = "{0} должен содержать как минимум {2} и максимум {1} символов.", MinimumLength = 6)]
        // [DataType(DataType.Password)]
        // [Display(Name = "Новый пароль (оставьте пустым, если не меняете)")]
        // public string? NewPassword { get; set; }

        // [DataType(DataType.Password)]
        // [Display(Name = "Подтверждение нового пароля")]
        // [Compare("NewPassword", ErrorMessage = "Новый пароль и подтверждение не совпадают.")]
        // public string? ConfirmNewPassword { get; set; }
    }
}
