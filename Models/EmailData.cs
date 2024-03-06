using System.ComponentModel.DataAnnotations;

namespace ContactPro.Models
{
    public class EmailData
    {
        [Required]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public int? Id { get; set; }

        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? GroupName { get; set; } = string.Empty;
    }
}
