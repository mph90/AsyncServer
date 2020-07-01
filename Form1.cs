using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace AsyncServer
{
    public partial class Form1 : Form
    {
        List<Socket> AllSockets = new List<Socket>();
        Socket socket;
        TcpListener listener;
        byte[] buffer = new byte[256];
        int port;
        string message;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            textBoxPort.Text = "1001";
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            port = int.Parse(textBoxPort.Text);
            Listen(port);
        }

        private void Listen(int port)
        {
            try
            {
                // Listen for input coming from any IP address on specified port
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                
                // Create an event handler for dealing with incoming connections
                listener.BeginAcceptTcpClient(new AsyncCallback(AcceptIncomingConnection), listener);
            }
            catch (Exception ex)
            {
                // Update display to show error message
                listBox1.Items.Add("Socket connection error:\n" + ex.ToString());
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                listBox1.SelectedIndex = -1;
            }
        }

        private void AcceptIncomingConnection(IAsyncResult info) // CALLBACK
        {
            // Which listener is this using?
            TcpListener l = (TcpListener)info.AsyncState;
            
            // Accept incoming socket connection
            Socket socket;
            socket = l.EndAcceptSocket(info);
           
            // Set up an event handler for receiving messages
            AllSockets.Add(socket);
            foreach (Socket i in AllSockets)
            {
                Receive(socket);
            }
            
            // Re-listen!
            listener.BeginAcceptTcpClient(new AsyncCallback(AcceptIncomingConnection), listener);
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            SendInput();
        }

        private void textBoxMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == 13)
            {
                SendInput();
            }
        }

        private void SendInput()
        {
            string displayMessage = "Server: " + textBoxMessage.Text;
            listBox1.Items.Add(displayMessage);
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            listBox1.SelectedIndex = -1;

            foreach (Socket i in AllSockets)
            {
                Transmit(i, displayMessage);
            }

            textBoxMessage.Text = String.Empty;
        }

        private void Transmit(Socket s, string text)
        {
            try
            {
                // Prepare message
                byte[] messageBytes = Encoding.ASCII.GetBytes(text);
                // Send it
                s.BeginSend(messageBytes, 0, messageBytes.Length, 0, new AsyncCallback(TransmitHandler), s);
            }
            catch (Exception) {}
        }

        private void TransmitHandler(IAsyncResult info) // CALLBACK
        {
            // Which socket is this using?
            Socket s = (Socket)info.AsyncState;
            int bytesSent = s.EndSend(info);
        }

        private void Receive(Socket s)
        {
            s.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None,
            ReceiveHandler, s);
        }

        private void ReceiveHandler(IAsyncResult info) // CALLBACK
        {
            try
            {
                // Which socket is this using?
                Socket s = (Socket)info.AsyncState;
                // Read message
                int numBytesReceived = s.EndReceive(info);
                message = Encoding.ASCII.GetString(buffer, 0, numBytesReceived);
                // Update display
                listBox1.Items.Add(message);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                listBox1.SelectedIndex = -1;
                // Reset the event handler for new incoming messages on socket s
                Receive(s);
            }
            catch (Exception) {}
            
            // Send the message to all connected clients
            foreach (Socket n in AllSockets)
            {
                Transmit(n, message);
            }
        }

        private void textBoxMessage_TextChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
