using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips
{
    public class BetterStopwatch : Stopwatch
    {
        long _startOffset;

        public BetterStopwatch(long startOffsetMS)
        {
            _startOffset = startOffsetMS;
        }

        public new long ElapsedMilliseconds
        {
            get { return _startOffset + base.ElapsedMilliseconds; }
        }
    }
}
