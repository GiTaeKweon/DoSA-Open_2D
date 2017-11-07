﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

using System.Runtime.InteropServices;

using Femm;
using Shapes;
using Nodes;
using Parts;
using gtLibrary;
using System.IO;

namespace Scripts
{

    public static class CProgramFEMM
    {
        private static IActiveFEMM m_accessFEMM = null;

        #region Constants

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_SHOW = 5;

        #endregion Constants

        #region APIs
        //-----------------------------------------------------------------------------
        // API 함수 사용
        //-----------------------------------------------------------------------------
        // [주의사항] 꼭 Class 안에 존재해야 함
        //

        [DllImport("user32.dll", EntryPoint = "MoveWindow")]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        //[DllImport("user32.dll")]
        //internal static extern Boolean ShowWindow(IntPtr hWnd, Int32 nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        //-----------------------------------------------------------------------------

        #endregion APIs

        internal static IActiveFEMM myFEMM
        {
            get
            {   
                if (checkFEMM() != true)
                {
                    m_accessFEMM = new ActiveFEMMClass();
                }

                return m_accessFEMM;
            }

            private set
            {
                m_accessFEMM = value;
            }
        }

        public static void moveFEMM(int iPosX, int iPosY, int iSizeX = 500, int iSizeY = 900)
        {
            Process[] processList = Process.GetProcessesByName("femm");

            if (processList.Length > 1)
            {
                CNotice.noticeWarning("FEMM 프로그램이 하나만 실행되어 있어야 합니다.");
                return;
            }

            if (processList.Length != 1)
                return;

            Process femmProcess = processList[0];

            Thread.Sleep(100);
            MoveWindow(femmProcess.MainWindowHandle, iPosX, iPosY, iSizeX, iSizeY, true);
        }

        public static void showFEMM()
        {
            Process[] processList = Process.GetProcessesByName("femm");

            if (processList.Length > 1)
            {
                CNotice.noticeWarning("FEMM 프로그램이 하나만 실행되어 있어야 합니다.");
                return;
            }

            if (processList.Length != 1)
                return;

            Process femmProcess = processList[0];

            Thread.Sleep(100);

            // 윈도우가 최소화 되어 있다면 활성화 시킨다
            ShowWindowAsync(femmProcess.MainWindowHandle, SW_SHOWNORMAL);

            // 윈도우에 포커스를 줘서 최상위로 만든다
            SetForegroundWindow(femmProcess.MainWindowHandle);
        }

        public static void killProcessOfFEMM()
        {
            int nCount = 0;

            Process[] processList = null;

            do
            {
                processList = Process.GetProcessesByName("femm");

                if (processList.Length > 0)
                    processList[0].Kill();

                Thread.Sleep(50);

                // 무한 루프를 방지한다.
                if (nCount > 100)
                    return;

                nCount++;

            } while (processList.Length > 0);

            myFEMM = null;
        }

        private static bool checkFEMM()
        {
            Process[] processList = Process.GetProcessesByName("femm");

            if (processList.Length < 1)
                return false;

            return true;
        }
    }
   
    /// <summary>
    /// 주의사항
    ///  - Script 객체는 스크립트 동작에 대해서만 그룹화하는 데 목적이있다.
    ///  - 내부에서 Script 를 제외한 Design 이나 Face 를 호출하는 동작은 추가하지 말아라.
    /// </summary>
    public class CScriptFEMM
    {
        const int MOVING_GROUP_NUM = 1;

        private string m_strBC;

        //public string prompt(string TextPrompt)
        //{
        //    return sendCommand("prompt (\"" + TextPrompt + "\")");
        //}

        //public bool msgBox(string TextMsgBox)
        //{
        //    sendCommand("messagebox(\"" + TextMsgBox + "\")");
        //    return true;
        //}

        private string sendCommand(string strCommand)
        {
            // ProgramFEMM 은 Static Class 라서 생성없이 바로 사용한다
            string strReturn = CProgramFEMM.myFEMM.call2femm(strCommand);

            if (strReturn.Contains("error")) 
            { 
                CNotice.printTrace(strReturn);
                return "error"; 
            }

            return strReturn;
        }

        public CScriptFEMM()
        {
            try
            {
                m_strBC = "\"" + "BC" + "\"";

                string strCommand;

                // 스크립트 생성과 동시에 전자기장 모델를 시작한다.
                strCommand = "newdocument(0)";
                sendCommand(strCommand);

                strCommand = "mi_probdef(0,\"millimeters\",\"axi\")";
                sendCommand(strCommand);
            
                //strCommand = "mi_setgrid(0.5,\"cart\")";
                strCommand = "mi_hidegrid()";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }
        }

        public void settingPost()
        {
            string strCommand;

            try
            {
                /// If legend is set to -1 all parameters are ignored and default values are used.
                strCommand = "mo_showdensityplot(1, 0, 1.5, 0, \"bmag\")";
                sendCommand(strCommand);

                /// If numcontours is -1 all parameters are ignored and default values are used.
                strCommand = "mo_showcontourplot(-1)";
                sendCommand(strCommand);

                /// 0 is none
                strCommand = "mo_showvectorplot(0,1)";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }
        }

        public void settingPre()
        {
            string strCommand;

            try
            {
                /// If numcontours is -1 all parameters are ignored and default values are used.
                strCommand = "mi_hidegrid()";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }
        }

        public void zoomFit(bool bExceptionZoomout = false)
        {
            string strCommand;

            try
            {
                strCommand = "mi_zoomnatural()";
                sendCommand(strCommand);

                if(bExceptionZoomout != true)
                {
                    strCommand = "mi_zoomout()";
                    sendCommand(strCommand);
                }
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }


        public void drawPoint(double x1, double y1)
        {
            string strCommand;

            float fX1, fY1;

            fX1 = (float)x1;
            fY1 = (float)y1;

            try
            {
                /// Point 을 추가한다
                strCommand = "mi_addnode(" + fX1.ToString() + "," + fY1.ToString() + ")";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }
        }

        public void drawLine(double x1, double y1, double x2, double y2, EMMoving emMoving = EMMoving.FIXED)
        {
            string strCommand;

            float fX1, fY1, fX2, fY2;

            fX1 = (float)x1;
            fY1 = (float)y1;
            fX2 = (float)x2;
            fY2 = (float)y2;

            try
            {
                /// Line 을 추가한다
                strCommand = "mi_addnode(" + fX1.ToString() + "," + fY1.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_addnode(" + fX2.ToString() + "," + fY2.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_addsegment(" + fX1.ToString() + "," + fY1.ToString() + "," + fX2.ToString() + "," + fY2.ToString() + ")";
                sendCommand(strCommand);

                /// 그룹을 지정하는 경우만 변경을 한다.
                if (emMoving == EMMoving.MOVING)
                {
                    /// 그룹 설정
                    ///  - Point 의 선택이 좌표계산 없이 바로 가능함
                    ///  - 또한 Point 만 그룹을 지정해도 이동이 가능하기 때문에 Point 만 설정함
                    strCommand = "mi_selectnode(" + fX1.ToString() + "," + fY1.ToString() + ")";
                    sendCommand(strCommand);

                    strCommand = "mi_selectnode(" + fX2.ToString() + "," + fY2.ToString() + ")";
                    sendCommand(strCommand);

                    strCommand = "mi_setgroup(" + MOVING_GROUP_NUM.ToString() + ")";
                    sendCommand(strCommand);

                    strCommand = "mi_clearselected()";
                    sendCommand(strCommand);      
                }
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        public void drawLine(CLine line, EMMoving emMoving = EMMoving.FIXED)
        {
            string strCommand;

            float fX1, fY1, fX2, fY2;

            fX1 = (float)line.m_startPoint.m_dX;
            fY1 = (float)line.m_startPoint.m_dY;
            fX2 = (float)line.m_endPoint.m_dX;
            fY2 = (float)line.m_endPoint.m_dY;

            try
            {
                strCommand = "mi_addnode(" + fX1.ToString() + "," + fY1.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_addnode(" + fX2.ToString() + "," + fY2.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_addsegment(" + fX1.ToString() + "," + fY1.ToString() + "," + fX2.ToString() + "," + fY2.ToString() + ")";
                sendCommand(strCommand);

                /// 그룹을 지정하는 경우만 변경을 한다.
                if (emMoving == EMMoving.MOVING)
                {
                    /// 그룹 설정
                    ///  - Point 의 선택이 좌표계산 없이 바로 가능함
                    ///  - 또한 Point 만 그룹을 지정해도 이동이 가능하기 때문에 Point 만 설정함
                    strCommand = "mi_selectnode(" + fX1.ToString() + "," + fY1.ToString() + ")";
                    sendCommand(strCommand);

                    strCommand = "mi_selectnode(" + fX2.ToString() + "," + fY2.ToString() + ")";
                    sendCommand(strCommand);

                    strCommand = "mi_setgroup(" + MOVING_GROUP_NUM.ToString() + ")";
                    sendCommand(strCommand);

                    strCommand = "mi_clearselected()";
                    sendCommand(strCommand);      
                }
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        public void drawArc(double x1, double y1, double x2, double y2, bool bDirectionArcBackword, EMMoving emMoving = EMMoving.FIXED)
        {
            string strCommand;

            float fX1, fY1, fX2, fY2;

            fX1 = (float)x1;
            fY1 = (float)y1;
            fX2 = (float)x2;
            fY2 = (float)y2;

            try
            {
                strCommand = "mi_addnode(" + fX1.ToString() + "," + fY1.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_addnode(" + fX2.ToString() + "," + fY2.ToString() + ")";
                sendCommand(strCommand);

                if (bDirectionArcBackword == true)
                {
                    strCommand = "mi_addarc(" + fX2.ToString() + "," + fY2.ToString() + "," + fX1.ToString() + "," + fY1.ToString() + "," + "90, 1)";
                    sendCommand(strCommand);
                }
                else
                {
                    strCommand = "mi_addarc(" + fX1.ToString() + "," + fY1.ToString() + "," + fX2.ToString() + "," + fY2.ToString() + "," + "90, 1)";
                    sendCommand(strCommand);
                }

                /// 그룹을 지정하는 경우만 변경을 한다.
                if (emMoving == EMMoving.MOVING)
                {
                    /// 그룹 설정
                    ///  - Point 의 선택이 좌표계산 없이 바로 가능함
                    ///  - 또한 Point 만 그룹을 지정해도 이동이 가능하기 때문에 Point 만 설정함
                    strCommand = "mi_selectnode(" + fX1.ToString() + "," + fY1.ToString() + ")";
                    sendCommand(strCommand);

                    strCommand = "mi_selectnode(" + fX2.ToString() + "," + fY2.ToString() + ")";
                    sendCommand(strCommand);

                    strCommand = "mi_setgroup(" + MOVING_GROUP_NUM.ToString() + ")";
                    sendCommand(strCommand);

                    strCommand = "mi_clearselected()";
                    sendCommand(strCommand);      
                }
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        public void addCircuitProp(string strCircuit, double dCurrent)
        {

            string strCommand;

            try
            {
                strCircuit = "\"" + strCircuit + "\"";

                // 마지막 파라메타 1 은 Serial 방식으로 추후 Turns 입력이 필요한다.
                strCommand = "mi_addcircprop(" + strCircuit + "," + dCurrent.ToString() + ",1)";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        public void setBlockProp(CPoint point,
                                string strMaterial, 
                                double dMeshsize, 
                                string strCircuit, 
                                double dMagnetAngle, 
                                EMMoving emMoving, 
                                int nTurns)
        {
            string strCommand;
            
            try
            {
                /// mode 변경 없이도 동작은 한다.
                strCommand = "mi_seteditmode(\"blocks\")";
                sendCommand(strCommand);

                strCommand = "mi_addblocklabel(" + point.m_dX + "," + point.m_dY + ")";
                sendCommand(strCommand);

                strCommand = "mi_selectlabel(" + point.m_dX + "," + point.m_dY + ")";
                sendCommand(strCommand);

                int nGroup;

                if (emMoving == EMMoving.MOVING)
                    nGroup = MOVING_GROUP_NUM;
                else
                    nGroup = 0;

                strMaterial = "\"" + strMaterial + "\"";
                strCircuit = "\"" + strCircuit + "\"";

                strCommand = "mi_setblockprop(" + strMaterial
                             + ",0," + dMeshsize.ToString() + "," + strCircuit
                             + "," + dMagnetAngle.ToString() + ","
                             + nGroup.ToString() + "," + nTurns.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_clearselected()";
                sendCommand(strCommand);

                // editmode 를 group 으로 바꾸어서 FEMM 마우스 동작을 막는다.
                lockEdit();
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        /// <summary>
        /// 주의사항
        ///  - 형상과 Group 이 지정된 후에 호출되어야 한다.
        /// </summary>
        public void moveMovingParts(double dMovingStroke)
        {
            string strCommand;

            try
            {
                /// mode 변경 없이도 동작은 한다.
                strCommand = "mi_seteditmode(\"group\")";
                sendCommand(strCommand);

                strCommand = "mi_selectgroup(" + MOVING_GROUP_NUM.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_movetranslate(" + "0," + dMovingStroke.ToString() + ")";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        internal void getMaterial(string strMaterial)
        {
            string strCommand;

            strMaterial = "\"" + strMaterial + "\"";

            try
            {
                strCommand = "mi_getmaterial(" + strMaterial + ")";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        internal void addBoundaryConditon()
        {
            string strCommand;

            try
            {
                strCommand = "mi_addboundprop(" + m_strBC + ",0,0,0,0,0,0,0,0,3)";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }
        }

        internal void drawBoundaryLine(double x1, double y1, double x2, double y2)
        {
            string strCommand;

            float fX1, fY1, fX2, fY2, fCenterX, fCenterY;

            fX1 = (float)x1;
            fY1 = (float)y1;
            fX2 = (float)x2;
            fY2 = (float)y2;
            fCenterX = (fX1 + fX2) / 2.0f;
            fCenterY = (fY1 + fY2) / 2.0f;

            try
            {
                /// Line 을 추가한다
                strCommand = "mi_addnode(" + fX1.ToString() + "," + fY1.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_addnode(" + fX2.ToString() + "," + fY2.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_addsegment(" + fX1.ToString() + "," + fY1.ToString() + "," + fX2.ToString() + "," + fY2.ToString() + ")";
                sendCommand(strCommand);


                /// 경계조건을 부여한다.
                strCommand = "mi_selectsegment(" + fCenterX.ToString() + "," + fCenterY.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_setsegmentprop(" + m_strBC + ",0,0)";
                sendCommand(strCommand);

                strCommand = "mi_clearselected()";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        internal void setRegionBlockProp(CPoint blockPoint, double dMeshSize)
        {
            string strCommand;

            try
            {
                strCommand = "mi_addblocklabel(" + blockPoint.m_dX + "," + blockPoint.m_dY + ")";
                sendCommand(strCommand);

                strCommand = "mi_selectlabel(" + blockPoint.m_dX + "," + blockPoint.m_dY + ")";
                sendCommand(strCommand);

                /// Region 의 물성치를 Default 물성치로 지정하여 Block 이 추가되지 않은 영역을 설정 한다.
                /// 
                strCommand = "mi_attachdefault()";
                sendCommand(strCommand);

                string strMaterial = "\"" + "Air" + "\"";

                if(dMeshSize == 0)
                    strCommand = "mi_setblockprop(" + strMaterial + ",1,0,\"none\",0,0,0)";
                else
                    strCommand = "mi_setblockprop(" + strMaterial + ",0," + dMeshSize.ToString() + ",\"none\",0,0,0)";
                sendCommand(strCommand);

                strCommand = "mi_clearselected()";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        internal void saveAs(string strExperimentFullName)
        {
            string strCommand;

            try
            {
                //-------------------------------------------------------------
                // 아주 중요
                //-------------------------------------------------------------
                //
                // 디렉토리에 들어있는 \\ 기호는 FEMM 에서 인식하지 못한다.
                // 따라서 디렉토리안의 \\ 기호를 / 기호로 변경한다
                strExperimentFullName = strExperimentFullName.Replace("\\", "/");
                //-------------------------------------------------------------

                strExperimentFullName = "\"" + strExperimentFullName + "\"";

                strCommand = "mi_saveas(" + strExperimentFullName + ")";
                sendCommand(strCommand);    
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }
        
        public double solveForce(string strFieldImageFullName = null)
        {
            string strCommand;
            double dForce;

            try
            {
                strCommand = "mi_analyse()";
                sendCommand(strCommand);

                strCommand = "mi_loadsolution()";
                sendCommand(strCommand);

                /// mi_loadsolution() 후에 호출해야 한다.
                /// 깜빡임이 심해서 이미지 저장때문 사용한다.            
                if (strFieldImageFullName != null)
                    settingPost();

                strCommand = "mi_seteditmode(\"group\")";
                sendCommand(strCommand);

                strCommand = "mo_groupselectblock(" + MOVING_GROUP_NUM.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mo_blockintegral(19)";
                dForce = Double.Parse(sendCommand(strCommand));

                strCommand = "mo_clearblock()";
                sendCommand(strCommand);

                if (null != strFieldImageFullName)
                {
                    //-------------------------------------------------------------
                    // 아주 중요
                    //-------------------------------------------------------------
                    //
                    // 디렉토리에 들어있는 \\ 기호는 FEMM 에서 인식하지 못한다.
                    // 따라서 디렉토리안의 \\ 기호를 / 기호로 변경한다
                    strFieldImageFullName = strFieldImageFullName.Replace("\\", "/");
                    //-------------------------------------------------------------

                    strFieldImageFullName = "\"" + strFieldImageFullName + "\"";

                    strCommand = "mo_savebitmap(" + strFieldImageFullName + ")";
                    sendCommand(strCommand);
                }
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return 0;
            }

            
            return dForce;
        }

        internal void lockEdit()
        {
            string strCommand;

            // edit 모드를 group 으로 지정해서 마우스 동작을 막는다.
            try
            {
                /// nodes, segments, arcsegments, blocks, group
                strCommand = "mi_seteditmode(\"group\")";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }
        }

        internal void selectLine(CPoint selectPoint)
        {
            string strCommand;

            try
            {
                /// nodes, segments, arcsegments, blocks, group
                strCommand = "mi_seteditmode(\"segments\")";
                sendCommand(strCommand);

                strCommand = "mi_selectsegment(" + selectPoint.m_dX + "," + selectPoint.m_dY + ")";
                sendCommand(strCommand);

                /// editmode 를 group 으로 바꾸어서 FEMM 마우스 동작을 막는다.
                /// - refreshView() 전에 실행해야 한다. 
                lockEdit();

                /// refresh 를 꼭 해야 색상이 변한다
                refreshView();
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        /// <summary>
        /// 1. 문제점
        /// FEMM 프로세스로 FEMM 의 실행여부를 확인할 수 있으나,
        /// ActiveFEMMClass() 로 생성된 FEMM 을 사용자가 종료하면 FEMM 프로세스가 정리되지 않는 문제가 있다.
        /// 
        /// 2. 해결방안
        /// FEMM 이 살아있는지를 확인하기 위하여 임의의 한점을 생성하고 위치를 확인하는 방법을 사용한다.
        /// </summary>
        public bool checkOpen()
        {
            string strCommand;
            string strReturn;

            double farX = 1e10;
            double farY = 1e10;

            try
            {
                // 확인점을 추가, 선택, 삭제하기 전에 기존 선택된 객체들은 선택을 해제 해주어야 한다.
                strCommand = "mi_clearselected()";
                sendCommand(strCommand);

                /// nodes, segments, arcsegments, blocks, group
                strCommand = "mi_seteditmode(\"nodes\")";
                sendCommand(strCommand);

                /// 아주 먼곳에 임의의 한점을 생성한다
                strCommand = "mi_addnode(" + farX.ToString() + "," + farY.ToString() + ")";
                sendCommand(strCommand);
                        
                strCommand = "mi_selectnode(" + farX.ToString() + "," + farY.ToString() + ")";
                strReturn = sendCommand(strCommand);

                /// 확인 후 삭제한다
                strCommand = "mi_deleteselectednodes()";
                sendCommand(strCommand);

                /// editmode 를 group 으로 바꾸어서 FEMM 마우스 동작을 막는다.
                /// - refreshView() 전에 실행해야 한다. 
                lockEdit();

                refreshView();
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return false;
            }

            if (strReturn == "error")
                return false;
            else
                return true;

        }

        internal void clearSelected()
        {
            string strCommand;

            try
            {
                strCommand = "mi_clearselected()";
                sendCommand(strCommand);

                /// refresh 를 꼭 해야 색상이 변한다
                refreshView();
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        public void refreshView()
        {
            string strCommand;

            try
            {
                /// refresh 를 꼭 해야 색상이 변한다
                strCommand = "mi_refreshview()";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        public void deleteAll()
        {
            string strCommand;

            try
            {
                /// 기본 그룹을 삭제한다.
                strCommand = "mi_selectgroup(" + 0.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_deleteselected()";
                sendCommand(strCommand);

                /// 이동 그룹을 삭제한다.
                strCommand = "mi_selectgroup(" + MOVING_GROUP_NUM.ToString() + ")";
                sendCommand(strCommand);

                strCommand = "mi_deleteselected()";
                sendCommand(strCommand);

                /// refresh 를 꼭 해야 색상이 변한다
                refreshView();
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        public void closePost()
        {
            string strCommand;

            try
            {
                /// refresh 를 꼭 해야 색상이 변한다
                strCommand = "mo_close()";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        internal void selectArc(CPoint selectPoint)
        {
            string strCommand;

            try
            {
                /// nodes, segments, arcsegments, blocks, group
                strCommand = "mi_seteditmode(\"arcsegments\")";
                sendCommand(strCommand);

                strCommand = "mi_selectarcsegment(" + selectPoint.m_dX + "," + selectPoint.m_dY + ")";
                sendCommand(strCommand);

                /// editmode 를 group 으로 바꾸어서 FEMM 마우스 동작을 막는다.
                /// - refreshView() 전에 실행해야 한다. 
                lockEdit();

                /// refresh 를 꼭 해야 색상이 변한다
                refreshView();
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }

        }

        internal void closeDesign()
        {
            string strCommand;

            try
            {
                /// nodes, segments, arcsegments, blocks, group
                strCommand = "mi_close()";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }
        }

        internal bool attachDefault(string strExperimentFullName, CPoint pointBoundaryBlock)
        {
            CReadFile readFile = new CReadFile();
            CManageFile manageFile = new CManageFile();

            List<string> listString = new List<string>();
            string strLine = string.Empty;
            char[] separators = { ' ', '\t' };
            string[] strArray;
 
            string strFileName = Path.GetFileNameWithoutExtension(strExperimentFullName);
            string strTempFileFullName = Path.Combine(Path.GetDirectoryName(strExperimentFullName), strFileName + "_temp.fem");

            if (manageFile.isExistFile(strExperimentFullName) == false)
            {
                CNotice.printTrace("FEMM 파일이 없습니다.");
                return false;
            }
            else
            {
                File.Move(strExperimentFullName, strTempFileFullName);
            }

            StreamWriter writeFile = new StreamWriter(strExperimentFullName);
            int iNumBlock = 0;
            int nCountBlock = 0;
            bool bBlockLabels = false;

            try
            {
                readFile.readAllLines(strTempFileFullName, ref listString);
  
                for (int i = 0; i < listString.Count; i++)
                {
                    strLine = listString[i];

                    strArray= strLine.Split(separators, StringSplitOptions.None);

                    if (strArray[0] == "[NumBlockLabels]")
                    {
                        iNumBlock = Int32.Parse(strArray[2]);
                        nCountBlock = 0;
                        bBlockLabels = true;

                        writeFile.WriteLine(strLine);

                        /// 구분 Label 행은 건너 뛴다.
                        continue;
                    }

                    if(bBlockLabels == true)
                    {
                        if(pointBoundaryBlock.m_dX == Double.Parse(strArray[0]) && pointBoundaryBlock.m_dY == Double.Parse(strArray[1]))
                        {
                            if(strArray.Length != 9)
                            {
                                CNotice.printTrace("BlockLabels 라인을 읽을때 문제가 발생했습니다.");
                                return false;
                            }

                            /// dettach block setting
                            strArray[8] = "2";
                            strLine = string.Empty;

                            foreach(string str in strArray)
                            {
                                strLine += str + '\t';
                            }
                        }

                        nCountBlock++;

                        if(nCountBlock >= iNumBlock)
                            bBlockLabels = false;
                    }

                    writeFile.WriteLine(strLine);
                }

                File.Delete(strTempFileFullName);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                writeFile.Close();
                return false;
            }

            writeFile.Close();
            return true;
        }

        internal void openDesign(string strExperimentFullName)
        {
            string strCommand;

            //-------------------------------------------------------------
            // 아주 중요
            //-------------------------------------------------------------
            //
            // 디렉토리에 들어있는 \\ 기호는 FEMM 에서 인식하지 못한다.
            // 따라서 디렉토리안의 \\ 기호를 / 기호로 변경한다
            strExperimentFullName = strExperimentFullName.Replace("\\", "/");
            //-------------------------------------------------------------

            strExperimentFullName = "\"" + strExperimentFullName + "\"";

            try
            {
                /// nodes, segments, arcsegments, blocks, group
                strCommand = "open(" + strExperimentFullName + ")";
                sendCommand(strCommand);
            }
            catch (Exception ex)
            {
                CNotice.printTrace(ex.Message);
                return;
            }
        }
    } 
}
