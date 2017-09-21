﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using DOGPlatform.XML;
using DOGPlatform.SVG;
using System.Drawing;
using System.IO;

namespace DOGPlatform
{
    class makeSectionFence
    {
        public static int iPositionXFirstWell = 200;

        static double transformX(double fHScaleWellDistance, string Xpoint)
        {
            return transformX(fHScaleWellDistance, double.Parse(Xpoint));
        }
        static double transformX(double fHScaleWellDistance, double Xpoint)
        {
            return fHScaleWellDistance * Xpoint + makeSectionGeo.iPositionXFirstWell - makeSectionGeo.iPositionXFirstWell * fHScaleWellDistance;
        }
        public static string generateFence(string dirSectionData, string pathSectionCss, string filenameSVGMap)
        {
            //定义页面大小 及 纵向平移
            List<ItemWellSection> listWellsSection = cXmlDocSectionGeo.makeListWellSection(pathSectionCss);

            //读取page设置全局类
            cXEGeopage curPage = new cXEGeopage(pathSectionCss);
            int iPageTopYOff = 0;

            cSVGDocSection svgSection = new cSVGDocSection(curPage.PageWidth, curPage.PageHeight, 0, 0, curPage.sUnit);

            //定义返回的xmlElement变量，作为调用各种加入的道的返回值
            XmlElement returnElemment;
            XmlDocument XDocSection = new XmlDocument();
            XDocSection.Load(pathSectionCss);
            #region 断层 绘制断层 并把断层数据存入List 供连层时计算使用
            XmlElement gLayerFault = svgSection.gLayerElement("断层");
            string faultPath = "/SectionMap/FaultInfor/FaultItem";
            List<itemDrawDataFaultItem> listFaultItem = new List<itemDrawDataFaultItem>();
            foreach (XmlNode xn in XDocSection.SelectNodes(faultPath))
            {
                itemDrawDataFaultItem faultItem = new itemDrawDataFaultItem(xn);
                string sID = xn.Attributes["id"].Value;
                double x1 = transformX(curPage.fHScaleWellDistance, xn.Attributes["x1"].Value);
                double y1 = double.Parse(xn.Attributes["y1"].Value) * curPage.fVscale;
                double x2 = transformX(curPage.fHScaleWellDistance, xn.Attributes["x2"].Value);
                double y2 = double.Parse(xn.Attributes["y2"].Value) * curPage.fVscale;

                faultItem.x1View = x1;
                faultItem.y1View = y1;
                faultItem.x2View = x2;
                faultItem.y2View = y2;
                faultItem.displacementView = faultItem.displacement * curPage.fHScaleWellDistance;

                XmlElement faultLine = svgSection.svgDoc.CreateElement("line");
                faultLine.SetAttribute("id", sID);
                faultLine.SetAttribute("stroke-width", "2");
                faultLine.SetAttribute("onclick", "getID(evt)");
                //左侧的绘制
                faultLine.SetAttribute("x1", x1.ToString("0.0"));
                faultLine.SetAttribute("y1", y1.ToString("0.0"));
                faultLine.SetAttribute("x2", x2.ToString("0.0"));
                faultLine.SetAttribute("y2", y2.ToString("0.0"));

                faultLine.SetAttribute("stroke", "red");
                faultLine.SetAttribute("fill", "red");
                svgSection.addgElement2Layer(gLayerFault, faultLine, svgSection.offsetX_gSVG, iPageTopYOff);

                listFaultItem.Add(faultItem);

            }
            #endregion
            #region   先画连接层 判断是否切割，非切割的直接连接，切割的得重画
            string pathTrack = "/SectionMap/ConnectInfor/ConnectItem";
            //首先得判断是否多边形与断层线相交
            XmlElement gLayerConnectJSJL = svgSection.gLayerElement("测井解释");
            XmlElement gLayerConnectLayer = svgSection.gLayerElement("分层");
            XmlElement gLayerConnectLitho = svgSection.gLayerElement("岩性");
            XmlElement gLayerConnectBase = svgSection.gLayerElement("连接");
            svgSection.addgLayer(gLayerConnectLayer, 0, 0);
            foreach (XmlNode xn in XDocSection.SelectNodes(pathTrack))
            {
                if (xn.Attributes["id"] != null)
                {
                    string sID = xn.Attributes["id"].Value;
                    int iShowMode = int.Parse(xn.Attributes["iShowMode"].Value);
                    string strTypeTrack = xn.Attributes["trackType"].Value;
                    string sFill = xn.Attributes["sFill"].Value;
                    #region 画连接层 模式1
                    if (iShowMode == 1)
                    {
                        XmlNode rect1 = xn.SelectSingleNode("rect1");
                        string sIDtrack1 = rect1.Attributes["sIDtrack"].Value;
                        string wellID1 = rect1.Attributes["wellID"].Value;
                        string sIDitem1 = rect1.Attributes["sIDitem"].Value;

                        string filePathOperWell1 = dirSectionData + "//" + wellID1 + ".xml";
                        cDataItemConnect item1 = new cDataItemConnect(pathSectionCss, filePathOperWell1, wellID1, sIDtrack1, sIDitem1);

                        XmlNode rect2 = xn.SelectSingleNode("rect2");
                        string sIDtrack2 = rect2.Attributes["sIDtrack"].Value;
                        string wellID2 = rect2.Attributes["wellID"].Value;
                        string sIDitem2 = rect2.Attributes["sIDitem"].Value;
                        string filePathOperWell2 = dirSectionData + "//" + wellID2 + ".xml";
                        cDataItemConnect item2 = new cDataItemConnect(pathSectionCss, filePathOperWell2, wellID2, sIDtrack2, sIDitem2);
                        if (wellID1 != "" && sIDtrack1 != "" && wellID2 != "" && sIDtrack2 != "")
                        {
                            List<string> ltPathd = makeConnectPath.makePathd(listFaultItem, item1, item2);
                            foreach (string d in ltPathd)
                            {
                                XmlElement connectPath = svgSection.svgDoc.CreateElement("path");
                                connectPath.SetAttribute("id", sID);
                                connectPath.SetAttribute("stroke-width", "1");
                                connectPath.SetAttribute("onclick", "getID(evt)");
                                connectPath.SetAttribute("stroke", "black");
                                connectPath.SetAttribute("d", d);
                                string fillType = "none";
                                if (strTypeTrack == TypeTrack.测井解释.ToString())
                                {
                                    fillType = codeReplace.codeReplaceJSJL2FillUrl(sFill);
                                    connectPath.SetAttribute("fill", fillType);
                                    svgSection.addgElement2Layer(gLayerConnectJSJL, connectPath, svgSection.offsetX_gSVG, iPageTopYOff);
                                }
                                else if (strTypeTrack == TypeTrack.分层.ToString())
                                {
                                    fillType = cSVGSectionTrackLayer.getLayerFillColor(sFill);
                                    connectPath.SetAttribute("fill", fillType);
                                    svgSection.addgElement2Layer(gLayerConnectLayer, connectPath, svgSection.offsetX_gSVG, iPageTopYOff);
                                }
                                else if (strTypeTrack == TypeTrack.岩性层段.ToString())
                                {
                                    fillType = cSVGSectionTrackLayer.getLayerFillColor(sFill);
                                    connectPath.SetAttribute("fill", fillType);
                                    svgSection.addgElement2Layer(gLayerConnectLitho, connectPath, svgSection.offsetX_gSVG, iPageTopYOff);
                                }
                                else
                                {
                                    connectPath.SetAttribute("fill", "none");
                                    svgSection.addgElement2Layer(gLayerConnectBase, connectPath, svgSection.offsetX_gSVG, iPageTopYOff);
                                }
                            }
                        }
                    }
                    #endregion

                    #region 尖灭模式
                    else
                    {
                        XmlNode rect1 = xn.SelectSingleNode("rect1");
                        string sIDtrack1 = rect1.Attributes["sIDtrack"].Value;
                        string wellID1 = rect1.Attributes["wellID"].Value;
                        string sIDitem1 = rect1.Attributes["sIDitem"].Value;

                        if (wellID1 != "" && sIDtrack1 != "")
                        {
                            string filePathOperWell1 = dirSectionData + "//" + wellID1 + ".xml";
                            cDataItemConnect connectItem = new cDataItemConnect(pathSectionCss, filePathOperWell1, wellID1, sIDtrack1, sIDitem1);

                            double fExtanceLength = 100;
                            int indexWell = listWellsSection.IndexOf(listWellsSection.SingleOrDefault(p => p.sJH == wellID1));
                            if (indexWell < listWellsSection.Count - 1) fExtanceLength = (listWellsSection[indexWell + 1].fXview - listWellsSection[indexWell].fXview) * 0.35;

                            string d = "";
                            if (iShowMode == (int)TypeModeGeoOperate.channelRight) //右河道
                            {
                                double x1 = connectItem.x1 + connectItem.width;
                                double y1 = connectItem.y1;
                                double x2 = connectItem.x1 + connectItem.width;
                                double y2 = connectItem.y1 + connectItem.height;
                                double x3 = x1 + fExtanceLength;
                                double y3 = y2;
                                double x4 = x1 + fExtanceLength;
                                double y4 = y1;
                                d = "M " + x1.ToString() + " " + y1.ToString() + " L " + x2.ToString() + " " + y2.ToString() +
                                     " Q " + x3.ToString() + " " + y3.ToString() + " " + x4.ToString() + " " + y4.ToString() + " z ";
                            }
                            if (iShowMode == (int)TypeModeGeoOperate.channelLeft) //左河道
                            {
                                double x1 = connectItem.x1;
                                double y1 = connectItem.y1;
                                double x2 = connectItem.x1;
                                double y2 = connectItem.y1 + connectItem.height;
                                double x3 = x1 - fExtanceLength;
                                double y3 = y2;
                                double x4 = x1 - fExtanceLength;
                                double y4 = y1;
                                d = "M " + x1.ToString() + " " + y1.ToString() + " L " + x2.ToString() + " " + y2.ToString() +
                                     " Q " + x3.ToString() + " " + y3.ToString() + " " + x4.ToString() + " " + y4.ToString() + " z ";
                            }

                            if (iShowMode == (int)TypeModeGeoOperate.barRight) //右坝
                            {
                                double x1 = connectItem.x1 + connectItem.width;
                                double y1 = connectItem.y1;
                                double x2 = connectItem.x1 + connectItem.width;
                                double y2 = connectItem.y1 + connectItem.height;
                                double x3 = x1 + fExtanceLength;
                                double y3 = y1;
                                double x4 = x1 + fExtanceLength;
                                double y4 = y2;
                                d = "M " + x2.ToString() + " " + y2.ToString() + " L " + x1.ToString() + " " + y1.ToString() +
                                     " Q " + x3.ToString() + " " + y3.ToString() + " " + x4.ToString() + " " + y4.ToString() + " z ";
                            }

                            if (iShowMode == (int)TypeModeGeoOperate.barLeft) //左坝
                            {
                                double x1 = connectItem.x1;
                                double y1 = connectItem.y1;
                                double x2 = connectItem.x1;
                                double y2 = connectItem.y1 + connectItem.height;
                                double x3 = x1 - fExtanceLength;
                                double y3 = y1;
                                double x4 = x1 - fExtanceLength;
                                double y4 = y2;
                                d = "M " + x2.ToString() + " " + y2.ToString() + " L " + x1.ToString() + " " + y1.ToString() +
                                     " Q " + x3.ToString() + " " + y3.ToString() + " " + x4.ToString() + " " + y4.ToString() + " z ";
                            }
                            if (iShowMode == (int)TypeModeGeoOperate.pinchOutRight) //右尖灭
                            {
                                double x1 = connectItem.x1 + connectItem.width;
                                double y1 = connectItem.y1;
                                double x2 = connectItem.x1 + connectItem.width;
                                double y2 = connectItem.y1 + connectItem.height;
                                d = "M " + x1.ToString() + " " + y1.ToString() + "h " + fExtanceLength.ToString() + "l -" + (0.2 * fExtanceLength).ToString() + " " + (connectItem.height / 2).ToString() + " " + "l " + (0.1 * fExtanceLength).ToString() + " " + (connectItem.height / 2).ToString() + " "
                                    + " L " + x2.ToString() + " " + y2.ToString() + " z ";
                            }

                            if (iShowMode == (int)TypeModeGeoOperate.pinchOutLeft) //左尖灭
                            {
                                double x1 = connectItem.x1;
                                double y1 = connectItem.y1;
                                double x2 = connectItem.x1;
                                double y2 = connectItem.y1 + connectItem.height;
                                d = "M " + x1.ToString() + " " + y1.ToString() + "h-" + fExtanceLength.ToString() + "l " + (0.2 * fExtanceLength).ToString() + " " + (connectItem.height / 2).ToString() + " " + "l -" + (0.1 * fExtanceLength).ToString() + " " + (connectItem.height / 2).ToString() + " "
                                    + " L " + x2.ToString() + " " + y2.ToString() + " z ";
                            }
                            XmlElement connectPath = svgSection.svgDoc.CreateElement("path");
                            connectPath.SetAttribute("id", sID);
                            connectPath.SetAttribute("stroke-width", "1");
                            connectPath.SetAttribute("onclick", "getID(evt)");
                            connectPath.SetAttribute("stroke", "black");
                            connectPath.SetAttribute("d", d);
                            string fillType = "none";
                            if (strTypeTrack == TypeTrack.测井解释.ToString())
                            {
                                fillType = codeReplace.codeReplaceJSJL2FillUrl(sFill);
                                connectPath.SetAttribute("fill", fillType);
                                svgSection.addgElement2Layer(gLayerConnectJSJL, connectPath, svgSection.offsetX_gSVG, iPageTopYOff);
                            }
                            else if (strTypeTrack == TypeTrack.分层.ToString())
                            {
                                fillType = cSVGSectionTrackLayer.getLayerFillColor(sFill);
                                connectPath.SetAttribute("fill", fillType);
                                svgSection.addgElement2Layer(gLayerConnectLayer, connectPath, svgSection.offsetX_gSVG, iPageTopYOff);
                            }
                        } //end if wellID 不能为空
                    }
                    #endregion
                }

            }
            #endregion
            //在listWellSection中循环，一口一口井的加入剖面
            for (int i = 0; i < listWellsSection.Count; i++)
            {
                string sJH = listWellsSection[i].sJH;
                string filePathTemplatOper = Path.Combine(dirSectionData, sJH + ".xml");
                //斜井模式
                cSVGSectionWell currentWell = makePathWell(svgSection, pathSectionCss, filePathTemplatOper,
                listWellsSection[i].fShowedDepthTop, listWellsSection[i].fShowedDepthBase, curPage.fVscale, curPage);
                //按显示的顶深拉高
                svgSection.addgElement2BaseLayer(currentWell.gWell, curPage.fMapScale * listWellsSection[i].fXview, curPage.fMapScale * listWellsSection[i].fYview - listWellsSection[i].fShowedDepthTop * curPage.fVscale);
                //加个井位标识
                returnElemment = cSVGSectionWell.gWellHead(svgSection.svgDoc, sJH, curPage.fMapScale * listWellsSection[i].fXview, curPage.fMapScale * listWellsSection[i].fYview, 18);
                svgSection.addgElement2BaseLayer(returnElemment, 0, 0);
            }
            svgSection.addgLayer(gLayerConnectBase, 0, 0);
            svgSection.addgLayer(gLayerConnectLitho, 0, 0);
            svgSection.addgLayer(gLayerConnectJSJL, 0, 0);
            svgSection.addgLayer(gLayerFault, 0, 0);
            string fileSVG = Path.Combine(cProjectManager.dirPathTemp, filenameSVGMap);
            svgSection.makeSVGfile(fileSVG);
            return fileSVG;
        }

        //设置well的摆放位置
        public static void setXYPositionViewFence(string pathSectionCss, List<ItemWellSection> listWellsSection)
        {
            float fHScale = float.Parse(cXmlBase.getNodeInnerText(pathSectionCss, cXEGeopage.xmlFullPathPageHorizonWellDistanceScale));
            //设置拉平高度 就是 给 fxview和fyview 赋值
            //第一口井默认位置,乘以水平系数 为了连接层；
            for (int i = 0; i < listWellsSection.Count; i++)
            {
                ItemWellSection itemWell = listWellsSection[i];
                Point headView = cCordinationTransform.transRealPointF2ViewPoint(
                   listWellsSection[i].WellPathList[0].dbX,  listWellsSection[i].WellPathList[0].dbY, cProjectData.dfMapXrealRefer, cProjectData.dfMapYrealRefer, cProjectData.dfMapScale);
                itemWell.fXview = headView.X;
                itemWell.fYview = headView.Y;
            }
        }

        public static void setXYPositionViewFenceRelative(string pathSectionCss, List<ItemWellSection> listWellsSection)
        {
            float fHScale = float.Parse(cXmlBase.getNodeInnerText(pathSectionCss, cXEGeopage.xmlFullPathPageHorizonWellDistanceScale));
            //设置拉平高度 就是 给 fxview和fyview 赋值
            double dfMapXrealRefer = 0;
            double dfMapYrealRefer = 10000;
            if (listWellsSection.Count > 0)
            {
                int iSacleUnit = 1000;
                dfMapXrealRefer = Math.Floor(listWellsSection.Select(p => p.dbX).ToList().Min() / iSacleUnit - 1) * iSacleUnit;
                dfMapYrealRefer = (Math.Ceiling(listWellsSection.Select(p => p.dbY).ToList().Max() / iSacleUnit) + 1) * iSacleUnit;
            }
            for (int i = 0; i < listWellsSection.Count; i++)
            {
                ItemWellSection itemWell = listWellsSection[i];
                Point headView = cCordinationTransform.transRealPointF2ViewPoint(
                   listWellsSection[i].WellPathList[0].dbX, listWellsSection[i].WellPathList[0].dbY, dfMapXrealRefer, dfMapYrealRefer, cProjectData.dfMapScale);
                itemWell.fXview = headView.X;
                itemWell.fYview = headView.Y;
            } 
        }

        public static void makeNewShowDepth(string pathSectionCss, List<ItemWellSection> listWellsSection)
        {
            List<string> ltStrSelectedXCM = cProjectData.ltStrProjectXCM;
            int _up = 10;
            int _down = 10;
            if (ltStrSelectedXCM.Count > 0)
            {
                //重新给显示的顶底赋值
                foreach (ItemWellSection item in listWellsSection)
                {
                    string sJH = item.sJH;
                    //有可能上下层有缺失。。。所以这块的技巧是找出深度序列，取最大最小值
                    cIOinputLayerDepth fileLayerDepth = new cIOinputLayerDepth();
                    List<float> fListDS1Return = fileLayerDepth.selectDepthListFromLayerDepthByJHAndXCMList(sJH, ltStrSelectedXCM);
                    if (fListDS1Return.Count > 0)  //返回值为空 说明所选层段整个缺失！
                    {
                        item.fShowedDepthTop = fListDS1Return.Min() - _up;
                        item.fShowedDepthBase = fListDS1Return.Max() + _down;
                        cXmlBase.setSelectedNodeChildNodeValue(pathSectionCss, sJH, "fShowTop", item.fShowedDepthTop.ToString("0"));
                        cXmlBase.setSelectedNodeChildNodeValue(pathSectionCss, sJH, "fShowBot", item.fShowedDepthBase.ToString("0"));
                    }
                }//end foreach
            }//end if
        }
        public static cSVGSectionWell makePathWell(cSVGDocSection svgSection, string pathSectionCss, string filePathTemplatOper, double dfDS1Show, double dfDS2Show, float fVScale, cXEGeopage curPage)
        {
            cSVGSectionWell wellGeoSingle = new cSVGSectionWell(svgSection.svgDoc);
            List<int> iListTrackWidth = new List<int>();
            //从配置文件读取显示深度
            string sJH = cXmlBase.getNodeInnerText(filePathTemplatOper, cXmlDocSectionWell.fullPathJH);
            //绝对值不放大fvscale，位置放大。
            ItemWell wellItem = cProjectData.ltProjectWell.FirstOrDefault(p => p.sJH == sJH);
            int iHeightMapTitle = int.Parse(cXmlBase.getNodeInnerText(filePathTemplatOper, cXEWellPage.fullPathMapTitleRectHeight));
            int iHeightTrackHead = int.Parse(cXmlBase.getNodeInnerText(filePathTemplatOper, cXEWellPage.fullPathTrackRectHeight));
            iListTrackWidth.Clear();
            XmlElement returnElemment;

            float dfDS1ShowTVD = cIOinputWellPath.getTVDByJHAndMD(wellItem, (float)dfDS1Show);

            int iYpositionTrackHead = Convert.ToInt16(dfDS1ShowTVD * fVScale) - iHeightTrackHead;

            foreach (XmlElement el_Track in cXmlDocSectionWell.getTrackNodes(filePathTemplatOper))
            {
                //初始化绘制道的基本信息
                trackDataDraw curTrackDraw = new trackDataDraw(el_Track);
                //增加道头
                returnElemment = cSVGSectionTrack.trackHead(svgSection.svgDoc, curTrackDraw.sTrackID, curTrackDraw.sTrackTitle, iYpositionTrackHead, iHeightTrackHead, curTrackDraw.iTrackWidth, curTrackDraw.iTrackHeadFontSize, curTrackDraw.sWriteMode);
                wellGeoSingle.addTrack(returnElemment, iListTrackWidth.Sum());

                //增加距离位置节点
                cXmlDocSectionGeo.addWellTrackXviewNode(pathSectionCss, sJH, curTrackDraw.sTrackID, iListTrackWidth.Sum());
                //先画曲线，再画道头和道框，这样好看
                #region  判断是否可见，可见才绘制
                if (curTrackDraw.iVisible > 0)
                {
                    #region 井深结构尺
                    if (el_Track["trackType"].InnerText == TypeTrack.深度尺.ToString())
                    {
                        itemDrawDataDepthRuler itemRuler = new itemDrawDataDepthRuler(el_Track);
                        //测试斜井gPathWellCone
                        returnElemment = cSVGSectionTrackWellRuler.gPathWellRuler(wellItem, svgSection, Convert.ToInt16(dfDS1Show), Convert.ToInt16(dfDS2Show), fVScale, itemRuler);
                        wellGeoSingle.addTrack(returnElemment, iListTrackWidth.Sum());
                    }
                    #endregion
                    #region 地层道
                    if (curTrackDraw.sTrackType == TypeTrack.分层.ToString())
                    {
                        XmlNode dataList = el_Track.SelectSingleNode("dataList");
                        if (dataList != null)
                        {
                            XmlNodeList dataItem = dataList.SelectNodes("dataItem");
                            foreach (XmlNode xn in dataItem)
                            {
                                ItemTrackDrawDataIntervalProperty item = new ItemTrackDrawDataIntervalProperty(xn);
                                if (item.top >= dfDS1Show && item.bot <= dfDS2Show)
                                {
                                    returnElemment = cSVGSectionTrackLayer.gTrackItemTVDLayer(svgSection.svgDoc, item, fVScale, curTrackDraw.iTrackFontSize, curTrackDraw.iTrackWidth);
                                    wellGeoSingle.addTrack(returnElemment, iListTrackWidth.Sum());
                                }
                            }
                        }
                    }
                    #endregion
                    #region 测井解释，旋回,化石道
                    if (cProjectManager.ltStrTrackTypeIntervalProperty.IndexOf(curTrackDraw.sTrackType) >= 0)
                    {
                        XmlNode dataList = el_Track.SelectSingleNode("dataList");
                        if (dataList != null)
                        {
                            XmlNodeList dataItem = dataList.SelectNodes("dataItem");
                            foreach (XmlNode xn in dataItem)
                            {
                                ItemTrackDrawDataIntervalProperty item = new ItemTrackDrawDataIntervalProperty(xn); if (item.top >= dfDS1Show && item.bot <= dfDS2Show)
                                {
                                    returnElemment = cSVGSectionTrackJSJL.gTrackItemTVDJSJL(svgSection.svgDoc, svgSection.svgDefs, item, fVScale, curTrackDraw.iTrackWidth);
                                    if (curTrackDraw.sTrackType == TypeTrack.沉积旋回.ToString()) returnElemment = cSVGSectionTrackCycle.gTrackItemTVDGeoCycle(svgSection.svgDoc, svgSection.svgDefs, item, fVScale, curTrackDraw.iTrackWidth);
                                    //     if (curTrackDraw.sTrackType == TypeTrack.描述.ToString()) returnElemment = cSVGSectionTrackDes.gTrackItemFossil(svgSection.svgDoc, svgSection.svgDefs, item, fVScale, curTrackDraw.iTrackWidth);
                                    wellGeoSingle.addTrack(returnElemment, iListTrackWidth.Sum());
                                }
                            }
                        }
                    }
                    #endregion
                    #region 岩性
                    if (curTrackDraw.sTrackType == TypeTrack.岩性层段.ToString())
                    {
                        XmlNode dataList = el_Track.SelectSingleNode("dataList");
                        if (dataList != null)
                        {
                            XmlNodeList dataItem = dataList.SelectNodes("dataItem");
                            foreach (XmlNode xn in dataItem)
                            {
                                itemDrawDataIntervalValue item = new itemDrawDataIntervalValue(xn);
                                if (item.top >= dfDS1Show && item.bot <= dfDS2Show)
                                {
                                    returnElemment = cSVGSectionTrackLitho.gTrackLithoTVDItem(wellItem, svgSection.svgDoc, svgSection.svgDefs, item, fVScale, curTrackDraw.iTrackWidth);
                                    wellGeoSingle.addTrack(returnElemment, iListTrackWidth.Sum());
                                }
                            }
                        }
                    }
                    #endregion
                    #region 曲线道
                    List<itemLogHeadInforDraw> ltItemLogHeadInforDraw = new List<itemLogHeadInforDraw>(); //记录绘制道头用，节省重新读取的时间
                    if (curTrackDraw.sTrackType == TypeTrack.曲线道.ToString())
                    {
                        cSVGSectionTrackLogCurveFill.listLogViewData4fill.Clear();
                        XmlNodeList xnList = el_Track.SelectNodes(".//Log");
                        int iLogNum = 0;
                        bool bGrid = false; //记录网格是否绘制过。
                        foreach (XmlElement xnLog in xnList)
                        {
                            iLogNum++;
                            itemLogHeadInforDraw curLogHead = new itemLogHeadInforDraw(xnLog);
                            ltItemLogHeadInforDraw.Add(curLogHead);
                            if (curLogHead.iIsLog > 0)
                            {
                                if (curLogHead.fLeftValue <= 0)
                                {
                                    curLogHead.fLeftValue = 1;
                                    cXmlBase.setSelectedNodeChildNodeValue(filePathTemplatOper, "", "leftValue", "1");
                                }
                                curLogHead.iLogGridGrade = cSVGSectionTrackLog.getNumGridGroupInLog(curLogHead.fLeftValue, curLogHead.fRightValue);
                            }

                            //曲线是否可见
                            if (curLogHead.iLogCurveVisible > 0)
                            {
                                trackDataListLog dlTrackDataListLog = cSVGSectionTrackLog.getLogSeriersFromLogFile(sJH, curLogHead.sLogName, dfDS1Show, dfDS2Show);

                                //画曲线
                                returnElemment = cSVGSectionTrackLog.gTrackTVDLog(wellItem, svgSection.svgDoc, curLogHead, curTrackDraw.iTrackWidth, dlTrackDataListLog.fListMD, dlTrackDataListLog.fListValue, fVScale);
                                wellGeoSingle.addTrack(returnElemment, iListTrackWidth.Sum());
                               
                            }//曲线可见

                        }//结束曲线循环
                    } //结束曲线if
                    #endregion 结束曲线道
                    #region 绘制测井图头,测井图信息很重要，图形加载数据要捕捉测井头的ID
                    if (curTrackDraw.sTrackType == TypeTrack.曲线道.ToString())
                    {
                        int iLogNum = 0;
                        foreach (itemLogHeadInforDraw curLogHead in ltItemLogHeadInforDraw)
                        {
                            iLogNum++;
                            if (curLogHead.iIsLog > 0)
                            {
                                if (curLogHead.fLeftValue <= 0) curLogHead.fLeftValue = 1;
                                curLogHead.iLogGridGrade = cSVGSectionTrackLog.getNumGridGroupInLog(curLogHead.fLeftValue, curLogHead.fRightValue);
                            }
                            if (curLogHead.iLogCurveVisible > 0)
                            {
                                //增加测井头
                                int iHeadLogSize = 14;
                                returnElemment = cSVGSectionTrack.addTrackItemLogHeadInfor(svgSection.svgDoc, curLogHead, iYpositionTrackHead + iHeightTrackHead, iLogNum, curTrackDraw.iTrackWidth, iHeadLogSize);
                                wellGeoSingle.addTrack(returnElemment, iListTrackWidth.Sum());
                            }
                        }
                    }
                    #endregion
                    //绘制道框
                    if (curPage.iShowTrackRect == 1)
                    {
                        returnElemment = cSVGSectionTrack.trackRect(svgSection.svgDoc, curTrackDraw.sTrackID, dfDS1ShowTVD, dfDS2Show, fVScale, curTrackDraw.iTrackWidth);
                        wellGeoSingle.addTrack(returnElemment, iListTrackWidth.Sum());
                    }
                    iListTrackWidth.Add(curTrackDraw.iTrackWidth);
                }
                #endregion

            } //end of for add track
            //增加图头
            returnElemment = wellGeoSingle.mapHeadTitle(sJH, iYpositionTrackHead - iHeightMapTitle, iYpositionTrackHead, iHeightMapTitle, iListTrackWidth.Sum(), iHeightMapTitle * 2 / 3);
            wellGeoSingle.addTrack(returnElemment, 0);
            return wellGeoSingle;
        }  
        

    }
}
