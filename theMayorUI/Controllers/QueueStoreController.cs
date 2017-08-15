using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace theMayorUI.Controllers
{
    [Produces("application/json")]
    [Route("api/QueueStore")]
    public class QueueStoreController : Controller
    {
        // GET: api/QueueStore
        [HttpGet]
        public IEnumerable<string> Get()
        {
            var col = GetStatefulCollection();

            return col.Result.ToArray();
        }

        private async Task<List<string>> GetStatefulCollection()
        {

            var queuestore = ServiceProxy.Create<queueItemStorageSrv.IQueueStore>(new Uri("fabric:/queueworkerStateless/queueItemStorageSrv"),
                       new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1));

            return await queuestore.GetQueueInformation();


        }

        // GET: api/QueueStore/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }
        
        // POST: api/QueueStore
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }
        
        // PUT: api/QueueStore/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
