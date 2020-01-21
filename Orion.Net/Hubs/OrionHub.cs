﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Orion.Net.Core.Scripts;

namespace Orion.Net.Hubs
{
    public class OrionHub : Hub
    {
        #region New connections

        /// <summary>
        /// Called by clients in order to notify server that the server is connected ;)
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="supportId"></param>
        /// <param name="clientLabel"></param>
        /// <returns></returns>
        public async Task Hello(string appId, string supportId, string clientLabel)
        {
            //Add Connection app to AppGroup named appId in case of reconnection
            await Groups.AddToGroupAsync(Context.ConnectionId, appId);

            //Add Connection app to SupportGroup named support Id
            await Groups.AddToGroupAsync(Context.ConnectionId, supportId);

            //Send to group supportGroup so clients in it too, specify only support ?
            await Clients.OthersInGroup(supportId).SendAsync("NewClient", new
            {
                UserName = clientLabel,
                AppId = appId
            });
        }

        /// <summary>
        /// Create SupportId groupe with support connectionId
        /// </summary>
        /// <param name="supportId"></param>
        /// <returns></returns>
        public async Task StartSupportGroupe(string supportId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, supportId);
        }

        #endregion

        #region Discuss with client for available commands

        /// <summary>
        /// Send a command to a dedicated client
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="commandTitle"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task SendCommandToClient(ExecuteScriptCommand scriptCommand)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, scriptCommand.ConnectionId);
            await Clients.Group(scriptCommand.ConnectionId).SendAsync("ExecuteCommand", scriptCommand.CommandTitle, scriptCommand.CommandParam);
        }

        /// <summary>
        /// Send an ask command to a dedicated client
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public async Task AskCommands(string appId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, appId);
            await Clients.OthersInGroup(appId).SendAsync("AskCommands");
        }

        /// <summary>
        /// Called by clients in order to notify server that the server is connected ;)
        /// </summary>
        /// <param name="clientLabel"></param>
        /// <returns></returns>
        public async Task ClientAnswerCommands(string appId, List<AvailableClientScript> availableScripts)
        {
            await Clients.OthersInGroup(appId).SendAsync("AnswerCommands", appId, availableScripts);
        }

        /// <summary>
        /// Called by clients in order to notify server of the result
        /// </summary>
        /// <param name="resultIdentifier"></param>
        /// <returns></returns>
        public async Task ResultCommandSent(string appId, Guid resultIdentifier, int resultType)
        {
            await Clients.OthersInGroup(appId).SendAsync("ResultSent", appId, resultIdentifier, resultType);
        }

        #endregion
    }
}