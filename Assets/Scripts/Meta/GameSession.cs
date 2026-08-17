using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    // Holds the current match's teams + result. Pure C#, no UnityEngine.
    public class GameSession
    {
        public const int TeamSize = 3;
        public const int Level = 5;        // fixed level for first playable

        public readonly List<string> playerTeam = new List<string>();
        public List<string> enemyTeam = new List<string>();
        public int matchSeed;
        public BattleResult lastResult;

        public bool PlayerTeamReady => playerTeam.Count == TeamSize;

        // Team-select tap: add if room and not present, else remove.
        public void TogglePlayer(string speciesId)
        {
            if (playerTeam.Contains(speciesId)) playerTeam.Remove(speciesId);
            else if (playerTeam.Count < TeamSize) playerTeam.Add(speciesId);
        }

        public void ClearPlayer() => playerTeam.Clear();
    }
}
