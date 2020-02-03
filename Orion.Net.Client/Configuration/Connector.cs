﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Orion.Net.Client.Scripts;
using Orion.Net.Core.Interfaces;
using Orion.Net.Core.Scripts;

namespace Orion.Net.Client.Configuration
{
    /// <summary>
    /// Client Connector to the <see cref="OrionHub"/>
    /// </summary>
    public class Connector : IAsyncDisposable
    {
        /// <summary>
        /// Client Connection to the hub
        /// </summary>
        private HubConnection hubConnection;
        /// <summary>
        /// Path to the Hub
        /// </summary>
        private string platformUri;
        /// <summary>
        /// List of <see cref="BaseClientScript"/>, each one corresponding to a executable command
        /// </summary>
        /// <remarks><see cref="commands"/> is empty by default, to add command, the Client App calls <see cref="AddCommandService{T}(T)"/></remarks>
        private readonly List<BaseClientScript> commands = new List<BaseClientScript>();
        /// <summary>
        /// Identifier of the Client Application
        /// </summary>
        /// <remarks>Use for connection purpose on the Hub</remarks>
        private readonly string appId;

        /// <summary>
        /// Constructor with instantiation of the GUID of <see cref="appId"/>
        /// </summary>
        public Connector() { 
            appId = Guid.NewGuid().ToString(); 
        }

        /// <summary>
        /// Dispose the connection to the Hub
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            if (hubConnection != null)
                await hubConnection.DisposeAsync();
        }

        /// <summary>
        /// Add a CommandService to the Client App
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        public void AddCommandService<T>(T command) where T : BaseClientScript
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            commands.Add(command);
        }

        /// <summary>
        /// <para>Connect to the server : On and Invoke 
        /// <list type="bullet">
        /// <item>On.AskCommands() : InvokeAsync.ClientAnswerCommands to send back <see cref="commands"/></item>
        /// <item>On.ExecuteCommand (string,string)</item>
        /// <item>On.StartAsync()</item>
        /// <item>InvokeAsync("Hello", appId, supportID, environmentLabel)</item>
        /// </list></para>
        /// Do not forget to call AddCommandService<>() to register IClientScript class 
        /// </summary>
        /// <param name="platformUri"></param>
        /// <param name="environmentLabel"></param>
        /// <param name="supportID"></param>
        /// <returns><see cref="commands"/> when the the Hub send "AskCommands"</returns>
        public async Task Connect(string platformUri, string environmentLabel, string supportID)
        {
            this.platformUri = platformUri.EndsWith("/") ? platformUri : platformUri + "/";

            hubConnection = new HubConnectionBuilder()
                                        .WithUrl(platformUri + "orionhub")
                                        .WithAutomaticReconnect()
                                        .Build();

            hubConnection.On("AskCommands", async () =>
            {
                // Ask client for available commands :
                await hubConnection.InvokeAsync("ClientAnswerCommands", appId, commands
                    .Select(e => new AvailableClientScript()
                    {
                        Title = e.Title,
                        Identifier = e.Identifier
                    }).ToList());
            });

            hubConnection.On<string, string>("ExecuteCommand", async (commandTitle, parameters) =>
            {
                // Ask client for available commands :
                var command = GetCommand(commandTitle);
                await command.Start(parameters);
            });

            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync("Hello", appId, supportID, environmentLabel);

        }

        /// <summary>
        /// Return a command from title
        /// </summary>
        /// <param name="commandTitle"></param>
        /// <returns></returns>
        private BaseClientScript GetCommand(string commandTitle)
        {
            return commands.FirstOrDefault(e => e.Title == commandTitle);
        }

        /// <summary>
        /// Send a result object to the platform and call hub to force refresh
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        internal async Task SendResultCommand<T>(T result) where T : ClientScriptResult
        {
            // Send result object to the correct uri :
            var dataUri = string.Empty;
            HttpContent content = null;

            switch (result.ResultType)
            {
                case ClientScriptResultType.ConsoleLog:
                    dataUri = platformUri + "api/v1/StringResultData";
                    content = new StringContent(JsonConvert.SerializeObject(result), Encoding.UTF8, "application/json");
                    break;
                case ClientScriptResultType.Image:
                    dataUri = platformUri + "api/v1/ImageResultData";
                    content = new StringContent(JsonConvert.SerializeObject(result), Encoding.UTF8, "application/json");
                    break;
                case ClientScriptResultType.File:
                    dataUri = platformUri + "api/v1/FileResultData";
                    content = new StringContent(JsonConvert.SerializeObject(result), Encoding.UTF8, "application/json");
                    break;
                default:
                    return;
            }

            using (var client = new HttpClient())
            {
                await client.PostAsync(dataUri, content);
            }

            // Notify server client that a result has been sent :
            await hubConnection.InvokeAsync("ResultCommandSent", appId, result.ResultIdentifier, result.ResultType);
        }
    }
}