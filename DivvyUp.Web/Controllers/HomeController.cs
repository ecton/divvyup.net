using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace DivvyUp.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }

        public async Task<JsonResult> Workers()
        {
            return new JsonResult(await DivvyUp.Service.AllWorkers());
        }

        public async Task<JsonResult> Queues()
        {
            var queueInfo = new List<object>();
            foreach (var queue in await DivvyUp.Service.AllQueues())
            {
                queueInfo.Add(new
                {
                    name = queue,
                    jobs = await DivvyUp.Service.AllJobsInQueue(queue)
                });

            }

            return new JsonResult(queueInfo);
        }

        public async Task<JsonResult> Failed()
        {
            return new JsonResult(await DivvyUp.Service.AllFailedJobs());
        }
    }
}
