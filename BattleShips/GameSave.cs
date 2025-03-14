namespace BattleShips
{
    public class GameSave
    {
        public short[][] PlayerSpaces { get; set; }
        public int PShipSpaces { get; set; }
        public int PShipsHit { get; set; }
        public int PShotsFired { get; set; }
        public short[][] EnemySpaces { get; set; }
        public int EShipSpaces { get; set; }
        public int EShipsHit { get; set; }
        public int EShotsFired { get; set; }
        public long Timer { get; set; }
        public int Difficulty { get; set; }
        public List<Vector2> CheckAround { get; set; }
        public List<Vector2> ShotTargets { get; set; }
    
        public bool Ongoing
        {
            get
            {
                if (PShipsHit == PShipSpaces || EShipsHit == EShipSpaces)
                    return false;
                return true;
            }
        }
    }
}
