using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace ClubMembership
{
    public class SessionExpire : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpContext.Current.Session["Group"] == null || HttpContext.Current.Session["CUSRID"] == null || HttpContext.Current.Session["compyid"] == null)
            {
                //                FormsAuthentication.SignOut();
                HttpContext.Current.GetOwinContext().Authentication.SignOut(Microsoft.AspNet.Identity.DefaultAuthenticationTypes.ApplicationCookie);
                filterContext.Result =
                new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "action", "Login" },
                    { "controller", "Account" },
                    { "returnUrl", filterContext.HttpContext.Request.RawUrl}
                });
                return;
            }
        }
    }
}