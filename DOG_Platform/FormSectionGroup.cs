﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using DOGPlatform.XML;
using DOGPlatform.SVG;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace DOGPlatform
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)] 
    public partial class FormSectionGroup : Form
    {
        private Stack<string> UndoList = new Stack<string>();
        private Stack<string> RedoList = new Stack<string>();
        private string dirHisUnto = cProjectManager.dirPathHis;
        int iSwichHis = 0; //是否备份开关
        //定义绘图数据的临时目录
        //定义联井剖面井号存储List
        public List<string> ltStrSelectedJH = new List<string>();
        //定义存储绘图剖面井数据结构
        List<ItemWellSection> listWellsSection = new List<ItemWellSection>();

        //tempXML存储路径
        static string mapID = "sectionFence" + DateTime.Now.ToString("MMddHHmmss");
        static string dirSectionData = Path.Combine(cProjectManager.dirPathUsedProjectData, mapID);
        string filePathSectionCss = Path.Combine(cProjectManager.dirPathUsedProjectData, mapID + cProjectManager.fileExtensionSectionFence);
       
        //记录上次页面位置，便于下次定位
        Point PscrollOffset = new Point(0, 0);
        string filePathSVG = "";

        string sJH = "";
        string sIDcurrentTrack = "";
        string sIDcurrentItem = "";
        string filePathOper = "";

        double fMapscale = (float)cProjectData.dfMapScale;

        public FormSectionGroup()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            InitFormWellsGroupControl();
        }
        public FormSectionGroup(string inputFilepath)
            : this()
        {
            openExistSectionFlow(inputFilepath);
        } 
        private void InitFormWellsGroupControl()
        {
            this.webBrowserSVG.ContextMenuStrip = cmsWebSVG;
            webBrowserSVG.ObjectForScripting = this;
        }

        void updateTVandList()
        {
            tvSectionEdit.Nodes.Clear();
            listWellsSection.Clear();
            //解析sectioncss文件，增加节点
            foreach (XmlElement elWell in cXmlDocSectionGeo.getWellNodes(filePathSectionCss))
            {
                ItemWellSection item = new ItemWellSection(elWell["JH"].InnerText);
                item.fShowedDepthTop = float.Parse(elWell["fShowTop"].InnerText);
                item.fShowedDepthBase = float.Parse(elWell["fShowBot"].InnerText);
                item.fXview = float.Parse(elWell["Xview"].InnerText);
                item.fYview = float.Parse(elWell["Yview"].InnerText);
                listWellsSection.Add(item);

                string filePath = Path.Combine(dirSectionData, item.sJH + ".xml");
                TreeNode tnWell = new TreeNode(item.sJH);
                tnWell.Tag = item.sJH;
                tnWell.Text = item.sJH;
                tnWell.Name = item.sJH;
                TreeViewSectionEditView.setupWellNode(tnWell, filePath);
                tvSectionEdit.Nodes.Add(tnWell);
                tnWell.Expand();
            }

        }
        
     
        private void btnScale_Click(object sender, EventArgs e)
        {
            fMapscale = fMapscale * 1.5F;
            MessageBox.Show("当前比例尺:" + fMapscale.ToString());
        }

        private void FormMapFence_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void btnBig_Click(object sender, EventArgs e)
        {
            fMapscale = fMapscale * 1.2F;
        }

        private void tsmiNew_Click(object sender, EventArgs e)
        {
            createNewFile();
        }
        void createNewFile()
        {
            FormSectionAddGroup formNew = new FormSectionAddGroup(filePathSectionCss, dirSectionData);
            var result = formNew.ShowDialog();
            if (result == DialogResult.OK)
            {
                updateTVandList();
                makeSVGmap();
            }
        }
        DateTime dtLastStop = DateTime.Now;
        void makeSVGmap()
        {
            PscrollOffset = cSectionUIoperate.getOffSet(this.webBrowserSVG);
            if (listWellsSection.Count > 0)
            {
                this.tbgEditView.Text = Path.GetFileNameWithoutExtension(this.filePathSectionCss);
                filePathSVG = makeSectionFence.generateFence( dirSectionData, this.filePathSectionCss, "testFence.svg");
                this.webBrowserSVG.Navigate(new Uri(filePathSVG));
            }

            //更新前把 数据备份，copy到历史数据，准备回退。
            if (iSwichHis == 0 && cPublicMethodBase.ExecDateDiff(dtLastStop, DateTime.Now).TotalSeconds >= 2) //0表示重新生成的，1表示回退的 回退的不用备份。
            {
                string dirPathUndo = Path.Combine(this.dirHisUnto, cIDmake.generateRandomDirName());
                Directory.CreateDirectory(dirPathUndo);
                string backFileName = Path.Combine(dirPathUndo, mapID + cProjectManager.fileExtensionSectionFence);
                File.Copy(this.filePathSectionCss, backFileName, true);
                cPublicMethodForm.DirectoryCopy(dirSectionData, dirPathUndo, true);
                UndoList.Push(dirPathUndo);
                if (UndoList.Count > 1) this.tsbUndo.Enabled = true;
                if (RedoList.Count > 1) this.tsbRedo.Enabled = true;
            }
            iSwichHis = 0;
            dtLastStop = DateTime.Now;
        }

        HtmlDocument htmlDoc;
        private void webBrowserSVG_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            htmlDoc = webBrowserSVG.Document;
            if (htmlDoc != null)
            {
                // 定位上次页面位置
                if (webBrowserSVG.Url == e.Url)
                {
                    //记录元素的位置，实现刷新页面自动滚动
                    webBrowserSVG.Document.Window.ScrollTo(PscrollOffset);
                }
                htmlDoc.MouseDown -= htmlDoc_MouseDown;
                htmlDoc.MouseDown += htmlDoc_MouseDown;
            }
        }

        PointD docCoord = new PointD();
        

        void htmlDoc_MouseDown(object sender, HtmlElementEventArgs e)
        {
            PscrollOffset = cSectionUIoperate.getOffSet(this.webBrowserSVG);
            Point ScreenCoord = new Point(e.MousePosition.X, e.MousePosition.Y);
            //double currentX = (ScreenCoord.X + PscrollOffset.X - makeSectionGeo.iPositionXFirstWell) / fWellDistanceHScale + makeSectionGeo.iPositionXFirstWell;
            //double currentDepth = (ScreenCoord.Y + PscrollOffset.Y) / fVscale - iPageTopElevation;
            //PointD docCoord = new PointD(currentX, currentDepth);
            //lblClick.Location = ScreenCoord;
            //ltMousePos.Add(docCoord);
            // Point BrowserCoord = webBrowserSVG.PointToClient(ScreenCoord);
            this.tsslblWb.Text = "记录点数=" + ltMousePos.Count.ToString() + " X=" + docCoord.X.ToString("0.00") + " Y=" + docCoord.Y.ToString("0.00");
            if (iOperateMode == (int)TypeModeGeoOperate.fault || iOperateMode == (int)TypeModeGeoOperate.revertFault)
            {
                if (ltMousePos.Count == 1)
                {
                    tsslblWb.Text = "正等待点击确认第二个断点位置。";
                }
                if (ltMousePos.Count == 2)
                {
                    faultMode(iOperateMode);
                    ltMousePos.Clear();
                    iOperateMode = 0;
                }
            }
        }

        void faultMode(int iOperateMode)
        {
            double displacement = 1.0;
            //string strRet = tsTextFaultDisplacement.Text;
            //double.TryParse(strRet, out displacement);
            //cXmlDocSectionGeo.addFaultItem(this.filePathSectionGeoCss, ltMousePos[0], ltMousePos[1], displacement);
            //tsTextFaultDisplacement.Visible = false;
            //tslblFaulDisplacement.Visible = false;
            //makeSVGmap();
        }

        private void tsBtnNewProject_Click(object sender, EventArgs e)
        {
            createNewFile(); 
        }

        private void tsmiOpen_Click(object sender, EventArgs e)
        {
            openExist();
        }
        void openExist()
        {
            OpenFileDialog ofdProjectPath = new OpenFileDialog();

            ofdProjectPath.Title = " 打开文件：";
            ofdProjectPath.Filter = string.Format("{0}文件|*{0}", cProjectManager.fileExtensionSectionFence);
            //设置默认文件类型显示顺序 
            ofdProjectPath.FilterIndex = 1;
            //保存对话框是否记忆上次打开的目录 
            ofdProjectPath.RestoreDirectory = true;

            if (ofdProjectPath.ShowDialog() == DialogResult.OK)
            {
                string fileOpen = ofdProjectPath.FileName;
                openExistSectionFlow(fileOpen);
            }
        }
        void openExistSectionFlow(string pathOpenExsitFile)
        {
            mapID = Path.GetFileNameWithoutExtension(pathOpenExsitFile);
            filePathSectionCss = Path.Combine(cProjectManager.dirPathUsedProjectData, mapID + cProjectManager.fileExtensionSectionFence);
            dirSectionData = Path.Combine(cProjectManager.dirPathUsedProjectData, mapID);
            updateTVandList();
            makeSVGmap();
        }

        private void tsBtnOpenProject_Click(object sender, EventArgs e)
        {
            openExist();
        }

        private void tsBtnZoonIn_Click(object sender, EventArgs e)
        {
            webBrowserSVG.Focus();
            SendKeys.Send("^{+}");
        }

        private void tsBtnZoomOut_Click(object sender, EventArgs e)
        {
            webBrowserSVG.Focus();
            SendKeys.Send("^{-}");
        }

        void scaleMap(float fCoeffect) 
        {
            fMapscale = float.Parse(cXmlBase.getNodeInnerText(filePathSectionCss, cXEGeopage.fullPathSacleMap));
            fMapscale = fMapscale + fCoeffect;
            cXmlBase.setNodeInnerText(filePathSectionCss, cXEGeopage.fullPathSacleMap, fMapscale.ToString("0.0"));
            for (int i = 0; i < listWellsSection.Count; i++)
            {
                ItemWellSection itemWell = listWellsSection[i];
                Point currentPositon = cCordinationTransform.getPointViewByJH(itemWell.sJH);
                itemWell.fXview =(float) fMapscale * currentPositon.X;
                itemWell.fYview =(float) fMapscale * currentPositon.Y;
                cXmlDocSectionGeo.setSelectedNodeChildNodeValue(filePathSectionCss, itemWell.sJH, "Xview", itemWell.fXview.ToString());
                cXmlDocSectionGeo.setSelectedNodeChildNodeValue(filePathSectionCss, itemWell.sJH, "Yview", itemWell.fYview.ToString());
            }
        }

        private void tsBtnReflush_Click(object sender, EventArgs e)
        {
            updateTVandList();
            makeSVGmap(); 
        }

        private void tsmiTrackDataImport_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = this.tvSectionEdit.SelectedNode;
            setUpIDByTN(currentNode);
            cSectionUIoperate.updateTrackData(filePathOper, this.sIDcurrentTrack); 
        }

        void setUpIDByTN(TreeNode currentNode)
        {
            if (currentNode != null)
            {
                if (currentNode.Level == 0)
                {
                    sJH = currentNode.Name;
                    this.sIDcurrentTrack = "";
                    this.sIDcurrentItem = "";
                    filePathOper = dirSectionData + "//" + sJH + ".xml";
                }
                else if (currentNode.Level == 1)
                {
                    this.sIDcurrentTrack = currentNode.Name;
                    sJH = currentNode.Parent.Name;
                    this.sIDcurrentItem = "";
                    filePathOper = dirSectionData + "//" + sJH + ".xml";
                }
                else if (currentNode.Level == 2)
                {
                    this.sIDcurrentTrack = currentNode.Parent.Name;
                    this.sIDcurrentItem = currentNode.Name;
                    sJH = currentNode.Parent.Parent.Name;
                    filePathOper = dirSectionData + "//" + sJH + ".xml";
                }
                this.tsslblJH.Text = sJH;
                this.tsslblTrack.Text = sIDcurrentTrack;
                this.tsslblDataItem.Text = sIDcurrentItem;
            }
        }

        private void tsmiLogData_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            setUpIDByTN(currentNode);

            if (sIDcurrentItem.StartsWith("idLog"))
            {
                string sJH = cXmlDocSectionWell.getNodeInnerText(this.filePathOper, cXmlDocSectionWell.fullPathJH);
                string sLogName = sIDcurrentItem.Remove(sIDcurrentItem.Length - 12).Remove(0, 5);
                FormSectionDataLog formInputDataTableSingleWell = new
                         FormSectionDataLog(sJH, sLogName, TypeTrack.曲线道.ToString(), filePathOper, sIDcurrentItem);
                formInputDataTableSingleWell.ShowDialog(); 
               
            } 
        }

        private void tsmiTrackDel_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = this.tvSectionEdit.SelectedNode;
            setUpIDByTN(currentNode);
            cSectionUIoperate.deleteItemByID(currentNode, filePathOper, sIDcurrentTrack);
        }

        private void tsmiTrackUp_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = this.tvSectionEdit.SelectedNode;
            setUpIDByTN(currentNode);
            cSectionUIoperate.selectTrackUp(this.tvSectionEdit, currentNode, filePathOper, sIDcurrentTrack);
        }

        private void tsmiTrackDown_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = this.tvSectionEdit.SelectedNode;
            setUpIDByTN(currentNode);
            cSectionUIoperate.selectTrackDown(this.tvSectionEdit, currentNode, filePathOper, this.sIDcurrentTrack);
        }

        private void tsmiShowState_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            if (currentNode != null)
            {
                setUpIDByTN(currentNode);
                FormSettingSectionGeoShowDepth newset = new FormSettingSectionGeoShowDepth(this.filePathSectionCss,this.sJH);
                newset.ShowDialog();
            }
        }

        private void tsmiTrackSetting_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = this.tvSectionEdit.SelectedNode;
            if (currentNode.Level == 1)
            {
                string sIDtrack = currentNode.Name;
                string sJH = currentNode.Parent.Name;
                string filePathOper = dirSectionData + "//" + sJH + ".xml";
                cSectionUIoperate.setNodeProperty(currentNode, filePathOper);
            }
            if (currentNode.Level == 0)
            {
                //string sIDtrack = currentNode.Name;
                //string sJH = currentNode.Parent.Name;
                //string filePathOper = dirSectionData + "//" + sJH + ".xml";
                //cSectionUIoperate.setTrackProperty(currentNode, filePathOper, sIDtrack);
            }
        }

        private void tsmiInsertTrackLog_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            if (currentNode != null)
            {
                setUpIDByTN(currentNode);
                addTrackCss(TypeTrack.曲线道);
            }
        }
        void addTrackCss(TypeTrack eTypeTrack)
        {
            int iTrackWidth = 50;
            if (eTypeTrack == TypeTrack.曲线道) iTrackWidth = 100;
            cXmlDocSectionWell.addTrackCss(filePathOper, eTypeTrack, iTrackWidth);
            updateTVandList();
        }
        private void tsmiInsertTrackCJSJ_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            if (currentNode != null)
            {
                setUpIDByTN(currentNode);
                addTrackCss(TypeTrack.测井解释);
            }
        }

        private void tsmiInsertTrackText_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            if (currentNode != null)
            {
                setUpIDByTN(currentNode);
                addTrackCss(TypeTrack.文本道);
            }
        }

        private void tvSectionEdit_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode selectNode = this.tvSectionEdit.SelectedNode;
            setUpIDByTN(selectNode);
            if (selectNode != null)
            {
                if (selectNode.Level > 0) cSectionUIoperate.setNodeProperty(selectNode, filePathOper);
            } //end if selectNode
        }

        private void tsmiSaveTemplate_Click(object sender, EventArgs e)
        {

        }

        private void tsmiSave_Click(object sender, EventArgs e)
        {
            cProjectManager.save2ProjectResultMap(filePathSVG);
        }

        private void tsmiAddLog_Click(object sender, EventArgs e)
        {
            TreeNode tn = tvSectionEdit.SelectedNode;
            string sJH = tn.Parent.Text;
            if (tn.Tag.ToString() == TypeTrack.曲线道.ToString())
            {
                FormSectionAddWellLog formAddLog = new FormSectionAddWellLog(sJH);
                if (formAddLog.ShowDialog() == DialogResult.OK)
                {
                    ItemLogHeadInfor logHead = formAddLog.logHeadRet;
                    //此处写入配置文件xml,tn.name 是 id
                    cXmlDocSectionWell.addLog(this.filePathOper, tn.Name, logHead);
                }
            }
        }

        private void tsbTreeView_Click(object sender, EventArgs e)
        {
            this.splitContainerSection.Panel1Collapsed = !this.splitContainerSection.Panel1Collapsed;
            if (this.splitContainerSection.Panel1Collapsed == false) updateTVandList();
        }

        private void tsbPageSet_Click(object sender, EventArgs e)
        {
            FormSettingPageFence newSetPage = new FormSettingPageFence(filePathSectionCss);
            if (newSetPage.ShowDialog() == DialogResult.OK) makeSVGmap();
        }

        private void tsmiIntervalMode_Click(object sender, EventArgs e)
        {
            FormSettingModeIntervalFence newSetPage = new FormSettingModeIntervalFence(this.filePathSectionCss, dirSectionData);
            if (newSetPage.ShowDialog() == DialogResult.OK) makeSVGmap();
        }

        private void tsBtnZoonOutHItem2_Click(object sender, EventArgs e)
        {
            setXYview(2.0F);
        }

        void setXYview(float inputHscale)
        {
            //修改每口井的Xview，Yview，在配置文件内
            if (File.Exists(filePathSectionCss))
            {
                XmlDocument XDocSection = new XmlDocument();
                XDocSection.Load(filePathSectionCss);
                //应该换配置文件里面的放大系数
                float fScaleMap = float.Parse(cXmlBase.getNodeInnerText(filePathSectionCss, cXEGeopage.fullPathSacleMap)) ;
                cXmlBase.setNodeInnerText(filePathSectionCss, cXEGeopage.fullPathSacleMap,(fScaleMap*inputHscale).ToString());
            }
            makeSVGmap();
        }

        #region 横向缩放
        private void tsBtnZoonOutHItem1_5_Click(object sender, EventArgs e)
        {
            setXYview(1.5F);
        }

        private void tsBtnZoonOutHItem1_2_Click(object sender, EventArgs e)
        {
            setXYview(1.2F);
        }

        private void tsBtnZoonOutHItem0_5_Click(object sender, EventArgs e)
        {
            setXYview(0.5F);
        }

        private void tsBtnZoonOutHItem0_8_Click(object sender, EventArgs e)
        {
            setXYview(0.8F);
        }

        private void tsBtnZoonOutHItem0_9_Click(object sender, EventArgs e)
        {
            setXYview(0.9F);
        }

        private void tsBtnZoonOutHItem1_1_Click(object sender, EventArgs e)
        {
            setXYview(5.0F);
        }

        #endregion

        private void tsmiRename_Click(object sender, EventArgs e)
        {
            string originalFileName = this.tbgEditView.Text;
            string originalFilePath = Path.Combine(cProjectManager.dirPathUsedProjectData, originalFileName + cProjectManager.fileExtensionSectionFence);
            string originalDir = Path.Combine(cProjectManager.dirPathUsedProjectData, originalFileName);
            FormInputBox inputBox = new FormInputBox("新文件名：", "请输入：", originalFileName);
            var result = inputBox.ShowDialog();
            if (result == DialogResult.OK)
            {
                string newInputFileName = inputBox.ReturnValueStr;
                string newfilepath = originalFilePath.Replace(originalFileName, newInputFileName);
                File.Copy(originalFilePath, newfilepath);
                cPublicMethodBase.DirectoryCopy(originalDir, Path.Combine(cProjectManager.dirPathUsedProjectData, newInputFileName), true);
                File.Delete(originalFilePath);
                if (Directory.Exists(originalDir)) Directory.Delete(originalDir, true);
                dirSectionData = Path.Combine(cProjectManager.dirPathUsedProjectData, newInputFileName);
                filePathSectionCss = Path.Combine(cProjectManager.dirPathUsedProjectData, newInputFileName + cProjectManager.fileExtensionSectionFence);
                this.tbgEditView.Text = newInputFileName;
            }
        }

        private void tsmiTemplateSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog modelFileSaveDialog = new SaveFileDialog();
            modelFileSaveDialog.Filter = "模板文件 | *" + cProjectManager.fileExtensionTemplate;
            modelFileSaveDialog.DefaultExt = cProjectManager.fileExtensionTemplate;
            modelFileSaveDialog.InitialDirectory = cProjectManager.dirPathTemplate;
            if (modelFileSaveDialog.ShowDialog() == DialogResult.OK)
            {
                string xtlFilePath = modelFileSaveDialog.FileName;
                TreeNode currentNode = this.tvSectionEdit.SelectedNode;
                setUpIDByTN(currentNode);
                File.Copy(this.filePathOper, xtlFilePath, true);
                cXmlDocSectionWell.save2XTM(xtlFilePath);
            }
            
        }

        private void tsmiTemplateUse_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = this.tvSectionEdit.SelectedNode;
            setUpIDByTN(currentNode);

            FormSectAddNewWell formNew = new FormSectAddNewWell(2); //通过2 构造函数 通知模板选择 模板从剖面分析来，不要井号
            var result = formNew.ShowDialog();
            if (result == DialogResult.OK)
            {
                string xtlFileName = formNew.ReturnFileNameXMT;
                bool bNew = true;
                if (File.Exists(filePathOper))
                {
                    DialogResult dialogResult = MessageBox.Show("确认应用新模板？", this.sJH + "剖面已存在", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.No) bNew = false;
                }
                if (bNew == true)
                {
                    string xtmPath = Path.Combine(cProjectManager.dirPathTemplate, xtlFileName);
                    cIOtemplate.copyTemplate(xtmPath, filePathOper, this.sJH);
                }
            }
        }

        private void tsmiEditWell_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            if (currentNode != null)
            {
                setUpIDByTN(currentNode);
                float fTopShow = 0;
                XmlNode selectWell = cXmlDocSectionGeo.selectNodeByID(this.filePathSectionCss, this.sJH);
                if (selectWell != null) fTopShow = float.Parse(selectWell["fShowTop"].InnerText);
                FormSectionWell editWell = new FormSectionWell(this.filePathOper, fTopShow);
                if (editWell.ShowDialog() == DialogResult.OK)
                {
                    updateTVandList();
                    makeSVGmap();
                }
            }
        }

        private void tsmiRemoveWell_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            if (currentNode != null)
            {
                setUpIDByTN(currentNode);
                DialogResult dialogResult = MessageBox.Show("确认移除: " + this.sJH, "移除井", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    cXmlDocSectionGeo.deleteWellSelect(this.filePathSectionCss, this.sJH);
                    File.Delete(this.filePathOper);
                    updateTVandList();
                    makeSVGmap();
                }
            }
        }

        private void tsmiInsertWell_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            if (currentNode != null)
            {
                setUpIDByTN(currentNode);
                insertWell();
            }
        }

        void insertWell()
        {
            FormSectAddNewWell formNew = new FormSectAddNewWell();
            var result = formNew.ShowDialog();
            if (result == DialogResult.OK)
            {
                string sJHInsert = formNew.ReturnJH;            //values preserved after close
                string xtlFileName = formNew.ReturnFileNameXMT;
                bool bNew = true;
                filePathOper = dirSectionData + "//" + sJHInsert + ".xml";
                if (File.Exists(filePathOper))
                {
                    DialogResult dialogResult = MessageBox.Show("是否新建并覆盖？", "文件已存在", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.No) bNew = false;
                }
                if (bNew == true)
                {
                    ItemWellSection wellSectionInsert = new ItemWellSection(sJHInsert, 0, 0);
                    //默认把显示深度设为全井段
                    wellSectionInsert.fShowedDepthTop = 0;
                    wellSectionInsert.fShowedDepthBase = wellSectionInsert.fWellBase;
                    Point headView = cCordinationTransform.transRealPointF2ViewPoint(
                   wellSectionInsert.WellPathList[0].dbX, wellSectionInsert.WellPathList[0].dbY, cProjectData.dfMapXrealRefer, cProjectData.dfMapYrealRefer, cProjectData.dfMapScale);
                    wellSectionInsert.fXview = headView.X;
                    wellSectionInsert.fYview = headView.Y;
                    //CSS的加入井 
                    cXmlDocSectionGeo.insertWell(this.filePathSectionCss, this.sJH, wellSectionInsert, 0);
                    //copy文件到目录
                    cIOtemplate.copyTemplate(xtlFileName, filePathOper, wellSectionInsert.sJH, wellSectionInsert.fShowedDepthTop, wellSectionInsert.fShowedDepthBase);
                    updateTVandList();
                    makeSVGmap();
                }
            }
        }

        private void tsBtnZoonInV1_1_Click(object sender, EventArgs e)
        {
            zoomDepth(1.1F); 
        }

        void zoomDepth(float inputUpVscale)
        {
            if (File.Exists(this.filePathSectionCss))
            {
                float fVscale = inputUpVscale * float.Parse(cXmlBase.getNodeInnerText(this.filePathSectionCss, cXEGeopage.fullPathSacleV));
                cXmlBase.setNodeInnerText(this.filePathSectionCss, cXEGeopage.fullPathSacleV, fVscale.ToString("0.00"));
                makeSVGmap();
            }
        }

        private void tsBtnZoonInV1_5_Click(object sender, EventArgs e)
        {
            zoomDepth(1.5F); 
        }

        private void tsBtnZoonInV1_2_Click(object sender, EventArgs e)
        {
            zoomDepth(1.2F); 
        }

        private void tsBtnZoonInV0_9_Click(object sender, EventArgs e)
        {
            zoomDepth(0.9F); 
        }

        private void tsBtnZoonInV0_8_Click(object sender, EventArgs e)
        {
            zoomDepth(0.8F); 
        }

        private void tsBtnZoonInV0_5_Click(object sender, EventArgs e)
        {
            zoomDepth(0.5F); 
        }

        private void tsBtnZoonInV2_Click(object sender, EventArgs e)
        {
            zoomDepth(2F); 
        }

        private void tsmiImportDataFromSectionWell_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            if (currentNode != null)
            {
                setUpIDByTN(currentNode);
                if ( sJH != "")
                {
                    //根据井号找到单井综合图配置文件路径
                    if (File.Exists(filePathOper))
                    {
                        //读取单井文件，读取图道
                        string sTrackID_match = "";
                    }

                }   
            }
          
        }

        private void tsbViewGlobe_Click(object sender, EventArgs e)
        {
            int PageWidth = int.Parse(cXmlBase.getNodeInnerText(filePathSectionCss, cXEGeopage.xmlFullPathPageWidth));
            int PageHeight = int.Parse(cXmlBase.getNodeInnerText(filePathSectionCss, cXEGeopage.xmlFullPathPageHeight));
            cSectionUIoperate.setOffSet(this.webBrowserSVG, new Point(PageHeight, PageWidth));
        }

        private void tsmiViewWell_Click(object sender, EventArgs e)
        {
            TreeNode currentNode = tvSectionEdit.SelectedNode;
            if (currentNode != null)
            {
                setUpIDByTN(currentNode);
                //获取井的位置
                XmlNode selectWell = cXmlDocSectionGeo.selectNodeByID(this.filePathSectionCss, this.sJH);
                if (selectWell != null)
                {
                    float fMapScale = float.Parse(cXmlBase.getNodeInnerText(filePathSectionCss, cXEGeopage.fullPathSacleMap));
                    int xView = (int)(fMapScale*float.Parse(selectWell["Xview"].InnerText));
                    int yView = (int)(fMapScale*float.Parse(selectWell["Yview"].InnerText));
                    cSectionUIoperate.setOffSet(this.webBrowserSVG, new Point(xView, yView));
                }
                
            }
        }

        private void tsmiWellPositionAdjust_Click(object sender, EventArgs e)
        {
            FormSettingFenceWellPositionView newset = new FormSettingFenceWellPositionView(this.filePathSectionCss, this.sJH);
            newset.ShowDialog();
        }

        #region 撤销重做
        private void tsUndo_Click(object sender, EventArgs e)
        {
            undo();
        }
        private void tsRedo_Click(object sender, EventArgs e)
        {
            redo();
        }
        void setUnDoRedoEnable()
        {
            if (RedoList.Count == 0)
            {
                this.tsbRedo.Enabled = false;
                this.tsmiRedo.Enabled = false;
            }
            else
            {
                this.tsbRedo.Enabled = true;
                this.tsmiRedo.Enabled = true;
            }
            if (UndoList.Count <= 1)
            {
                this.tsbUndo.Enabled = false;
                this.tsmiUndo.Enabled = false;
            }
            else
            {
                this.tsbUndo.Enabled = true;
                this.tsmiUndo.Enabled = true;
            }
        }
        void redo()
        {
            if (RedoList.Count > 0)
            {
                iSwichHis = 1;
                UndoList.Push(RedoList.Pop());
                string curHisDir = UndoList.Peek();
                //文件还原
                // 还原联井文件
                string sectionHisCss = Path.Combine(curHisDir, Path.GetFileName(this.filePathSectionCss));
                File.Copy(sectionHisCss, this.filePathSectionCss, true);
                string[] filePaths = Directory.GetFiles(curHisDir, "*.xml");
                foreach (string filePath in filePaths)
                {
                    string newFilePath = Path.Combine(dirSectionData, Path.GetFileName(filePath));
                    File.Copy(filePath, newFilePath, true);
                }
                updateTVandList();
                makeSVGmap();
            }
            setUnDoRedoEnable();
        }
        void undo()
        {
            if (UndoList.Count > 1)
            {
                iSwichHis = 1;
                RedoList.Push(UndoList.Pop());
                if (RedoList.Count > 0)
                {
                    this.tsbRedo.Enabled = true;
                    this.tsmiUndo.Enabled = true;
                }
                string curHisDir = UndoList.Peek();
                //文件还原
                // 还原联井文件
                string sectionCss = Path.Combine(curHisDir, Path.GetFileName(this.filePathSectionCss));
                File.Copy(sectionCss, this.filePathSectionCss, true);
                //还原各井文件
                string[] filePaths = Directory.GetFiles(curHisDir, "*.xml");
                foreach (string filePath in filePaths)
                {
                    string newFilePath = Path.Combine(dirSectionData, Path.GetFileName(filePath));
                    File.Copy(filePath, newFilePath, true);
                }
                updateTVandList();
                makeSVGmap();
            }
            setUnDoRedoEnable();
        }
        #endregion

        private void tsbConnectLayer_Click(object sender, EventArgs e)
        {
             selectConnectMode((int)TypeModeGeoOperate.connectTwo);
        }

        List<string> ltSelectID = new List<string>();
        List<PointD> ltMousePos = new List<PointD>();
        int iOperateMode = 0;

        void selectConnectMode(int iOption)
        {
            ltSelectID.Clear();
            iOperateMode = iOption;
            this.tsslblIDinfor.Text = string.Format("已选择{0}个对象,操作模式{1}", ltSelectID.Count, (TypeModeGeoOperate)iOperateMode);
        }
    }
} 
