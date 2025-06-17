using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsService.Models
{
    public class ChecklistItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChecklistId { get; set; }
        [ForeignKey("ChecklistId")]
        public virtual Checklist? Checklist { get; set; }

        [Required]
        [StringLength(200)]
        public string TaskDescription { get; set; } = string.Empty;

        public bool IsCompleted { get; set; } = false;
    }
}
