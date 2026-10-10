using UnityEngine;

namespace LordLyrical.Characters
{
    // Authored from the user's jerumovements.mp4 frame study; not extracted mocap.
    // Times are narrative seconds. Contact/recovery phases are independent of the base clip.
    public static class JeruReferenceScore
    {
        public enum Motif { Sweep, Accent, Point, Present, Listen, Gather, Open, Salute }
        public readonly struct Cue
        {
            public readonly float peak,prepare,attack,hold,recover,power,gaze;
            public readonly int side;
            public readonly Motif motif;
            public Cue(float peak,Motif motif,int side,float power,float gaze,
                       float prepare=.25f,float attack=.32f,float hold=.16f,float recover=.60f)
            {
                this.peak=peak;this.motif=motif;this.side=side;this.power=power;this.gaze=gaze;
                this.prepare=prepare;this.attack=attack;this.hold=hold;this.recover=recover;
            }
            public float Start=>peak-attack-prepare;
            public float End=>peak+hold+recover;
        }
        // Left-hand emphasis reflects the reference. The free support hand rests naturally
        // in our microphone-free staging rather than gripping an invisible microphone.
        public static readonly Cue[] Cues={
            new(1.9f,Motif.Salute,0,.55f,0,.3f,.45f,.2f,.65f),
            new(7.0f,Motif.Accent,0,.65f,-6,.3f,.4f,.15f,.65f),
            new(12.4f,Motif.Sweep,0,.75f,10,.3f,.4f,.2f,.65f),
            new(18.7f,Motif.Point,1,.65f,18,.3f,.4f,.25f,.65f),
            new(53.8f,Motif.Present,0,.72f,-8,.3f,.4f,.2f,.65f),
            new(57.6f,Motif.Accent,1,.8f,12,.3f,.4f,.15f,.55f),
            new(61.7f,Motif.Open,0,.85f,0,.3f,.45f,.3f,.65f),
            new(109.4f,Motif.Gather,0,.55f,0,.3f,.45f,.25f,.7f),
            new(112.2f,Motif.Present,1,.65f,12,.3f,.45f,.2f,.65f),
            new(4.5f,Motif.Present,0,.55f,-10,.40f,.65f,.50f,.95f),
            new(9.8f,Motif.Open,1,.55f,12,.45f,.75f,.35f,1.0f),
            new(16.5f,Motif.Present,1,.65f,8,.40f,.60f,.40f,.90f),
            new(21.615f,Motif.Sweep,0,.90f,-8,.25f,.34f,.18f,.60f),
            new(24.264f,Motif.Accent,0,1,-4,.30f,.26f,.10f,.52f),
            new(26.588f,Motif.Point,0,.93f,-15,.22f,.32f,.55f,.72f),
            new(28.937f,Motif.Sweep,0,.78f,4,.24f,.36f,.12f,.64f),
            new(30.936f,Motif.Accent,0,.92f,8,.30f,.25f,.13f,.50f),
            new(32.960f,Motif.Present,1,.82f,16,.30f,.38f,.22f,.60f),
            new(34.934f,Motif.Listen,0,.92f,-10,.24f,.38f,.26f,.58f),
            new(37.608f,Motif.Point,0,.92f,-18,.25f,.32f,.35f,.64f),
            new(39.607f,Motif.Accent,0,.88f,-5,.26f,.28f,.12f,.54f),
            new(42.281f,Motif.Point,1,.85f,18,.32f,.36f,.45f,.74f),
            new(44.280f,Motif.Sweep,0,.95f,2,.26f,.32f,.15f,.63f),
            new(46.604f,Motif.Accent,0,1,-7,.28f,.27f,.10f,.54f),
            new(49.602f,Motif.Present,0,.85f,-15,.32f,.44f,.38f,.78f),
            new(51.627f,Motif.Open,1,.88f,8,.30f,.48f,.40f,.90f),
            new(55.600f,Motif.Present,1,.70f,13,.38f,.58f,.38f,.88f),
            new(59.623f,Motif.Gather,0,1,0,.48f,.60f,.40f,1.00f),
            new(64.100f,Motif.Point,0,.65f,-10,.40f,.50f,.25f,.90f),
            // The duel and acknowledgement have dedicated aimed poses in Battle().
            new(114.50f,Motif.Salute,0,.92f,0,.45f,.70f,.65f,1.10f),
            new(118.00f,Motif.Open,1,.42f,8,.40f,.65f,.35f,.85f)
        };
        public readonly struct HandPose
        {
            public readonly Vector3 position,fingers,palm;
            public readonly float curl,index,thumb,spread;
            public HandPose(Vector3 p,Vector3 f,Vector3 n,float curl=.24f,float index=.24f,float thumb=.18f,float spread=0)
            {position=p;fingers=f;palm=n;this.curl=curl;this.index=index;this.thumb=thumb;this.spread=spread;}
            public static HandPose Lerp(HandPose a,HandPose b,float u)
            {
                u=Mathf.Clamp01(u);
                return new HandPose(Vector3.Lerp(a.position,b.position,u),
                    Vector3.Slerp(a.fingers.normalized,b.fingers.normalized,u),
                    Vector3.Slerp(a.palm.normalized,b.palm.normalized,u),
                    Mathf.Lerp(a.curl,b.curl,u),Mathf.Lerp(a.index,b.index,u),
                    Mathf.Lerp(a.thumb,b.thumb,u),Mathf.Lerp(a.spread,b.spread,u));
            }
        }
        public static float Ease(float u){u=Mathf.Clamp01(u);return u*u*u*(u*(u*6-15)+10);}
        public static HandPose Rest(int side)
        {
            float s=side==0?-1:1;
            return new HandPose(new Vector3(s*.34f,.94f,.22f),new Vector3(s*.09f,-1,.13f),
                new Vector3(-s,.03f,.12f),.23f,.17f,.22f,.05f);
        }
        public static HandPose LivingRest(int side,float t)
        {
            var rest=Rest(side);float breath=Mathf.Sin(t*.63f+side*.7f);
            return new HandPose(rest.position+new Vector3(Mathf.Sin(t*.37f+side)*.004f,breath*.008f,0),rest.fingers,rest.palm,
                rest.curl+.025f*Mathf.Sin(t*.61f+side),rest.index,rest.thumb,rest.spread);
        }
        public static HandPose Target(Cue cue,int side)
        {
            float s=side==0?-1:1;float gaze=Mathf.Sin(cue.gaze*Mathf.Deg2Rad);
            switch(cue.motif){
                case Motif.Salute:return new HandPose(new Vector3(-.13f,1.35f,.23f),new Vector3(.7f,.4f,0),Vector3.back,.09f,.08f,.13f,.12f);
                case Motif.Sweep:return new HandPose(new Vector3(s*.10f,1.25f,.35f),new Vector3(-s*.8f,-.15f,.5f),Vector3.forward,.18f,.13f,.18f,.28f);
                case Motif.Accent:return new HandPose(new Vector3(s*.38f,1.48f,.38f),new Vector3(s*.28f,.70f,.65f),Vector3.forward,.34f,.11f,.10f,.80f);
                case Motif.Point:return new HandPose(new Vector3(s*.26f+gaze*.25f,1.42f,.54f),new Vector3(gaze,.03f,1),Vector3.down,.70f,.03f,.27f,.04f);
                case Motif.Present:return new HandPose(new Vector3(s*.45f,1.17f,.42f),new Vector3(s*.30f,.10f,1),Vector3.up,.17f,.14f,.12f,.40f);
                case Motif.Listen:return new HandPose(new Vector3(s*.17f,1.46f,.12f),new Vector3(s*.08f,1,.08f),new Vector3(-s,0,.1f),.09f,.06f,.18f,.10f);
                case Motif.Gather:return new HandPose(new Vector3(s*(side==0?.43f:.48f),side==0?1.77f:1.66f,.33f),new Vector3(s*.20f,.80f,.4f),new Vector3(-s*.2f,.15f,1),.13f,.10f,.13f,.42f);
                default:return new HandPose(new Vector3(s*.58f,side==0?1.28f:1.19f,.28f),new Vector3(s*.50f,.12f,.85f),Vector3.up,.15f,.12f,.12f,.35f);
            }
        }
        public static float Envelope(Cue cue,float t)
        {
            if(t<cue.Start||t>=cue.End)return 0;
            if(t<cue.peak)return Ease((t-(cue.peak-cue.attack))/cue.attack);
            if(t<=cue.peak+cue.hold)return 1;
            return 1-Ease((t-cue.peak-cue.hold)/cue.recover);
        }
        public static bool Active(float t,out Cue cue)
        {
            cue=default;float best=-1;
            foreach(var c in Cues)if(t>=c.Start&&t<c.End+(c.motif==Motif.Gather||c.motif==Motif.Open?.12f:0)){
                float score=Envelope(c,t);if(score>best){best=score;cue=c;}
            }return best>=0;
        }
        public static HandPose Hand(Cue cue,int side,float t)
        {
            var arm=HandCore(cue,side,t);
            var wrist=HandCore(cue,side,t-.065f);
            var digits=HandCore(cue,side,t-.10f);
            return new HandPose(arm.position,wrist.fingers,wrist.palm,digits.curl,digits.index,digits.thumb,digits.spread);
        }
        static HandPose HandCore(Cue cue,int side,float t)
        {
            var rest=Rest(side);bool both=cue.motif==Motif.Gather||cue.motif==Motif.Open;
            if(side!=cue.side&&!both){
                // A modest support response instead of a mirrored second lead gesture.
                var support=new HandPose(new Vector3((side==0?-1:1)*.34f,1.00f,.23f),
                    new Vector3(0,-.8f,.35f),new Vector3(side==0?1:-1,0,.2f),.26f,.19f,.20f,.08f);
                return HandPose.Lerp(rest,support,Envelope(cue,t-.08f)*.42f);
            }
            float time=t-(both&&side!=cue.side?.12f:0);
            var prepare=new HandPose(rest.position+new Vector3(0,-.025f,-.025f),
                rest.fingers,rest.palm,rest.curl+.06f,rest.index+.03f,rest.thumb,rest.spread);
            if(time<cue.peak-cue.attack)
                return HandPose.Lerp(rest,prepare,Ease((time-cue.Start)/cue.prepare)*cue.power);
            var target=Target(cue,side);
            // The sweep travels through the accent instead of stopping at every sample.
            if(cue.motif==Motif.Sweep){
                float across=Mathf.Clamp01((time-(cue.peak-cue.attack))/(cue.attack+cue.hold+.22f));
                target=new HandPose(target.position+Vector3.right*(side==0?1:-1)*Mathf.Lerp(-.09f,.07f,across),
                    target.fingers,target.palm,target.curl,target.index,target.thumb,target.spread);
            }
            HandPose phase;
            if(time<cue.peak)phase=HandPose.Lerp(prepare,target,Ease((time-(cue.peak-cue.attack))/cue.attack));
            else if(time<=cue.peak+cue.hold)phase=target;
            else phase=HandPose.Lerp(target,rest,Ease((time-cue.peak-cue.hold)/cue.recover));
            return HandPose.Lerp(rest,phase,cue.power);
        }
    }
}
