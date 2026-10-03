using System;
namespace LordLyrical
{
    public sealed class SynchronizedClock
    {
        public bool Ready{get;private set;}public double Offset{get;private set;}
        public double Rtt{get;private set;}=double.PositiveInfinity;public int Samples{get;private set;}
        double bestRtt=double.PositiveInfinity,last;
        public double Now(double local)=>local+Offset;
        public void Reset(){Ready=false;Samples=0;bestRtt=double.PositiveInfinity;Rtt=double.PositiveInfinity;Offset=0;last=0;}
        public bool Sample(double t0,double serverReceive,double serverSend,double t3)
        {
            double rtt=(t3-t0)-(serverSend-serverReceive),offset=((serverReceive-t0)+(serverSend-t3))*.5;
            if(!double.IsFinite(rtt)||!double.IsFinite(offset)||rtt<0||rtt>.5||t3<t0)return false;
            if(t3-last>15)bestRtt=double.PositiveInfinity;
            if(rtt>bestRtt+.012)return false;
            bestRtt=Math.Min(bestRtt,rtt);Rtt=rtt;last=t3;
            Offset=Ready?Offset+(offset-Offset)*.15:offset;Samples++;Ready=Samples>=3;return true;
        }
    }
}
