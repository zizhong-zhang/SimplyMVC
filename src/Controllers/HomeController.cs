using System;
using System.Web.Mvc;
using Microsoft.Extensions.Logging;
using Xero.Authorisation.Integration.Common;
using Action = Xero.Authorisation.Integration.Common.Action;

namespace WebApplication14.Controllers
{
    public class HomeController : Controller
    {
        private readonly SideBySide _sideBySide;
        private readonly AuthOnly _authOnly;
        private readonly ILogger<Authorisation> _logger;

        public HomeController(SideBySide sideBySide, AuthOnly authOnly, ILogger<Authorisation> logger)
        {
            _sideBySide = sideBySide;
            _authOnly = authOnly;
            _logger = logger;
        }
        public ActionResult Index()
        {
            try
            {
                var result = _authOnly.Assert(new Action("bills", "test"));
                result = _sideBySide.Assert(new Action("bills", "test"));
                //result = _sideBySide.Assert(new Action("bills", "test"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "------- running auth only-------------");
            }

            return Content("OK");
        }
    }
}
