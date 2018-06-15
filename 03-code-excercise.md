
##### In this lab, you will create and host multiple child bots orchestrated by a master bot on top of Azure Service Fabric for OneBank Corp. Ltd. These child bots will serve different domains of a banking sector. First, you will create and host a MasterBot which helps in forwarding the incoming request to multiple child bots, then you will develop business specific child bots such as AccountsBot for Account Management and InsuranceBot to Buy Insurance.  

## Excercise 1 : Developing & Hosting Master, Accounts and Insurance Bots

*Since every service inside Azure Service Fabric is a console application, and Bot projects are meant to run on a Web based application. You will first have to prepare the bot projects to run on a Http endpoint. We will achieve this by Self hosting the Web API using OWIN.*

**Task I : Add OWIN Communication Listener**

1. In Visual Studio Solution explorer, locate the `OneBank.Common` project and create a new C# class by right-clicking on the project.
2. Name this class as `OwinCommunicationListener` and replace it with following
    ~~~csharp
    namespace OneBank.Common
    {
        using System;
        using System.Fabric;
        using System.Threading;
        using System.Threading.Tasks;
        using Autofac;
        using Microsoft.Owin.Hosting;
        using Microsoft.ServiceFabric.Services.Communication.Runtime;
        using Owin;

        public class OwinCommunicationListener : ICommunicationListener
        {
            private readonly string endpointName;

            private readonly ServiceContext serviceContext;

            private readonly Action<IAppBuilder> startup;

            private string listeningAddress;

            private string publishAddress;

            private IDisposable webAppHandle;

            public OwinCommunicationListener(Action<IAppBuilder> startup, ServiceContext serviceContext, string endpointName)
            {
                this.startup = startup ?? throw new ArgumentNullException(nameof(startup));
                this.serviceContext = serviceContext ?? throw new ArgumentNullException(nameof(serviceContext));
                this.endpointName = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            }

            public void Abort()
            {
                this.StopHosting();
            }

            public Task CloseAsync(CancellationToken cancellationToken)
            {
                this.StopHosting();
                return Task.FromResult(true);
            }

            public Task<string> OpenAsync(CancellationToken cancellationToken)
            {
                string ipAddress = FabricRuntime.GetNodeContext().IPAddressOrFQDN;
                var serviceEndpoint = this.serviceContext.CodePackageActivationContext.GetEndpoint(this.endpointName);
                var protocol = serviceEndpoint.Protocol;
                int port = serviceEndpoint.Port;

                if (this.serviceContext is StatefulServiceContext)
                {
                    StatefulServiceContext statefulServiceContext = this.serviceContext as StatefulServiceContext;
                    this.listeningAddress = $"{protocol}://+:{port}/{statefulServiceContext.PartitionId}/{statefulServiceContext.ReplicaId}/{Guid.NewGuid()}";
                }
                else if (this.serviceContext is StatelessServiceContext)
                {
                    this.listeningAddress = $"{protocol}://+:{port}";
                }
                else
                {
                    throw new InvalidOperationException();
                }

                this.publishAddress = this.listeningAddress.Replace("+", ipAddress);

                try
                {
                    this.webAppHandle = WebApp.Start(this.listeningAddress, appBuilder => this.startup.Invoke(appBuilder));
                    return Task.FromResult(this.publishAddress);
                }
                catch (Exception)
                {
                    this.StopHosting();
                    throw;
                }
            }

            private void StopHosting()
            {
                if (this.webAppHandle != null)
                {
                    try
                    {
                        this.webAppHandle.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }
    }
    ~~~

3. In Visual Studio Solution explorer, locate the `OneBank.MasterBot` project and double click on `MasterBot.cs` file.
4. Find the method `CreateServiceInstanceListeners`, and replace the definition with following code.
    ~~~csharp
    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
    {
        var endpoints = this.Context.CodePackageActivationContext.GetEndpoints()
                                .Where(endpoint => endpoint.Protocol == EndpointProtocol.Http || endpoint.Protocol == EndpointProtocol.Https)
                                .Select(endpoint => endpoint.Name);

        return endpoints.Select(endpoint => new ServiceInstanceListener(
            context => new OwinCommunicationListener(Startup.ConfigureApp, this.Context, endpoint), endpoint));
    }
    ~~~

5. In `OneBank.MasterBot` project, locate the `ServiceManifest.xml` file and add an HTTP endpoint inside the `<Endpoints>` element 
    ~~~xml
    <Resources>
        <Endpoints>
        <Endpoint Name="ServiceEndpoint" Type="Input" Protocol="http" Port="8770" />
        </Endpoints>
    </Resources>
    ~~~

    > Notice the `Type` and `Port` of the Master bot endpoint. These values should be different for all child bots as shown in the next step

6. Similarly, locate the `OneBank.AccountsBot`project and double click on AccountsBot.cs file.
7. Find the method `CreateServiceInstanceListeners`, and replace the definition with following code.
    ~~~csharp
    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
    {
        var endpoints = this.Context.CodePackageActivationContext.GetEndpoints()
                                .Where(endpoint => endpoint.Protocol == EndpointProtocol.Http || endpoint.Protocol == EndpointProtocol.Https)
                                .Select(endpoint => endpoint.Name);

        return endpoints.Select(endpoint => new ServiceInstanceListener(
            context => new OwinCommunicationListener(Startup.ConfigureApp, this.Context, endpoint), endpoint));
    }
    ~~~

8. In `OneBank.AccountsBot` project, locate the `ServiceManifest.xml` file and add an HTTP endpoint inside the `<Endpoints>` element
    ~~~xml
    <Resources>
        <Endpoints>
        <Endpoint Name="ServiceEndpoint" Type="Internal" Protocol="http" Port="8771" />
        </Endpoints>
    </Resources>
    ~~~

9. Again, you would do the same for the InsuranceBot by replacing the definition for `CreateServiceInstanceListeners` with following code
    ~~~csharp
    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
    {
        var endpoints = this.Context.CodePackageActivationContext.GetEndpoints()
                                .Where(endpoint => endpoint.Protocol == EndpointProtocol.Http || endpoint.Protocol == EndpointProtocol.Https)
                                .Select(endpoint => endpoint.Name);

        return endpoints.Select(endpoint => new ServiceInstanceListener(
            context => new OwinCommunicationListener(Startup.ConfigureApp, this.Context, endpoint), endpoint));
    }
    ~~~
  
10. And then, add an endpoint in the ServiceManifest.xml file under `<Endpoints>` element
    ~~~xml
    <Resources>
        <Endpoints>
        <Endpoint Name="ServiceEndpoint" Type="Internal" Protocol="http" Port="8772" />
        </Endpoints>
    </Resources>
    ~~~
    > Http port for the AccountsBot & InsuranceBot must be different than MasterBot. Also the `Type` should also be `Internal` so that you don't expose the child bots directly outside of the Service Fabric cluster. Only MasterBot should be exposed to a publicily accessible endpoint.

**Task II : Create a basic master root dialog.**

1. In `OneBank.MasterBot` project, locate the `Dialogs` folder, and add a new C# class.
2. Name this class as `MasterRootDialog` and replace the existing code with below class.
    ~~~csharp
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace OneBank.MasterBot.Dialogs
    {
        [Serializable]
        public class MasterRootDialog : IDialog<object>
        {
            public Task StartAsync(IDialogContext context)
            {
                context.Wait(this.MessageReceivedAsync);
                return Task.CompletedTask;
            }

            public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
            {
                await context.PostAsync("Hello there! Welcome to OneBank.");
                await context.PostAsync("I am the Master bot");

                PromptDialog.Choice(context, ResumeAfterChoiceSelection, new List<string>() { "Account Management", "Buy Insurance" }, "What would you like to do today?");           
            }

            private async Task ResumeAfterChoiceSelection(IDialogContext context, IAwaitable<string> result)
            {
                var choice = await result;

                if (choice.Equals("Account Management", StringComparison.OrdinalIgnoreCase))
                {
                    await context.PostAsync("Forward me to AccountsBot");
                }
                else if (choice.Equals("Buy Insurance", StringComparison.OrdinalIgnoreCase))
                {
                    await context.PostAsync("Forward me to InsuranceBot");
                }
                else
                {
                    context.Done(1);
                }
            }
        }
    }
    ~~~

**Task III: Observe the application by running it.**

1. On top of the Visual Studio, click on `Start` button to run the application.
    >Please make sure that `OneBank.FabricApp` is set as your StartUp project. As you are running the application for the first time, it may take a couple of minutes to boot up the cluster.

    ![startApp]

2. A pop-up may appear to seek permission to `Refresh Application` on the cluster. Click `Yes` as shown in the screenshot

    ![refreshApp]

3. On the bottom right of the desktop, click on `^` icon, then look for service fabric icon, right-click on it choose the first option `Manage Local Cluster`

    ![openLocalCluster]

4. Service Fabric explorer will appear in internet explorer. Compare the state of application as shown in the screenshot below

    ![localClusterState]

5. Navigate to Desktop, and double click on Bot Framework Emulator

    ![startBotEmulator]

6. Set the URL of the MasterBot in the Address bar. The URL must be `http://localhost:8770/api/messages`. And then click `Connect` as shown in the screenshot below. As soon as you click it, you would see a few log traces at the bottom right of the screen. If the response code is 202, then everything has been configured properly so far.

    ![setBotUrl]

7. In the extreme bottom of the Emulator, under **Type your message** pane, Type `Hi` and wait for the response. That's how it should ideally look like

    ![sayHi]

## Excercise 2 : Forward incoming requests from Master Bot to Child bots

*There are different ways to forward the incoming request from Master bot to Child bots. For example:- Bot Framework's Direct Line API is one of them. But if we use this, all the forwarded requests will flow over the internet. To overcome this issue, you will be using the Http communication client of Service Fabric so that all the traffic flows within the cluster.* 

**Task I: Create and Register Http Communication Client** 

1. In `OneBank.Common` project, create a new C# class by the name of `HttpCommunicationClient` and replace it with the following
    ~~~csharp
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    namespace OneBank.Common
    {
        public class HttpCommunicationClient : ICommunicationClient
        {
            public HttpCommunicationClient(HttpClient httpClient)
            {
                this.HttpClient = httpClient;
            }

            public HttpClient HttpClient { get; private set; }

            public ResolvedServicePartition ResolvedServicePartition { get; set; }

            public string ListenerName { get; set; }

            public ResolvedServiceEndpoint Endpoint { get; set; }

            public string HttpEndPoint
            {
                get
                {
                    JObject addresses = JObject.Parse(this.Endpoint.Address);
                    return (string)addresses["Endpoints"].First();
                }
            }
        }
    }
    ~~~

2. In `OneBank.Common` project, create a new empty C# interface by the name of `IHttpCommunicationClientFactory`
    ~~~csharp
    namespace Gorenje.DA.Fabric.Communication.HttpCommunication
    {
        using Microsoft.ServiceFabric.Services.Communication.Client;

        public interface IHttpCommunicationClientFactory : ICommunicationClientFactory<HttpCommunicationClient>
        {
        }
    }
    ~~~

3. Add another class in `OneBank.Common` project and name it as HttpCommunicationClientFactory
    ~~~csharp
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    namespace OneBank.Common
    {
        [Serializable]
        public class HttpCommunicationClientFactory : CommunicationClientFactoryBase<HttpCommunicationClient>, IHttpCommunicationClientFactory
        {
            private readonly HttpClient httpClient;

            public HttpCommunicationClientFactory(HttpClient httpClient)
            {
                this.httpClient = httpClient;
            }

            protected override void AbortClient(HttpCommunicationClient client)
            {
            }

            protected override Task<HttpCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpCommunicationClient(this.httpClient));
            }

            protected override bool ValidateClient(HttpCommunicationClient client)
            {
                return true;
            }

            protected override bool ValidateClient(string endpoint, HttpCommunicationClient client)
            {
                return true;
            }
        }
    }

    ~~~

4. In `OneBank.MasterBot` project, locate the Startup.cs class and register the `HttpCommunicationClientFactory` created in the previous step
    ~~~csharp
    Conversation.UpdateContainer(
                    builder =>
                    {
                        builder.Register(c => new HttpCommunicationClientFactory(new HttpClient()))
                        .As<IHttpCommunicationClientFactory>().SingleInstance();
                    });

    config.DependencyResolver = new AutofacWebApiDependencyResolver(Conversation.Container);
    ~~~

5. That's how your Startup.cs class of MasterBot should look like
    ~~~csharp
    public static class Startup
    {
        /// <summary>
        /// The ConfigureApp
        /// </summary>
        /// <param name="appBuilder">The <see cref="IAppBuilder" /></param>
        public static void ConfigureApp(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });
            
            config.Filters.Add(new HandleExceptionAttribute());
            appBuilder.UseWebApi(config);

            // New Addition
            Conversation.UpdateContainer(
                builder =>
                {
                    builder.Register(c => new HttpCommunicationClientFactory(new HttpClient()))
                     .As<IHttpCommunicationClientFactory>().SingleInstance();
                });

            config.DependencyResolver = new AutofacWebApiDependencyResolver(Conversation.Container);
            // New Addition
        }
    ~~~

**Task II: Modify `MasterRootDialog` class created in `Excercise 1`**

1. In `OneBank.MasterBot` project, find the `MasterRootDialog` class and append new method called `ForwardToChildBot` as shown below
    ~~~csharp
    public async Task<HttpResponseMessage> ForwardToChildBot(string serviceName, string path, object model, IDictionary<string, string> headers = null)
    {
        var clientFactory = Conversation.Container.Resolve<IHttpCommunicationClientFactory>();
        var client = new ServicePartitionClient<HttpCommunicationClient>(clientFactory, new Uri(serviceName));

        HttpResponseMessage response = null;

        await client.InvokeWithRetry(async x =>
        {
            var targetRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"),
                RequestUri = new Uri($"{x.HttpEndPoint}/{path}")
            };

            if (headers != null)
            {
                foreach (var key in headers.Keys)
                {
                    targetRequest.Headers.Add(key, headers[key]);
                }
            }

            response = await x.HttpClient.SendAsync(targetRequest);
        });

        return response;
    }
    ~~~

2. Locate the `MessageReceivedAsync` method in the `MasterRootDialog` class and replace the following line 
    ~~~csharp
    await context.PostAsync("Forward me to AccountsBot"); 
    ~~~

    with

    ~~~csharp
    await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.AccountsBot", "api/messages", context.Activity);
    ~~~

3. Similary, replace the following line
    ~~~csharp
    await context.PostAsync("Forward me to InsuranceBot"); 
    ~~~

    with

    ~~~csharp
    await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.InsuranceBot", "api/messages", context.Activity);
    ~~~

4. That's how your `MasterRootDialog` should look like now
    ~~~csharp
    [Serializable]
    public class MasterRootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(this.MessageReceivedAsync);
            return Task.CompletedTask;
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            if (currentBotCtx == "Accounts")
            {
                await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.AccountsBot", "api/messages", context.Activity);
            }
            else if (currentBotCtx == "Insurance")
            {
                await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.InsuranceBot", "api/messages", context.Activity);
            }
            else
            {
                await context.PostAsync("Hello there! Welcome to OneBank.");
                await context.PostAsync("I am the Master bot");

                PromptDialog.Choice(context, ResumeAfterChoiceSelection, new List<string>() { "Account Management", "Buy Insurance" }, "What would you like to do today?");
            }                      
        }

        private async Task ResumeAfterChoiceSelection(IDialogContext context, IAwaitable<string> result)
        {
            var choice = await result;

            if (choice.Equals("Account Management", StringComparison.OrdinalIgnoreCase))
            {
                await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.AccountsBot", "api/messages", context.Activity, headers);
            }
            else if (choice.Equals("Buy Insurance", StringComparison.OrdinalIgnoreCase))
            {
                await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.InsuranceBot", "api/messages", context.Activity, headers);
            }
            else
            {
                context.Done(1);
            }
        }

        public async Task<HttpResponseMessage> ForwardToChildBot(string serviceName, string path, object model, IDictionary<string, string> headers = null)
        {
            var clientFactory = Conversation.Container.Resolve<IHttpCommunicationClientFactory>();
            var client = new ServicePartitionClient<HttpCommunicationClient>(clientFactory, new Uri(serviceName));

            HttpResponseMessage response = null;

            await client.InvokeWithRetry(async x =>
            {
                var targetRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"),
                    RequestUri = new Uri($"{x.HttpEndPoint}/{path}")
                };

                targetRequest.Headers.Add("X-My-Operation-Id", OperationContext.Get().OperationId);

                if (headers != null)
                {
                    foreach (var key in headers.Keys)
                    {
                        targetRequest.Headers.Add(key, headers[key]);
                    }
                }

                response = await x.HttpClient.SendAsync(targetRequest);
            });

            string s = await response.Content.ReadAsStringAsync();
            return response;
        }
    }
    ~~~
**Task III: Create and regsiter `AccountsEchoDialog` in AccountsBot**

1. In `OneBank.AccountsBot` project, locate the `Dialogs` folder, and add a new C# class.
2. Name this class as `AccountsEchoDialog` and replace the existing code with below class.
    ~~~csharp
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace OneBank.AccountsBot.Dialogs
    {
        [Serializable]
        public class AccountsEchoDialog
        {
            private int count = 1;

            public async Task StartAsync(IDialogContext context)
            {
                await Task.CompletedTask;
                context.Wait(this.MessageReceivedAsync);
            }

            public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
            {
                var message = await argument;

                await context.PostAsync($"[From AccountsBot] - You said {message.Text} - Count {count++}");
                context.Wait(this.MessageReceivedAsync);
            }
        }
    }
    ~~~

3. Locate the `AccountsBotController` under `Controllers` folder and add the following line inside if condition of the Post method
    ~~~csharp
    await Conversation.SendAsync(activity, () => new AccountsEchoDialog());
    ~~~

4. That's how your Post method should look
    ~~~csharp
    [HttpPost]
        [Route("")]
        public async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                // New Addition
                await Conversation.SendAsync(activity, () => new AccountsEchoDialog());
                // New Addition
            }
            else
            {
                this.HandleSystemMessage(activity);
            }

            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    ~~~

**Task IV: Create and register `InsuranceEchoDialog` in InsuranceBot**

1. In `OneBank.InsuranceBot` project, locate the `Dialogs` folder, and add a new C# class.
2. Name this class as `InsuranceEchoDialog` and replace the existing code with below class.
    ~~~csharp
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace OneBank.AccountsBot.Dialogs
    {
        [Serializable]
        public class InsuranceEchoDialog
        {
            private int count = 1;

            public async Task StartAsync(IDialogContext context)
            {
                await Task.CompletedTask;
                context.Wait(this.MessageReceivedAsync);
            }

            public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
            {
                var message = await argument;

                await context.PostAsync($"[From InsuranceBot] - You said {message.Text} - Count {count++}");
                context.Wait(this.MessageReceivedAsync);
            }
        }
    }
    ~~~

3. Locate the InsuranceBotController and add the following line inside if condition of the Post method
    ~~~csharp
    await Conversation.SendAsync(activity, () => new InsuranceEchoDialog());
    ~~~

4. That's how your Post method should look
    ~~~csharp
    [HttpPost]
    [Route("")]
    public async Task<HttpResponseMessage> Post([FromBody] Activity activity)
    {
        if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
        {
            // New Addition
            await Conversation.SendAsync(activity, () => new InsuranceEchoDialog());
            // New Addition
        }
        else
        {
            this.HandleSystemMessage(activity);
        }

        return new HttpResponseMessage(HttpStatusCode.Accepted);
    }
    ~~~

**Task V: Let's re-run the bot again and see what happens this time**

1. Looks like master bot was able to forward the request to the AccountsBot, but something down the line is failing and due to this we get an error back. This happens due to the absense of Bot State in all 3 bots. So, let's move on to next exercise to develop the bot state.
    ![botStateError]

## Excercise 3 : Service Fabric Bot State

*Although, there are several in-built options to configure the bot state such as ImMemoryBotDataStore, TableStorageBotDataStore etc. But since we are running the Bots on Service Fabric we could potentially use the Reliable collections of Service fabric to persist the state.
And for this, you will be leveraging the Actor programming model of Azure Service Fabric.*

**Task I: Create Stateful Reliable Actors** 

1. In `OneBank.BotStateActor.Interface`, locate the `IBotStateActor` interface and add the following four methods
    ~~~csharp
    Task<BotStateContext> GetBotStateAsync(string key, CancellationToken cancellationToken);

    Task<BotStateContext> SaveBotStateAsync(string key, BotStateContext dialogState, CancellationToken cancellationToken);

    Task InsertBotStateAsync(string key, BotStateContext dialogState, CancellationToken cancellationToken);

    Task<bool> DeleteBotStateAsync(string key, CancellationToken cancellationToken);
    ~~~

2. In `OneBank.BotStateActor.Interface`, create a new model class by the name of BotStateContext to store the BotData.
    ~~~csharp
    using System;

    namespace OneBank.BotStateActor
    {
        [Serializable]
        public class BotStateContext
        {
            public string BotId { get; set; }

            public string UserId { get; set; }

            public string ChannelId { get; set; }

            public string ConversationId { get; set; }

            public DateTime TimeStamp { get; set; }

            public byte[] Data { get; set; }

            public string ETag { get; set; }
        }
    }
    ~~~

3. In `OneBank.BotStateActor` project, find the BotStateActor.cs class and replace the existing code with following code.
    ~~~csharp
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Actors.Client;
    using OneBank.BotStateActor.Interfaces;
    using Microsoft.ServiceFabric.Data;
    using OneBank.BotStateActor;

    namespace BotStateActor
    {
        [StatePersistence(StatePersistence.Persisted)]
        internal class BotStateActor : Actor, IBotStateActor
        {
            public BotStateActor(ActorService actorService, ActorId actorId)
                : base(actorService, actorId)
            {
            }

            public async Task<BotStateContext> GetBotStateAsync(string key, CancellationToken cancellationToken)
            {
                ActorEventSource.Current.ActorMessage(this, $"Getting bot state from actor key - {key}");
                ConditionalValue<BotStateContext> result = await this.StateManager.TryGetStateAsync<BotStateContext>(key, cancellationToken);

                if (result.HasValue)
                {
                    return result.Value;
                }
                else
                {
                    return null;
                }
            }

            public async Task<BotStateContext> SaveBotStateAsync(string key, BotStateContext dialogState, CancellationToken cancellationToken)
            {
                ActorEventSource.Current.ActorMessage(this, $"Adding bot state for actor key - {key}");
                return await this.StateManager.AddOrUpdateStateAsync(
                    key,
                    dialogState,
                    (k, v) =>
                        {
                            return dialogState.ETag != "*" && dialogState.ETag != v.ETag ? throw new Exception() : v = dialogState;
                        },
                    cancellationToken);
            }

            public async Task InsertBotStateAsync(string key, BotStateContext dialogState, CancellationToken cancellationToken)
            {
                ActorEventSource.Current.ActorMessage(this, $"Inserting bot state for actor key - {key}");
                await this.StateManager.AddStateAsync(key, dialogState, cancellationToken);
            }

            public async Task<bool> DeleteBotStateAsync(string key, CancellationToken cancellationToken)
            {
                ActorEventSource.Current.ActorMessage(this, $"Deleting bot state for actor key - {key}");
                return await this.StateManager.TryRemoveStateAsync(key, cancellationToken);
            }

            protected override Task OnActivateAsync()
            {
                ActorEventSource.Current.ActorMessage(this, "Actor activated.");
                return Task.FromResult(0);
            }       
        }
    }
    ~~~

4. In `OneBank.Common` project create a new class by the name of ServiceFabricBotDataStore and replace the exisitng code with following
    ~~~csharp
    namespace OneBank.Common
    {
        using System;
        using System.IO;
        using System.IO.Compression;
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Bot.Builder.Dialogs;
        using Microsoft.Bot.Builder.Dialogs.Internals;
        using Microsoft.Bot.Connector;
        using Microsoft.ServiceFabric.Actors;
        using Microsoft.ServiceFabric.Actors.Client;
        using Newtonsoft.Json;
        using OneBank.BotStateActor;
        using OneBank.BotStateActor.Interfaces;
        
        public class ServiceFabricBotDataStore : IBotDataStore<BotData>
        {
            private static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };

            private readonly string botName;

            public ServiceFabricBotDataStore(string botName)
            {
                this.botName = botName;
            }

            public async Task<bool> FlushAsync(IAddress key, CancellationToken cancellationToken)
            {
                return await Task.FromResult(true);
            }

            public async Task<BotData> LoadAsync(IAddress key, BotStoreType botStoreType, CancellationToken cancellationToken)
            {
                var botStateActor = this.GetActorInstance(key.UserId, key.ChannelId);
                BotStateContext botStateContext = await botStateActor.GetBotStateAsync(this.GetStateKey(key, botStoreType), cancellationToken);

                if (botStateContext != null)
                {
                    return new BotData(botStateContext.ETag, Deserialize(botStateContext.Data));
                }
                else
                {
                    return new BotData(string.Empty, null);
                }
            }

            public async Task SaveAsync(IAddress key, BotStoreType botStoreType, BotData data, CancellationToken cancellationToken)
            {
                var stateKey = this.GetStateKey(key, botStoreType);

                BotStateContext botStateContext = new BotStateContext
                {
                    BotId = key.BotId,
                    ChannelId = key.ChannelId,
                    ConversationId = key.ConversationId,
                    UserId = key.UserId,
                    Data = Serialize(data.Data),
                    ETag = data.ETag,
                    TimeStamp = DateTime.UtcNow
                };

                var botStateActor = this.GetActorInstance(key.UserId, key.ChannelId);

                if (string.IsNullOrEmpty(botStateContext.ETag))
                {
                    botStateContext.ETag = Guid.NewGuid().ToString();
                    await botStateActor.SaveBotStateAsync(stateKey, botStateContext, cancellationToken);
                }
                else if (botStateContext.ETag == "*")
                {
                    if (botStateContext.Data != null)
                    {
                        await botStateActor.SaveBotStateAsync(stateKey, botStateContext, cancellationToken);
                    }
                    else
                    {
                        await botStateActor.DeleteBotStateAsync(stateKey, cancellationToken);
                    }
                }
                else
                {
                    if (botStateContext.Data != null)
                    {
                        await botStateActor.SaveBotStateAsync(stateKey, botStateContext, cancellationToken);
                    }
                    else
                    {
                        await botStateActor.DeleteBotStateAsync(stateKey, cancellationToken);
                    }
                }
            }

            private static byte[] Serialize(object data)
            {
                using (var cmpStream = new MemoryStream())
                using (var stream = new GZipStream(cmpStream, CompressionMode.Compress))
                using (var streamWriter = new StreamWriter(stream))
                {
                    var serializedJSon = JsonConvert.SerializeObject(data, SerializationSettings);
                    streamWriter.Write(serializedJSon);
                    streamWriter.Close();
                    stream.Close();
                    return cmpStream.ToArray();
                }
            }

            private static object Deserialize(byte[] bytes)
            {
                using (var stream = new MemoryStream(bytes))
                using (var gz = new GZipStream(stream, CompressionMode.Decompress))
                using (var streamReader = new StreamReader(gz))
                {
                    return JsonConvert.DeserializeObject(streamReader.ReadToEnd());
                }
            }

            private IBotStateActor GetActorInstance(string userId, string channelId)
            {
                return ActorProxy.Create<IBotStateActor>(new ActorId($"{userId}-{channelId}"), new Uri("fabric:/OneBank.FabricApp/BotStateActorService"));
            }

            private string GetStateKey(IAddress key, BotStoreType botStoreType)
            {
                switch (botStoreType)
                {
                    case BotStoreType.BotConversationData:
                        return $"{this.botName}:{key.ChannelId}:conversation:{key.ConversationId}";

                    case BotStoreType.BotUserData:
                        return $"{this.botName}:{key.ChannelId}:user:{key.ConversationId}";

                    case BotStoreType.BotPrivateConversationData:
                        return $"{this.botName}:{key.ChannelId}:private:{key.ConversationId}:{key.UserId}";

                    default:
                        throw new ArgumentException("Unsupported bot store type!");
                }
            }
        }
    }
    ~~~

5. In `OneBank.MasterBot`, locate the Startup.cs file, under Conversation.UpdateContainer section, add the following code
    ~~~csharp
    var store = new ServiceFabricBotDataStore("Master");
                        builder.Register(c => new CachingBotDataStore(store, CachingBotDataStoreConsistencyPolicy.LastWriteWins))
                            .As<IBotDataStore<BotData>>()
                            .AsSelf()
                            .InstancePerLifetimeScope();
    ~~~ 

6. That's how your Startup class of MasterBot should look like.
    ~~~csharp
    public static class Startup
    {
        public static void ConfigureApp(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });

            config.Filters.Add(new HandleExceptionAttribute());
            appBuilder.UseWebApi(config);

            Conversation.UpdateContainer(
                builder =>
                {
                    // New Addition
                    var store = new ServiceFabricBotDataStore("Master");
                    builder.Register(c => new CachingBotDataStore(store, CachingBotDataStoreConsistencyPolicy.LastWriteWins))
                        .As<IBotDataStore<BotData>>()
                        .AsSelf()
                        .InstancePerLifetimeScope();
                    // New Addition
                    builder.Register(c => new HttpCommunicationClientFactory(new HttpClient()))
                     .As<IHttpCommunicationClientFactory>().SingleInstance();
                });

            config.DependencyResolver = new AutofacWebApiDependencyResolver(Conversation.Container);
        }
    }
    ~~~

7. In `OneBank.AccountsBot`, locate the Startup.cs file, under Conversation.UpdateContainer section, add the following code
    ~~~csharp
    Conversation.UpdateContainer(
                    builder =>
                    {
                        var store = new ServiceFabricBotDataStore("Accounts");
                        builder.Register(c => new CachingBotDataStore(store, CachingBotDataStoreConsistencyPolicy.LastWriteWins))
                            .As<IBotDataStore<BotData>>()
                            .AsSelf()
                            .InstancePerLifetimeScope();
                    });

    config.DependencyResolver = new AutofacWebApiDependencyResolver(Conversation.Container);
    ~~~
    > The value in the constructor of `ServiceFabricBotDataStore` must be different for all bots. 

8. In `OneBank.InsuranceBot`, locate the Startup.cs file, under Conversation.UpdateContainer section, add the following code
    ~~~csharp
    Conversation.UpdateContainer(
                    builder =>
                    {
                        var store = new ServiceFabricBotDataStore("Insurance");
                        builder.Register(c => new CachingBotDataStore(store, CachingBotDataStoreConsistencyPolicy.LastWriteWins))
                            .As<IBotDataStore<BotData>>()
                            .AsSelf()
                            .InstancePerLifetimeScope();
                    });

    config.DependencyResolver = new AutofacWebApiDependencyResolver(Conversation.Container);
    ~~~
    > The value in the constructor of `ServiceFabricBotDataStore` must be different for all bots.

**Task II: Modify the `MasterRootDialog` class in `OneBank.MasterBot` to persist the selection made by the user in the first prompt. We should do this to maintain the sticky session between the end-user and the child bot so that all subsequent requests directly goes to child bot without performing any redirection logic again on master bot.**

1. In `MasterRootDialog`, locate `ResumeAfterChoiceSelection` method and replace the exisitng definitation with the following code 
    ~~~csharp
    private async Task ResumeAfterChoiceSelection(IDialogContext context, IAwaitable<string> result)
    {
        var choice = await result;

        if (choice.Equals("Account Management", StringComparison.OrdinalIgnoreCase))
        {
            var botDataStore = Conversation.Container.Resolve<IBotDataStore<BotData>>();
            var key = Address.FromActivity(context.Activity);
            var conversationData = await botDataStore.LoadAsync(key, BotStoreType.BotConversationData, CancellationToken.None);
            conversationData.SetProperty<string>("CurrentBotContext", "Accounts");
            await botDataStore.SaveAsync(key, BotStoreType.BotConversationData, conversationData, CancellationToken.None);

            await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.AccountsBot", "api/messages", context.Activity);
        }
        else if (choice.Equals("Buy Insurance", StringComparison.OrdinalIgnoreCase))
        {
            var botDataStore = Conversation.Container.Resolve<IBotDataStore<BotData>>();
            var key = Address.FromActivity(context.Activity);
            var conversationData = await botDataStore.LoadAsync(key, BotStoreType.BotConversationData, CancellationToken.None);
            conversationData.SetProperty<string>("CurrentBotContext", "Insurance");
            await botDataStore.SaveAsync(key, BotStoreType.BotConversationData, conversationData, CancellationToken.None);

            await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.InsuranceBot", "api/messages", context.Activity);
        }
        else
        {
            context.Done(1);
        }
    }
    ~~~

2. In `MasterRootDialog`, locate `ResumeAfterChoiceSelection` method and replace the exisitng definitation with the following code 
    ~~~csharp
    public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
    {
        var currentBotCtx = context.ConversationData.GetValueOrDefault<string>("CurrentBotContext");

        if (currentBotCtx == "Accounts")
        {
            await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.AccountsBot", "api/messages", context.Activity);
        }
        else if (currentBotCtx == "Insurance")
        {
            await ForwardToChildBot("fabric:/OneBank.FabricApp/OneBank.InsuranceBot", "api/messages", context.Activity);
        }
        else
        {
            await context.PostAsync("Hello there! Welcome to OneBank.");
            await context.PostAsync("I am the Master bot");

            PromptDialog.Choice(context, ResumeAfterChoiceSelection, new List<string>() { "Account Management", "Buy Insurance" }, "What would you like to do today?");
        }                      
    }
    ~~~

**Task III: Run the bot again and observe the differences as compared to the result of Excercise 2**

1. This time the Master bot has forwarded the request to the Accounts bot without any errors.
    ![botStateSuccess]

2. You will notice that our newly created Service Fabric based Bot State is working and emitting logs
    ![botStateActorEvents]

3. Since, we made a few changes to the `MasterRootDialog` to persist the session affinity between the user and the child bot. We may now also do a small test to see the results for the same. 
    - In the extreme bottom, inside **Type your message** pane, Type `Hi` and wait for the response.   
    - Once the response appears, Select `Account Management` from the Hero card and wait for the response from the AccountsBot
    - Now once again type something in the text box and observe the response. You should notice that the response is coming from the Accounts Bot instead of Master Bot.

    ![stickyChildBots]

## Excersice 4 : Enable Authentication

**Task I: Modify StartUp.cs class of MasterBot and replace the MicrosoftAppId and MicrosoftAppPassword with the actual value.**

1. Collect the value of MicrosoftAppId and MicrosoftAppPassword from Azure portal (As shown in Lab 1)

2. Place the value of MicrosoftAppId and MicrosoftAppPassword in the double quotes below and paste the code snippet inside the Startup class of `MasterBot` 
~~~csharp
config.Filters.Add(new BotAuthentication() { MicrosoftAppId = "", MicrosoftAppPassword = "" });
            var microsoftAppCredentials = Conversation.Container.Resolve<MicrosoftAppCredentials>();
            microsoftAppCredentials.MicrosoftAppId = "";
            microsoftAppCredentials.MicrosoftAppPassword = "";
~~~

3. Place the same values you retrieved in step 1 and paste the code snippet inside the Startup class of `AccountsBot`
~~~csharp
config.Filters.Add(new BotAuthentication() { MicrosoftAppId = "", MicrosoftAppPassword = "" });
            var microsoftAppCredentials = Conversation.Container.Resolve<MicrosoftAppCredentials>();
            microsoftAppCredentials.MicrosoftAppId = "";
            microsoftAppCredentials.MicrosoftAppPassword = "";
~~~

3. Similarly, place the same values you retrieved in step 1 and paste the code snippet inside the Startup class of `InsuranceBot`

~~~csharp
config.Filters.Add(new BotAuthentication() { MicrosoftAppId = "", MicrosoftAppPassword = "" });
            var microsoftAppCredentials = Conversation.Container.Resolve<MicrosoftAppCredentials>();
            microsoftAppCredentials.MicrosoftAppId = "";
            microsoftAppCredentials.MicrosoftAppPassword = "";
~~~

**Task II: Setup Call Context**

*Apart from setting up the AppId and Password, you will also have to pass the Authorization token from MasterBot to ChildBot. In order to do this, we have to catch the Authorization header from the Request object, persist it for the duration of that call and pass it on to the child bot while forwarding the request.*

1. In `OneBank.Common` project, create a new C# class by right-clicking on the project
2. Name this class as `RequestCallContext` and replace the existing code with the following.
    ~~~csharp
    namespace OneBank.Common
    {
        using System.Collections.Concurrent;
        using System.Threading;

        public class RequestCallContext
        {
            public static AsyncLocal<string> AuthToken { get; set; } = new AsyncLocal<string>();
        }
    }
    ~~~

3. In `OneBank.MasterBot` project, under Controllers folder, double click on MasterBotController class, and add as a first line in the POST method. 

    ~~~csharp
    RequestCallContext.AuthToken.Value = $"Bearer {this.Request.Headers.Authorization.Parameter}";
    ~~~

4. That's how your MasterBotController's POST method should look

    ~~~csharp
        [HttpPost]
        [Route("")]
        public async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // New Addition
            RequestCallContext.AuthToken.Value = $"Bearer {this.Request.Headers.Authorization.Parameter}";
            // New Addition

            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new MasterRootDialog());
            }
            else
            {
                this.HandleSystemMessage(activity);
            }

            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    ~~~

5.  In `MasterBotRootDialog`, look for `ForwardToChildBot` method and add the below line anywhere before sending the http request.
    ~~~csharp
    targetRequest.Headers.Add("Authorization", RequestCallContext.AuthToken.Value);
    ~~~

6. That's how your `ForwardToChildBot` method in `MasterRootDialog` should look like at the end.
    ~~~csharp
    public async Task<HttpResponseMessage> ForwardToChildBot(string serviceName, string path, object model, IDictionary<string, string> headers = null)
        {
            var clientFactory = Conversation.Container.Resolve<IHttpCommunicationClientFactory>();
            var client = new ServicePartitionClient<HttpCommunicationClient>(clientFactory, new Uri(serviceName));

            HttpResponseMessage response = null;

            await client.InvokeWithRetry(async x =>
            {
                var targetRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"),
                    RequestUri = new Uri($"{x.HttpEndPoint}/{path}")
                };

                // New Addition
                targetRequest.Headers.Add("Authorization", RequestCallContext.AuthToken.Value);
                // New Addition

                if (headers != null)
                {
                    foreach (var key in headers.Keys)
                    {
                        targetRequest.Headers.Add(key, headers[key]);
                    }
                }

                response = await x.HttpClient.SendAsync(targetRequest);
            });

            string s = await response.Content.ReadAsStringAsync();
            return response;
        }
    ~~~

**Task III: Observe the changes**

1. First, run the application without specifying the MicrosoftAppId and MicrosoftAppPassword. You would see a 401 responce at the bottom right of the emulator as shown in the screenshot below.
![botAuthenticationError]

2. Now, Specify the MicrosoftAppId and MicrosoftPassword and click connect as shown in the screenshot below. This time it should be working absolutely fine.
![botAuthenticationPassed]

### Excercise 5 : Logging

**Task I:** Setup Application Insights 
1. Install Nuget package `ApplicationInsights.OwinExtensions`
![nuGetForSolution]
![aiNugetPackage]

2. In all three Startup.cs files across the solution, add the below code snippet as the first step
    ~~~csharp
    TelemetryConfiguration.Active.InstrumentationKey = "<GET_THE_KEY_FROM_AZURE_PORTAL>";
    TelemetryConfiguration.Active.TelemetryInitializers.Add(new OperationIdTelemetryInitializer());
    appBuilder.UseApplicationInsights(null, new OperationIdContextMiddlewareConfiguration { OperationIdFactory = IdFactory.FromHeader("X-My-Operation-Id") });
    ~~~

3. In `MasterBotRootDialog`, look for `ForwardToChildBot` method and add the below line anywhere before sending the http request.
    ~~~csharp
    targetRequest.Headers.Add("X-My-Operation-Id", OperationContext.Get().OperationId);
    ~~~

4.  That's how your `ForwardToChildBot` method in `MasterRootDialog` should look like at the end.
    ~~~csharp
    public async Task<HttpResponseMessage> ForwardToChildBot(string serviceName, string path, object model, IDictionary<string, string> headers = null)
        {
            var clientFactory = Conversation.Container.Resolve<IHttpCommunicationClientFactory>();
            var client = new ServicePartitionClient<HttpCommunicationClient>(clientFactory, new Uri(serviceName));

            HttpResponseMessage response = null;

            await client.InvokeWithRetry(async x =>
            {
                var targetRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"),
                    RequestUri = new Uri($"{x.HttpEndPoint}/{path}")
                };

                targetRequest.Headers.Add("Authorization", RequestCallContext.AuthToken.Value);

                // New Addition
                targetRequest.Headers.Add("X-My-Operation-Id", OperationContext.Get().OperationId);
                // New Addition

                if (headers != null)
                {
                    foreach (var key in headers.Keys)
                    {
                        targetRequest.Headers.Add(key, headers[key]);
                    }
                }

                response = await x.HttpClient.SendAsync(targetRequest);
            });

            string s = await response.Content.ReadAsStringAsync();
            return response;
        }
    ~~~

5. Run the bot again and send a few messages. Wait for a few seconds and check the Request being tracked automatically in the Azure Portal as shown below
![aiAzurePortal]

[startApp]: https://asfabricstorage.blob.core.windows.net:443/images/19.png
[refreshApp]: https://asfabricstorage.blob.core.windows.net:443/images/18.png
[startBotEmulator]: https://asfabricstorage.blob.core.windows.net:443/images/20.png
[setBotUrl]: https://asfabricstorage.blob.core.windows.net:443/images/21.png
[sayHi]: https://asfabricstorage.blob.core.windows.net:443/images/22.png
[botStateError]: https://asfabricstorage.blob.core.windows.net:443/images/23.png
[botStateSuccess]: https://asfabricstorage.blob.core.windows.net:443/images/24.png
[botStateActorEvents]: https://asfabricstorage.blob.core.windows.net:443/images/25.png
[stickyChildBots]: https://asfabricstorage.blob.core.windows.net:443/images/26.png
[botAuthenticationError]: https://asfabricstorage.blob.core.windows.net:443/images/27.png
[botAuthenticationPassed]: https://asfabricstorage.blob.core.windows.net:443/images/28.png
[nuGetForSolution]: https://asfabricstorage.blob.core.windows.net:443/images/29.png
[aiNugetPackage]: https://asfabricstorage.blob.core.windows.net:443/images/31.png
[aiAzurePortal]: https://asfabricstorage.blob.core.windows.net:443/images/32.png
[openLocalCluster]: https://asfabricstorage.blob.core.windows.net:443/images/33.png
[localClusterState]: https://asfabricstorage.blob.core.windows.net:443/images/34.png
