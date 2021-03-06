#region Copyright (C) 2005-2009 Team MediaPortal

// Copyright (C) 2005-2009 Team MediaPortal
// http://www.team-mediaportal.com
// 
// This Program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2, or (at your option)
// any later version.
// 
// This Program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GNU Make; see the file COPYING.  If not, write to
// the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
// http://www.gnu.org/copyleft/gpl.html

#endregion

using System;
#if TRACE
using System.Diagnostics;
#endif
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using IRServer.Plugin.Properties;
using IrssUtils;

namespace IRServer.Plugin
{

  #region Enumerations

  internal enum IrTransStatus
  {
    STATUS_MESSAGE = 1,
    STATUS_TIMING = 2,
    STATUS_DEVICEMODE = 3,
    STATUS_RECEIVE = 4,
    STATUS_LEARN = 5,
    STATUS_REMOTELIST = 6,
    STATUS_COMMANDLIST = 7,
    STATUS_TRANSLATE = 8,
    STATUS_FUNCTION = 9,
    STATUS_DEVICEMODEEX = 10,
    STATUS_DEVICEDATA = 11,
    STATUS_LCDDATA = 12,
    STATUS_FUNCTIONEX = 13,
    STATUS_DEVICEMODEEXN = 14,
    STATUS_IRDB = 15,
    STATUS_TRANSLATIONFILE = 16,
    STATUS_IRDBFILE = 17,
    STATUS_BUSLIST = 18,
    STATUS_LEARNDIRECT = 19,
    STATUS_IRDBFLASH = 20,
    STATUS_ANALOGINPUT = 21,
  }

  internal enum IrTransCommand
  {
    COMMAND_SEND = 1,
    COMMAND_LRNREM = 2,
    COMMAND_LRNTIM = 3,
    COMMAND_LRNCOM = 4,
    COMMAND_CLOSE = 5,
    COMMAND_STATUS = 6,
    COMMAND_RESEND = 7,
    COMMAND_LRNRAW = 8,
    COMMAND_LRNRPT = 9,
    COMMAND_LRNTOG = 10,
    COMMAND_SETSTAT = 11,
    COMMAND_LRNLONG = 12,
    COMMAND_LRNRAWRPT = 13,
    COMMAND_RELOAD = 14,
    COMMAND_LCD = 15,
    COMMAND_LEARNSTAT = 16,
    COMMAND_TEMP = 17,
    COMMAND_GETREMOTES = 18,
    COMMAND_GETCOMMANDS = 19,
    COMMAND_STORETRANS = 20,
    COMMAND_LOADTRANS = 21,
    COMMAND_SAVETRANS = 22,
    COMMAND_FLASHTRANS = 23,
    COMMAND_FUNCTIONS = 24,
    COMMAND_TESTCOM = 25,
    COMMAND_LONGSEND = 26,
    COMMAND_SHUTDOWN = 27,
    COMMAND_SENDCCF = 28,
    COMMAND_LCDINIT = 29,
    COMMAND_SETSWITCH = 30,
    COMMAND_STATUSEX = 31,
    COMMAND_RESET = 32,
    COMMAND_DEVICEDATA = 33,
    COMMAND_STARTCLOCK = 34,
    COMMAND_LCDSTATUS = 35,
    COMMAND_FUNCTIONEX = 36,
    COMMAND_MCE_CHARS = 37,
    COMMAND_SUSPEND = 38,
    COMMAND_RESUME = 39,
    COMMAND_DELETECOM = 40,
    COMMAND_EMPTY = 41,
    COMMAND_SETSTAT2 = 42,
    COMMAND_STATUSEXN = 43,
    COMMAND_BRIGHTNESS = 44,
    COMMAND_DEFINECHAR = 45,
    COMMAND_STOREIRDB = 46,
    COMMAND_FLASHIRDB = 47,
    COMMAND_SAVEIRDB = 48,
    COMMAND_LOADIRDB = 49,
    COMMAND_LED = 50,
    COMMAND_TRANSFILE = 51,
    COMMAND_IRDBFILE = 52,
    COMMAND_LISTBUS = 53,
    COMMAND_SENDCCFSTR = 54,
    COMMAND_LEARNDIRECT = 55,
    COMMAND_TESTCOMEX = 56,
    COMMAND_SENDCCFSTRS = 57,
    COMMAND_SETSTATEX = 58,
    COMMAND_DELETEREM = 59,
    COMMAND_READ_ANALOG = 60,
  }

  #endregion Enumerations

  #region Interop Structures

  [StructLayout(LayoutKind.Sequential)]
  internal struct NETWORKRECV
  {
    public UInt32 ClientID;
    public Int16 StatusLen;
    public Int16 StatusType;
    public Int16 Address;
    public UInt16 CommandNum;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string Remote;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string Command;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)] public string Data;
  }

  /*
  [StructLayout(LayoutKind.Sequential)]
  struct NETWORKSTATUS
  {
	  public UInt32 ClientID;
	  public Int16 StatusLen;
	  public Int16 StatusType;
	  public Int16 Address;
	  public UInt16 NetStatus;
	  public UInt16 StatusLevel;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2)]
    public string Align;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
	  public string Message;
  }
  
  [StructLayout(LayoutKind.Sequential)]
  struct NETWORKCOMMAND
  {
	  public byte netcommand;
    public byte mode;
    public UInt16 timeout;
    public Int32 address;
    public Int32 protocol_version;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string remote;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
    public string command;
    public byte trasmit_freq;
  }

  [StructLayout(LayoutKind.Sequential)]
  struct LCDCOMMAND
  {
    public byte netcommand;
    public byte mode;
    public byte lcdcommand;
    public byte timeout;
    public Int32 address;
    public Int32 protocol_version;
    public byte wid;
    public byte hgt;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
    public string framebuffer;
  }
  */

  #endregion Interop Structures

  /// <summary>
  /// IR Server Plugin for IRTrans.
  /// </summary>
  public class IRTransTransceiver : PluginBase, IConfigure, IRemoteReceiver
  {
    #region Constants

    private const string DefaultRemoteModel = "mediacenter";
    private const string DefaultServerAddress = "localhost";
    private const int DefaultServerPort = 21000;

    private const int IRTransClientID = 0;
    private const int IRTransProtocolVer = 209;
    private static readonly string ConfigurationFile = Path.Combine(ConfigurationPath, "IRTrans Transceiver.xml");

    #endregion Constants

    #region Variables

    private string _irTransRemoteModel;
    private string _irTransServerAddress;
    private int _irTransServerPort;
    private AsyncCallback _pfnCallBack;
    private RemoteHandler _remoteButtonHandler;

    private Socket _socket;

    #endregion Variables

    #region Implementation

    /// <summary>
    /// Name of the IR Server plugin.
    /// </summary>
    /// <value>The name.</value>
    public override string Name
    {
      get { return "IRTrans"; }
    }

    /// <summary>
    /// IR Server plugin version.
    /// </summary>
    /// <value>The version.</value>
    public override string Version
    {
      get { return "1.4.2.0"; }
    }

    /// <summary>
    /// The IR Server plugin's author.
    /// </summary>
    /// <value>The author.</value>
    public override string Author
    {
      get { return "and-81"; }
    }

    /// <summary>
    /// A description of the IR Server plugin.
    /// </summary>
    /// <value>The description.</value>
    public override string Description
    {
      get { return "IRTrans Transceiver"; }
    }

    /// <summary>
    /// Gets a display icon for the plugin.
    /// </summary>
    /// <value>The icon.</value>
    public override Icon DeviceIcon
    {
      get { return Resources.Icon; }
    }

    #region IConfigure Members

    /// <summary>
    /// Configure the IR Server plugin.
    /// </summary>
    public void Configure(IWin32Window owner)
    {
      LoadSettings();

      Configure config = new Configure();

      config.ServerAddress = _irTransServerAddress;
      config.ServerPort = _irTransServerPort;
      config.RemoteModel = _irTransRemoteModel;

      if (config.ShowDialog(owner) == DialogResult.OK)
      {
        _irTransServerAddress = config.ServerAddress;
        _irTransServerPort = config.ServerPort;
        _irTransRemoteModel = config.RemoteModel;

        SaveSettings();
      }
    }

    #endregion

    #region IRemoteReceiver Members

    /// <summary>
    /// Callback for remote button presses.
    /// </summary>
    /// <value>The remote callback.</value>
    public RemoteHandler RemoteCallback
    {
      get { return _remoteButtonHandler; }
      set { _remoteButtonHandler = value; }
    }

    #endregion

    /// <summary>
    /// Detect the presence of this device.
    /// </summary>
    public override DetectionResult Detect()
    {
      try
      {
        LoadSettings();

        if (Connect(_irTransServerAddress, _irTransServerPort))
        {
          _socket.Close();
          return DetectionResult.DevicePresent;
        }
      }
      catch (Exception ex)
      {
        IrssLog.Error("{0,15}: exception {1}", Name, ex.Message);
        return DetectionResult.DeviceException;
      }

      return DetectionResult.DeviceNotFound;
    }

    /// <summary>
    /// Start the IR Server plugin.
    /// </summary>
    public override void Start()
    {
      LoadSettings();

      if (Connect(_irTransServerAddress, _irTransServerPort))
        BeginReceive();
      else
        throw new InvalidOperationException("Failed to connect");
    }

    /// <summary>
    /// Suspend the IR Server plugin when computer enters standby.
    /// </summary>
    public override void Suspend()
    {
      Stop();
    }

    /// <summary>
    /// Resume the IR Server plugin when the computer returns from standby.
    /// </summary>
    public override void Resume()
    {
      Start();
    }

    /// <summary>
    /// Stop the IR Server plugin.
    /// </summary>
    public override void Stop()
    {
      if (_socket == null)
        return;

      try
      {
        _socket.Close();
      }
#if TRACE
      catch (SocketException ex)
      {
        // Nothing to worry about
        Trace.WriteLine(ex.ToString());
      }
#else
      catch (SocketException)
      {
        // Nothing to worry about
      }
#endif
      finally
      {
        _socket = null;
      }
    }

    /// <summary>
    /// Loads the settings.
    /// </summary>
    private void LoadSettings()
    {
      try
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(ConfigurationFile);

        _irTransRemoteModel = doc.DocumentElement.Attributes["RemoteModel"].Value;
        _irTransServerAddress = doc.DocumentElement.Attributes["ServerAddress"].Value;
        _irTransServerPort = int.Parse(doc.DocumentElement.Attributes["ServerPort"].Value);
      }
#if TRACE
      catch (Exception ex)
      {
        Trace.WriteLine(ex.ToString());
#else
      catch
      {
#endif

        _irTransRemoteModel = DefaultRemoteModel;
        _irTransServerAddress = DefaultServerAddress;
        _irTransServerPort = DefaultServerPort;
      }
    }

    /// <summary>
    /// Saves the settings.
    /// </summary>
    private void SaveSettings()
    {
      try
      {
        using (XmlTextWriter writer = new XmlTextWriter(ConfigurationFile, Encoding.UTF8))
        {
          writer.Formatting = Formatting.Indented;
          writer.Indentation = 1;
          writer.IndentChar = (char) 9;
          writer.WriteStartDocument(true);
          writer.WriteStartElement("settings"); // <settings>

          writer.WriteAttributeString("RemoteModel", _irTransRemoteModel);
          writer.WriteAttributeString("ServerAddress", _irTransServerAddress);
          writer.WriteAttributeString("ServerPort", _irTransServerPort.ToString());

          writer.WriteEndElement(); // </settings>
          writer.WriteEndDocument();
        }
      }
#if TRACE
      catch (Exception ex)
      {
        Trace.WriteLine(ex.ToString());
      }
#else
      catch
      {
      }
#endif
    }

    /// <summary>
    /// Connects to the specified address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="port">The port.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    private bool Connect(string address, int port)
    {
      // TODO: put this on a thread, retry every 30 seconds ...
      try
      {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(address, port);

        // Send Client ID to Server
        byte[] sendData = BitConverter.GetBytes(IRTransClientID);
        _socket.Send(sendData, sendData.Length, SocketFlags.None);
      }
#if TRACE
      catch (SocketException ex)
      {
        Trace.WriteLine("IRTransTransceiver: " + ex.ToString());
#else
      catch (SocketException)
      {
#endif
        return false;
      }

      return true;
    }

    /// <summary>
    /// Starts receiving.
    /// </summary>
    private void BeginReceive()
    {
      try
      {
        if (_pfnCallBack == null)
          _pfnCallBack = OnDataReceived;

        CSocketPacket socketPkt = new CSocketPacket();
        socketPkt.ThisSocket = _socket;

        _socket.BeginReceive(socketPkt.ReceiveBuffer, 0, socketPkt.ReceiveBuffer.Length, SocketFlags.None, _pfnCallBack,
                             socketPkt);
      }
#if TRACE
      catch (SocketException ex)
      {
        Trace.WriteLine(ex.ToString());
      }
#else
      catch (SocketException)
      {
      }
#endif
    }

    /// <summary>
    /// Called when data received.
    /// </summary>
    /// <param name="asyncResult">The async result.</param>
    private void OnDataReceived(IAsyncResult asyncResult)
    {
      try
      {
        CSocketPacket theSockId = (CSocketPacket) asyncResult.AsyncState;

        int bytesReceived = theSockId.ThisSocket.EndReceive(asyncResult);

        IntPtr ptrReceive = Marshal.AllocHGlobal(bytesReceived);
        Marshal.Copy(theSockId.ReceiveBuffer, 0, ptrReceive, bytesReceived);
        NETWORKRECV received = (NETWORKRECV) Marshal.PtrToStructure(ptrReceive, typeof (NETWORKRECV));

        /*
          Log.Info("IRTrans: Command Start --------------------------------------------");
          Log.Info("IRTrans: Client       = {0}", netrecv.clientid);
          Log.Info("IRTrans: Status       = {0}", (IrTransStatus)netrecv.statustype);
          Log.Info("IRTrans: Remote       = {0}", netrecv.remote);
          Log.Info("IRTrans: Command Num. = {0}", netrecv.command_num.ToString());
          Log.Info("IRTrans: Command      = {0}", netrecv.command);
          Log.Info("IRTrans: Data         = {0}", netrecv.data);
          Log.Info("IRTrans: Command End ----------------------------------------------");
        */

        switch ((IrTransStatus) received.StatusType)
        {
          case IrTransStatus.STATUS_RECEIVE:
            if (received.Remote.Trim().Equals(_irTransRemoteModel, StringComparison.OrdinalIgnoreCase))
            {
              try
              {
                string keyCode = received.Command.Trim();

                if (_remoteButtonHandler != null)
                  _remoteButtonHandler(Name, keyCode);
              }
#if TRACE
              catch (Exception ex)
              {
                Trace.WriteLine(ex.ToString());
              }
#else
              catch
              {
              }
#endif
            }
            break;

            //case IrTransStatus.STATUS_LEARN:

          default:
            break;
        }

        Marshal.FreeHGlobal(ptrReceive);
        BeginReceive();
      }
      catch (ObjectDisposedException)
      {
      }
#if TRACE
      catch (SocketException ex)
      {
        Trace.WriteLine(ex.ToString());
      }
#else
      catch (SocketException)
      {
      }
#endif
    }

    #endregion Implementation
  }
}