using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using YiDian.EventBus;

namespace YiDian.EventManager.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        // GET api/values
        [HttpPost]
        public ActionResult<CheckResult> Reg(string app, string version, [FromBody]ClassMeta meta)
        {
            return new CheckResult();
        }
        [HttpGet]
        public ActionResult<CheckResult> Check(string app, string version)
        {
            return new CheckResult();
        }
        [HttpGet]
        public ActionResult<string> Version(string app)
        {
            return "1.0";
        }
        [HttpGet]
        public ActionResult<AppMetas> ListEvent(string app)
        {
            var appmeta = new AppMetas() { Name = "quote", Version = "1.0" };
            var meta = new ClassMeta()
            {
                Name = "CA"
            };
            meta.Properties.Add(new PropertyMetaInfo() { Name = "p1", Type = PropertyMetaInfo.P_String });
            appmeta.MetaInfos.Add(meta);
            return new JsonResult(appmeta);
        }
        [HttpGet]
        public string EventId(string app, string name)
        {
            return "a";
        }
        [HttpGet]
        public ActionResult<List<EventId>> AllIds(string app, string name)
        {
            return new JsonResult(new List<EventId>());
        }
    }
}
