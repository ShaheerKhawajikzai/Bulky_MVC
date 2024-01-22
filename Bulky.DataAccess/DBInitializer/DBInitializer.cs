using Bulky.DataAccess.Data;
using Bulky.Models.Models;
using Bulky.Utitlity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.DBInitializer
{
    public class DBInitializer : IDBInitializer
    {
        private readonly RoleManager<IdentityRole> _roleManger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;

        public DBInitializer(RoleManager<IdentityRole> roleManager,
               UserManager<IdentityUser> userManager, ApplicationDbContext applicationDbContext)
        {
            _roleManger = roleManager;
            _userManager = userManager;
            _db = applicationDbContext;
        }
        public void Initialize()
        {
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }

            catch (Exception ex) { }

            //create role if they are not created.
            if (!_roleManger.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                _roleManger.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManger.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManger.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManger.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();

                //if roles are not created, then we will create admin user as well

                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "shaheersk12@gmail.com",
                    Email = "shaheersk12@gmail.com",
                    Name = "Shaheer Khan",
                    PhoneNumber = "1234567890",
                    StreetAddress = "Mohalla Hafiz Sammandar Khan,Street Khawjikzai",
                    State = "Pakistan",
                    PostalCode = "1234567890",
                    City = "Dera Ismail Khan"

                }, "Shaheer123!").GetAwaiter().GetResult();

                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(x => x.Email == "shaheersk12@gmail.com");
                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();

            }
            return;
        }
    }
}
