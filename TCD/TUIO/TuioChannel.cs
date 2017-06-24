using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUIO;

namespace TCD.Sys.TUIO
{
    public class TuioChannel
    {
        #region Events
        //OnIncomingTuioCursor
        public delegate void AddOnIncomingTuioCursorDelegate(TuioCursor cur, IncomingType type);
        public static event AddOnIncomingTuioCursorDelegate OnIncomingTuioCursor;
        public static void OnIncomingTuioCursorEvent(TuioCursor cur, IncomingType type)
        {
            try { OnIncomingTuioCursor(cur, type); }
            catch { }
        }       
        //OnTuioRefresh
        public delegate void AddOnTuioRefreshDelegate(TuioTime t);
        public static event AddOnTuioRefreshDelegate OnTuioRefresh;
        public static void OnTuioRefreshEvent(TuioTime t)
        {
            try { OnTuioRefresh(t); }
            catch { }
        }
        #endregion

        private TuioClient client;
        private TuioChannelHelper helper;

        public Dictionary<long, TuioCursorContainer> CursorList { get; set; }
        public bool IsRunning { get; set; }
        private object cursorSync = new object();

        public TuioChannel()
        {
            TuioChannelHelper.OnIncomingTuioCursor += TuioChannelHelper_OnIncomingTuioCursor;
            TuioChannelHelper.OnTuioRefresh += TuioChannelHelper_OnTuioRefresh;
            helper = new TuioChannelHelper();
        }

        public bool Connect(int port = 3333)
        {
            CursorList = new Dictionary<long, TuioCursorContainer>();
            try
            {
                client = new TuioClient(port);
                client.addTuioListener(helper);
                client.connect();
                IsRunning = true;
                return true;
            }
            catch
            {
                IsRunning = false;
                return false;
            }
        }
        public void Disconnect()
        {
            if (client == null) return;
            client.removeTuioListener(helper);
            client.disconnect();
            IsRunning = false;
        }
        private void TuioChannelHelper_OnIncomingTuioCursor(TuioCursor c, IncomingType type)
        {
            switch (type)
            {
                case IncomingType.New:
                    lock (cursorSync)
                        CursorList.Add(c.SessionID, new TuioCursorContainer(c, type));
                    break;
                case IncomingType.Update:
                    lock (cursorSync)
                    {
                        CursorList[c.SessionID].TuioCursor.update(c);
                        CursorList[c.SessionID].Type = type;
                    }
                    break;
                case IncomingType.Remove:
                    lock (cursorSync)
                        CursorList[c.SessionID].Type = type;
                    break;
            }
        }
        private void TuioChannelHelper_OnTuioRefresh(TuioTime t)
        {
            OnTuioRefreshEvent(t);
        }
    }
}
