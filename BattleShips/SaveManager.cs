using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleShips
{
    internal class SaveManager
    {
        public static SaveManager Instance
        {
            get
            {
                if (Instance == null)
                    Instance = new SaveManager();
                return Instance;
            }
            private set
            {
                Instance = value;
            }
        }
        public string SavePath { get; set; }
        public string SaveName { get; set; }
        public int MaxSaves { get; set; }

        private SaveManager() 
        { 
            //set default values
            MaxSaves = 0;
            SavePath = string.Empty;
            SaveName = string.Empty;
        }

        #region Methods
        public void ClearOldSaves()
        {
            //if max saves hasn't been set, return
            if (MaxSaves <= 0)
                return;

            //if file doesn't exist return
            if (!Directory.Exists(SavePath))
                return;

            var files = FilesByDate;

            //if there is 1 or less files, return
            if (files.Length < 2)
                return;

            //delete oldest files until max saves is reached
            if (files.Length >= MaxSaves)
            {
                for (int i = files.Length -1; i > MaxSaves - 1; i--)
                {
                    File.Delete(files[i].FullName);
                }
            }
        }

        public GameSave? GetGameSave(string name)
        {
            var save = JsonSerializer.Deserialize<GameSave>(File.ReadAllText(SavePath + name));
            if (save == null)
                return null;

            //check if valid
            if (!IsValid(save))
                return null;

            return save;
        }

        private bool IsValid(GameSave save)
        {
            //clamp difficulty
            save.Difficulty = Math.Clamp(save.Difficulty, 0, 2);

            //clamp timer
            save.Timer = Math.Max(save.Timer, 0);

            //check height
            if (save.PlayerSpaces.Length != save.EnemySpaces.Length)
                return false;

            for (int board = 0; board < 2; board++)
            {
                //switch board on loop
                var curB = save.PlayerSpaces;
                var curShots = save.PShots;
                var curShipS = save.PShipSpaces;
                var curShipH = save.PShipsHit;
                if (board == 1)
                {
                    curB = save.EnemySpaces;
                    curShots = save.EShots;
                    curShipS = save.EShipSpaces;
                    curShipH = save.EShipsHit;
                }
                
                //check width
                for (int row = 0; row < curB.Length; row++)
                {
                    if (curB[row].Length != save.PlayerSpaces[0].Length)
                        return false;
                }

                //check spaces
                for (int y = 0; y < curB.Length; y++)
                {
                    for (int x = 0; x < curB[y].Length; x++)
                    {
                        ref var space = ref curB[y][x];
                        if (space < 0 || space > 3)
                            return false;

                        if (space == 1 || space == 3)
                            curShipS--;
                        if (space == 3)
                            curShipH--;
                        if (space > 1)
                        {
                            if (!curShots.Any(v => v.Equals(new Vector2(x,y))))
                            {
                                return false;
                            }
                        }    
                    }
                }

                if (curShipH != 0)
                    return false;
                if (curShipS != 0)
                    return false;
            }
            return true;
        }
        #endregion

        #region Getters and Setters
        public FileInfo[] FilesByDate
        {
            get
            {
                //check whether the directory exists
                if (Empty)
                    return Array.Empty<FileInfo>();

                //return files sorted by creation time (latest -> oldest)
                return new DirectoryInfo(SavePath).GetFiles().OrderBy(f => f.CreationTime).ToArray();
            }
        }
    
        public int LatestSaveNum
        {
            get
            {
                //if found no files, return 0
                var files = FilesByDate;
                if (files.Length == 0)
                    return 0;

                for (int i = 0; i < files.Length; i++)
                {
                    //prepare string for parse
                    string prep = files[i].Name;
                    prep = prep.Split('.')[0]; //get rid of file extension
                    if (prep.Length <= SaveName.Length) //get rid of savename, and continue if not in correct naming scheme
                        continue;
                    prep = prep.Substring(SaveName.Length);

                    //attempt to parse the prepped string to an integer
                    int num;
                    if (Int32.TryParse(prep, out num))
                        return num; //if passed return parsed number
                }
                //if found no valid files, return 0
                return 0;
            }
        }

        public bool Empty
        {
            get
            {
                if (!Directory.Exists(SavePath))
                    return true;
                if (Directory.GetFiles(SavePath).Length == 0)
                    return true;
                return false;
            }
        }

        public GameSave? LatestSave
        {
            get
            {
                return GetGameSave(FilesByDate[0].Name);
            }
        }
        #endregion
    }
}
