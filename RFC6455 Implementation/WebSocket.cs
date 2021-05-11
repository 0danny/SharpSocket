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

namespace RFC6455_Implementation
{
    public class WebSocket
    {
        /*
         * RFC6455 Websocket Implementation
         * 
         * Author: DannyOCE
         * Date: 5/10/2021
         * Time Taken: Too Long (3+ Hours)
         */

        private string GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private event EventHandler<MessageRecievedEventArgs> MessageRecieved;

        public WebSocket()
        {
            Console.Title = "WebSocket RFC6455 | Dan";

            Console.SetWindowSize(60,20);
            

            acceptClient();

            Console.ReadLine();
        }

        public void acceptClient()
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 80);

            server.Start();

            Console.WriteLine("Waiting for connection.");

            //Block the thread.
            IL_RESTART:
            TcpClient client = server.AcceptTcpClient();

            Console.WriteLine("Connected to client.");

            if (clientThread(client) == true)
            {
                goto IL_RESTART;
            }
        }

        public bool clientThread(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            MessageRecieved += messageRecieved;

            while (true)
            {
                while (!stream.DataAvailable) ;

                byte[] byteData = new byte[client.Available];
                stream.Read(byteData, 0, byteData.Length);
                string data = Encoding.UTF8.GetString(byteData);

                if (data.Contains("Sec-WebSocket-Key"))
                {
                    //Handle websocket handshake

                    string rawKey = Regex.Match(data, "Sec-WebSocket-Key:(.*)").Groups[1].Value.Trim();
                    string encodedKey = HashLibary.websocketEncode(rawKey + GUID);

                    string handshake = string.Join("\r\n", new string[] {
                        "HTTP/1.1 101 Switching Protocols",
                        "Upgrade: websocket",
                        "Connection: Upgrade",
                        "Sec-WebSocket-Accept: " + encodedKey
                    }) + "\r\n\r\n";

                    byte[] handshakeBytes = Encoding.UTF8.GetBytes(handshake);

                    stream.Write(handshakeBytes, 0, handshakeBytes.Length);
                    Console.WriteLine("Handshake Sent.");
                }
                else
                {
                    //Handle data transfer

                    /*
                     * Comparing first bit [1]0000001 with 10000000
                     * Should give us 10000000 checking if first bit (or least significant) is 1
                     * If it is then 
                     */


                    // '0b' is the a string used to tell the compiler that the following numbers are in binary
                    bool fin = (byteData[0] & 0b10000000) != 0;
                    int opCode = (byteData[0] - 128); //Risky as the first 4 bits may not always be == 1000
                    //Better to just do (byteData[0] - 0b10001111);

                    if (opCode == 8) //Close control frame
                    {
                        Console.WriteLine("The connection has been closed.");

                        client.Close();
                        client.Dispose();
                        stream.Close();
                        stream.Dispose();
                        break;
                    }

                    bool mask = (byteData[1] & 0b10000000) != 0;

                    /*
                     * 128 in Binary == 10000000
                     * Payload is byte number [1] for 'hello' which is == 10000101
                     * The actual length of the message is the 4 trailing bytes at the end
                     * Therefore the first 1000 == 128 in 32-bit int so we remove it from that byte to get the length
                     * Like normal subtraction
                     */
                    ulong messageLength = (ulong)(byteData[1] - 128);

                    byte[] maskingBytes = new byte[] { byteData[2], byteData[3], byteData[4], byteData[5] };

                    int offset = 6;

                    if (fin == false)
                    {
                        throw new OverflowException("Fragmentation not supported.");
                    }

                    if (messageLength == 126)
                    {
                        byte[] extendedLengthBytes = new byte[] { byteData[2], byteData[3] };

                        HashLibary.endianChecker(extendedLengthBytes);

                        ushort extendedLength = BitConverter.ToUInt16(extendedLengthBytes, 0);

                        //Moving everything foward 2 bytes
                        maskingBytes = new byte[] { byteData[4], byteData[5], byteData[6], byteData[7] };

                        offset = 8;

                        messageLength = extendedLength;

                        Console.WriteLine("Got Uint16 length: " + extendedLength);
                    }

                    /* Doesn't work yet as a single byte array cannot hold more than 64964 bytes == 65 kilobytes
                     * Data will have to be sent in fragmentation frames.
                     */
                    if (messageLength == 127)
                    {
                        byte[] extendedLengthBytes = new byte[] { byteData[2], byteData[3], byteData[4], byteData[5], byteData[6], byteData[7], byteData[8], byteData[9] };

                        HashLibary.endianChecker(extendedLengthBytes);

                        ulong extendedLength = BitConverter.ToUInt64(extendedLengthBytes, 0);

                        maskingBytes = new byte[] { byteData[10], byteData[11], byteData[12], byteData[13] };

                        offset = 14;

                        messageLength = extendedLength;

                        Console.WriteLine("Got an Uint64 Length: " + extendedLength);

                        throw new OverflowException("Fragmentation not supported.");
                    }

                    byte[] decodedBytes = new byte[messageLength];

                    for (int i = 0; i < (int)messageLength; i++)
                    {
                        /* i % 4 basically outputs this (modulo)
                         * 
                         * 0, 1, 2, 3, 0, 1, 2, 3
                         * 
                         * It is looping through the array of masking bytes and everytime the remainder
                         * hits the maskingBytes length the output == 0
                         * 
                         * Example: i == 2 | maskingBytes.Length == 4 the % of it == 2
                         * 
                         * decodedBytes[i] = (byte)(byteData[i + offset] ^ maskingBytes[i % maskingBytes.Length]);
                         * 
                         * This line is a fancy way of 'byteData[i + offset]' starting at the 6th byte which is
                         * where payload_data begins. Then we are using the logical XOR operand '^' with the first masking key
                         * in the array and we are going down the array using all the keys to XOR each of the bytes 
                         * which is removing the mask.
                         * 
                         */

                        //decodedBytes[i] = (byte)HashLibary.xorByte(byteData[i + offset], maskingBytes[i % maskingBytes.Length]);

                        decodedBytes[i] = (byte)(byteData[i + offset] ^ maskingBytes[i % maskingBytes.Length]);
                    }

                    MessageRecievedEventArgs args = new MessageRecievedEventArgs();
                    args.fin = fin;
                    args.masked = mask;
                    args.messageData = Encoding.UTF8.GetString(decodedBytes);
                    args.messageLength = messageLength;
                    args.opCode = opCode;
                    args.client = client;
                    OnMessageRecieved(args);

                    /* TODO:
                     * Add support for fragmentation frames due to byte size not being large enough.
                     * ^^^ Turns out javascript websocket libary is dogshit and doesn't support fragmentation
                     */
                }
            }

            return true;
        }

        private void messageRecieved(object sender, MessageRecievedEventArgs e)
        {
            Console.WriteLine("Event Handler called: " + e.messageData + " | OpCode: " + e.opCode);

            sendFrame(e.client, "Closing connection.", OpCode.close);
        }

        protected virtual void OnMessageRecieved(MessageRecievedEventArgs e)
        {
            MessageRecieved?.Invoke(this, e);
        }

        public void sendFrame(TcpClient client, string frame, OpCode opCode)
        {
            NetworkStream stream = client.GetStream();

            byte[] frameBytes = Encoding.UTF8.GetBytes(frame);
            byte[] message = new byte[2 + frame.Length]; //First two bytes containing meta data, rest contains payload data.

            message[0] = (byte)opCode; //1000 0001
            message[1] = (byte)frame.Length; //Length, no masking

            int offset = 2;

            for (int i = offset; i < message.Length; i++)
            {
                message[i] = frameBytes[i - offset]; //rest payload data
            }

            stream.Write(message, 0, message.Length);
        }

        public enum OpCode : byte
        {
            text = 129,
            ping = 137,
            pong = 138,
            close = 136
        }

        public static void Main(string[] args)
        {
            WebSocket p = new WebSocket();
        }
    }

    public class MessageRecievedEventArgs : EventArgs
    {
        public bool fin { get; set; }
        public bool masked { get; set; }
        public int opCode { get; set; }
        public ulong messageLength { get; set; }
        public string messageData { get; set; }
        public TcpClient client { get; set; }
    }

    public class HashLibary
    {
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
            byte[] rawHash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(text));

            return Convert.ToBase64String(rawHash);
        }
    }
}
