using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUIO;

namespace TCD.Sys.TUIO
{
    public class TuioCursorContainer
    {
        public TuioCursor TuioCursor { get; set; }
        public IncomingType Type { get; set; }

        public TuioCursorContainer(TuioCursor cur, IncomingType type)
        {
            TuioCursor = cur;
            Type = type;
        }
    }
}
