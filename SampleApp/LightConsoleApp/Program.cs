﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Threading;

namespace LightConsoleApp
{
    class Program
    {
        static DeviceClient deviceClient;
        static string deviceConnectionString;

        private static bool light;

        static void Main(string[] args)
        {           
            Console.WriteLine("Welcome to Home Light simulation app!");
            Console.WriteLine("Please Enter Device Connection String");
            deviceConnectionString = Console.ReadLine();

            Console.WriteLine("To turn on the light, type +. To turn off, type -");

            try
            {
                //MainAsync(args).Wait();
                Thread t = new Thread(() => { RunSimulation(args); });
                t.IsBackground = true;
                t.Start();


                char input;
                while (true)
                {
                    ConsoleKeyInfo info = Console.ReadKey();
                    if (info != null)
                    {
                        input = info.KeyChar;
                        switch (input)
                        {
                            case '+':
                                Console.WriteLine("Recevied input from user. Changing light to on");
                                light = true;
                                break;
                            case '-':
                                Console.WriteLine("Recevied input from user. Changing light to off");
                                light = false;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (AggregateException ae)
            {
                Console.WriteLine(ae.InnerException);
            }
        }

        private static void RunSimulation(string[] args)
        {
            while (true)
            {
                MainAsync(args).Wait();
            }
        }

        static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Simulated device\n");
            deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);

            await Task.WhenAll(SendDeviceToCloudMessagesAsync(),
            ReceiveCloudToDeviceMessageAsync(),
            LightSensor());                                       
        }

        private static Task LightSensor()
        {
            while (true)
            {
                // change the light status every 5 minutes
                Task.Delay(5 * 60 * 1000).Wait();
                Console.WriteLine("Sensor changing status");
                light = !light;
            }
        }

        private static async Task SendDeviceToCloudMessagesAsync()
        {          
            while (true)
            {
                await sendMessage("Report", "State");

                Task.Delay(1000).Wait();
            }
        }

        private static async Task sendMessage(string messageType, string messageSubType)
        {
            DeviceMessage msg = new DeviceMessage(light);
            string json = JsonConvert.SerializeObject(msg);
            var message = new Message(Encoding.UTF8.GetBytes(json))
            {
                Properties = {
                        { "MessageType", messageType},
                        { "MessageSubType", messageSubType}
                    }
            };

            await deviceClient.SendEventAsync(message);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, json);
        }

        private static async Task<Message> ReceiveCloudToDeviceMessageAsync()
        {
            while (true)
            {
                var receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    await deviceClient.CompleteAsync(receivedMessage);
                    processMessage(receivedMessage);
                }

                Task.Delay(1000).Wait();
            }
        }

        private static async void processMessage(Message message)
        {
            if (message == null) return;

            var messageData = Encoding.ASCII.GetString(message.GetBytes());

            // TODO: add error handling
            string messageType = message.Properties["MessageType"];
            string messageSubType = message.Properties["MessageSubType"];

            if (messageType.Equals("CommandRequest"))
            {
                switch (messageSubType)
                {
                    case "SetState":
                        setState(messageData);
                        break;
                    case "LatestState":
                        break;
                    case "ReportState":
                        await sendMessage("Report", "State");
                        break;
                }
            }
            else if (messageType.Equals("InquiryResponse") && messageSubType.Equals("GetState"))
            {
                await sendMessage("Report", "State");
            }
        }

        private static void setState(string messageData)
        {
            // expect the message data to hold a state for the light - on or off
            // "{"status" : "on"}"           
            DeviceMessage msg = JsonConvert.DeserializeObject<DeviceMessage>(messageData);
            Console.WriteLine("Received a status change: " + messageData);
            light = msg.status.Equals("on");
        }

    }

    internal class DeviceMessage
    {
        public string status;

        public DeviceMessage(bool isOn)
        {
            status = isOn ? "on" : "off";
        }
    }
}