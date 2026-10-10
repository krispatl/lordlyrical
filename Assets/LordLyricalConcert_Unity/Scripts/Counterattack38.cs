using System;
using UnityEngine;
namespace LordLyrical {
 public static class Counterattack38 {
  public static CounterCharge38[] Empty(){var a=new CounterCharge38[10];for(int i=0;i<10;i++)a[i]=new CounterCharge38();return a;}
  public static bool Valid(CounterCharge38[] a)=>a!=null&&a.Length==10&&Array.TrueForAll(a,c=>c!=null&&float.IsFinite(c.expires)&&float.IsFinite(c.releaseAt)&&c.releaseSerial>=0&&c.hand>=0&&c.hand<2);
  public static void Grant(CounterCharge38 c,float t){if(c.expires<=t&&!Enhanced(c,t,c.hand))c.expires=t+8;}
  public static void Cancel(CounterCharge38 c){c.expires=c.releaseAt=-100;}
  public static bool Enhanced(CounterCharge38 c,float t,int hand)=>c.releaseSerial>0&&c.hand==hand&&t>=c.releaseAt&&t<c.releaseAt+1;
  public static float Beam(CounterCharge38 c,float t,int hand){
   if(c.expires>t){c.expires=-100;c.releaseAt=t;c.releaseSerial++;c.hand=hand;}
   return Enhanced(c,t,hand)?2:1;
  }
 }
}
