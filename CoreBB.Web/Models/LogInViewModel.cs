using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CoreBB.Web.Models
{
    public class LogInViewModel
    {
        [Required, DisplayName("Name")]
        public string Name { get; set; }

        [Required, DisplayName("Password")]
        public string Password { get; set; }
    }
}
