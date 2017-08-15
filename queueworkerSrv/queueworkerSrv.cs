using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace queueworkerSrv
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class queueworkerSrv : StatelessService
    {
        public queueworkerSrv(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            long iterations = 0;

            //connection string for the queue
            var connectionString = "Endpoint=sb://themayor.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YlKnifGR3vDgAiK52OSs9rXt2PEenoksE/I+gViwJ1E=";
            var queueName = "hackprocessdemo";

            var client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            string nodeip = this.Context.NodeContext.IPAddressOrFQDN;
            string nodename = this.Context.NodeContext.NodeName;

            //using an event message listener 
            client.OnMessage(message =>
            {
                var bodyText = message.GetBody<string>();
                var msgId = message.MessageId;
                
                bodyText = "SF: " + nodeip + " - Nodename: " + nodename + " --- " + bodyText;
                var queuestore = ServiceProxy.Create<queueItemStorageSrv.IQueueStore>(new Uri("fabric:/queueworkerStateless/queueItemStorageSrv"),
                    new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1));
                var successful = queuestore.SetQueueInformation(bodyText);
                if (successful.Result)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "{0} added message to storage", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            });


            while (true)
            {

                cancellationToken.ThrowIfCancellationRequested();
                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0} iteration-{1}", nodename, iterations);
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }
}
