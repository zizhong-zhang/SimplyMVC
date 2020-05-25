using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.Extensions.Logging;
using Xero.Authorisation.Integration.Common;
using Action = Xero.Authorisation.Integration.Common.Action;

namespace WebApplication14.Controllers
{
    public class SideBySide : IDoAuth
    {
        private readonly IAuthorisation _auth;

        public SideBySide(IAuthorisation auth)
        {
            _auth = auth;
        }
        public Outcome Assert(Action action)
        {
            return _auth.Assert(action);
        }

        public Task<Outcome> AssertAsync(Action action)
        {
            return _auth.AssertAsync(action);
        }
    }

    public class AuthOnly : IDoAuth
    {
        private readonly IAuthorisation _auth;

        public AuthOnly(IAuthorisation auth)
        {
            _auth = auth;
        }
        public Outcome Assert(Action action)
        {
            return _auth.Assert(action);
        }

        public Task<Outcome> AssertAsync(Action action)
        {
            return _auth.AssertAsync(action);
        }
    }

    public interface IDoAuth
    {
        Outcome Assert(Action action);

        Task<Outcome> AssertAsync(Action action);
    }

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
                _logger.LogInformation("------- running auth only-------------");

                var b = _authOnly.Assert(new Action("invoices", "test"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "------- running auth only-------------");
            }

            try
            {
                _logger.LogInformation("------- running side by side but flag switch off-------------");
                var a = _sideBySide.Assert(new Action("invoices", "test"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "------- running side by side but flag switch off-------------");
            }
            
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("------- running side by side in a task -------------");
                        var a = await _sideBySide.AssertAsync(new Action("bills", "test"));
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "------- running side by side in a task -------------");
                        throw;
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "side by side enabled with task");
            }

            try
            {
                _logger.LogInformation("------- running side by side -------------");
                var a = _sideBySide.Assert(new Action("bills", "test"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "------- running side by side -------------");
            }
            return Content("OK");
        }
    }
}
