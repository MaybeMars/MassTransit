// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Transports.Msmq.Tests.TestFixtures
{
	using System;
	using EndpointConfigurators;
	using Internal;
	using MassTransit.Tests.TextFixtures;
	using Rhino.Mocks;
	using Services.Subscriptions;

	public class MsmqEndpointTestFixture :
		EndpointTestFixture<MsmqTransportFactory>
	{
		protected Uri LocalEndpointUri { get; set; }
		protected Uri RemoteEndpointUri { get; set; }

		private ISubscriptionService SubscriptionService { get; set; }

		protected IServiceBus LocalBus { get; set; }
		protected IServiceBus RemoteBus { get; set; }

	    public MsmqEndpointTestFixture()
		{
			EndpointConfiguratorImpl.Defaults(x =>
			{
				x.CreateMissingQueues = true;
				x.CreateTransactionalQueues = false;
				x.PurgeOnStartup = true;
			});

			LocalEndpointUri = new Uri("msmq://localhost/mt_client");
			RemoteEndpointUri = new Uri("msmq://localhost/mt_server");
		}

		protected override void EstablishContext()
		{
			base.EstablishContext();

			LocalEndpoint = EndpointCache.GetEndpoint(LocalEndpointUri);
			RemoteEndpoint = EndpointCache.GetEndpoint(RemoteEndpointUri);

			SetupSubscriptionService();

			LocalBus = Configuration.ServiceBusConfigurator.New(x =>
				{
					x.AddService<SubscriptionPublisher>();
					x.AddService<SubscriptionConsumer>();
					x.ReceiveFrom(LocalEndpointUri);
				});

			RemoteBus = Configuration.ServiceBusConfigurator.New(x =>
				{
					x.AddService<SubscriptionPublisher>();
					x.AddService<SubscriptionConsumer>();
					x.ReceiveFrom(RemoteEndpointUri);
				});
		}

		protected void Purge(IEndpointAddress address)
		{
			var management = MsmqEndpointManagement.New(address.Uri);
			management.Purge();
		}

		private void SetupSubscriptionService()
		{
			SubscriptionService = new LocalSubscriptionService();
			ObjectBuilder.Stub(x => x.GetInstance<IEndpointSubscriptionEvent>())
				.Return(SubscriptionService);

			ObjectBuilder.Stub(x => x.GetInstance<SubscriptionPublisher>())
				.Return(null)
				.WhenCalled(invocation =>
					{
						// Return a unique instance of this class
						invocation.ReturnValue = new SubscriptionPublisher(SubscriptionService);
					});

			ObjectBuilder.Stub(x => x.GetInstance<SubscriptionConsumer>())
				.Return(null)
				.WhenCalled(invocation =>
					{
						// Return a unique instance of this class
						invocation.ReturnValue = new SubscriptionConsumer(SubscriptionService);
					});
		}


		public IEndpoint LocalEndpoint { get; private set; }
		public IEndpoint RemoteEndpoint { get; private set; }

		protected override void TeardownContext()
		{
			LocalBus.Dispose();
			LocalBus = null;

			RemoteBus.Dispose();
			RemoteBus = null;

			LocalEndpoint = null;

			base.TeardownContext();
		}
	}
}