# SharpSocket
Multi-Threaded RFC6455 Implementation for C#

Support for server to client
 
Opcode support for: closing connection, ping, pong, text.

Support for more than 126 & 127 character lengths using extended length bytes.

And my very bad XOR function included :)

[Protocol Implemented](https://tools.ietf.org/html/rfc6455 "RFC6455")

This is by no means a complete implementation, it is still missing elements such as
HTTP authorization, SSL, Proxy Support, Extensions etc.

It is more seen as a simpler project for educational purposes.

## Example Use

```c#
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
```

### Target Specific Controls

```c#
webSocket.users[0].sendFrame("Hey user 0!", User.OpCode.text); //<-- Sending messages to specific users.

webSocket.users[0].sendFrame("Ping!", User.OpCode.ping); //<-- Sending a ping message

webSocket.users[0].close() //<-- Closing targets connection

webSocket.stop(); //<-- Closing all connections/threads to users & closing server.
```
