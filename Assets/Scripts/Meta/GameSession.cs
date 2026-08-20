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
        public Dictionary<string, int> playerLevels;   // per-species level for the player team (null = fixed Level)
        public int enemyLevel = Level;                 // enemy team level (career scales this)
        public int careerStageIndex = -1;              // >=0 while fighting a career stage, else casual
        public int matchSeed;
        public BattleResult lastResult;

        // Ready with 1..TeamSize picks — the count chooses the mode (1v1 / 2v2 / 3v3).
        // The enemy team is generated to match this count (see GameController.StartBattle).
        public bool PlayerTeamReady => playerTeam.Count >= 1 && playerTeam.Count <= TeamSize;

        // Team-select tap: add if room and not present, else remove.
        public void TogglePlayer(string speciesId)
        {
            if (playerTeam.Contains(speciesId)) playerTeam.Remove(speciesId);
            else if (playerTeam.Count < TeamSize) playerTeam.Add(speciesId);
        }

        public void ClearPlayer() => playerTeam.Clear();
    }
}
