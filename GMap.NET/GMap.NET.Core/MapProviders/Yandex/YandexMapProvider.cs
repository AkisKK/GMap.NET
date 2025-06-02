using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Yandex;

public abstract class YandexMapProviderBase : GMapProvider
{
    #region GMapProvider Members

    public override Guid Id => throw new NotImplementedException();

    public override string Name => throw new NotImplementedException();

    public override PureProjection Projection => MercatorProjectionYandex.Instance;

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [this];

            return m_Overlays;
        }
    }

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        throw new NotImplementedException();
    }

    #endregion

    protected string Version = "4.6.9";

    public readonly string Server /*d{'_'}b*/ = /*{^_^}*/
        Stuff.GString /*{"_"}*/( /* ;}~ */"MECxW6okUK3Ir7a9ue/vIA==" /* ;}~ */);

    public readonly string ServerRu /*d{'_'}b*/ = /*{^_^}*/
        Stuff.GString /*{"_"}*/( /* ;}~ */"MECxW6okUK0FRlRPbF0BQg==" /* ;}~ */);

    public readonly string ServerCom /*d{'_'}b*/ = /*{^_^}*/
        Stuff.GString /*{"_"}*/( /* ;}~ */"MECxW6okUK2JNHOW5AuimA==" /* ;}~ */);
}

/// <summary>
///     YenduxMap provider
/// </summary>
public class YandexMapProvider : YandexMapProviderBase
{
    public static readonly YandexMapProvider Instance;

    YandexMapProvider()
    {
        ReferrerUrl = "http://" + ServerCom + "/";
    }

    static YandexMapProvider()
    {
        Instance = new YandexMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("82DC969D-0491-40F3-8C21-4D90B67F47EB");

    public override string Name { get; } = "YandexMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom, LanguageStr);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom, string language)
    {
        return string.Format(m_UrlFormat,
                             m_UrlServer,
                             GetServerNum(pos, 4) + 1,
                             Version,
                             pos.X,
                             pos.Y,
                             zoom,
                             language,
                             Server);
    }

    static readonly string m_UrlServer = "vec";
    static readonly string m_UrlFormat = "http://{0}0{1}.{7}/tiles?l=map&v={2}&x={3}&y={4}&z={5}&lang={6}";
}
