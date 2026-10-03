using System;
using UnityEngine;

namespace LordLyrical
{
    public enum RootKey { C, Cs, D, Ds, E, F, Fs, G, Gs, A, Bb, B }
    public enum Faction { StoneChoir, HighElves, Pitch, Prowl, Founders, Howlers }
    public enum SessionPhase { Boot, InitializingXR, ConnectingNetwork, WaitingForAssignment, Ready, Armed, Countdown, Running, Ending, Complete, Resetting, Error }
    [Serializable] public class ParticipantSlot
    {
        public RootKey key; public string archetype; public Faction faction; public Color color;
        public Vector3 spawn; public string ability;
        public ParticipantSlot(RootKey k,string a,Faction f,string hex,string power,int index)
        {key=k;archetype=a;faction=f;ColorUtility.TryParseHtmlString(hex,out color);ability=power;
         float angle=(index/10f*240-120)*Mathf.Deg2Rad;spawn=new Vector3(Mathf.Sin(angle)*4.2f,0,-2+Mathf.Cos(angle)*2.8f);}
    }
    public static class RootKeys
    {
        public static readonly ParticipantSlot[] Slots={
            new(RootKey.D,"The Warrior",Faction.StoneChoir,"#E88A20","AmplitudeStrike",0),
            new(RootKey.C,"The Foundation",Faction.StoneChoir,"#AF7F24","AmplitudeStrike",1),
            new(RootKey.E,"The Blade",Faction.HighElves,"#168DFF","ResonanceShatter",2),
            new(RootKey.G,"The Balanced",Faction.HighElves,"#A0D5BC","ChordBarrage",3),
            new(RootKey.F,"The Healer",Faction.HighElves,"#FFDC82","ResonantField",4),
            new(RootKey.B,"The Shadow",Faction.Pitch,"#6754DE","InfrasoundDread",5),
            new(RootKey.Cs,"The Seeker",Faction.Prowl,"#A3F5FF","Sonar",6),
            new(RootKey.Fs,"The Architect",Faction.Founders,"#DBEDFF","FloorRebuild",7),
            new(RootKey.Bb,"The Discordant",Faction.Howlers,"#F02F62","AntiFrequency",8),
            new(RootKey.A,"The Ancient",Faction.Founders,"#FFBA48","HarmonicSupport",9)};
        public static Color Spectrum(float phase)=>Color.HSVToRGB(Mathf.Repeat(phase,1),.75f,1);
    }
    [Serializable] public class ShowSnapshot
    {
        public string bootId,phase,mode,sdk;public int revision,runId,authorityClientNo,protocol;
        public double epoch,duration;
        public bool IsValid=>!string.IsNullOrEmpty(bootId)&&protocol==8&&sdk=="0.17.4"&&duration==121&&
                             !double.IsNaN(epoch)&&!double.IsInfinity(epoch)&&Enum.TryParse<SessionPhase>(phase,out _);
    }
    [Serializable] public class ParticipantRecord { public int slot,clientNo;public string deviceId,key;public bool connected; }
    [Serializable] public class Roster {public ParticipantRecord[] participants=Array.Empty<ParticipantRecord>();}
    [Serializable] public class Cue
    {public string id,category,action;public double time,authoredTime,duration;public int roles=-1,seed;public int[] vfx=Array.Empty<int>();}
    [Serializable] public class CueTable { public Cue[] cues=Array.Empty<Cue>(); }
    public struct ShowFrame
    {
        public double time,musicTime;public bool vortex,fissured,repaired,dragonFractured,art,deepSonar,title;
        public float wallActivation,dragonReveal,dragonDissolve,amplitude,titleProgress;
        public static ShowFrame At(double t)
        {
            t=Math.Clamp(t,0,121);
            return new ShowFrame{time=t,musicTime=t,vortex=t<21,fissured=t>=26&&t<52,repaired=t>=52&&t<63,
                dragonFractured=t>=64.673,art=t>=98,deepSonar=t>=83&&t<90,title=t>=111,
                wallActivation=t>=59&&t<61?1:0,dragonReveal=Mathf.Clamp01((float)((t-63)/6)),
                dragonDissolve=Mathf.Clamp01((float)((t-98)/3)),amplitude=t>=92?1:.3f,
                titleProgress=Mathf.Clamp01((float)((t-111)/8))};
        }
    }
}
