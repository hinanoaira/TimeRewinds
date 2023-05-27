using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using BaseX;
using System;
using System.Threading.Tasks;
using System.Reflection;

namespace TimeRewinds
{
    public class TimeRewinds : NeosMod
    {
        public override string Name => "TimeRewinds";
        public override string Author => "HinanoAira";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/HinanoAira/TimeRewinds";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("jp.hinasense.TimeRewinds");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(World), "VerifyJoinRequest")]
        private class RunUserJoined_Patch
        {
            static void Postfix(World __instance)
            {
                var traverse = Traverse.Create(__instance);
                var users = traverse.Field<UserBag>("_users").Value;
                if(users.Count == 1)
                {
                    var timeTraverse = traverse.Property("Time");
                    timeTraverse.SetValue(new TimeController(__instance));
                }
            }
        }
    }
}