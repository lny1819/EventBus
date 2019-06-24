using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using YiDian.EventBus;

namespace YiDian.EventManager.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        static Dictionary<string, AppMetas> dic = new Dictionary<string, AppMetas>();
        // GET api/values
        [HttpPost]
        public ActionResult<CheckResult> Reg(string app, string version, [FromBody]ClassMeta meta)
        {
            if (!dic.ContainsKey(app))
            {
                dic[app] = new AppMetas
                {
                    Name = app,
                    Version = version
                };
            }
            dic[app].MetaInfos.Add(meta);
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
            return dic[app];
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
