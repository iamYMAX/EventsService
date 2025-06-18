using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList

namespace EventsService.ViewModels
{
    // ViewModel for an individual user shown in the linking page
    public class UserToLinkViewModel
    {
        public int UserId { get; set; } // AspNetUser Id (User.Id)
        [Display(Name = "Имя пользователя (Email)")]
        public string UserName { get; set; } = string.Empty;
        [Display(Name = "Имя")]
        public string? FirstName { get; set; }
        [Display(Name = "Фамилия")]
        public string? LastName { get; set; }
        public bool IsAlreadyLinkedToClient { get; set; } // True if User.Client navigation property is not null or Client.UserId points to this user
        public int? LinkedClientId { get; set; } // If linked, the ID of the Client record
        public string? LinkedClientName { get; set; } // If linked, the Name of the Client record
    }

    // ViewModel for an individual client profile shown in the linking page
    public class ClientToLinkViewModel
    {
        public int ClientId { get; set; } // Client.Id
        [Display(Name = "Имя клиента")]
        public string ClientName { get; set; } = string.Empty;
        public bool IsAlreadyLinkedToUser { get; set; } // True if Client.UserId is not null
        public int? LinkedUserId { get; set; } // If linked, the ID of the User record
        public string? LinkedUserName { get; set; } // If linked, the UserName of the User
    }

    // Main ViewModel for the UserClientLinking/Index.cshtml page
    public class UserClientLinkingIndexViewModel
    {
        [Display(Name = "Пользователи с ролью 'Клиент' (еще не связаны с профилем клиента)")]
        public List<UserToLinkViewModel> UnlinkedClientRoleUsers { get; set; } = new List<UserToLinkViewModel>();

        [Display(Name = "Профили клиентов (еще не связаны с пользователем)")]
        public List<ClientToLinkViewModel> UnlinkedClientProfiles { get; set; } = new List<ClientToLinkViewModel>();

        [Display(Name = "Связанные пары Пользователь-Клиент")]
        public List<UserToLinkViewModel> LinkedUserClientPairs { get; set; } = new List<UserToLinkViewModel>(); // Using UserToLinkViewModel as it has client info

        // For forms: selecting a user to link
        [Display(Name = "Выберите пользователя (с ролью 'Клиент')")]
        public int? SelectedUserIdToLink { get; set; }
        public SelectList? AvailableUnlinkedUsers { get; set; }

        // For forms: selecting a client profile to link to the selected user
        [Display(Name = "Выберите существующий профиль клиента")]
        public int? SelectedClientIdToLink { get; set; }
        public SelectList? AvailableUnlinkedClients { get; set; }

        // For forms: creating a new client profile for the selected user
        [Display(Name = "Или создайте новый профиль клиента для выбранного пользователя:")]
        public bool CreateNewClientProfile { get; set; } = false; // Checkbox

        [StringLength(100)]
        [Display(Name = "Имя нового клиента")]
        public string? NewClientName { get; set; } // Required if CreateNewClientProfile is true

        [StringLength(20)]
        [Display(Name = "Телефон нового клиента")]
        public string? NewClientPhoneNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Организация нового клиента")]
        public string? NewClientOrganization { get; set; }

        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email нового клиента")]
        public string? NewClientEmail { get; set; }
    }
}
