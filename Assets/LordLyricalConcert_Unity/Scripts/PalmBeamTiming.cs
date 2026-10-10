namespace LordLyrical
{
    public static class PalmBeamTiming
    {
        public const float InstructionStart=21,Unlock=21.35f,InstructionEnd=27,End=111;
        public static bool Allowed(float songTime)=>songTime>=Unlock&&songTime<End;
        public static bool Teaching(float songTime)=>songTime>=InstructionStart&&songTime<InstructionEnd;
    }
}
