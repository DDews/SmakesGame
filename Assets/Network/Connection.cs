using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Connection {
    public string externalIP;
    public string internalIP;
    public int listenPort;
    public int connectPort;

    public Connection(string externalIP, string internalIP)
    {
        this.externalIP = externalIP;
        this.internalIP = internalIP;
        this.listenPort = -1;
        this.connectPort = -1;
    }
    public Connection(string externalIP, string internalIP, int listenPort, int connectPort)
    {
        this.externalIP = externalIP;
        this.internalIP = internalIP;
        this.listenPort = listenPort;
        this.connectPort = connectPort;
    }
    
    public byte[] Receive()
    {
        IPAddress ip;
        IPAddress.TryParse(internalIP, out ip);
        IPEndPoint RemoteIPEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
        try
        {
            using (UdpClient c = new UdpClient(listenPort))
            {
                return c.Receive(ref RemoteIPEndPoint);
            }
        } catch (Exception e)
        {
            Debug.Log(e);
            Debug.Log(e.StackTrace);
        }
        return null;
    }
    public void Send(byte[] data)
    {
        using (UdpClient c = new UdpClient(connectPort))
        {
            c.Send(data, data.Length, externalIP, connectPort);
        }
    }

    public override string ToString()
    {
        return externalIP + ':' + connectPort + '\n' + internalIP + ':' + listenPort;
    }
}
