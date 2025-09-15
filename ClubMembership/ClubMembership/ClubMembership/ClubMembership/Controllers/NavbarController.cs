using ClubMembership.Data;
using ClubMembership.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ClubMembership.Controllers
{
    public class NavbarController : Controller
    {
        // GET: Navbar
        public ActionResult Navbar(string controller, string action)
        {
            // Decide first to avoid hitting session-dependent menu code for user sidebar
            var isAuthenticated = Request.IsAuthenticated;
            var isAdmin = isAuthenticated && (
                User.IsInRole("Admin") ||
                (Session != null && Session["Group"] != null && Session["Group"].ToString() == "Admin")
            );

            if (isAuthenticated && !isAdmin)
            {
                // Non-admin authenticated users: render sidebar, no need to build menu model
                return PartialView("_sidebar", Enumerable.Empty<MenuNavbar>());
            }

            // Admins and unauthenticated: render existing navbar
            var data = new MenuNavData();
            var navbar = data.itemsPerUser(controller, action, User.Identity.Name);
            return PartialView("_navbar", navbar);
        }
    }
}