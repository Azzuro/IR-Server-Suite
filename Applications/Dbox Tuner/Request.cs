#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace DboxTuner
{

  /// <summary>
  /// Encapsulates an http request for communicating with the DreamBox.
  /// </summary>
  public class Request
  {

    #region Variables

    string _Url       = String.Empty;
    string _UserName  = String.Empty;
    string _Password  = String.Empty;

    #endregion Variables

    #region Constructor

    public Request(string url, string username, string password)
    {
      _Url      = url;
      _UserName = username;
      _Password = password;
    }

    #endregion Constructor

    public string PostData(string command, params int[] optTimeout)
    {
      try
      {
        int timeout = 2000;
        if (optTimeout.GetLength(0) != 0)
        {
          timeout = optTimeout[0];
        }

        Uri uri = new Uri(_Url + command);
        WebRequest request = WebRequest.Create(uri);
        request.Credentials = new NetworkCredential(_UserName, _Password);
        request.Timeout = timeout;
        string encodemap = "iso-8859-1";

        if (Data._boxtype == "Enigma v1" || Data._boxtype == "Enigma v2")
          encodemap = "utf-8";

        WebResponse response = request.GetResponse();
        Stream receiveStream = response.GetResponseStream();

        // back to iso encoding sorry , should work anywhere in EU
        // which it doesn't, because dreambox use utf-8 encoding, making all UTF-8 extended characters a multibyte garble if we encode those to iso
        Encoding encode = System.Text.Encoding.GetEncoding(encodemap);
        StreamReader reader = new StreamReader(receiveStream, encode);
        string str = reader.ReadToEnd();

        return str;
      }
      catch
      {
        return String.Empty;
      }
    }

  }

}
