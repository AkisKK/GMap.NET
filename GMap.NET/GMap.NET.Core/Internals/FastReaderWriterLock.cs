#if !MONO
#define UseFastResourceLock
#endif
using System;
using System.Runtime.InteropServices;

namespace GMap.NET.Internals;

#if !MONO
#endif

/// <summary>
///     custom ReaderWriterLock
///     in Vista and later uses integrated Slim Reader/Writer (SRW) Lock
///     http://msdn.microsoft.com/en-us/library/aa904937(VS.85).aspx
///     http://msdn.microsoft.com/en-us/magazine/cc163405.aspx#S2
/// </summary>
public sealed class FastReaderWriterLock : IDisposable
{
#if !MONO
    private static class NativeMethods
    {
        // Methods
        [DllImport("Kernel32", ExactSpelling = true)]
        internal static extern void AcquireSRWLockExclusive(ref IntPtr srw);

        [DllImport("Kernel32", ExactSpelling = true)]
        internal static extern void AcquireSRWLockShared(ref IntPtr srw);

        [DllImport("Kernel32", ExactSpelling = true)]
        internal static extern void InitializeSRWLock(out IntPtr srw);

        [DllImport("Kernel32", ExactSpelling = true)]
        internal static extern void ReleaseSRWLockExclusive(ref IntPtr srw);

        [DllImport("Kernel32", ExactSpelling = true)]
        internal static extern void ReleaseSRWLockShared(ref IntPtr srw);
    }

    IntPtr m_LockSRW = IntPtr.Zero;

    public FastReaderWriterLock()
    {
        if (m_UseNativeSRWLock)
        {
            NativeMethods.InitializeSRWLock(out m_LockSRW);
        }
        else
        {
#if UseFastResourceLock
            m_PLock = new FastResourceLock();
#endif
        }
    }

#if UseFastResourceLock
    ~FastReaderWriterLock()
    {
        Dispose(false);
    }

    void Dispose(bool _)
    {
        if (m_PLock != null)
        {
            m_PLock.Dispose();
            m_PLock = null;
        }
    }

    FastResourceLock m_PLock;
#endif

    static readonly bool
        m_UseNativeSRWLock =
            Stuff.IsRunningOnVistaOrLater() &&
            IntPtr.Size == 4; // works only in 32-bit mode, any ideas on native 64-bit support? 

#endif

#if !UseFastResourceLock
   Int32 busy = 0;
   Int32 readCount = 0;
#endif

    public void AcquireReaderLock()
    {
#if !MONO
        if (m_UseNativeSRWLock)
        {
            NativeMethods.AcquireSRWLockShared(ref m_LockSRW);
        }
        else
#endif
        {
#if UseFastResourceLock
            m_PLock.AcquireShared();
#else
        Thread.BeginCriticalRegion();

        while(Interlocked.CompareExchange(ref busy, 1, 0) != 0)
        {
           Thread.Sleep(1);
        }

        Interlocked.Increment(ref readCount);

        // somehow this fix deadlock on heavy reads
        Thread.Sleep(0);
        Thread.Sleep(0);
        Thread.Sleep(0);
        Thread.Sleep(0);
        Thread.Sleep(0);
        Thread.Sleep(0);
        Thread.Sleep(0);

        Interlocked.Exchange(ref busy, 0);
#endif
        }
    }

    public void ReleaseReaderLock()
    {
#if !MONO
        if (m_UseNativeSRWLock)
        {
            NativeMethods.ReleaseSRWLockShared(ref m_LockSRW);
        }
        else
#endif
        {
#if UseFastResourceLock
            m_PLock.ReleaseShared();
#else
        Interlocked.Decrement(ref readCount);
        Thread.EndCriticalRegion();
#endif
        }
    }

    public void AcquireWriterLock()
    {
        try
        {
#if !MONO
            if (m_UseNativeSRWLock)
            {
                NativeMethods.AcquireSRWLockExclusive(ref m_LockSRW);
            }
            else
#endif
            {
#if UseFastResourceLock
                m_PLock.AcquireExclusive();
#else
                Thread.BeginCriticalRegion();

                while(Interlocked.CompareExchange(ref busy, 1, 0) != 0)
                {
                   Thread.Sleep(1);
                }

                while(Interlocked.CompareExchange(ref readCount, 0, 0) != 0)
                {
                   Thread.Sleep(1);
                }
#endif
            }
        }
        catch (Exception) { }
    }

    public void ReleaseWriterLock()
    {
        try
        {
#if !MONO
            if (m_UseNativeSRWLock)
            {
                NativeMethods.ReleaseSRWLockExclusive(ref m_LockSRW);
            }
            else
#endif
            {
#if UseFastResourceLock
                m_PLock.ReleaseExclusive();
#else
                Interlocked.Exchange(ref busy, 0);
                Thread.EndCriticalRegion();
#endif
            }
        }
        catch (Exception) { }
    }

    #region IDisposable Members

    public void Dispose()
    {
#if UseFastResourceLock
        Dispose(true);
        GC.SuppressFinalize(this);
#endif
    }

    #endregion
}
