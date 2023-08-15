using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.Threading.Tasks;
using FrooxEngine.LogiX.ProgramFlow;
using System.Collections.Generic;
using System.Linq;

namespace TimeRewinds
{
    public class TimeRewinds : NeosMod
    {
        private static Dictionary<SyncPlayback, IUpdatable> syncPlaybacks = new Dictionary<SyncPlayback, IUpdatable>();
        private static Dictionary<SyncTime, IUpdatable> syncTimes = new Dictionary<SyncTime, IUpdatable>();

        public override string Name => "TimeRewinds";
        public override string Author => "HinanoAira";
        public override string Version => "0.1.0";
        public override string Link => "https://github.com/HinanoAira/TimeRewinds";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("jp.hinasense.TimeRewinds");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(World), "VerifyJoinRequest")]
        private class RunUserJoined_Patch
        {
            private static void Postfix(World __instance, ref Task<JoinGrant> __result)
            {
                syncPlaybacks = syncPlaybacks.Where(e => !e.Value.IsDestroyed).ToDictionary(e => e.Key, e => e.Value);
                syncTimes = syncTimes.Where(e => !e.Value.IsDestroyed).ToDictionary(e => e.Key, e => e.Value);
                if (__result.Result.granted)
                {
                    var traverse = Traverse.Create(__instance);
                    var users = traverse.Field<UserBag>("_users").Value;
                    Msg($"[TimeRewind]UsersCount: {users.Count}");
                    if (users.Count == 1)
                    {
                        Msg($"[TimeRewind]TimeRewinding...");
                        var timeTraverse = traverse.Property("Time");
                        timeTraverse.SetValue(new TimeController(__instance));
                        foreach (var item in syncPlaybacks)
                        {
                            __instance.RunSynchronously(() =>
                            {
                                if (item.Key.Loop)
                                {
                                    item.Key.Position = 0f;
                                }
                                else if(!item.Key.IsStreaming)
                                {
                                    item.Key.Stop();
                                }
                            });
                        }
                        Msg($"[TimeRewind]SyncPlaybacks Reset Count:{syncPlaybacks.Count}");
                        foreach (var item in syncTimes)
                        {
                            __instance.RunSynchronously(() => item.Key.SetNow());
                        }
                        Msg($"[TimeRewind]SyncTimes Reset Count:{syncTimes.Count}");
                        Msg($"[TimeRewind]TimeRewind Completed.");
                    }
                }
            }
        }
        [HarmonyPatch(typeof(SyncPlayback), "PlaybackChanged")]
        private class SyncPlayback_PlaybackChanged_Patch
        {
            private static void Postfix(SyncPlayback __instance)
            {
                if (!syncPlaybacks.Keys.Contains(__instance))
                {
                    IWorldElement element = __instance;
                    while (true)
                    {
                        if (element.Parent == null)
                        {
                            break;
                        }
                        element = element.Parent;
                        if (element.Parent is IUpdatable c)
                        {
                            syncPlaybacks.Add(__instance, c);
                            syncPlaybacks = syncPlaybacks.Where(e => !e.Value.IsDestroyed).ToDictionary(e => e.Key, e => e.Value);
                            Debug($"[TimeRewind]SyncPlaybacks Count:{syncTimes.Count}");
                            break;
                        }
                        if (element.Parent == __instance.World.RootSlot)
                        {
                            break;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(SyncTime), "TimeChanged")]
        private class SyncTime_TimeChanged_Patch
        {
            private static void Postfix(SyncTime __instance)
            {
                if (!syncTimes.Keys.Contains(__instance))
                {
                    IWorldElement element = __instance;
                    while (true)
                    {
                        if(element.Parent == null)
                        {
                            break;
                        }
                        element = element.Parent;
                        if (element.Parent is IUpdatable c)
                        {
                            syncTimes.Add(__instance, c);
                            syncTimes = syncTimes.Where(e => !e.Value.IsDestroyed).ToDictionary(e => e.Key, e => e.Value);
                            Debug($"[TimeRewind]SyncTimes Count:{syncTimes.Count}");
                            break;
                        }
                        if (element.Parent == __instance.World.RootSlot)
                        {
                            break;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(LocalLeakyImpulseBucket), "Trigger")]
        private class LocalLeakyImpulseBucket_Trigger_Patch
        {
            private static void Prefix(LocalLeakyImpulseBucket __instance)
            {
                var _lastPulse = (double)Traverse.Create(__instance).Field("_lastPulse").GetValue();
                if (__instance.Time.WorldTime < _lastPulse)
                {
                    __instance.Reset();
                }
            }
        }
        [HarmonyPatch(typeof(LocalImpulseTimeout), "Trigger")]
        private class LocalImpulseTimeout_Trigger_Patch
        {
            private static void Prefix(LocalImpulseTimeout __instance)
            {
                var _pulseBlockedUntil = (double)Traverse.Create(__instance).Field("_pulseBlockedUntil").GetValue();
                if (__instance.Time.WorldTime + (double)__instance.TimeoutSeconds.Evaluate(0f) <= _pulseBlockedUntil)
                {
                    __instance.Reset();
                }
            }
        }
        [HarmonyPatch(typeof(TimerNode), "OnCommonUpdate")]
        private class TimerNode_OnCommonUpdate_Patch
        {
            private static void Prefix(TimerNode __instance)
            {
                var traverse = Traverse.Create(__instance).Field("_lastPulse");
                var _lastPulse = (double)traverse.GetValue();
                if(__instance.Time.WorldTime < _lastPulse)
                {
                    traverse.SetValue(0.0);
                }
            }
        }
    }
}