using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AIs;
using Harmony;
using JetBrains.Annotations;
using UnityEngine;

public class PeacefulEnemies : Mod
{
    private HarmonyInstance m_harmony;

    private const string ModName = "PeacefulEnemies";
    private const string HarmonyId = "com.janniksam.greenhell.peacefulenemies";
    
    public void Start()
    {
        Debug.Log(string.Format("Mod {0} has been loaded!", ModName));

        m_harmony = HarmonyInstance.Create(HarmonyId);

        PatchAttackGoals();
    }

    private void PatchAttackGoals()
    {
        var aiGoals = ReflectiveEnumerator.GetDerivedTypes<AIGoal>();
        foreach (var aiGoalType in aiGoals)
        {
            var goal = InstantiateGoal(aiGoalType);
            if (goal == null)
            {
                continue;
            }
            if (!goal.IsAttackGoal())
            {
                continue;
            }

            Debug.Log(string.Format("{0}: Patching {1}... ", ModName, goal.GetType().Name));
            var original = aiGoalType.GetMethod("ShouldPerform");
            var prefix = typeof(GoalAttackPatch).GetMethod("Prefix");
            m_harmony.Patch(original, new HarmonyMethod(prefix));
        }

        Debug.Log(string.Format("{0}: Patching done... ", ModName));
    }

    private static AIGoal InstantiateGoal(Type aiGoal)
    {
        var goal = (AIGoal) Activator.CreateInstance(aiGoal);
        var enumVal = aiGoal.ToString().Remove(0, 8);
        AIGoalType type;
        if (!Enum.TryParse(enumVal, out type))
        {
            return null;
        }

        goal.m_Type = type;
        return goal;
    }

    [UsedImplicitly]
    public class GoalAttackPatch
    {
        [UsedImplicitly]
        public static bool Prefix(
                // ReSharper disable once InconsistentNaming
                // ReSharper disable once RedundantAssignment
                ref bool __result)
        {
            // never attack
            __result = false;

            // skip the old logic
            return false;
        }
    }


    public void OnModUnload()
    {
        Debug.Log(string.Format("Mod {0} has been unloaded!", ModName));
        m_harmony.UnpatchAll(HarmonyId);
    }
}

public static class ReflectiveEnumerator
{
    public static IEnumerable<Type> GetDerivedTypes<T>() where T : class
    {
        var assembly = Assembly.GetAssembly(typeof(T));
        foreach (var type in assembly.GetTypes().Where(myType => myType.IsClass &&
                                                                 myType.IsSubclassOf(typeof(T))))
        {
            yield return type;
        }
    }
}