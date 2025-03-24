using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleShips
{
    // Save manager is a singleton that manages gamesave files in a specified folder
    internal class SaveManager
    {
        private static SaveManager _instance;
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
        public void SaveFile(short[][] pSpaces, int pShipSpaces, int pShipsHit, Vector2[] pShots, short[][] eSpaces, int eShipSpaces, int eShipsHit, Vector2[] eShots, long timer, int diff, List<Vector2> shotTargs, List<Vector2> checkAround)
        {
            //create a save class and fill out all the variables
            var save = new GameSave();
            //player board
            save.PlayerSpaces = pSpaces;
            save.PShipSpaces = pShipSpaces;
            save.PShipsHit = pShipsHit;
            save.PShots = pShots;

            //enemy board
            save.EnemySpaces = eSpaces;
            save.EShipSpaces = eShipSpaces;
            save.EShipsHit = eShipsHit;
            save.EShots = eShots;

            //game state
            save.Timer = timer;
            save.Difficulty = diff;
            save.ShotTargets = shotTargs;
            save.CheckAround = checkAround;

            //convert to json and write file in the save folder
            var json = JsonSerializer.Serialize(save);
            var num = LatestSaveNum;
            if (GetOngoingSave == null)
                num++;
            File.WriteAllText(SavePath + SaveName + num, json);
        }

        public void ClearOldSaves()
        {
            //if max saves hasn't been set, return
            if (MaxSaves <= 0)
                return;

            //if file doesn't exist return
            if (Empty)
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
            if (Empty)
                return null;

            var fullName = @$"{SavePath}{name}";
            var dir = Directory.GetFiles(SavePath);

            if (!Directory.GetFiles(SavePath).Contains(fullName))
                return null;

            //deserialise file
            GameSave? save;
            try
            {
                save = JsonSerializer.Deserialize<GameSave>(File.ReadAllText(fullName));
            }
            catch
            {
                save = null;
            }
            if (save == null)
                return null;

            //check if valid
            if (!IsValid(save))
                return null;

            return save;
        }

        private bool IsValid(GameSave save)
        {
            //check difficulty
            if (save.Difficulty < 0 || save.Difficulty > 2)
                return false;

            //check timer
            if (save.Timer < 0)
                return false;

            //check height
            if (save.PlayerSpaces.Length != save.EnemySpaces.Length)
                return false;

            //loop through both boards
            for (int board = 0; board < 2; board++)
            {
                //switch board on loop
                var curB = save.PlayerSpaces;
                var curShots = save.PShots.ToList();
                var curShipS = save.PShipSpaces;
                var curShipH = save.PShipsHit;
                if (board == 1)
                {
                    curB = save.EnemySpaces;
                    curShots = save.EShots.ToList();
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
                            curShots.RemoveAll(v => v.Equals(new Vector2(x,y)));
                        }    
                    }
                }

                //if the number of ships hit, or number of ships spaces is wrong, return false
                if (curShipH != 0)
                    return false;
                if (curShipS != 0)
                    return false;

                //if number of shots was wrong, return false
                if (curShots.Count != 0)
                    return false;
            }


            return true;
        }

        public bool IsOngoing(GameSave save)
        {
            //check if either board has won
            if (save.EShipsHit == save.EShipSpaces)
                return false;
            if (save.PShipsHit == save.PShipSpaces)
                return false;
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
                List<FileInfo> files = new DirectoryInfo(SavePath).GetFiles().OrderBy(f => f.CreationTime).ToList();
                files.Reverse();

                //remove invalid
                //loop through all files
                for (int i = 0; i < files.Count; i++)
                {
                    //try to deserialise current file
                    GameSave? save;
                    try
                    {
                        save = JsonSerializer.Deserialize<GameSave>(File.ReadAllText(files[i].FullName));
                    }
                    catch
                    {
                        save = null;
                    }

                    //if failed to deserialise, set current file entry to null and continue
                    if (save == null)
                    {
                        files[i] = null;
                        continue;
                    }

                    //check if save file is valid
                    if (!IsValid(save))
                        files[i] = null;
                }

                //remove all files that were found to be invalid
                files.RemoveAll(v => v == null);
                
                return files.ToArray();
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
                //check whether directory exists, and whether it has any files
                if (!Directory.Exists(SavePath))
                    return true;
                if (Directory.GetFiles(SavePath).Length == 0)
                    return true;
                return false;
            }
        }

        public GameSave? GetOngoingSave
        {
            get
            {
                var files = FilesByDate;
                if (files == Array.Empty<FileInfo>())
                    return null;

                var file = GetGameSave(files[0].Name);
                if (!IsOngoing(file))
                    return null;
                return file;
            }
        }

        public static SaveManager Instance
        {
            // Sets an instance of itself if empty, otherwise returns the instance
            get
            {
                if (_instance == null)
                    _instance = new SaveManager();
                return _instance;
            }
        }
        #endregion
    }
}
