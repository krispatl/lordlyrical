using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.OpenXR.Features.PICOSupport;
using LordLyrical;
public static class HandsOnlyReview
{
    static void Need(bool condition,string reason){if(!condition)throw new Exception("HANDS ONLY: "+reason);}
    static void Straight(ref HandPoseInput.GestureEvidence evidence){evidence.AddFinger(Vector3.zero,Vector3.forward*.03f,Vector3.forward*.05f,Vector3.forward*.07f);}
    static void Folded(ref HandPoseInput.GestureEvidence evidence){evidence.AddFinger(Vector3.zero,Vector3.forward*.03f,new Vector3(0,-.02f,.03f),new Vector3(0,-.02f,.01f));}
    [MenuItem("Lord Lyrical/Production/Validate Hands Only")]
    public static void Run()
    {
        var settings=PICOProjectSetting.GetProjectConfig();
        Need(settings.isHandTracking&&settings.highFrequencyHand&&settings.handTrackingSupportType==Unity.XR.PXR.HandTrackingSupport.HandsOnly,"PICO hand-only configuration missing");
        var evidence=new HandPoseInput.GestureEvidence();
        Straight(ref evidence);Straight(ref evidence);
        Need(!evidence.Open&&!evidence.Closed&&evidence.ValidFingers==2,"Two fingers must not activate a gesture");
        Straight(ref evidence);
        Need(evidence.Open&&!evidence.Closed&&evidence.ValidFingers==3,"One missing finger must not discard three known open fingers");
        evidence.AddFinger(Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero);
        evidence.AddFinger(new Vector3(float.NaN,0,0),Vector3.zero,Vector3.zero,Vector3.zero);
        Need(evidence.Open&&evidence.ValidFingers==3,"Invalid samples changed the evidence");
        evidence=new HandPoseInput.GestureEvidence();Folded(ref evidence);Folded(ref evidence);Folded(ref evidence);
        Need(evidence.Closed&&!evidence.Open&&evidence.ValidFingers==3,"Three folded fingers must guard");
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var go=new GameObject("Hand-only input fixture");var rig=go.AddComponent<TrackedRig>();
            var hand=go.GetComponent<HandPoseInput>();if(!hand)hand=go.AddComponent<HandPoseInput>();typeof(TrackedRig).GetProperty("Hands").SetValue(rig,hand);var beams=go.AddComponent<ManualResonanceBeams>();
            typeof(ManualResonanceBeams).GetField("rig",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(beams,rig);
            var input=typeof(ManualResonanceBeams).GetMethod("Input",BindingFlags.Instance|BindingFlags.NonPublic);
            object[] args={0,Vector3.one,Vector3.one,true};
            Need(!(bool)input.Invoke(beams,args),"Untracked hands fired a beam");Need(!(bool)args[3],"Untracked input was labelled as hand data");
            var state=new HandPoseInput.HandState{tracked=true,open=true,validFingers=3,position=new Vector3(.2f,1.4f,.4f),direction=Vector3.forward};
            typeof(HandPoseInput).GetProperty("Left").SetValue(hand,state);
            Need((bool)input.Invoke(beams,args)&&(bool)args[3]&&(Vector3)args[1]==state.position,"Open palm did not supply its actual ray");
            state.open=false;state.closed=true;typeof(HandPoseInput).GetProperty("Left").SetValue(hand,state);
            Need(!(bool)input.Invoke(beams,args),"Closed palm continued firing");
            typeof(HandPoseInput).GetProperty("Left").SetValue(hand,default(HandPoseInput.HandState));
            Need(!rig.LeftTracked&&!rig.RightTracked&&!(bool)input.Invoke(beams,args),"Lost hands retained controller tracking or firing");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        var report=new StringBuilder("CANDIDATE20 HAND-ONLY INPUT VALIDATION\n");
        report.AppendLine("PASS PICO configuration: hand tracking required, hands-only mode, high frequency enabled.");
        report.AppendLine("PASS two known fingers cannot fire/guard; three known fingers can classify despite a missing fourth.");
        report.AppendLine("PASS zero-length and nonfinite finger samples do not vote.");
        report.AppendLine("PASS open tracked palm supplies its own ray; closed or untracked hand cannot fire.");
        report.AppendLine("PASS untracked hands are not labelled as tracked controller hands.");
        report.AppendLine("SCOPE: editor fixtures and serialized settings. Actual sensor tracking and OS hand-mode activation remain unverified.");
        string directory=MotionPolishValidation.Out+"/Production/HandsOnly20";Directory.CreateDirectory(directory);File.WriteAllText(directory+"/Validation.txt",report.ToString());Debug.Log(report);
    }
}
