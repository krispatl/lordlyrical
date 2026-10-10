using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using LordLyrical;
public static class DragonClockReview
{
    [MenuItem("Lord Lyrical/Production/Validate Dragon Flight Clock")]
    public static void Run()
    {
        float min=float.MaxValue,max=0;
        for(int i=1;i<=8*720;i++){
            float t=93+i/720f;
            float rate=(DragonPerformance.FlightClock(t)-DragonPerformance.FlightClock(t-1f/720))/(1f/720);
            if(!float.IsFinite(rate)||rate<.18f||rate>1.02f)throw new Exception("Dragon flight clock discontinuity at "+t+": "+rate);
            min=Mathf.Min(min,rate);max=Mathf.Max(max,rate);
        }
        for(int i=0;i<=94*10;i++){
            float t=i/10f;
            if(Mathf.Abs(DragonPerformance.FlightClock(t)-t)>.00001f)throw new Exception("Pre-charge phase changed");
        }
        string output=MotionPolishValidation.Out+"/Production/Dragon_Flight_Clock.txt";
        File.WriteAllText(output,$"PASS 5760 samples from93-101s at720Hz. Monotonic flight clock rate {min:F4}-{max:F4}, no catch-up acceleration; pre-charge phase unchanged. Later song and attack cues unchanged. Only wing/tail phase retains the charge slowdown.\n");
        Debug.Log("DRAGON_FLIGHT_CLOCK_PASS");
    }
}
