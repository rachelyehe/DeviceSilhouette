﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateManagementServiceWebAPI.Models.DeviceMessage
{
    /// <summary>
    /// API representation of a message
    /// </summary>
    public class MessageModel
    {
        /// <summary>
        /// The device ID for the message
        /// </summary>
        public string DeviceId { get; internal set; }
        /// <summary>
        /// The timestamp for when the message was enqueued
        /// </summary>
        public DateTime TimeStamp { get; internal set; }
        /// <summary>
        /// The version number for the message. This is a unique identifier for the message within a device id
        /// </summary>
        public int Version { get; internal set; }

        /// <summary>
        /// The main type for the message (e.g. CommandRequest, Report)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The subtype for the message (e.g. SetState for CommandRequest messages)
        /// </summary>
        public string Subtype { get; set; }

        /// <summary>
        /// The correlation id for messages. Used to link related messages
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Application metadata (used by the application to store data associated with a command, e.g. the origin or reason)
        /// </summary>
        public JToken AppMetadata { get; set; }

        /// <summary>
        /// The message body. For state reports this is the state
        /// </summary>
        public JToken Values { get; set; }

        /// <summary>
        /// The message time-to-live (TTL) in milliseconds
        /// </summary>
        public long MessageTtlMs { get; set; }
    }
}

