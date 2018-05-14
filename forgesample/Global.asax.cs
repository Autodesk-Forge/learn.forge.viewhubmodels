using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using System.Web.SessionState;

namespace forgesample
{
  public class WebApiApplication : System.Web.HttpApplication
  {
    protected void Application_Start()
    {
      GlobalConfiguration.Configure(WebApiConfig.Register);
    }

    // Enable session on WebAPI app
    // https://stackoverflow.com/a/17539008/4838205
    protected void Application_PostAuthorizeRequest()
    {
      HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
    }
    private bool IsWebApiRequest()
    {
      return HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith("~/api");
    }
  }
}
