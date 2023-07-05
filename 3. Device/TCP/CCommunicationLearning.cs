using Lib.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vila.Communication.Net;

namespace MvcVisionSystem._3._Device.TCP
{
    public class CCommunicationLearning
    {
        public enum CommandLearning
        {
            StartTraining,
            StopTraining,
            StartDefect,
            StopDefect,
        }

        private CTCPAsync Yolov5Comm { get; set; } = new CTCPAsync();
        
        public CCommunicationLearning()
        {
            Yolov5Comm.IsStringData = true;                    // 문자열로 데이터를 처리한다.
            Yolov5Comm.IsStringUnicode = false;                 // ASCII 
            Yolov5Comm.IsAutoConnectTry = true;                 // 연결이 끊어질 경우 자동으로 Retry를 수행한다.

            Yolov5Comm.nID = 2;                                 // 통신 ID는 1
            Yolov5Comm.sName = "Vision_TCP";                     // Vision 과의 연결 TCP/IP 통신
            
            Yolov5Comm.SetCallbackReceive(OnServerReceiveFunction);
            Yolov5Comm.SetCallbackConnect(OnServerConnectFunction);
            Yolov5Comm.SetCallbackDisconnect(OnServerDisconnectFunction);

            Yolov5Comm.SetListen();
        }

        public void Send(String data )
        {
            try
            {
                Yolov5Comm.Send(data );
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
            }
        }

        public void SendData(String data, Bitmap bitmap)
        {
            try
            {
                Yolov5Comm.SendData(data, (Image)bitmap);
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
            }
        }

        private void OnServerReceiveFunction(IAsyncResult ar)
        {
            byte[] byData;
            string sMsg;

            while (Yolov5Comm.GetByteData(out byData))
            {
                sMsg = Encoding.ASCII.GetString(byData, 0, byData.Length);

                switch (sMsg)
                {
                    case "StartTraining":
                        //StartTraining();                        
                        break;
                    case "StopTraining":
                        //StopTraining();
                        break;
                    case "StartDefect":
                        //StartDefect();
                        break;
                    case "StopDefect":
                        //StopDefect();
                        break;
                    default:
                        Console.WriteLine($"Unknown command: {sMsg}");
                        break;
                }

                //m_strRecvBuffer += sMsg;

                //RecvieMeaasge(sMsg);           // 수신된 데이터 처리

                //if (IGlobal.Instance.System.UseCim)
                //{
                //    RecvieMeaasge(sMsg);           // 수신된 데이터 처리
                //}
                //else
                //{
                //    CLogger.WriteLog(LOG.Comm, "CIM NOT USE");
                //    CLogger.WriteLog(LOG.NORMAL, "[Success] Recive Message --> {0}", sMsg);
                //}
            }
        }

        // Clinet가 연결에 성공했을 때
        private void OnServerConnectFunction(IAsyncResult ar)
        {
            CTCPAsync socket = (ar.AsyncState as CTCPAsync);
            CLOG.COMM($"---- Server connect success ID:{socket.nID}, Name:{socket.sName}");
        }

        // Clinet가 연결이 끊어졌을 때
        private void OnServerDisconnectFunction(IAsyncResult ar)
        {
            CTCPAsync socket = (ar.AsyncState as CTCPAsync);
            CLOG.COMM($"**** Server Disconnect ID:{socket.nID}, Name:{socket.sName}");

            // m_log2.Write($"**** Server Disconnect ID:{socket.nID}, Name:{socket.sName}");
        }
    }
}
