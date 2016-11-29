﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Pipes;



namespace Vow_win_ski.IPC
{
    class PipeServer
    {
        private NamedPipeServerStream Server;
        private List<Message> Messages; 
        private Thread thread;
        private byte[] data; 

        //===================================================================================================================================
        private const byte sender = 1;
        private const byte receiver = 0;
        private const byte disconnecter = 2;

       

        //===================================================================================================================================
        public PipeServer()
        {
            Server = new NamedPipeServerStream("SERWER", PipeDirection.InOut);
            Console.WriteLine("Utworzono Serwer IPC");
            Start();

           

        }
        //===================================================================================================================================
        public void Build()
        {
            Server.WaitForConnection();
            Console.WriteLine("Serwer oczekuje na polaczenie");
            Messages = new List<Message>();
            data = new byte[4];
        }
        //===================================================================================================================================
        public byte[] ServerReceiver()
        {
            Console.WriteLine("Serwer otrzymal dane");
            Server.Read(data, 0, 4);
            return data;
        }
        //===================================================================================================================================
        public void StoreMessage()
        {
            Messages.Add(new Message(data[1], data[2], data[3])); 
        }
        //===================================================================================================================================
        public void Switch()
        {
            if (data[0] == receiver) 
            {
                StoreMessage();
            }
            else if (data[0] == sender) 
            {
                ServerWriter(data[1], data[3]);
            }
            else if (data[0] == disconnecter) 
            {
                Server.Disconnect();
                Console.WriteLine("Client rozlaczony z serwerem");
            }
        }
        //===================================================================================================================================

        public void ServerWriter(byte receiverId, byte senderId)
        {
           
            byte[] temp = new byte[4];

            if (Messages.Where(x => x.GetReceiverId() == receiverId).Count() == 0)
            {
                temp[0] = 0;              
                Server.Write(temp, 0, 4);
            }
            else {

                for (int i = 0; i < Messages.Count; i++)
                {

                    if (Messages[i].GetReceiverId() == receiverId) 
                    {
                        Console.WriteLine("Serwer wyslal dane");
                        temp[0] = 1;
                        temp[1] = Messages[i].GetReceiverId();
                        temp[2] = (byte)Messages[i].getMessage();
                        temp[3] = Messages[i].GetSenderId();

                        Server.Write(temp, 0, 4);
                        Messages.RemoveAt(i);


                        break;
                    }
                }
                      
            }
        }
        //===================================================================================================================================
        public void ServerInit()
        {
            Build();
            while (true)
            {
                if (Server.IsConnected)
                {
                    data = ServerReceiver();
                    Switch();
                }
                else
                {
                    Console.WriteLine("Serwer oczekuje na polaczenie");
                    Server.WaitForConnection();
                }
            }
        }

       

        //===================================================================================================================================
        public void Start()
        {
            thread = new Thread(ServerInit);
            thread.Start();
        }
        //===================================================================================================================================
       
    }
}
