using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Data;

namespace queueItemStorageSrv
{
    public interface IQueueStore : IService
    {
        Task<List<string>> GetQueueInformation();
        Task<bool> SetQueueInformation(string newMessage);
    }

    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class queueItemStorageSrv : StatefulService, IQueueStore
    {
        private readonly CancellationToken serviceCancellationToken;

        public queueItemStorageSrv(StatefulServiceContext context)
            : base(context)
        { }

        ////ICOMMUNICATIONS LISTENER CODE TO ALLOW DOWNSTREAM COMMUNICATIONS for Kestral
        //public void Abort()
        //{
        //    //throw new NotImplementedException();
        //}

        //public Task CloseAsync(CancellationToken cancellationToken)
        //{
        //    //throw new NotImplementedException();
        //    return null;
        //}

        //public Task<string> OpenAsync(CancellationToken cancellationToken)
        //{
        //    //throw new NotImplementedException();
        //    return null;
        //}

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
               { new ServiceReplicaListener(context => this.CreateServiceRemotingListener(context), "QueueStore", false) };

            
            //kestral implementation 
            //return new ServiceReplicaListener[] {
            //  new ServiceReplicaListener(serviceContext =>
            //    new KestrelCommunicationListener(serviceContext, (url, listener) =>
            //    new WebHostBuilder()
            //        .UseKestrel()
            //        .ConfigureServices(
            //             services => services
            //                 .AddSingleton<StatefulServiceContext>(serviceContext)
            //                 .AddSingleton<IReliableStateManager>(this.StateManager))
            //        .UseContentRoot(Directory.GetCurrentDirectory())
            //        .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
            //        //.UseStartup<Startup>()
            //        .UseUrls(url)
            //        .Build()
            //    ))
            //};
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var myCollection = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, string>>("QueueData");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ServiceEventSource.Current.ServiceMessage(this.Context, "Running {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }        
        }

        /// <summary>
        /// Returns all the stateful items in the queue 
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetQueueInformation()
        {
            CancellationToken token = this.serviceCancellationToken;

            IReliableDictionary<long, string> queueinfo = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, string>>("QueueData");

            List<string> colMessages = new List<string>();
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                IAsyncEnumerable<KeyValuePair<long, string>> enumerable = await queueinfo.CreateEnumerableAsync(tx);
                IAsyncEnumerator<KeyValuePair<long, string>> enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(token))
                {
                    colMessages.Add(enumerator.Current.Value);
                }
            }
            return colMessages;
        }

        /// <summary>
        /// Adds elements to the queuestorage 
        /// </summary>
        /// <param name="newMessage"></param>
        /// <returns></returns>
        public async Task<bool> SetQueueInformation(string newMessage)
        {
            try
            {
                CancellationToken token = this.serviceCancellationToken;

                IReliableDictionary<long, string> queueinfo = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, string>>("QueueData");

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    long newid = Environment.TickCount;
                    if (!await queueinfo.ContainsKeyAsync(tx, newid))
                    {
                        await queueinfo.AddAsync(tx, newid, newMessage);
                    }
                    await tx.CommitAsync();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
