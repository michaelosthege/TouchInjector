using TUIO;

namespace TCD.Sys.TUIO
{
    internal class TuioChannelHelper : TuioListener
    {
        #region Events
        //OnIncomingTuioCursor
        public delegate void AddOnIncomingTuioCursorDelegate(TuioCursor cur, IncomingType type);
        public static event AddOnIncomingTuioCursorDelegate OnIncomingTuioCursor;

        //OnTuioRefresh
        public delegate void AddOnTuioRefreshDelegate(TuioTime t);
        public static event AddOnTuioRefreshDelegate OnTuioRefresh;
        #endregion

        #region Incoming
        public void addTuioObject(TuioObject o) {}
        public void updateTuioObject(TuioObject o) {}
        public void removeTuioObject(TuioObject o) {}

        public void addTuioCursor(TuioCursor c)
        {
            try { OnIncomingTuioCursor(c, IncomingType.New); }
            catch { }
        }

        public void updateTuioCursor(TuioCursor c)
        {
            try { OnIncomingTuioCursor(c, IncomingType.Update); }
            catch { }
        }

        public void removeTuioCursor(TuioCursor c)
        {
            try { OnIncomingTuioCursor(c, IncomingType.Remove); }
            catch { }
        }

        public void addTuioBlob(TuioBlob b) { }
        public void updateTuioBlob(TuioBlob b) { }
        public void removeTuioBlob(TuioBlob b) { }

        public void refresh(TuioTime frameTime)
        {
            try { OnTuioRefresh(frameTime); }
            catch { }
        }
        #endregion
    }
}
