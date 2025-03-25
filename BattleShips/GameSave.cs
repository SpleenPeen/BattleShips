namespace BattleShips
{
    //A class outlining the savefile structure
    public class GameSave
    {
        public short[][] PlayerSpaces { get; set; }
        public int PShipSpaces { get; set; }
        public int PShipsHit { get; set; }
        public Vector2[] PShots { get; set; }
        public short[][] EnemySpaces { get; set; }
        public int EShipSpaces { get; set; }
        public int EShipsHit { get; set; }
        public Vector2[] EShots { get; set; }
        public long Timer { get; set; }
        public int Difficulty { get; set; }
        public List<Vector2> CheckAround { get; set; }
        public List<Vector2> ShotTargets { get; set; }
    }
}
