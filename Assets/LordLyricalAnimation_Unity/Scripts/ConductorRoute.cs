using UnityEngine;
namespace LordLyrical
{
    public static class ConductorRoute
    {
        static readonly float[] times={0,67,68.8f,71.2f,72.2f,73.2f,74,76,77.4f,80.6f,82,84.5f,86,89,90.2f,94,99,103,107,121};
        static readonly Vector3[] positions={
            new(0,0,5),new(0,0,5),new(-.35f,0,4.9f),new(-.35f,0,4.9f),
            new(-1.25f,0,4.7f),new(-1.25f,0,4.7f),new(-1.65f,0,4.35f),new(-1.65f,0,4.35f),
            new(-.8f,0,4.6f),new(-.8f,0,4.6f),new(.55f,0,4.8f),new(.55f,0,4.8f),
            new(.2f,0,5.1f),new(.2f,0,5.1f),new(-.25f,0,4.8f),new(-.25f,0,4.8f),
            new(-.25f,0,4.8f),new(0,0,5),new(0,0,5),new(0,0,5)};
        static readonly float[] distances=BuildDistances();
        public static Vector3 Position(float t)
        {
            int i=0;while(i<times.Length-2&&t>=times[i+1])i++;
            return Vector3.Lerp(positions[i],positions[i+1],BattleScore.S(times[i],times[i+1],t));
        }
        public static Vector3 Velocity(float t)=>(Position(t+.01f)-Position(t-.01f))/.02f;
        public static Quaternion Rotation(float t)
        {
            float face=BattleScore.W(66.5f,68.8f,100,104,t);
            float yaw=174+BattleScore.W(72.8f,73.3f,74.2f,75,t)*14-BattleScore.W(80.6f,82,84,85,t)*14;
            return Quaternion.Euler(0,face*yaw,0);
        }
        public static float TurnRate(float t)=>Mathf.DeltaAngle(Rotation(t-.01f).eulerAngles.y,Rotation(t+.01f).eulerAngles.y)/.02f;
        static float[] BuildDistances(){var a=new float[7261];var previous=Position(0);for(int i=1;i<a.Length;i++){var p=Position(i/60f);a[i]=a[i-1]+Vector3.Distance(p,previous);previous=p;}return a;}
        public static float Distance(float t){float f=Mathf.Clamp(t*60,0,7260);int i=Mathf.Min(7259,Mathf.FloorToInt(f));return Mathf.Lerp(distances[i],distances[i+1],f-i);}
    }
}
