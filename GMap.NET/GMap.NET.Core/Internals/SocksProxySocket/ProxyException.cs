﻿/*
    Copyright © 2002, The KPD-Team
    All rights reserved.
    http://www.mentalis.org/

  Redistribution and use in source and binary forms, with or without
  modification, are permitted provided that the following conditions
  are met:

    - Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer. 

    - Neither the name of the KPD-Team, nor the names of its contributors
       may be used to endorse or promote products derived from this
       software without specific prior written permission. 

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
  FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
  THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
  STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
  OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;

namespace GMap.NET.Internals.SocksProxySocket;

/// <summary>
///     The exception that is thrown when a proxy error occurs.
/// </summary>
internal class ProxyException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the ProxyException class.
    /// </summary>
    public ProxyException() : this("An error occurred while talking to the proxy server.") { }

    /// <summary>
    ///     Initializes a new instance of the ProxyException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ProxyException(string message) : base(message) { }

    /// <summary>
    ///     Initializes a new instance of the ProxyException class.
    /// </summary>
    /// <param name="socks5Error">The error number returned by a SOCKS5 server.</param>
    public ProxyException(int socks5Error) : this(Socks5ToString(socks5Error)) { }

    /// <summary>
    ///     Converts a SOCKS5 error number to a human readable string.
    /// </summary>
    /// <param name="socks5Error">The error number returned by a SOCKS5 server.</param>
    /// <returns>A string representation of the specified SOCKS5 error number.</returns>
    public static string Socks5ToString(int socks5Error)
    {
        return socks5Error switch
        {
            0 => "Connection succeeded.",
            1 => "General SOCKS server failure.",
            2 => "Connection not allowed by rule set.",
            3 => "Network unreachable.",
            4 => "Host unreachable.",
            5 => "Connection refused.",
            6 => "TTL expired.",
            7 => "Command not supported.",
            8 => "Address type not supported.",
            _ => "Unspecified SOCKS error.",
        };
    }
}
