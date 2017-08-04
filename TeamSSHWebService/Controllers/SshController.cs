using Microsoft.AspNetCore.Mvc;
using TeamSSHWebService.Models;

namespace TeamSSHWebService.Controllers
{
    [Route("api/[controller]/[action]")]
    public class SshController : Controller
    {
        #region Public Methods

        [HttpPost]
        public IActionResult Register([FromBody] SshRegisterModel model)
        {
            return this.Ok(1);
        }

        #endregion
    }
}
