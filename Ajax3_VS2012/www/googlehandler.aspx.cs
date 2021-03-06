using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BruTile.Web;

/// <summary>
/// The maphandler class takes a set of GET or POST parameters and returns a map as PNG (this reminds in many ways of the way a WMS server work).
/// Required parameters are: WIDTH, HEIGHT, ZOOM, X, Y, MAP
/// </summary>
public partial class maphandler : System.Web.UI.Page
{
	internal static System.Globalization.NumberFormatInfo numberFormat_EnUS = new System.Globalization.CultureInfo("en-US", false).NumberFormat;

    protected void Page_Load(object sender, EventArgs e)
	{
		int Width = 0;
		int Height = 0;
		double centerX = 0;
		double centerY = 0;
		double Zoom = 0;
        string[] Layer;

        //Parse request parameters
		if(!int.TryParse(Request.Params["WIDTH"],out Width))
			throw(new ArgumentException("Invalid parameter"));
		if(!int.TryParse(Request.Params["HEIGHT"], out Height))
			throw (new ArgumentException("Invalid parameter"));
		if(!double.TryParse(Request.Params["ZOOM"], System.Globalization.NumberStyles.Float, numberFormat_EnUS, out Zoom))
			throw (new ArgumentException("Invalid parameter"));
		if(!double.TryParse(Request.Params["X"], System.Globalization.NumberStyles.Float, numberFormat_EnUS, out centerX))
			throw (new ArgumentException("Invalid parameter"));
		if(!double.TryParse(Request.Params["Y"], System.Globalization.NumberStyles.Float, numberFormat_EnUS, out centerY))
			throw (new ArgumentException("Invalid parameter"));

		//Params OK
		SharpMap.Map map = InitializeMap(new System.Drawing.Size(Width, Height));
		
		map.Center = new GeoAPI.Geometries.Coordinate(centerX, centerY);
		map.Zoom = Zoom;

		//Generate map
		System.Drawing.Bitmap img = (System.Drawing.Bitmap) map.GetMap();

		//Stream the image to the client
		Response.ContentType = "image/png";
		System.IO.MemoryStream MS = new System.IO.MemoryStream();
		img.Save(MS, System.Drawing.Imaging.ImageFormat.Png);

		// tidy up  
		img.Dispose();
		byte[] buffer = MS.ToArray();
		Response.OutputStream.Write(buffer, 0, buffer.Length);

        
	}
  
	private SharpMap.Map InitializeMap(System.Drawing.Size size)
	{
		return MapHelper.InitializeGoogleMap(GoogleMapType.GoogleMap);
	
	}
}
