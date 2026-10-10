using System;
using System.IO;
using UnityEngine;
using LordLyrical;
public static class Production32CombatValidation
{
    public static string OutputOverride;
    static void Need(bool ok,string why){if(!ok)throw new Exception("PRODUCTION32 COMBAT: "+why);}
    public static void Run()
    {
        var encounter=new ConcertEncounter();encounter.occupied[0]=true;
        // Clear enemy targets: exercise the finale target using production ray tests.
        foreach(var orc in encounter.State.orcs)orc.health=0;
        Vector3 heart=new Vector3(0,8,11),origin=new Vector3(0,1.5f,2),ray=(heart-origin).normalized;
        encounter.SetDuelTargets(new Vector3(0,2,5),heart);
        Need(encounter.Beam(0,92,.8f,origin,ray),"Finale contact rejected");
        Need(encounter.State.dragonHitSerial==1&&encounter.State.finaleCharge>0,"No visible response or contribution");
        Need(encounter.Beam(0,92.1f,.8f,origin,ray),"Sustained damage rejected");
        Need(encounter.State.dragonHitSerial==1,"Continuous contact restarts recoil");
        Need(encounter.Beam(0,92.7f,.8f,origin,ray)&&encounter.State.dragonHitSerial==2,"Reaction does not rearm");
        Need(HeroDuelScore.PlayerReaction(92.85f,encounter.State)>.9f,"Reaction missing");
        Need(HeroDuelScore.PlayerReaction(93.4f,encounter.State)==0,"Reaction persists after envelope");
        var copy=JsonUtility.FromJson<ConcertState>(JsonUtility.ToJson(encounter.State));
        Need(copy.dragonHitSerial==2&&copy.dragonHitPoint==encounter.State.dragonHitPoint,"Network roundtrip lost impact");
        Need(!encounter.Beam(0,93.5f,.8f,origin,Vector3.left)&&encounter.State.dragonHitSerial==2,"Miss creates reaction");
        Need(!encounter.Beam(0,100.1f,.8f,origin,ray),"Contribution after finale window");
        encounter.Reset(1,0);Need(encounter.State.dragonHitSerial==0&&HeroDuelScore.PlayerReaction(92,encounter.State)==0,"Replay retains impact");
        var go=new GameObject("Duel particle verification");var fx=go.AddComponent<DuelContactVFX>();fx.Initialize();
        try{
            int maximum=0;
            for(int frame=67*72;frame<101*72;frame++){
                fx.Render(frame/72f,origin,heart,copy,1);maximum=Math.Max(maximum,fx.ActiveParticles);
                Need(fx.ActiveParticles<=DuelContactVFX.Budget,"Pool capacity exceeded");
            }
            fx.Render(92.85f,origin,heart,copy,1);int expected=fx.ActiveParticles;
            fx.Render(98.2f,origin,heart,copy,1);fx.Render(92.85f,origin,heart,copy,1);
            Need(expected>0&&expected==fx.ActiveParticles,"Seek changes particle count");
            fx.Render(101,origin,heart,copy,1);Need(fx.ActiveParticles==0,"Particles remain after destruction");
            File.WriteAllText((OutputOverride??Production32Review.Out)+"/Combat32_Validation.txt","PASS finale ray hit/miss, sustained contribution with bounded recoil, network JSON, replay reset, no late contribution.\nPASS 2448 particle frames, deterministic seek and destruction cleanup. Maximum active particles: "+maximum+" / "+DuelContactVFX.Budget+".\n");
        }finally{UnityEngine.Object.DestroyImmediate(go);}
        Debug.Log("PRODUCTION32_COMBAT_PASS");
    }
}
