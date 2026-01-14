using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace alicedress.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Адрес")]
        public string? Address { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}