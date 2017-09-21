﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq;
using System.Text;
using DOGPlatform.XML;
using System.IO;
using System.Xml;

namespace DOGPlatform.SVG
{
    class cSVGSectionTrackOilGrade
    {

        public static XmlElement gTrackOilGrade(XmlDocument svgDoc, XmlElement svgDefs, ItemTrackDrawDataIntervalProperty item, double fVScale, int iTrackwidth)
        {
            return gTrackOilGrade(svgDoc, svgDefs, item.sID, item.top, item.bot, fVScale, item.sProperty, iTrackwidth);
        }
        public static XmlElement gTrackItemTVDOilGrade(XmlDocument svgDoc, XmlElement svgDefs, ItemTrackDrawDataIntervalProperty item, double fVScale, int iTrackwidth)
        {
            return gTrackOilGrade(svgDoc, svgDefs, item.sID, item.topTVD, item.botTVD, fVScale, item.sProperty, iTrackwidth);
        }
        public static XmlElement gTrackOilGrade(XmlDocument svgDoc, XmlElement svgDefs, string sID, double fTop, double fBot, double fVScale, string sCodeInput, int iTrackwidth)
        {
            fTop *= fVScale;
            fBot *= fVScale;
            string sPatternCode = sCodeInput;
            XmlElement gRect = svgDoc.CreateElement("rect");
            gRect.SetAttribute("onclick", "getID(evt)");
            gRect.SetAttribute("id", sID);
            gRect.SetAttribute("x", "0");
            gRect.SetAttribute("y", fTop.ToString());
            gRect.SetAttribute("width", (iTrackwidth).ToString());
            gRect.SetAttribute("height", (fBot - fTop).ToString());
            gRect.SetAttribute("style", "stroke-width:0.5");
            gRect.SetAttribute("stroke", "black");
            //从目录中查找包含pattern的svg原文件,文件名和sLithoID一致，svgdefs增加定义。
            string filePathPatternSVG = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pattern", "hyjb", sPatternCode + ".svg");
            if (File.Exists(filePathPatternSVG))
            {
                cSVGoperate.addSVGpatternDefs(svgDoc, svgDefs, sPatternCode, filePathPatternSVG);
                gRect.SetAttribute("fill", string.Format("url(#{0})", sPatternCode));
            }
            else
            {
                gRect.SetAttribute("fill", "white");
            }
            return gRect;
        }
    }
}
