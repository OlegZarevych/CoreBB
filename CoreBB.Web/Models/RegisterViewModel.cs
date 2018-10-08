using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CoreBB.Web.Models
{
    public class RegisterViewModel
    {
        [Required, DisplayName("Name")]
        public string Name { get; set; }

        [Required, DisplayName("Password")]
        public string Password { get; set; }

        [Required, DisplayName("Repeat Password")]
        public string RepeatPassword { get; set; }

        [Required, DisplayName("Self-Introduction")]
        public string Description { get; set; }
    }
}
