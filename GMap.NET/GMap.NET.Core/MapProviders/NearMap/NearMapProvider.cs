using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.NearMap;

/// <summary>
///     http://en.wikipedia.org/wiki/NearMap
///     NearMap originally allowed personal use of images for free for non-enterprise users.
///     However this free access ended in December 2012, when the company modified its business model to user-pay
/// </summary>
public abstract class NearMapProviderBase : GMapProvider
{
    public NearMapProviderBase()
    {
        // credentials doesn't work ;/
        //Credential = new NetworkCredential("-", "-");

        //try ForceBasicHttpAuthentication(...);
    }

    #region GMapProvider Members

    public override Guid Id => throw new NotImplementedException();

    public override string Name => throw new NotImplementedException();

    public override PureProjection Projection => MercatorProjection.Instance;

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

    public static new int GetServerNum(GPoint pos, int max)
    {
        // var hostNum=((opts.nodes!==0)?((tileCoords.x&2)%opts.nodes):0)+opts.nodeStart;
        return (int)(pos.X & 2) % max;
    }

    static readonly string m_SecureStr = "Vk52edzNRYKbGjF8Ur0WhmQlZs4wgipDETyL1oOMXIAvqtxJBuf7H36acCnS9P";

    public static string GetSafeString(GPoint pos)
    {
        #region -- source --

        /*
        TileLayer.prototype.differenceEngine=function(s,a)
        {
            var offset=0,result="",aLen=a.length,v,p;
            for(var i=0; i<aLen; i++)
            {
                v=parseInt(a.charAt(i),10);
                if(!isNaN(v))
                {
                    offset+=v;
                    p=s.charAt(offset%s.length);
                    result+=p
                }             
            }
            return result
        };    
      
        TileLayer.prototype.getSafeString=function(x,y,nmd)
        {
             var arg=x.toString()+y.toString()+((3*x)+y).toString();
             if(nmd)
             {
                arg+=nmd
             }
             return this.differenceEngine(TileLayer._substring,arg)
        };  
       */

        #endregion

        string arg = pos.X.ToString() + pos.Y.ToString() + (3 * pos.X + pos.Y).ToString();

        string ret = "&s=";
        int offset = 0;
        for (int i = 0; i < arg.Length; i++)
        {
            offset += int.Parse(arg[i].ToString());
            ret += m_SecureStr[offset % m_SecureStr.Length];
        }

        return ret;
    }
}

/// <summary>
///     NearMap provider - http://www.nearmap.com/
/// </summary>
public class NearMapProvider : NearMapProviderBase
{
    public static readonly NearMapProvider Instance;

    NearMapProvider()
    {
        ReferrerUrl = "http://www.nearmap.com/";
    }

    static NearMapProvider()
    {
        Instance = new NearMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("E33803DF-22CB-4FFA-B8E3-15383ED9969D");

    public override string Name { get; } = "NearMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        return string.Format(m_UrlFormat, GetServerNum(pos, 3), pos.X, pos.Y, zoom, GetSafeString(pos));
    }

    static readonly string m_UrlFormat = "http://web{0}.nearmap.com/kh/v=nm&hl=en&x={1}&y={2}&z={3}&nml=Map_{4}";
}
