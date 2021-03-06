// Copyright 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using GeoAPI.Geometries;

namespace AjaxMap
{
    /// <summary>
    /// The Ajax Map Control is a javascript controlled map that is able to refresh
    /// the map without the whole webpage has to do a roundtrip to the server.
    /// </summary>
    /// <remarks>
    /// <para>This webcontrol is tested with both IE and FireFox.</para>
    /// <para>The webcontrol creates a client-side javascript object named after
    /// the ClientID of this control
    /// and appends "Obj" to it. Below are a list of some of the properties
    /// and methods of the client-side object. The <see cref="OnViewChanging"/> 
    /// and <see cref="OnViewChange"/> client-side events
    /// are also is parsing a reference to this object.</para>
    /// <list type="table">
    /// <listheader><term>Method/Property</term><description>Description</description></listheader>
    /// <item><term>.minX</term><description>World coordinate of the left side of the current view</description></item>
    /// <item><term>.maxY</term><description>World coordinate of the top of the current view</description></item>
    /// <item><term>.GetCenter()</term><description>Gets a center point object with the current view (use the .x and .y properties of the returned object for the coordinates)</description></item>
    /// <item><term>.zoom</term><description>The current zoom level of the map (map width)</description></item>
    /// <item><term>.zoomAmount</term><description>The amount to zoom on a zoom-in event (negative values equals zoom out)</description></item>
    /// <item><term>.container</term><description>Reference to the map box element</description></item>
    /// <item><term>.statusbar</term><description>Reference to the statusbar element</description></item>
    /// </list>
    /// </remarks>
    [DefaultProperty("Map")]
    [ToolboxData("<{0}:AjaxMapControl runat=\"server\"></{0}:AjaxMapControl>")]
    [Designer("AjaxMap.AjaxMapControlDesigner", "System.ComponentModel.Design.IDesigner")]
    public class AjaxMapControl : System.Web.UI.WebControls.CompositeControl, INamingContainer, ICallbackEventHandler
    {
        internal static System.Globalization.NumberFormatInfo numberFormat_EnUS = new System.Globalization.CultureInfo("en-US", false).NumberFormat;

        SharpMap.Map map;
        private System.Web.UI.WebControls.Image imgMap1;
        private System.Web.UI.WebControls.Image imgMap2;
        private HtmlGenericControl spanCursorLocation;
        private HtmlGenericControl divTopBar;
        private HtmlGenericControl divScaleBar;
        private HtmlGenericControl divScaleFrame;
        private HtmlGenericControl divScaleText;
        private HtmlGenericControl divZoomLayer;
        private HtmlGenericControl divCanvas;
        private HtmlGenericControl divMeasure;
        private string hiddenLayers;

        private int _ZoomSpeed;

        /// <summary>
        /// Sets the speed which the zoom is (lower = faster).
        /// The default value is 15
        /// </summary>
        public int ZoomSpeed
        {
            get { return _ZoomSpeed; }
            set { _ZoomSpeed = value; }
        }

        private int _FadeSpeed;

        /// <summary>
        /// Sets the speed of the fade (lower = faster).
        /// The default value is 10
        /// </summary>
        public int FadeSpeed
        {
            get { return _FadeSpeed; }
            set { _FadeSpeed = value; }
        }

        private string _OnViewChange;

        /// <summary>
        /// Client-side method to call when map view have changed
        /// </summary>
        [Bindable(false)]
        [Category("Behavior")]
        [DefaultValue("")]
        public string OnViewChange
        {
            get { return _OnViewChange; }
            set { _OnViewChange = value; }
        }

        private string _OnViewChanging;

        /// <summary>
        /// Client-side method to call when map are starting to update
        /// </summary>
        [Bindable(false)]
        [Category("Behavior")]
        [DefaultValue("")]
        public string OnViewChanging
        {
            get { return _OnViewChanging; }
            set { _OnViewChanging = value; }
        }

        private string _OnClickEvent;

        /// <summary>
        /// Gets or sets the clientside method to call when custom click-event is active.
        /// </summary>
        [Bindable(false)]
        [Category("Behavior")]
        [DefaultValue("")]
        [Description("Clientside method to call when custom click-event is active")]
        public string OnClickEvent
        {
            get { return _OnClickEvent; }
            set { _OnClickEvent = value; }
        }

        /// <summary>
        /// Gets the name of the clientside ClickEvent property on the map object.
        /// </summary>
        public string ClickEventPropertyName
        {
            get { return this.ClientID + "Obj.clickEvent"; }
        }

        /// <summary>
        /// Gets the name of the clientside ToggleClickEvent method to enable or disable 
        /// the custom click-event on the map object.
        /// </summary>
        public string ToggleClickEventMethodName
        {
            get { return this.ClientID + "Obj.toggleClickEvent"; }
        }
        /// <summary>
        /// Gets the name of the clientside DisableClickEvent method to disable 
        /// the custom click-event on the map object.
        /// </summary>
        public string DisableClickEventMethodName
        {
            get { return this.ClientID + "Obj.disableClickEvent"; }
        }
        /// <summary>
        /// Gets the name of the clientside EnableClickEvent method to enable
        /// the custom click-event on the map object.
        /// </summary>
        public string EnableClickEventMethodName
        {
            get { return this.ClientID + "Obj.enableClickEvent"; }
        }

        private bool _UseCache;

        /// <summary>
        /// Sets whether the control should use the http cache or call a specific maphandler
        /// </summary>
        [Bindable(false)]
        [Category("Behavior")]
        [DefaultValue(false)]
        public bool UseCache
        {
            get { return _UseCache; }
            set { _UseCache = value; }
        }

        private string _StatusBarText = "[X], [Y] - Map width=[ZOOM]";

        /// <summary>
        /// Text shown on the map status bar.
        /// </summary>
        /// <remarks>
        /// <para>Use [X] and [Y] to display cursor position in world coordinates and [ZOOM] for displaying the zoom value.</para>
        /// <para>The default value is "[X], [Y] - Map width=[ZOOM]"</para>
        /// </remarks>
        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("[X], [Y] - Map width=[ZOOM]")]
        public string StatusBarText
        {
            get { return _StatusBarText; }
            set { _StatusBarText = value; }
        }

        private string _ResponseFormat = "myMapHandler.aspx?Width=[WIDTH]&Height=[HEIGHT]&Zoom=[ZOOM]&X=[X]&Y=[Y]&Layers=[LAYERS]";

        /// <summary>
        /// Formatting of the callback response used when <see cref="UseCache"/> is false.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use [X] and [Y] for center position, [ZOOM] for zoom value,
        /// [WIDTH] for image width and [WIDTH] for image height. These values will automatically
        /// be replaced by the current values. The return-result should correspond to the url of
        /// a maphandler that renders the map from these values
        /// </para>
        /// <para>myMapHandler.aspx?Width=[WIDTH]&amp;Height=[HEIGHT]&amp;Zoom=[ZOOM]&amp;X=[X]&amp;Y=[Y]</para>
        /// </remarks>
        [Bindable(false)]
        [Category("Data")]
        [DefaultValue("myMapHandler.aspx?Width=[WIDTH]&Height=[HEIGHT]&Zoom=[ZOOM]&X=[X]&Y=[Y]&Layers=[LAYERS]")]
        public string ResponseFormat
        {
            get { return _ResponseFormat; }
            set { _ResponseFormat = value; }
        }

        private bool _DisplayStatusBar;
        /// <summary>
        /// Specifies whether the statusbar is visible or not.
        /// </summary>
        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue(true)]
        public bool DisplayStatusBar
        {
            get { return _DisplayStatusBar; }
            set { _DisplayStatusBar = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AjaxMapControl"/>
        /// </summary>
        public AjaxMapControl()
        {
            ZoomSpeed = 15;
            FadeSpeed = 10;
            _DisplayStatusBar = true;
        }

        /// <summary>
        /// The <see cref="SharpMap.Map"/> that is to be rendered in the control
        /// </summary>
        [Bindable(false)]
        [Category("Data")]
        [DefaultValue("")]
        [Localizable(true)]
        public SharpMap.Map Map
        {
            get { return map;}
            set { map = value; }
        }

        #region ICallbackEventHandler Members

        private string callbackArg = "";

        /// <summary>
        /// Returns the result of the callback event that targets <see cref="AjaxMap.AjaxMapControl"/>
        /// </summary>
        /// <returns></returns>
        public string GetCallbackResult()
        {
            EnsureChildControls();
            if (callbackArg.Trim() == "") return String.Empty;
            string[] vals = callbackArg.Split(new char[] { ';' });
            try
            {
                map.Zoom = double.Parse(vals[2], numberFormat_EnUS);
                map.Center = new Coordinate(double.Parse(vals[0], numberFormat_EnUS), double.Parse(vals[1], numberFormat_EnUS));
                map.Size = new System.Drawing.Size(int.Parse(vals[3]), int.Parse(vals[4]));
                hiddenLayers = vals[5].ToString();
                return GenerateMap();
                //If you want to use the Cache for storing the map, instead of a maphandler,
                //uncomment the following lines, and comment the above return statement
                /*System.Drawing.Image img = map.GetMap();
                string imgID = SharpMap.Web.Caching.InsertIntoCache(1, img);
                return "getmap.aspx?ID=" + HttpUtility.UrlEncode(imgID);*/
            }
            catch { return String.Empty; }
        }

        /// <summary>
        /// Creates the arguments for the callback handler in the
        /// <see cref="System.Web.UI.ClientScriptManager.GetCallbackEventReference(System.Web.UI.Control,string,string,string)"/> method. 
        /// </summary>
        /// <param name="eventArgument"></param>
        public void RaiseCallbackEvent(string eventArgument)
        {
            callbackArg = eventArgument;
        }

        /// <summary>
        /// Sends server control content to a provided HtmlTextWriter object, which writes the content to be rendered on the client.
        /// </summary>
        /// <param name="writer">The HtmlTextWriter object that receives the server control content.</param>
        protected override void Render(HtmlTextWriter writer)
        {
            base.Render(writer);
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use
        /// composition-based implementation to create any child controls they
        /// contain in preparation for posting back or rendering
        /// </summary>
        protected override void CreateChildControls()
        {
            if (!Page.IsCallback)
            {
                GenerateMapBox();
                GenerateClientScripts();
            }
            //base.CreateChildControls();
        }

        /// <summary>
        /// Returns a Url to the map
        /// </summary>
        private string GenerateMap()
        {
            if (_UseCache)
            {
                System.Drawing.Image img = Map.GetMap();
                string imgID = SharpMap.Web.Caching.InsertIntoCache(1, img);
                return "getmap.aspx?ID=" + HttpUtility.UrlEncode(imgID);
            }
            else
            {
                string response = _ResponseFormat.Replace("[WIDTH]", map.Size.Width.ToString()).
                                      Replace("[HEIGHT]", map.Size.Height.ToString()).
                                      Replace("[ZOOM]", map.Zoom.ToString(numberFormat_EnUS)).
                                      Replace("[X]", map.Center.X.ToString(numberFormat_EnUS)).
                                      Replace("[Y]", map.Center.Y.ToString(numberFormat_EnUS)).
                                      Replace("[LAYERS]", hiddenLayers);
                return response;
            }
        }
        /// <summary>
        /// Registers the client-side scripts and creates an initialize script for the current map
        /// </summary>
        private void GenerateClientScripts()
        {
            //Include scriptresources
            string wz_scriptLocation = Page.ClientScript.GetWebResourceUrl(this.GetType(), "AjaxMap.clientscripts.wz_jsgraphics.js");
            Page.ClientScript.RegisterClientScriptInclude("AjaxMap.clientscripts.wz_jsgraphics.js", wz_scriptLocation);
            string scriptLocation = Page.ClientScript.GetWebResourceUrl(this.GetType(), "AjaxMap.clientscripts.AjaxMap.js");
            Page.ClientScript.RegisterClientScriptInclude("AjaxMap.AjaxMap.js", scriptLocation);

            string obj = this.ClientID + "Obj";
            string newline = Environment.NewLine;
            string setvarsScript = "SetVars_" + this.ClientID + "();" + newline +
                "function SetVars_" + this.ClientID + "() {" + newline +
                obj + " = SharpMap_Init('" + this.ClientID + "','"
                + imgMap1.ClientID + "','" + imgMap2.ClientID + "','" + (_DisplayStatusBar ? spanCursorLocation.ClientID : "") + "','" +
                (_DisplayStatusBar ? _StatusBarText : "") + "');" + newline;
            setvarsScript +=
                obj + ".zoom = " + map.Zoom.ToString(numberFormat_EnUS) + ";" + newline +
                obj + ".minX = " + map.Envelope.MinX.ToString(numberFormat_EnUS) + ";" + newline +
                obj + ".maxY = " + map.Center.Y.ToString(numberFormat_EnUS) + "+" + obj + ".zoom/" + obj + ".container.offsetWidth*" + obj + ".container.offsetHeight*0.5;" + newline +
                obj + ".defMinX = " + map.Envelope.MinX.ToString(numberFormat_EnUS) + ";" + newline +
                obj + ".defMaxY = " + map.Center.Y.ToString(numberFormat_EnUS) + "+" + obj + ".zoom/" + obj + ".container.offsetWidth*" + obj + ".container.offsetHeight*0.5;" + newline +
                obj + ".minZoom = " + map.MinimumZoom.ToString(numberFormat_EnUS) + ";" + newline +
                obj + ".maxZoom = " + map.MaximumZoom.ToString(numberFormat_EnUS) + ";" + newline +
                obj + ".zoomSpeed = " + this._ZoomSpeed.ToString() + ";" + newline +
                obj + ".fadeSpeed = " + this._FadeSpeed.ToString() + ";" + newline +
                obj + ".zoomLayer = WebForm_GetElementById('" + divZoomLayer.ClientID + "');" + newline +
                obj + ".statusbar = WebForm_GetElementById('" + spanCursorLocation.ClientID + "');" + newline +
                obj + ".measure = WebForm_GetElementById('" + divMeasure.ClientID + "');" + newline +
                obj + ".scalebar = WebForm_GetElementById('" + divScaleBar.ClientID + "');" + newline +
                obj + ".scaletext = WebForm_GetElementById('" + divScaleText.ClientID + "');" + newline;
            if (_UseCache)
                setvarsScript += obj + ".map.src = '" + this.GenerateMap() + "';" + newline;
            else
                setvarsScript += obj + ".map1.src = '" + _ResponseFormat.Replace("[WIDTH]", "'+" + obj + ".container.offsetWidth+'").
                                      Replace("[HEIGHT]", "'+" + obj + ".container.offsetHeight+'").
                                      Replace("[ZOOM]", "'+" + obj + ".zoom+'").
                                      Replace("[X]", map.Center.X.ToString(numberFormat_EnUS)).
                                      Replace("[Y]", map.Center.Y.ToString(numberFormat_EnUS)).
                                      Replace("[LAYERS]", "'+" + obj + ".hiddenLayers+'") + "';" + newline;
            if (_OnViewChange != null && _OnViewChange.Trim() != "")
                setvarsScript += obj + ".onViewChange = function() { " + _OnViewChange + "(" + obj + "); }" + newline;
            if (_OnViewChanging != null && _OnViewChanging.Trim() != "")
                setvarsScript += obj + ".onViewChanging = function() { " + _OnViewChanging + "(" + obj + "); }" + newline;

            setvarsScript += "}";

            //Register scripts in page
            ClientScriptManager cm = Page.ClientScript;
            //cm.RegisterClientScriptBlock(this.GetType(), "SetVars_" + this.ClientID, setvarsScript, true);
            cm.RegisterStartupScript(this.GetType(), "SetVars_" + this.ClientID, setvarsScript, true);
            //The following doesn't really do anything, but it cheats ASP.NET to include its callback scripts
            cm.GetCallbackEventReference(this, "SharpMap_MapOnClick(event,this)", "SharpMap_RefreshMap", "null", "SharpMap_AjaxOnError", true);

            //this.Controls.Add(new LiteralControl("<script type=\"text/javascript\">SetVars_" + this.ClientID + "();</script>\r\n"));      
        }

        private void GenerateMapBox()
        {
            this.Style.Add("overflow", "hidden");
            this.ID = "ajaxMap";
            this.Style.Add("z-index", "101");
            this.Style.Add("cursor", "pointer");
            this.Style.Add("position", "relative");
            this.Style.Add("display", "block");
            if (this.Style["BackColor"] != null)
                this.Style.Add("background", System.Drawing.ColorTranslator.ToHtml(map.BackColor));

            imgMap1 = new System.Web.UI.WebControls.Image();
            imgMap2 = new System.Web.UI.WebControls.Image();
            imgMap1.Attributes["galleryimg"] = "false"; //Disable Internet Explorer image toolbar
            imgMap2.Attributes["galleryimg"] = "false"; //Disable Internet Explorer image toolbar						

            imgMap1.Style.Add("position", "absolute");
            imgMap1.Style.Add("Z-index", "10");
            imgMap2.Style.Add("position", "absolute");
            imgMap2.Style.Add("visibility", "hidden");
            imgMap2.Style.Add("opacity", "0");
            imgMap2.Style.Add("mozopacity", "0");
            imgMap2.Style.Add("filter", "'ALPHA(opacity=0)'");
            imgMap2.Style.Add("Z-index", "9");

            this.Controls.Add(imgMap1);
            this.Controls.Add(imgMap2);

            if (_DisplayStatusBar)
            {

                spanCursorLocation = new HtmlGenericControl("span");
                spanCursorLocation.ID = "statusBar";
                spanCursorLocation.InnerText = "";
                spanCursorLocation.Style.Add("filter", "ALPHA(opacity=100)");
                divTopBar = new HtmlGenericControl("div");
                divTopBar.ID = "statusContainer";
                divTopBar.Style.Clear();
                divTopBar.Style.Add("Z-index", "20");
                divTopBar.Style.Add("border-bottom", "1px solid #000");
                divTopBar.Style.Add("position", "absolute");
                divTopBar.Style.Add("filter", "ALPHA(opacity=50)");
                divTopBar.Style.Add("opacity ", "0.5");
                divTopBar.Style.Add("background", "#fff");
                divTopBar.Style.Add("width", "100%");
                divTopBar.Controls.Add(spanCursorLocation);
                this.Controls.Add(divTopBar);
            }
            divScaleBar = new HtmlGenericControl("div");
            divScaleBar.ID = "scaleBar";
            divScaleBar.Style.Clear();
            divScaleBar.Style.Add("position", "relative");
            divScaleBar.Style.Add("height", "0.3em");
            divScaleBar.Style.Add("width", "12px");
            divScaleBar.Style.Add("overflow", "hidden");
            divScaleBar.Style.Add("border-bottom", "solid 1px #000");
            divScaleBar.Style.Add("border-right", "solid 1px #000");
            divScaleBar.Style.Add("border-left", "solid 1px #000");

            divScaleText = new HtmlGenericControl("div");
            divScaleText.ID = "scaleText";
            divScaleText.Style.Clear();
            divScaleText.Style.Add("position", "relative");
            divScaleText.InnerText = "5 mi";

            divScaleFrame = new HtmlGenericControl("div");
            divScaleFrame.ID = "scaleFrame";
            divScaleFrame.Style.Clear();
            divScaleFrame.Style.Add("position", "absolute");
            divScaleFrame.Style.Add("text-align", "center");
            divScaleFrame.Style.Add("background-color", "#fff");
            divScaleFrame.Style.Add("opacity ", "0.7");
            divScaleFrame.Style.Add("filter", "ALPHA(opacity=70)");
            divScaleFrame.Style.Add("Z-index", "21");
            divScaleFrame.Style.Add("padding", "0.3em");
            divScaleFrame.Style.Add("left", "60%");
            divScaleFrame.Style.Add("top", "95%");
            divScaleFrame.Controls.Add(divScaleBar);
            divScaleFrame.Controls.Add(divScaleText);
            this.Controls.Add(divScaleFrame);

            divZoomLayer = new HtmlGenericControl("div");
            divZoomLayer.ID = "zoomBox";
            divZoomLayer.Style.Clear();
            divZoomLayer.Style.Add("z-index", "22");
            divZoomLayer.Style.Add("border", "1px dashed #0000ff");
            divZoomLayer.Style.Add("background-color", "#ccccff");
            divZoomLayer.Style.Add("opacity ", "0.7");
            divZoomLayer.Style.Add("filter", "ALPHA(opacity=70)");
            divZoomLayer.Style.Add("overflow", "hidden");
            divZoomLayer.Style.Add("position", "absolute");
            divZoomLayer.Style.Add("line-height", "0px");
            divZoomLayer.Style.Add("visibility", "hidden");
            this.Controls.Add(divZoomLayer);

            divCanvas = new HtmlGenericControl("div");
            divCanvas.ID = "canvas";
            divCanvas.Style.Add("z-index", "23");
            divCanvas.Style.Add("position", "absolute");
            divCanvas.Style.Add("height", "100%");
            divCanvas.Style.Add("width", "100%");
            divCanvas.Controls.Add(new LiteralControl("<script type=\"text/javascript\">var jg = new jsGraphics(\"" + this.ClientID + "_" + divCanvas.ClientID + "\");</script>\r\n"));
            this.Controls.Add(divCanvas);

            divMeasure = new HtmlGenericControl("div");
            divMeasure.ID = "measure";
            divMeasure.Style.Clear();
            divMeasure.Style.Add("z-index", "24");
            divMeasure.Style.Add("position", "absolute");
            divMeasure.Style.Add("background-color", "#fff");
            divMeasure.Style.Add("visibility", "hidden");
            this.Controls.Add(divMeasure);
        }

        #endregion
    }
}
