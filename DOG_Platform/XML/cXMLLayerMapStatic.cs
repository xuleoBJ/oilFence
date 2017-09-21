﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using DOGPlatform.XML;
using DOGPlatform.SVG;
using System.Drawing;

namespace DOGPlatform.XML
{
    class ItemLayerWellPattern
    {
        public string sJH;
        public string sXCM;
        public double X;
        public double Y;
        public string sText;
        public string sProperty="0";
        public object obj;
        public ItemLayerWellPattern(string _sJH,string _sXCM)
        {
            this.sJH = _sJH;
            this.sXCM = _sXCM;
        }
        public ItemLayerWellPattern(string _sJH)
        {
            this.sJH = _sJH;
        }
        public ItemLayerWellPattern(XmlNode xn)
        {
            this.sJH = xn.Attributes["id"].Value;
            this.X = double.Parse(xn["X"].InnerText);
            this.Y  = double.Parse(xn["Y"].InnerText);
            if (xn["sText"] != null) this.sText = xn["sText"].InnerText;
            if (xn["sProperty"] != null) this.sProperty = xn["sProperty"].InnerText;
        }
    }


    class cXMLLayerMapStatic : cXmlDocLayer
    {
        public static void addWellPosition2Layer(string filePathLayerCss, string sLayerID,List<ItemDicLayerDataStatic> listItemWell)
        {
            XmlDocument wellTemplateXML = new XmlDocument();
            wellTemplateXML.Load(filePathLayerCss);
            string sPath = string.Format("//*[@id='{0}']", sLayerID);
            XmlNode XselectNode = wellTemplateXML.SelectSingleNode(sPath);

            //先删除原有数据
            XmlNodeList dataListNode = XselectNode.SelectNodes("dataList");
            for (int i = dataListNode.Count - 1; i >= 0; i--) dataListNode[i].ParentNode.RemoveChild(dataListNode[i]);

            //创建一个新的dataList
            XmlElement eleDataList = wellTemplateXML.CreateElement("dataList");
            eleDataList.SetAttribute("id", cIDmake.idDataList()); 

            foreach (ItemDicLayerDataStatic item in listItemWell)
            {
                XmlElement dataItem = wellTemplateXML.CreateElement("dataItem");
                dataItem.SetAttribute("id", item.sJH);

                XmlElement newNode;
                newNode = wellTemplateXML.CreateElement("xcm");
                newNode.InnerText = item.sXCM.ToString();
                dataItem.AppendChild(newNode);

                newNode = wellTemplateXML.CreateElement("X");
                newNode.InnerText = item.dbX.ToString();
                dataItem.AppendChild(newNode);

                newNode = wellTemplateXML.CreateElement("Y");
                newNode.InnerText = item.dbY.ToString();
                dataItem.AppendChild(newNode);

                newNode = wellTemplateXML.CreateElement("wellType");
                newNode.InnerText = "0";
                dataItem.AppendChild(newNode);
                eleDataList.AppendChild(dataItem);
            }

            XselectNode.AppendChild(eleDataList);
            wellTemplateXML.Save(filePathLayerCss);
        }

        public static void addWellStaticDataDic2XML(string filePathLayerCss,  string sXCM, List<ItemDicLayerDataStatic> listItemWell)
        {
            XmlDocument xmlLayerMap = new XmlDocument();
            xmlLayerMap.Load(filePathLayerCss);
            XmlNodeList xnList = xmlLayerMap.SelectNodes("/LayerMapConfig/DataDicStatic");
            //或许Layer标签的节点
            foreach (XmlNode xn in xnList)
            {
                if ( xn.Attributes["XCM"].Value == sXCM) xn.ParentNode.RemoveChild(xn);
            }

            XmlNode currentNode = xmlLayerMap.SelectSingleNode("/LayerMapConfig");
            XmlNode nodeLayer = xmlLayerMap.CreateElement("DataDicStatic");
            XmlAttribute nodeType = xmlLayerMap.CreateAttribute("XCM");
            nodeType.Value = sXCM;
            nodeLayer.Attributes.Append(nodeType);
            XmlElement eleMent;

            eleMent = xmlLayerMap.CreateElement("data");
            foreach (ItemDicLayerDataStatic item in listItemWell)
            {
                XmlElement itemWell = xmlLayerMap.CreateElement("item");

                XmlAttribute content = xmlLayerMap.CreateAttribute("LayerDataDicText");
                content.Value = ItemDicLayerDataStatic.item2string(item);
                itemWell.Attributes.Append(content);
               //  itemWell.InnerText = ItemDicLayerDataStatic.item2string(item);

                 XmlElement JH = xmlLayerMap.CreateElement("JH");
                 JH.InnerText = item.sJH;
                 itemWell.AppendChild(JH);
                
                 XmlElement ds1 = xmlLayerMap.CreateElement("ds1");
                 ds1.InnerText = item.fDS1_md.ToString();
                 itemWell.AppendChild(ds1);

                 XmlElement ds2 = xmlLayerMap.CreateElement("ds2");
                 ds2.InnerText = item.fDS2_md.ToString();
                 itemWell.AppendChild(ds2);


                 XmlElement dX = xmlLayerMap.CreateElement("X");
                 dX.InnerText = item.dbX.ToString();
                 itemWell.AppendChild(dX);

                 XmlElement dY = xmlLayerMap.CreateElement("Y");
                 dY.InnerText = item.dbY.ToString();
                 itemWell.AppendChild(dY);

                 XmlElement dZ = xmlLayerMap.CreateElement("X");
                 dZ.InnerText = item.dbZ.ToString();
                 itemWell.AppendChild(dZ);

                 XmlElement dchd = xmlLayerMap.CreateElement("dchd");
                 dchd.InnerText = item.fDCHD.ToString();
                 itemWell.AppendChild(dchd);

                 XmlElement sh = xmlLayerMap.CreateElement("sh");
                 sh.InnerText = item.fYXHD.ToString();
                 itemWell.AppendChild(sh);

                 XmlElement yxhd = xmlLayerMap.CreateElement("yxhd");
                 yxhd.InnerText = item.fYXHD.ToString();
                 itemWell.AppendChild(yxhd);

                 XmlElement kxd = xmlLayerMap.CreateElement("kxd");
                 kxd.InnerText = item.fKXD.ToString();
                 itemWell.AppendChild(kxd);

                 XmlElement stl = xmlLayerMap.CreateElement("stl");
                 stl.InnerText = item.fSTL.ToString();
                 itemWell.AppendChild(stl);

                 XmlElement bhd = xmlLayerMap.CreateElement("bhd");
                 bhd.InnerText = item.fBHD.ToString();
                 itemWell.AppendChild(bhd);

                 eleMent.AppendChild(itemWell);
            }
            nodeLayer.AppendChild(eleMent);

            currentNode.AppendChild(nodeLayer);
            xmlLayerMap.Save(filePathLayerCss);
        }
      
        public static void addStaticWellPos2XML(string filePathLayerCss, string sXCM, List<ItemWellMapPosition> listItemWell)
        {
            XmlDocument xmlLayerMap = new XmlDocument();
            xmlLayerMap.Load(filePathLayerCss);
            XmlNodeList xnList = xmlLayerMap.SelectNodes("/LayerMapConfig/DataWellPositionStatic");
            //或许Layer标签的节点
            foreach (XmlNode xn in xnList)
            {
                if (xn.Attributes["XCM"].Value == sXCM) xn.ParentNode.RemoveChild(xn);
            }

            XmlNode currentNode = xmlLayerMap.SelectSingleNode("/LayerMapConfig");
            XmlNode nodeLayer = xmlLayerMap.CreateElement("DataWellPositionStatic");
            XmlAttribute nodeID = xmlLayerMap.CreateAttribute("XCM");
            nodeID.Value = sXCM;
            nodeLayer.Attributes.Append(nodeID);

            XmlElement eleMent;

            eleMent = xmlLayerMap.CreateElement("data");
            foreach (ItemWellMapPosition item in listItemWell)
            {
                XmlElement itemdata = xmlLayerMap.CreateElement("item");
                itemdata.InnerText = ItemWellMapPosition.item2string(item);
                eleMent.AppendChild(itemdata);
            }

            currentNode.AppendChild(nodeLayer);
            xmlLayerMap.Save(filePathLayerCss);
        }

        public static string wellPatternCode(string sProperty) 
        {
            if (sProperty == "0") return "10基础井网采油井";
          return "0井";
        }
        public static XmlElement gWellPattern(cSVGDocLayerMap svgLayer,  ItemLayerWellPattern itemWell , int JHFontSize, int radis, int iCirlceWidth, int DX_JHText, int DY_JHText)
        {
            XmlDocument svgDoc = svgLayer.svgDoc;
            XmlElement svgDefs = svgLayer.svgDefs;
            XmlElement gWellItem = svgDoc.CreateElement("g");

            XmlElement gWellRect = svgDoc.CreateElement("rect");
            gWellRect.SetAttribute("id", itemWell.sJH);
            gWellRect.SetAttribute("onclick", "getID(evt)");

            gWellRect.SetAttribute("x", "0");
            gWellRect.SetAttribute("y", "0");
            gWellRect.SetAttribute("width", (radis * 2).ToString());
            gWellRect.SetAttribute("height", (radis * 2).ToString());
            gWellRect.SetAttribute("style", "stroke-width:0");
            gWellRect.SetAttribute("stroke", "black");
            gWellRect.SetAttribute("fill", "none");
            gWellRect.SetAttribute("fill-opacity", "1");


            string sPatterID = wellPatternCode(itemWell.sProperty);
            string filePathPatternSVG = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pattern", "wellChinese", "开发井", sPatterID + ".svg");
            //按照井别配置井符号填充rect
            if (File.Exists(filePathPatternSVG))//需要查找井pattern存在
            {
                cSVGoperate.addSVGpatternDefs(svgDoc, svgDefs, sPatterID, filePathPatternSVG);
                gWellRect.SetAttribute("fill", string.Format("url(#{0})", sPatterID));
            }
            else //不存在默认填充井pattern
            {
                gWellRect.SetAttribute("fill", "none");
            }
            gWellItem.AppendChild(gWellRect);

            XmlElement gJHText = svgDoc.CreateElement("text");
            gJHText.SetAttribute("x", (-DX_JHText).ToString());
            gJHText.SetAttribute("y", (+DY_JHText).ToString());
            gJHText.SetAttribute("font-size", JHFontSize.ToString());
            gJHText.SetAttribute("font-style", "normal");
            gJHText.InnerText = itemWell.sJH;
            gWellItem.AppendChild(gJHText);

            return gWellItem;
        }

        public static XmlElement gFault(cSVGDocLayerMap svgLayer,  ItemFaultLine curItemdata,List<Point> listPointView )
        {
            XmlDocument svgDoc = svgLayer.svgDoc;
            XmlElement svgDefs = svgLayer.svgDefs;
            XmlElement gPloylineFault = svgDoc.CreateElement("g");
             
            XmlElement gPloylineFaultPolygon = svgDoc.CreateElement("polyline");
      
            gPloylineFaultPolygon.SetAttribute("onclick", "getID(evt)");
            gPloylineFaultPolygon.SetAttribute("stroke-width", "5");
            gPloylineFaultPolygon.SetAttribute("stroke", "red");
            gPloylineFaultPolygon.SetAttribute("stroke-dasharray","0");
            gPloylineFaultPolygon.SetAttribute("fill", "none");
            gPloylineFaultPolygon.SetAttribute("ill-opacity", "1");
            string _points = "";
            foreach (Point pd in listPointView)
            {
                    _points += pd.X.ToString("0.0") + ',' + pd.Y.ToString("0.0") + " ";
            }
            gPloylineFaultPolygon.SetAttribute("points", _points);
            gPloylineFault.AppendChild(gPloylineFaultPolygon); 
 
            return gPloylineFault;
        }
    }
}
