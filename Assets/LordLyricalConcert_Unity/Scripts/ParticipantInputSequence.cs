using System;
namespace LordLyrical
{
    // The epoch is advertised through the sender's own network client variables.
    // A reconnect can restart its sequence without admitting delayed old-session packets.
    public sealed class ParticipantInputSequence
    {
        readonly int[] last=new int[10],owners=new int[10];
        readonly string[] epochs=new string[10];
        int run=int.MinValue;
        public static int OwnerOf(Roster roster,int slot)
        {
            if(roster?.participants!=null)
                foreach(var p in roster.participants)
                    if(p!=null&&p.connected&&p.slot==slot)return p.clientNo;
            return int.MinValue;
        }
        public bool TryAccept(int currentRun,int packetRun,int slot,int sender,int owner,
                              string epoch,string advertisedEpoch,int sequence)
        {
            if(slot<0||slot>=10||packetRun!=currentRun||sender!=owner||sequence<=0||
               string.IsNullOrEmpty(epoch)||epoch.Length!=32||epoch!=advertisedEpoch)return false;
            if(run!=currentRun){Array.Clear(last,0,last.Length);Array.Clear(epochs,0,epochs.Length);run=currentRun;}
            bool replacement=owners[slot]!=sender||epochs[slot]!=epoch;
            if(!replacement&&sequence<=last[slot])return false;
            owners[slot]=sender;epochs[slot]=epoch;last[slot]=sequence;return true;
        }
    }
}
