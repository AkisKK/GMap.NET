﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GMap.NET.Internals;

/// <summary>
///     etc functions...
/// </summary>
internal class Stuff
{
    public static string EnumToString(Enum value)
    {
        var fi = value.GetType().GetField(value.ToString());
        var attributes =
            (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

        return attributes.Length > 0 ? attributes[0].Description : value.ToString();
    }

    [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
    [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int x, int y);

    public static readonly Random Random = new();

    public static void Shuffle<T>(List<T> deck)
    {
        int n = deck.Count;

        for (int i = 0; i < n; ++i)
        {
            int r = i + Random.Next(n - i);
            (deck[i], deck[r]) = (deck[r], deck[i]);
        }
    }

    public static MemoryStream CopyStream(Stream inputStream, bool seekOriginBegin)
    {
        const int readSize = 32 * 1024;
        byte[] buffer = new byte[readSize];
        var ms = new MemoryStream();
        {
            int count;
            while ((count = inputStream.Read(buffer, 0, readSize)) > 0)
            {
                ms.Write(buffer, 0, count);
            }
        }

        if (seekOriginBegin)
        {
            inputStream.Seek(0, SeekOrigin.Begin);
        }

        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    public static bool IsRunningOnVistaOrLater()
    {
        var os = Environment.OSVersion;

        if (os.Platform == PlatformID.Win32NT)
        {
            var vs = os.Version;

            if (vs.Major >= 6 && vs.Minor >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsRunningOnWin7OrLater()
    {
        var os = Environment.OSVersion;

        if (os.Platform == PlatformID.Win32NT)
        {
            var vs = os.Version;

            if (vs.Major >= 6 && vs.Minor > 0)
            {
                return true;
            }
        }

        return false;
    }

    public static void RemoveInvalidPathSymbols(ref string url)
    {
        char[] ilg = Path.GetInvalidFileNameChars();
        foreach (char c in ilg)
        {
            url = url.Replace(c, '_');
        }
    }

    #region -- encryption --

    static string EncryptString(string message, string passphrase)
    {
        byte[] results;

        using var hashProvider = SHA1.Create();
        byte[] tdesKey = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(passphrase));
        Array.Resize(ref tdesKey, 16);

        using var tdesAlgorithm = TripleDES.Create();
        tdesAlgorithm.Key = tdesKey;
        tdesAlgorithm.Mode = CipherMode.ECB;
        tdesAlgorithm.Padding = PaddingMode.PKCS7;

        byte[] dataToEncrypt = Encoding.UTF8.GetBytes(message);

        // Step 5. Attempt to encrypt the string
        try
        {
            using var encryptor = tdesAlgorithm.CreateEncryptor();
            results = encryptor.TransformFinalBlock(dataToEncrypt, 0, dataToEncrypt.Length);
        }
        finally
        {
            // Clear the TripleDes and Hash provider services of any sensitive information
            tdesAlgorithm.Clear();
            hashProvider.Clear();
        }

        // Step 6. Return the encrypted string as a base64 encoded string
        return Convert.ToBase64String(results);
    }

    static string DecryptString(string message, string passphrase)
    {
        byte[] results;

        using var hashProvider = SHA1.Create();
        byte[] tdesKey = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(passphrase));
        Array.Resize(ref tdesKey, 16);

        // Step 2. Create a new TripleDESCryptoServiceProvider object
        using var tdesAlgorithm = TripleDES.Create();
        // Step 3. Setup the decoder
        tdesAlgorithm.Key = tdesKey;
        tdesAlgorithm.Mode = CipherMode.ECB;
        tdesAlgorithm.Padding = PaddingMode.PKCS7;

        // Step 4. Convert the input string to a byte[]
        byte[] dataToDecrypt = Convert.FromBase64String(message);

        // Step 5. Attempt to decrypt the string
        try
        {
            using var decryptor = tdesAlgorithm.CreateDecryptor();
            results = decryptor.TransformFinalBlock(dataToDecrypt, 0, dataToDecrypt.Length);
        }
        finally
        {
            // Clear the TripleDes and HashProvider services of any sensitive information
            tdesAlgorithm.Clear();
            hashProvider.Clear();
        }

        // Step 6. Return the decrypted string in UTF8 format
        return Encoding.UTF8.GetString(results, 0, results.Length);
    }

    public static string EncryptString(string message)
    {
        return EncryptString(message, m_Manifesto);
    }

    public static string GString(string message)
    {
        string ret = DecryptString(message, m_Manifesto);

        return ret;
    }

    static readonly string m_Manifesto =
        "GMap.NET is great and Powerful, Free, cross platform, open source .NET control.";

    #endregion
}
