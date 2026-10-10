using UnityEngine;
namespace LordLyrical
{
    [CreateAssetMenu(menuName="Lord Lyrical/Experience Configuration")]
    public class ExperienceConfiguration:ScriptableObject
    {
        public string roomId="lord-lyrical",serverOverride="";
        public bool autoDiscovery=true,operatorMode=false,debugLogging=true,spatialAccents=true,useSourceChamber=false;
        public string showMode="Adaptive",targetHeadsetProfile="PICO OpenXR / 72 Hz";
        public int requiredParticipants=10,targetRefreshRate=72,vfxQuality=1;
        public double countdownDuration=17,sequenceDuration=121;
        public AudioClip track;public TextAsset cueJson;
        public GameObject orcModel,dragonModel,lordModel;
        public GameObject chamberModel;
        public GameObject finishingModel,orcWeapon;
        public VoiceCueLibrary voiceCues;
        public GameObject[] participantModels=new GameObject[10];
        public Texture2D lordPortrait;
        public bool Validate(out string error)
        {error=track==null?"Master audio missing":cueJson==null?"Cue table missing":sequenceDuration!=121?"Sequence must be 121 seconds":string.IsNullOrWhiteSpace(roomId)?"Room missing":!autoDiscovery&&string.IsNullOrWhiteSpace(serverOverride)?"Fixed server address required when discovery is disabled":"";return error.Length==0;}
    }
}
