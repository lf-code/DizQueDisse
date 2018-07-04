using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DizQueDisse.Models
{
    public class MyUser : IdentityUser
    {
        //[Key]
        //new public string UserName { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "UserName")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

    }

    public class LoginAzure
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
