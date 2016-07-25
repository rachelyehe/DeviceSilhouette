﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using DeviceRepository.Interfaces;
using DeviceRichState;
using Microsoft.ServiceFabric.Data;
using StorageProviderService;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using CommonUtils;

namespace DeviceRepository
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class DeviceRepositoryActor : Actor, IDeviceRepositoryActor
    {
        //private const string StateName = "silhouetteMessages";
        private const string StateName = "silhouetteMessage";
        private readonly IStorageProviderRemoting StorageProviderServiceClient = ServiceProxy.Create<IStorageProviderRemoting>(new Uri("fabric:/StateManagementService/StorageProviderService"));
        private readonly MessagePurger _messagePurger;
        private readonly double _messagesRetentionMilliseconds;

        private IActorTimer _purgeTimer;

        public DeviceRepositoryActor(double messagesRetentionMilliseconds)
        {
            _messagesRetentionMilliseconds = messagesRetentionMilliseconds;
            _messagePurger = new MessagePurger(messagesRetentionMilliseconds);
        }

        private async Task PurgeStates(object arg)
        {
            var stateMessages = await StateManager.TryGetStateAsync<List<DeviceState>>(StateName);
            if (stateMessages.HasValue)
            {
                var messages = stateMessages.Value;
                var indexOfLastPurgeableMessage = _messagePurger.GetIndexOfLastPurgeableMessage(messages);
                messages.RemoveRange(0, indexOfLastPurgeableMessage + 1);
            }
        }

        public Task<string> GetDeviceStatus()
        {
            var status = StateManager.TryGetStateAsync<string>("deviceStatus").Result;
            if (status.HasValue)
                return Task.FromResult(status.Value);
            else
                return Task.FromResult(String.Empty);
        }

        public Task SetDeviceStatus(string status)
        {
            StateManager.SetStateAsync<string>("deviceStatus", status);
            return Task.FromResult(true);
        }

        public async Task<DeviceState> GetLastKnownReportedStateAsync()
        {
            DeviceState state = null;
            // search in silhouetteMessages
            var stateMessages = await GetDeviceStateMessagesAsync();
            if (stateMessages != null)
            {
                state = stateMessages.OrderByDescending(m => m.Timestamp)
                                                    .Where(m => m.MessageType == MessageType.Report)
                                                    .FirstOrDefault();
            }

            return state;
        }

        public async Task<DeviceState> GetLastKnownRequestedStateAsync()
        {
            DeviceState state = null;
            // search in silhouetteMessages
            var stateMessages = await GetDeviceStateMessagesAsync();
            if (stateMessages != null)
            {
                state = stateMessages.OrderByDescending(m => m.Timestamp)
                                        .Where(m => m.MessageType == MessageType.CommandRequest && m.MessageSubType == MessageSubType.New)
                                        .FirstOrDefault();
            }

            return state;
        }



        public async Task<DeviceState> GetDeviceStateAsync()
        {
            var stateMessage = await StateManager.TryGetStateAsync<DeviceState>(StateName);

            if (stateMessage.HasValue)
                return stateMessage.Value;
            else
                return null;
        }

        public async Task<List<DeviceState>> GetDeviceStateMessagesAsync()
        {
            var stateMessages = await StateManager.TryGetStateAsync<List<DeviceState>>(StateName);
            if (stateMessages.HasValue)
                return stateMessages.Value;
            else
                return null;
        }

        public async Task<DeviceState> GetMessageByVersionAsync(int version)
        {
            var messages = await GetDeviceStateMessagesAsync();
            if (messages == null)
            {
                return null;
            }
            return messages.FirstOrDefault(m => m.Version == version);

        }
        public async Task<MessageList> GetMessagesAsync(int pageSize, int? continuation)
        {
            var messages = await GetDeviceStateMessagesAsync();
            if (messages == null)
            {
                return null;
            }
            var messagesToReturn = messages
                                    .OrderBy(m => m.Timestamp)
                                    .SkipUntil(m => m.Version == continuation || continuation == null)
                                    .Take(pageSize + 1) // take one extra to get the next continuation token!
                                    .ToList();

            int? newContinuation = null;
            if (messagesToReturn.Count > pageSize)
            {
                newContinuation = messagesToReturn[pageSize].Version; // set next continuation value to be the Version of the next item in the list
                messagesToReturn.RemoveAt(pageSize);
            }
            return new MessageList
            {
                Messages = messagesToReturn,
                Continuation = newContinuation
            };

        }

        public async Task<DeviceState> SetDeviceStateAsync(DeviceState state)
        {
            // check if this state is for this actor : DeviceID == ActorId
            if (state.DeviceId == this.GetActorId().ToString())
            {
                var lastState = await StateManager.TryGetStateAsync<DeviceState>(StateName);

                if (lastState.HasValue)
                    state.Version = (lastState.Value.Version < Int32.MaxValue) ? (lastState.Value.Version + 1) : 1;

                // persist the message and add to actor state (in parallel)
                await Task.WhenAll(
                    PersistMessage(state),
                    AddDeviceMessageToMessageListAsync(state)
                    );

                await StateManager.SetStateAsync(StateName, state);
                return state;
            }
            else
            {
                ActorEventSource.Current.ActorMessage(this, "State invalid, device is {0} silhouette is {1}.", state.DeviceId, this.GetActorId().ToString());
                return null;
            }

        }

        private async Task PersistMessage(DeviceState state)
        {
            if (!state.Persisted)
            {
                await StorageProviderServiceClient.StoreStateMessageAsync(state);
                state.Persisted = true;
            }
        }

        private async Task AddDeviceMessageToMessageListAsync(DeviceState state)
        {
            var stateMessages = await StateManager.TryGetStateAsync<List<DeviceState>>(StateName);
            var messages = stateMessages.HasValue ? stateMessages.Value : new List<DeviceState>();
            
            messages.Add(state);
            await StateManager.SetStateAsync(StateName, messages);
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");
            _purgeTimer = RegisterTimer(PurgeStates, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        protected override async Task OnDeactivateAsync()
        {
            if (_purgeTimer != null)
            {
                UnregisterTimer(_purgeTimer);
            }
        }


    }
}
