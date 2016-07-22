﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using DeviceRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceRichState;

namespace DeviceRepository.Tests
{
    [TestClass()]
    public class MessagePurgerTests_GetIndexOfLastPurgeableMessage
    {

        // TODO - add tests for scenarios to cover requirements in https://github.com/dx-ted-emea/pudding/wiki/3.7-Long-term-persistency-and-analytics

        [TestInitialize]
        public void Initialize()
        {
            SystemTime.Reset();
            _messageRetentionInMilliseconds = -1;
            _messages = null;
        }



        [TestMethod()]
        public void WhenNoMessagesInTheList_ThenNoMessagesAreIdentifiedToPurge()
        {
            var baseDateTime = new DateTime(2016, 07, 22, 10, 00, 00, DateTimeKind.Utc);
            var messages = new List<DeviceState>();

            WithSystemTimeUtc(baseDateTime);
            WithMessageRetentionOf(10 * Minutes);
            WithMessages(messages);
            ExpectLastPurgeIndexToBe(-1);
        }

        [TestMethod()]
        public void WhenNoMessagesAreOutsideTheRetentionPeriod_ThenNoMessagesAreIdentifiedToPurge()
        {
            var baseDateTime = new DateTime(2016, 07, 22, 10, 00, 00, DateTimeKind.Utc);

            WithSystemTimeUtc(baseDateTime);
            WithMessageRetentionOf(10 * Minutes);
            var messages = new List<DeviceState>
            {
                /* index 0 */ ReportedState(baseDateTime + TimeSpan.FromMinutes(-9), persisted:true),
                /* index 1 */ ReportedState(baseDateTime + TimeSpan.FromMinutes(-8), persisted:true),
            };
            WithMessages(messages);
            ExpectLastPurgeIndexToBe(-1);
        }

        [TestMethod()]
        public void WhenNoMessagesArePersisted_ThenNoMessagesAreIdentifiedToPurge()
        {
            var baseDateTime = new DateTime(2016, 07, 22, 10, 00, 00, DateTimeKind.Utc);

            WithSystemTimeUtc(baseDateTime);
            WithMessageRetentionOf(10 * Minutes);
            var messages = new List<DeviceState>
            {
                /* index 0 */ ReportedState(baseDateTime + TimeSpan.FromMinutes(-20), persisted:false), // not persisted
                /* index 1 */ ReportedState(baseDateTime + TimeSpan.FromMinutes(-9), persisted:false),  // in retention window
            };
            WithMessages(messages);
            ExpectLastPurgeIndexToBe(-1);
        }

        [TestMethod()]
        public void WhenAllMessagesArePersisted_ThenOnlyMessagesOutsideTheRetentionWindowArePurged()
        {
            var baseDateTime = new DateTime(2016, 07, 22, 10, 00, 00, DateTimeKind.Utc);

            WithSystemTimeUtc(baseDateTime);
            WithMessageRetentionOf(10 * Minutes);
            var messages = new List<DeviceState>
            {
                // Index 0 is before the retention window, is persisted, and has later StateReport => can be purged
                /* index 0 */ ReportedState(baseDateTime + TimeSpan.FromMinutes(-20), persisted:true), 
                /* index 1 */ ReportedState(baseDateTime + TimeSpan.FromMinutes(-9), persisted:true),
                /* index 2 */ ReportedState(baseDateTime + TimeSpan.FromMinutes(-6), persisted:true),
            };
            WithMessages(messages);
            ExpectLastPurgeIndexToBe(0);
        }

        #region helpers
        private int _messageRetentionInMilliseconds;
        private List<DeviceState> _messages;

        private const int Seconds = 1000;
        private const int Minutes = 60 * Seconds;

        private const string DeviceId = "a-device";
        private DeviceState ReportedState(DateTime timestamp, bool persisted)
        {
            var message = new DeviceState(
                DeviceId,
                "{}",
                "{}",
                MessageType.Reported,
                MessageStatus.Received
                )
            {
                Persisted = persisted,
                _timestamp =timestamp
            };
            return message;
        }

        private void WithSystemTimeUtc(DateTime systemDateTime)
        {
            SystemTime.UtcNow = () => systemDateTime;
        }

        private void WithMessageRetentionOf(int messageRetentionInMilliseconds)
        {
            _messageRetentionInMilliseconds = messageRetentionInMilliseconds;
        }
        private void WithMessages(List<DeviceState> messages)
        {
            _messages = messages;
        }
        private void ExpectLastPurgeIndexToBe(int expectedIndex)
        {
            var messagePurger = new MessagePurger(_messageRetentionInMilliseconds);
            int actualIndex = messagePurger.GetIndexOfLastPurgeableMessage(_messages);
            Assert.AreEqual(expectedIndex, actualIndex);
        }

        #endregion
    }
}