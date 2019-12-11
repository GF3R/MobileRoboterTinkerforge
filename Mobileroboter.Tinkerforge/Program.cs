using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tinkerforge;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Mobileroboter.Tinkerforge
{
    internal class Program
    {
        private static string HOST = "localhost";
        private static int PORT = 4223;
        private static string UID = "XYZ"; // Change to your UID


        private static string Dist1Id = "zn1";
        private static string Dist2Id = "znd";
        private static string CompassBox = "L5F";
        private static string CompassRobot = "L5G";


        private static string MqttServer = "tailor.cloudmqtt.com";
        private static string MqttUser = "nkmgzcde";
        private static string MqttPassword = "od2KPEBXR7uk";

        private static MqttClient _client;

        private static void Main(string[] args)
        {
            var ipcon = new IPConnection();
            ipcon.Connect(HOST, PORT);
            _client = new MqttClient(MqttServer, 17124, false, new MqttSslProtocols(), null, null);
            _client.Connect(Guid.NewGuid().ToString(), MqttUser, MqttPassword);
            var distanceUs1 = new BrickletDistanceUS(Dist1Id, ipcon);
            var distanceUs2 = new BrickletDistanceUS(Dist2Id, ipcon);
            var CompassBoxBricklet = new BrickletCompass(CompassBox, ipcon);
            var CompassRobotBricklet = new BrickletCompass(CompassRobot, ipcon);
            var colorR = 0;
            var colorG = 0;
            var colorB = 0;

            while (true)
            {
                var val1 = CalculateCM(distanceUs1.GetDistanceValue());
                var val2 = CalculateCM(distanceUs2.GetDistanceValue());
                var boxHeading = CompassBoxBricklet.GetHeading();
                var robotHeading = CompassRobotBricklet.GetHeading();
                
                _client.Publish("/mobile/distance",
                    Encoding.UTF8.GetBytes(
                        "{{\n" +
                        $"\"robotHeading\":{robotHeading / 10},\n" +
                        $"\"boxHeading\":{boxHeading / 10},\n" +
                        $"\"dist1\":{val1},\n" +
                        $"\"dist2\":{val2},\n" +
                        "\"color\":{{\n" +
                            $"\"red\":{colorR},\n" +
                            $"\"blue\":{colorB},\n" +
                            $"\"green\":{colorG}\n" +
                            "}}\n" +
                        "}}"),
                    MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

                Thread.Sleep(500);
                
            }
        }

        public static double CalculateCM(int value)
        {
            return (value - 2.78) * 0.094;
        }

        public static string CalculateXY(double S_unten, double S_links)
        {
            var Grad_Box = 200 / 180 * Math.PI;
            var Grad_Rob = 20 / 180 * Math.PI;
            var Y_Box = 10;
            var X_Box = 10;
            var x = S_links * Math.Cos(Grad_Box - Grad_Rob) - S_unten * Math.Sin(Grad_Box - Grad_Rob);
            var y = S_unten * Math.Cos(Grad_Box - Grad_Rob) + S_links * Math.Sin(Grad_Box - Grad_Rob);

            if (x < 0 || Math.Cos(Grad_Box - Grad_Rob) - Math.Sin(Grad_Box - Grad_Rob) < 0) x = X_Box + x;
            if (y < 0 || Math.Cos(Grad_Box - Grad_Rob) + Math.Sin(Grad_Box - Grad_Rob) < 0) y = Y_Box + y;

            return $"X: {x}  Y:{y}";
        }
    }
}