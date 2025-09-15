using ClubMembership.Models;
using ClubMembership.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;

namespace ClubMembership
{
  
    public class MenuNavData
    {
        private readonly ApplicationDbContext context = new ApplicationDbContext();
        public IEnumerable<MenuNavbar> navbarItems()
        {
            //" + Session["CUSRID"] + "
            //var uname = HttpSessionStateBase["CUSRID"].ToString();
            var amenu = new List<MenuNavbar>();

            //var query = context.Database.SqlQuery<MenuRoleMaster>("selecgit statust * from MenuRoleMaster where Roles='admin'");
            var query = context.Database.SqlQuery<MenuRoleMaster>("select * from MenuRoleMaster where Roles='" + System.Web.HttpContext.Current.Session["CUSRID"].ToString() + "'");
            foreach (var data in query)
            {
                //amenu.Add(new MenuNavbar { MenuGId = Convert.ToInt32(data.MenuGId),
                //                           MenuGIndex = Convert.ToInt32(data.MenuGIndex),
                //                           LinkText  = data.LinkText,
                //                           ActionName = data.ActionName,
                //                           ControllerName = data.ControllerName,
                //                           username = System.Web.HttpContext.Current.Session["CUSRID"].ToString(),// "admin",
                //                           imageClass = "fa fa-fw fa-dashboard", estatus = true });

                amenu.Add(new MenuNavbar { MenuGId = Convert.ToInt32(data.MenuGId),
                                           MenuGIndex = Convert.ToInt32(data.MenuGIndex),
                                           LinkText  = data.LinkText,
                                           ActionName = data.ActionName,
                                           ControllerName = data.ControllerName,
                                           username = System.Web.HttpContext.Current.Session["CUSRID"].ToString(),// "admin",
                                           imageClass = data.ImageClassName, estatus = true });
            }

            //var menu = new List<Navbar>();
            //menu.Add(new Navbar { Id = 1, nameOption = "Dashboard", controller = "Home", action = "Index", imageClass = "fa fa-fw fa-dashboard", estatus = true });
            //menu.Add(new Navbar { Id = 2, nameOption = "Charts", controller = "Home", action = "Charts", imageClass = "fa fa-fw fa-bar-chart-o", estatus = true });
            //menu.Add(new Navbar { Id = 3, nameOption = "Tables", controller = "Home", action = "Tables", imageClass = "fa fa-fw fa-table", estatus = true });
            //menu.Add(new Navbar { Id = 4, nameOption = "Forms", controller = "Home", action = "Forms", imageClass = "fa fa-fw fa-edit", estatus = true });
            //menu.Add(new Navbar { Id = 5, nameOption = "Bootstrap Elements", controller = "Home", action = "BootstrapElements", imageClass = "fa fa-fw fa-desktop", estatus = true });
            //menu.Add(new Navbar { Id = 6, nameOption = "Bootstrap Grid", controller = "Home", action = "BootstrapGrid", imageClass = "fa fa-fw fa-wrench", estatus = true });
            //menu.Add(new Navbar { Id = 7, nameOption = "Blank Page", controller = "Home", action = "BlankPage", imageClass = "fa fa-fw fa-file", estatus = true });

            return amenu.ToList();
        }

        public IEnumerable<User> users()
        {
            //var users = new List<User>();
            //users.Add(new User { Id = 1, user = "admin", password = "12345", estatus = true, RememberMe = true });
            //users.Add(new User { Id = 2, user = "lvasquez", password = "lvasquez", estatus = true, RememberMe = false });
            //users.Add(new User { Id = 3, user = "invite", password = "12345", estatus = false, RememberMe = false });

            var users = new List<User>();

            var query = context.Database.SqlQuery<AspNetUser>("select * from AspNetUsers");
            foreach (var data1 in query)
            {
                users.Add(new User
                {
                    Id = data1.Id,
                    user = data1.UserName,
                    password = data1.PasswordHash,
                    estatus = true,
                    RememberMe = true
                });
            }
            return users.ToList();
        }

        public IEnumerable<Roles> roles()
        {
            var roles = new List<Roles>();
            roles.Add(new Roles { rowid = 1, idUser = 1, idMenu = 1, status = true });
            roles.Add(new Roles { rowid = 2, idUser = 1, idMenu = 2, status = true });
            roles.Add(new Roles { rowid = 3, idUser = 1, idMenu = 3, status = true });
            roles.Add(new Roles { rowid = 4, idUser = 1, idMenu = 4, status = true });
            roles.Add(new Roles { rowid = 5, idUser = 1, idMenu = 5, status = true });
            roles.Add(new Roles { rowid = 6, idUser = 1, idMenu = 6, status = true });
            roles.Add(new Roles { rowid = 7, idUser = 1, idMenu = 7, status = true });
            roles.Add(new Roles { rowid = 8, idUser = 2, idMenu = 1, status = true });
            roles.Add(new Roles { rowid = 9, idUser = 2, idMenu = 2, status = true });
            roles.Add(new Roles { rowid = 10, idUser = 2, idMenu = 3, status = true });
            roles.Add(new Roles { rowid = 11, idUser = 2, idMenu = 4, status = true });
            roles.Add(new Roles { rowid = 12, idUser = 2, idMenu = 5, status = true });
            roles.Add(new Roles { rowid = 13, idUser = 3, idMenu = 1, status = true });
            roles.Add(new Roles { rowid = 14, idUser = 3, idMenu = 2, status = true });

            return roles.ToList();
        }

        public IEnumerable<MenuNavbar> itemsPerUser(string controller, string action, string userName)
        {
            
            IEnumerable<MenuNavbar> items = navbarItems();
            //IEnumerable<Roles> rolesNav = roles();
            IEnumerable<User> usersNav = users();

            var navbar =  items.Where(p => p.ControllerName == controller && p.action == action).Select(c => { c.activeli = "active"; return c; }).ToList();

            //navbar = (from nav in items
            //          join user in usersNav on nav.username equals user.user
            //          where nav.username == userName

            navbar = (from nav in items
                      where nav.username == userName

                      select new MenuNavbar
                      {
                          MenuGId = nav.MenuGId,
                          MenuGIndex = nav.MenuGIndex,
                          LinkText = nav.LinkText,
                          ControllerName = nav.ControllerName,
                          ActionName = nav.ActionName,
                          imageClass = nav.imageClass,
                          estatus = nav.estatus,
                          activeli = nav.activeli
                      }).ToList();

            //navbar = (from nav in items
            //          join rol in rolesNav on nav.MenuGId equals rol.idMenu
            //          join user in usersNav on rol.idUser equals user.Id
            //          where user.user == userName
            //          select new MenuNavbar
            //          {
            //              MenuGId = nav.MenuGId,
            //              MenuGIndex = nav.MenuGIndex,
            //              LinkText = nav.LinkText,
            //              ControllerName = nav.ControllerName,
            //              ActionName = nav.ActionName,
            //              imageClass = nav.imageClass,
            //              estatus = nav.estatus,
            //              activeli = nav.activeli
            //          }).ToList();

            return navbar.ToList();
        }

    }
}