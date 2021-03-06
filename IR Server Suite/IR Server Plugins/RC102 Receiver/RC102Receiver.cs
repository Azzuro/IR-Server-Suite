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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using IRServer.Plugin.Properties;
using IrssUtils;
using Microsoft.Win32.SafeHandles;

namespace IRServer.Plugin
{
  /// <summary>
  /// IR Server Plugin for RC102 and compatible input devices.
  /// </summary>
  [CLSCompliant(false)]
  public class RC102Receiver : PluginBase, IRemoteReceiver
  {
    #region Constants

    private const int DeviceBufferSize = 4;

    private const string DeviceID = "vid_147a&pid_e019";
    //const string DeviceID = "vid_147a&pid_e001"; // 501
    //const string DeviceID = "vid_147a&pid_e02a"; // 507
    //const string DeviceID = "vid_0e6a&pid_6002"; // 507??

    #endregion Constants

    #region Variables

    private byte[] _deviceBuffer;
    private FileStream _deviceStream;

    private int _lastCode = -1;
    private DateTime _lastCodeTime = DateTime.Now;
    private RemoteHandler _remoteButtonHandler;

    #endregion Variables

    #region Implementation

    /// <summary>
    /// Name of the IR Server plugin.
    /// </summary>
    /// <value>The name.</value>
    public override string Name
    {
      get { return "RC102"; }
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
      get { return "Support for RC102 remote receiver"; }
    }

    /// <summary>
    /// Gets a display icon for the plugin.
    /// </summary>
    /// <value>The icon.</value>
    public override Icon DeviceIcon
    {
      get { return Resources.Icon; }
    }

    /// <summary>
    /// Callback for remote button presses.
    /// </summary>
    /// <value>The remote callback.</value>
    public RemoteHandler RemoteCallback
    {
      get { return _remoteButtonHandler; }
      set { _remoteButtonHandler = value; }
    }

    /// <summary>
    /// Detect the presence of this device.
    /// </summary>
    public override DetectionResult Detect()
    {
      try
      {
        Guid guid = new Guid();
        Win32.HidD_GetHidGuid(ref guid);

        string devicePath = FindDevice(guid);

        if (devicePath != null)
        {
          return DetectionResult.DevicePresent;
        }
      }
      catch (Win32Exception ex)
      {
        if (ex.NativeErrorCode != 13)
        {
          IrssLog.Error("{0,15}: exception {1}|{2}", Name, ex.NativeErrorCode, ex.Message);
          return DetectionResult.DeviceException;
        }
        IrssLog.Debug("{0,15}: exception {1} | {2} | {3}", Name, ex.NativeErrorCode, ex.Message, ex.GetType());
      }
      catch (Exception ex)
      {
        IrssLog.Error("{0,15}: exception {1} type: {2}", Name, ex.Message, ex.GetType());
        return DetectionResult.DeviceException;
      }

      return DetectionResult.DeviceNotFound;
    }

    /// <summary>
    /// Start the IR Server plugin.
    /// </summary>
    public override void Start()
    {
      Guid guid = new Guid();
      Win32.HidD_GetHidGuid(ref guid);

      string devicePath = FindDevice(guid);
      if (String.IsNullOrEmpty(devicePath))
        throw new InvalidOperationException("Device not found");

      SafeFileHandle deviceHandle = Win32.CreateFile(devicePath, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open,
                                               Win32.EFileAttributes.Overlapped, IntPtr.Zero);
      int lastError = Marshal.GetLastWin32Error();

      if (deviceHandle.IsInvalid)
        throw new Win32Exception(lastError, "Failed to open device");

      // TODO: Add device removal notification.
      //_deviceWatcher.RegisterDeviceRemoval(deviceHandle);

      _deviceBuffer = new byte[DeviceBufferSize];
      _deviceStream = new FileStream(deviceHandle, FileAccess.Read, _deviceBuffer.Length, true);
      _deviceStream.BeginRead(_deviceBuffer, 0, _deviceBuffer.Length, OnReadComplete, null);
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
      if (_deviceStream == null)
        return;

      try
      {
        _deviceStream.Dispose();
      }
      catch (IOException)
      {
        // we are closing the stream so ignore this
      }
      finally
      {
        _deviceStream = null;
      }
    }

    /// <summary>
    /// Finds the device.
    /// </summary>
    /// <param name="classGuid">The class GUID.</param>
    /// <returns>Device path.</returns>
    private static string FindDevice(Guid classGuid)
    {
      // 0x12 = DIGCF_PRESENT | DIGCF_DEVICEINTERFACE
      IntPtr handle = Win32.SetupDiGetClassDevs(ref classGuid, 0, IntPtr.Zero, 0x12);
      int lastError = Marshal.GetLastWin32Error();

      if (handle.ToInt32() == -1)
        throw new Win32Exception(lastError);

      string devicePath = null;

      for (int deviceIndex = 0; ; deviceIndex++)
      {
        Win32.DeviceInfoData deviceInfoData = new Win32.DeviceInfoData();
        deviceInfoData.Size = Marshal.SizeOf(deviceInfoData);

        if (!Win32.SetupDiEnumDeviceInfo(handle, deviceIndex, ref deviceInfoData))
        {
          // out of devices or do we have an error?
          lastError = Marshal.GetLastWin32Error();
          if (lastError != 0x0103 && lastError != 0x007E)
          {
            Win32.SetupDiDestroyDeviceInfoList(handle);
            throw new Win32Exception(Marshal.GetLastWin32Error());
          }

          Win32.SetupDiDestroyDeviceInfoList(handle);
          break;
        }

        Win32.DeviceInterfaceData deviceInterfaceData = new Win32.DeviceInterfaceData();
        deviceInterfaceData.Size = Marshal.SizeOf(deviceInterfaceData);

        if (!Win32.SetupDiEnumDeviceInterfaces(handle, ref deviceInfoData, ref classGuid, 0, ref deviceInterfaceData))
        {
          Win32.SetupDiDestroyDeviceInfoList(handle);
          throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        uint cbData = 0;

        if (!Win32.SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, IntPtr.Zero, 0, ref cbData, IntPtr.Zero) && cbData == 0)
        {
          Win32.SetupDiDestroyDeviceInfoList(handle);
          throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        Win32.DeviceInterfaceDetailData deviceInterfaceDetailData = new Win32.DeviceInterfaceDetailData();
        deviceInterfaceDetailData.Size = 5;

        if (!Win32.SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, ref deviceInterfaceDetailData, cbData,
                                          IntPtr.Zero, IntPtr.Zero))
        {
          Win32.SetupDiDestroyDeviceInfoList(handle);
          throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (deviceInterfaceDetailData.DevicePath.IndexOf(DeviceID, StringComparison.OrdinalIgnoreCase) != -1)
        {
          Win32.SetupDiDestroyDeviceInfoList(handle);
          devicePath = deviceInterfaceDetailData.DevicePath;
          break;
        }
      }

      return devicePath;
    }

    private void OnReadComplete(IAsyncResult asyncResult)
    {
      try
      {
        if (_deviceStream.EndRead(asyncResult) == DeviceBufferSize && _deviceBuffer[1] == 0)
        {
          TimeSpan timeSpan = DateTime.Now - _lastCodeTime;

          int keyCode = _deviceBuffer[2];

          if (keyCode != _lastCode || timeSpan.Milliseconds > 250)
          {
            if (_remoteButtonHandler != null)
              _remoteButtonHandler(Name, keyCode.ToString());

            _lastCodeTime = DateTime.Now;
          }

          _lastCode = keyCode;
        }

        _deviceStream.BeginRead(_deviceBuffer, 0, _deviceBuffer.Length, OnReadComplete, null);
      }
      catch (Exception)
      {
      }
    }

    #endregion Implementation
  }
}