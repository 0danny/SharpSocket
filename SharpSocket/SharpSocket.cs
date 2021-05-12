using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Collections;
using System.Threading;

namespace SharpSocket
{
    public class SharpSocket
    {
        /*
         * RFC6455 Websocket Implementation
         * 
         * Author: Danny
         * Date: 5/10/2021
         * Time Taken: Too Long (3+ Hours)
         */

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        private string address = "127.0.0.1";
        private int port = 80;
        private bool acceptingNewClients = true;

        private TcpListener serverObject;
        public List<SocketUser> users = new List<SocketUser>();

        public SharpSocket(string address, int port)
        {
            this.address = address;
            this.port = port;

            Logger.write("Server created: " + address + ":" + port);
        }

        public void start()
        {
            serverObject = new TcpListener(IPAddress.Parse(address), port);

            serverObject.Start();

            Logger.write("Server started.");

            Thread acceptThread = new Thread(new ThreadStart(acceptLoop));
            acceptThread.Name = "Accept Loop";
            acceptThread.Start();
        }

        public void stop()
        {
            acceptingNewClients = false;

            foreach (SocketUser user in users)
            {
                user.sendFrame("Closing connections.", User.OpCode.close);
            }
        }

        private void acceptLoop()
        {
            while (acceptingNewClients)
            {
                TcpClient client = serverObject.AcceptTcpClient();

                Logger.write("New user connected.");

                SocketUser user = new SocketUser(client, this);

                

                users.Add(user);
            }
        }

        public virtual void OnMessageReceived(MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public bool fin { get; set; }
        public bool masked { get; set; }
        public int opCode { get; set; }
        public ulong messageLength { get; set; }
        public string messageData { get; set; }
        public SocketUser socketUser { get; set; }
    }

    public class HashLibary
    {
        private static string GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        public static void endianChecker(byte[] data)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
        }

        //Custom implementation by Dan | 5/11/2021 4:21 PM
        public static byte xorByte(byte left, byte right)
        {
            string leftBits = Convert.ToString(left, 2).PadLeft(8, '0');
            string rightBits = Convert.ToString(right, 2).PadLeft(8, '0');

            string decodedByte = "";

            for (int i = 0; i < leftBits.Length; i++)
            {
                if (leftBits[i] == rightBits[i])
                {
                    decodedByte += "0";
                }
                else
                {
                    decodedByte += "1";
                }
            }
            
            return Convert.ToByte(decodedByte, 2);
        }

        //Custom implementation by Dan | 5/10/2021 7:32 PM
        public static string websocketEncode(string text)
        {
            byte[] rawHash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(text + GUID));

            return Convert.ToBase64String(rawHash);
        }
    }
}
