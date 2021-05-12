using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpSocket;

namespace SharpSocket.Example
{
    public class Program
    {
        SharpSocket webSocket = new SharpSocket("127.0.0.1", 80);

        public Program()
        {
            Console.Title = "SharpSocket Example";

            webSocket.start();

            webSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(messageReceived);

            //webSocket.users[0].sendFrame("Hey user 0!", User.OpCode.text); <-- Sending messages to specific users.

            //webSocket.stop(); <-- Closing all connections/threads to users & closing server.

            Console.ReadLine();
        }

        private void messageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Got message from user: " + e.messageData);

            e.socketUser.sendFrame("Hello, its the server!", User.OpCode.text);
        }

        public static void Main(string[] args)
        {
            Program p = new Program();
        }
    }
}
