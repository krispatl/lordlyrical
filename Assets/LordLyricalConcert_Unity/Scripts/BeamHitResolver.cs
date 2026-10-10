using UnityEngine;
namespace LordLyrical
{
    public struct BeamContact
    {
        public bool hit; public int orc; public float distance; public Vector3 point,normal;
    }
    public static class BeamHitResolver
    {
        public const float Range=22;
        public static bool Capsule(Vector3 origin,Vector3 direction,Vector3 feet,out float distance,out Vector3 normal)
        {
            const float radius=.55f;float best=float.PositiveInfinity;
            Vector3 bottom=feet+Vector3.up*.55f,top=feet+Vector3.up*2.2f,offset=origin-bottom;
            float a=direction.x*direction.x+direction.z*direction.z,b=offset.x*direction.x+offset.z*direction.z,c=offset.x*offset.x+offset.z*offset.z-radius*radius;
            if(a>.000001f){
                float d=b*b-a*c;
                if(d>=0){float root=Mathf.Sqrt(d);float n=(-b-root)/a,f=(-b+root)/a;
                    float yn=origin.y+n*direction.y,yf=origin.y+f*direction.y;
                    if(n>=0&&yn>=bottom.y&&yn<=top.y)best=n;
                    if(f>=0&&yf>=bottom.y&&yf<=top.y)best=Mathf.Min(best,f);
                }
            }
            if(ManualResonanceBeams.RaySphere(origin,direction,bottom,radius,out float low))best=Mathf.Min(best,low);
            if(ManualResonanceBeams.RaySphere(origin,direction,top,radius,out float high))best=Mathf.Min(best,high);
            distance=best;var point=origin+direction*(float.IsFinite(best)?best:0);
            normal=(point-new Vector3(feet.x,Mathf.Clamp(point.y,bottom.y,top.y),feet.z)).normalized;
            return float.IsFinite(best);
        }
        public static int Target(ConcertState state,Vector3 origin,Vector3 direction,float range,out float distance,out Vector3 normal)
        {
            int result=-1;distance=range;normal=-direction;
            if(state?.orcs==null||!ManualResonanceBeams.Finite(origin)||!ManualResonanceBeams.Finite(direction)||direction.sqrMagnitude<.9f)return -1;
            for(int i=0;i<state.orcs.Length;i++){
                var o=state.orcs[i];if(o.health<=0)continue;
                if(Capsule(origin,direction,o.position,out float d,out var n)&&d<distance){distance=d;normal=n;result=i;}
            }return result;
        }
        public static BeamContact Trace(ConcertState state,float t,Vector3 origin,Vector3 direction)
        {
            var result=new BeamContact{distance=Range,orc=-1,normal=-direction,point=origin+direction*Range};
            if(Physics.Raycast(origin,direction,out var wall,Range,~0,QueryTriggerInteraction.Ignore)){
                result.hit=true;result.distance=wall.distance;result.point=wall.point;result.normal=wall.normal;
            }
            if(t>=26&&t<54){
                int i=Target(state,origin,direction,result.distance,out float d,out var n);
                if(i>=0){result.hit=true;result.orc=i;result.distance=d;result.point=origin+direction*d;result.normal=n;}
            }
            return result;
        }
    }
}
