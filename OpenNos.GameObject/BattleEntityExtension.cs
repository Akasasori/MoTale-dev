namespace OpenNos.GameObject.Battle
{
    public static class BattleEntityExtension
    {
        public static void ApplyScoreArena(this BattleEntity attacker, BattleEntity defender)
        {
            defender.Character.ArenaDie++;
            defender.Character.CurrentDie++;
            defender.Character.ArenaTc = 0;
            defender.Character.CurrentTc = 0;
            defender.Character.GenerateAscrPacket();

            attacker.Character.ArenaKill++;
            attacker.Character.ArenaTc++;
            attacker.Character.CurrentKill++;
            attacker.Character.CurrentTc++;
            attacker.Character.GenerateAscrPacket();
        }
    }
}
