using System.ComponentModel.DataAnnotations;

namespace BlazorEcommerce.Data.Models
{
    public class OrderHeader
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Total")]
        public double OrderTotal { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Name")]
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [Required]
        public string Email { get; set; } = string.Empty;

        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
