using Microsoft.AspNetCore.Mvc;

namespace TeamSSHWebService.Controllers
{
    public class HomeController : Controller
    {
        #region Public Methods

        public IActionResult Index()
        {
            return this.View();
        }

        public IActionResult About()
        {
            this.ViewData["Message"] = "Your application description page.";
            return this.View();
        }

        public IActionResult Contact()
        {
            this.ViewData["Message"] = "Your contact page.";
            return this.View();
        }

        public IActionResult Error()
        {
            return this.View();
        }

        #endregion
    }
}
