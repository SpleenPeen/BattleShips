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

            //check if save file tampered and fix when necessary
            save.Difficulty = Math.Clamp(save.Difficulty, 0, 2);

            //check board sizes
            var maxHeight = Math.Max(save.PlayerSpaces.Length, save.EnemySpaces.Length);
            int maxWidth = 0;
            for (int board = 0; board < 2; board++)
            {
                var curB = save.PlayerSpaces;
                if (board == 1)
                    curB = save.EnemySpaces;
                for (int row = 0; row < curB.Length; row++)
                    maxWidth = Math.Max(maxWidth, curB[row].Length);
            }

            if (maxWidth == 0  || maxHeight == 0)
                return null;

            //fill out with blanks 

            return save;
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
