﻿using DeviceRichState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceRepository
{
    public class MessagePurger
    {
        private readonly double _messagesRetentionMilliseconds;

        public MessagePurger(double messagesRetentionMilliseconds)
        {
            _messagesRetentionMilliseconds = messagesRetentionMilliseconds;
        }

        public void Purge(List<DeviceMessage> messages)
        {
            var indexOfLastPurgeableMessage = GetIndexOfLastPurgeableMessage(messages);
            if (indexOfLastPurgeableMessage >= 0)
            {
                messages.RemoveRange(0, indexOfLastPurgeableMessage + 1);
            }
        }

        ///// <summary>
        ///// Return a list of messages to purge based on purge conditions. 
        ///// Requirements in https://github.com/dx-ted-emea/pudding/wiki/3.7-Long-term-persistency-and-analytics        
        ///// </summary>
        ///// <param name="messages"></param>
        ///// <returns></returns>

        //public List<DeviceMessage> GetPurgableMessages(List<DeviceMessage> messages)
        //{
        //    List<DeviceMessage> purgeMessages = new List<DeviceMessage>();

        //    // last reported message
        //    DeviceMessage lastReported = messages.OrderByDescending(m => m.Timestamp)
        //                                            .Where(m => m.MessageType == MessageType.Report)
        //                                            .FirstOrDefault();

        //    var latestMessageTimeStampToPurge = SystemTime.UtcNow().AddMilliseconds(-_messagesRetentionMilliseconds);

        //    messages.ForEach(m => checkMessage(m, lastReported, latestMessageTimeStampToPurge, messages, purgeMessages));

        //    // if retaining a message than all messages with same correlation id should be retained and all later messages
        //    var retained = messages.Except(purgeMessages).ToList<DeviceMessage>();
        //    retained.ForEach(m => purgeMessages.RemoveAll(p => retainMessage(m, p)));            

        //    return purgeMessages;
        //}

        //private bool retainMessage(DeviceMessage retained, DeviceMessage purged)
        //{
        //    return (retained.CorrelationId == purged.CorrelationId) && (purged.Timestamp >= retained.Timestamp);
        //}

        //private void checkMessage(DeviceMessage message, DeviceMessage lastReported, DateTime latestMessageTimeStampToPurge, List<DeviceMessage> messages, List<DeviceMessage> purgeMessages)
        //{
        //    // message was persisted, message is older than the retention time limit and this is not the latest reorted state   
        //    if (!message.Equals(lastReported) && message.Persisted && (message.Timestamp < latestMessageTimeStampToPurge))
        //    {
        //        // persist command request with no response
        //        if (message.MessageType.Equals(MessageType.CommandRequest))
        //        {
        //            // find all messages with same correlation id and type == response
        //            var res = messages.FindAll(m => (m.CorrelationId == message.CorrelationId) && m.MessageType.Equals(MessageType.CommandResponse));
        //            if (res.Count != 0)
        //            {
        //                purgeMessages.Add(message);
        //            }
        //        }
        //        else
        //        {
        //            purgeMessages.Add(message);
        //        }
        //    }
        //}

        public int GetIndexOfLastPurgeableMessage(List<DeviceMessage> messages)
        {
            // Handle requirements in https://github.com/dx-ted-emea/pudding/wiki/3.7-Long-term-persistency-and-analytics


            if (messages.Count == 0)
            {
                return -1;
            }

            int latestReportedStateMessageIndex = -1;

            DateTime latestMessageTimeStampToPurge = SystemTime.UtcNow().AddMilliseconds(-_messagesRetentionMilliseconds);
            bool gotMessageInRetentionTimeWindow = false;
            int latestMessageBeforeRetentionTimeWindowIndex = -1;

            // Can't persist after the latest persisted message in a chain of persisted messages
            // e.g. in the following sequence lastPersistedMessageInSequenceIndex would be 2 as that is the 
            //      last message that is persisted before the non-persisted message
            //   index     0 1 2 3 4 5 6
            //   persisted y y y n y y y
            bool gotNonPersistedMessage = false;
            int lastPersistedMessageInSequenceIndex = -1;

            for (int messageIndex = 0; messageIndex < messages.Count; messageIndex++)
            {
                var message = messages[messageIndex];

                // latestReportedStateMessageIndex
                if (message.MessageType == MessageType.Report && message.MessageSubType == MessageSubType.State)
                {
                    latestReportedStateMessageIndex = messageIndex;
                }

                // lastPersistedMessageInSequenceIndex
                if (!gotNonPersistedMessage && !message.Persisted)
                {
                    gotNonPersistedMessage = true;
                    lastPersistedMessageInSequenceIndex = messageIndex - 1;
                }

                // latestMessageBeforeRetentionTimeWindowIndex
                if (message.Timestamp > latestMessageTimeStampToPurge
                     && !gotMessageInRetentionTimeWindow)
                {
                    gotMessageInRetentionTimeWindow = true;
                    latestMessageBeforeRetentionTimeWindowIndex = messageIndex - 1;
                }
            }

            if (!gotNonPersistedMessage)
            {
                lastPersistedMessageInSequenceIndex = messages.Count - 1;
            }

            if (latestReportedStateMessageIndex < 0)
            {
                // no reported state messages
                return Min(latestMessageBeforeRetentionTimeWindowIndex, lastPersistedMessageInSequenceIndex);
            }
            else
            {
                return Min(latestReportedStateMessageIndex, latestMessageBeforeRetentionTimeWindowIndex, lastPersistedMessageInSequenceIndex);
            }
        }
        
        public int Min(int value1, int value2, int value3)
        {
            return Min(Min(value1, value2), value3);
        }
        public int Min(int value1, int value2)
        {
            return Math.Min(value1, value2);
        }
    }
}
