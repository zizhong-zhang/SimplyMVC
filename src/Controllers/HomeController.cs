using System.Net.Http;
using System.Web.Mvc;

namespace WebApplication14.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            using (var client = new HttpClient())
            {
                return Content("OK");
            }
        }
    }
}
