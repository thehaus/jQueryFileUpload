using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Http;

namespace GHUploader
{
    public class Global : HttpApplication
    {
        protected void Application_BeginRequest()
        {
            if (Request.Headers.AllKeys.Contains("Origin", StringComparer.OrdinalIgnoreCase) &&
                Request.HttpMethod == "OPTIONS")
            {
                Response.Flush();
            }
        }

        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);            
        }
    }
}