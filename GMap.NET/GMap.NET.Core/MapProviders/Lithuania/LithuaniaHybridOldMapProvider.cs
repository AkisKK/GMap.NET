﻿using System;

namespace GMap.NET.MapProviders.Lithuania;

/// <summary>
///     LithuaniaHybridOldMap, from 2010 data, provider
/// </summary>
public class LithuaniaHybridOldMapProvider : LithuaniaMapProviderBase
{
    public static readonly LithuaniaHybridOldMapProvider Instance;

    LithuaniaHybridOldMapProvider()
    {
    }

    static LithuaniaHybridOldMapProvider()
    {
        Instance = new LithuaniaHybridOldMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("35C5C685-E868-4AC7-97BE-10A9A37A81B5");

    public override string Name { get; } = "LithuaniaHybridMapOld";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??=
                [
                    LithuaniaOrtoFotoOldMapProvider.Instance,
                    LithuaniaHybridMapProvider.Instance
                ];

            return m_Overlays;
        }
    }

    #endregion
}
