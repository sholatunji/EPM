using EPM.Mouser.Interview.Models;
using Microsoft.AspNetCore.Mvc;

namespace EPM.Mouser.Interview.Web.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> IndexAsync()
        {
            var wr = new Data.WarehouseRepository();
            IEnumerable<Product> products = await wr.List();
            return View(products);
        }

        public async Task<IActionResult> View(long id)
        {
            if (id == 0)
                return NotFound();

            var wr = new Data.WarehouseRepository();
            var product = await wr.Get(id);
            if (product == null)
                return NotFound();
            return View(product);
        }

    }
}
