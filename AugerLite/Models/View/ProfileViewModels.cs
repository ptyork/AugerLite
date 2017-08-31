using System.ComponentModel.DataAnnotations;

namespace Auger.Models.View
{
    public class UserProfileModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }


        [Display(Name = "Email address"), DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Display(Name = "Preferred Theme")]
        public string Theme { get; set; }

        public UserProfileModel()
        {
        }

        public UserProfileModel(ApplicationUser user)
        {
            UserId = user.Id;
            Email = user.Email;
            FirstName = user.FirstName;
            LastName = user.LastName;
            UserName = user.UserName;
            Theme = user.Theme;
        }
    }
}