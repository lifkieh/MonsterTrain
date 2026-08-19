namespace MTA.Meta
{
    // First-playable screen states. Pure C#: no UnityEngine, edit-mode testable.
    public enum GamePhase { MainMenu, TeamSelect, Battle, Result, Progress, Collection, Detail, Career, Daily, Settings, About, Onboarding, Quests, Achievements, Dex }

    // Minimal state machine driving the playable loop:
    // MainMenu -> TeamSelect -> Battle -> Result -> (TeamSelect | MainMenu).
    public class GameFlow
    {
        public GamePhase Phase { get; private set; } = GamePhase.MainMenu;
        public event System.Action<GamePhase> OnPhaseChanged;

        public void GoMainMenu()   => Set(GamePhase.MainMenu);
        public void GoTeamSelect() => Set(GamePhase.TeamSelect);
        public void GoBattle()     => Set(GamePhase.Battle);
        public void GoResult()     => Set(GamePhase.Result);
        public void GoProgress()   => Set(GamePhase.Progress);
        public void GoCollection() => Set(GamePhase.Collection);
        public void GoDetail()     => Set(GamePhase.Detail);
        public void GoCareer()     => Set(GamePhase.Career);
        public void GoDaily()      => Set(GamePhase.Daily);
        public void GoSettings()   => Set(GamePhase.Settings);
        public void GoAbout()      => Set(GamePhase.About);
        public void GoOnboarding()   => Set(GamePhase.Onboarding);
        public void GoQuests()       => Set(GamePhase.Quests);
        public void GoAchievements() => Set(GamePhase.Achievements);
        public void GoDex()          => Set(GamePhase.Dex);

        void Set(GamePhase p)
        {
            Phase = p;
            OnPhaseChanged?.Invoke(p);
        }
    }
}
