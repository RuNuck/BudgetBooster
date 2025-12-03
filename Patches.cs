using Game;
using HarmonyLib;
using Unity.Entities;

namespace BudgetBooster
{
    /// <summary>
    /// Harmony patches to integrate the Budget Booster system into the game.
    /// </summary>
    
    /// <summary>
    /// Patch to register our system when the game initializes its systems.
    /// </summary>
    [HarmonyPatch(typeof(SystemOrder), "Initialize")]
    public class SystemOrderPatch
    {
        public static void Postfix(UpdateSystem updateSystem)
        {
            try
            {
                // Create and register our budget booster system
                updateSystem.World.GetOrCreateSystemManaged<BudgetBoosterSystem>();
                
                // Schedule it to run during the GameSimulation phase
                // This ensures city data is available when we run
                updateSystem.UpdateAt<BudgetBoosterSystem>(SystemUpdatePhase.GameSimulation);

                Mod.Log.Info("BudgetBoosterSystem registered with GameSimulation phase");
            }
            catch (System.Exception ex)
            {
                Mod.Log.Error($"Failed to register BudgetBoosterSystem: {ex.Message}");
            }
        }
    }
}
