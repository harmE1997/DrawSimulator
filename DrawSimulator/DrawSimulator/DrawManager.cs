using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DrawSimulator
{
    public class DrawManager
    {
        public static string TeamsSaveFile = "AvailableTeams.json";
        public static string PotsSaveFile = "Pots.json";
        public static string AssociationsSaveFile = "Associations.json";

        private int nrTeamsPerPot;
        public Dictionary<int, List<string>> Pots { get; set; }
        public Dictionary<string, Team> AvailableTeams { get; private set; }

        private Dictionary<string, Team> TeamsInDraw;

        public DrawManager()
        {
            Pots = new Dictionary<int, List<string>>();
            AvailableTeams = new Dictionary<string, Team>();
        }

        #region Public Operators
        public async Task<List<string>> RunDraw(bool allowfromownpot, int nrteamsperpot)
        {
            //remove the last draw
            foreach (var team in AvailableTeams)
            {
                team.Value.DrawnTeams.Clear();
                team.Value.DrawnAssociations.Clear();
                for (int x = 1; x < Pots.Count + 1; x++)
                    team.Value.DrawnTeams.Add(x, new List<string>());
            }

            Random rand = new Random();
            var results = new List<string>();
            TeamsInDraw = GetTeamsInDraw();
            nrTeamsPerPot = nrteamsperpot;
            SetDrawPots();

            foreach (var Pot in Pots)
            {
                foreach (var team in Pot.Value)
                {
                    foreach (var drawpotkey in Pots.Keys)
                    {
                        if (!allowfromownpot)
                        {
                            if (drawpotkey == Pot.Key)
                                continue;
                        }

                        bool noroom = false;
                        bool deadlock = false;
                        while (!noroom && !deadlock)
                        {
                            if (TeamsInDraw[team].DrawnTeams[drawpotkey].Count < nrteamsperpot)
                            {
                                var drawpot = TeamsInDraw[team].DrawPots[drawpotkey];
                                if (drawpot.Count == 0)
                                {
                                    deadlock = true;
                                    return new List<string>();
                                    //continue;
                                }
                                var teamindex = rand.Next(0, drawpot.Count);
                                var drawnteam = drawpot[teamindex];
                                AllocateTeam(team, drawnteam, drawpotkey, Pot.Key);
                                SetDrawPots();
                            }

                            else
                                noroom = true;
                        }
                    }
                    results.Add(TeamsInDraw[team].DrawResultToString());
                }
            }

            if (!VerifyDraw())
                results = new List<string>();

            return results;
        }

        public void AddNewTeam(string newteamname, string newteamassociation, string newteamprohibitedteams, string newteamprohibitedassociations)
        {
            if (AvailableTeams.ContainsKey(newteamname))
                return;

            var prohibitedteams = new List<string>();
            if (!string.IsNullOrEmpty(newteamprohibitedteams))
                prohibitedteams = newteamprohibitedteams.Split(",").ToList();

            var prohibitedassociations = new List<string>();
            if (!string.IsNullOrEmpty(newteamprohibitedassociations))
                prohibitedassociations = newteamprohibitedassociations.Split(",").ToList();

            var newteam = new Team(newteamname, newteamassociation, prohibitedteams, prohibitedassociations);
            AvailableTeams.Add(newteamname, newteam);
            SaveToJson(AvailableTeams, TeamsSaveFile);
        }

        public void RemoveTeam(string team)
        {
            AvailableTeams.Remove(team);
            Pots[GetPot(team)].Remove(team);
            SaveToJson(AvailableTeams, TeamsSaveFile);
            SaveToJson(Pots, PotsSaveFile);
        }

        public void RefreshPots(int nrpots)
        {
            Pots = new();
            for (int i = 1; i < nrpots + 1; i++)
                Pots.Add(i, new List<string>());
        }

        #endregion

        #region JSON Methods

        public T ReadFromJson<T>(string filepath, T defaultvalue)
        {
            if (!File.Exists(filepath))
            {
                SaveToJson<T>(defaultvalue, filepath);
                return defaultvalue;
            }

            string input = File.ReadAllText(filepath);
            var res = JsonSerializer.Deserialize<T>(input, new JsonSerializerOptions { WriteIndented = true });
            return res;
        }

        public void SaveToJson<T>(T data, string filepath)
        {
            try
            {
                string output = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filepath, output);
            }

            catch { return; }
        }

        #endregion

        #region Private Support Functions

        private void SetDrawPots()
        {
            foreach (var team in TeamsInDraw)
            {
                foreach (var drawpotkey in Pots.Keys)
                {
                    if (TeamsInDraw[team.Key].DrawPots.ContainsKey(drawpotkey))
                        TeamsInDraw[team.Key].DrawPots[drawpotkey] = GenerateDrawPot(team.Key, drawpotkey);
                    else
                        TeamsInDraw[team.Key].DrawPots.Add(drawpotkey, GenerateDrawPot(team.Key, drawpotkey));
                }
            }

            FindAndFillTeamWithOnlyOneOutcome();
        }
        private List<string> GenerateDrawPot(string currentteam, int potkey)
        {
            var pot = Pots[potkey];
            var drawpot = new List<string>();
            foreach (var team in pot)
            {
                if (TeamsInDraw[team].ValidateTeam(TeamsInDraw[currentteam], GetPot(currentteam), nrTeamsPerPot)
                    && TeamsInDraw[currentteam].ValidateTeam(TeamsInDraw[team], GetPot(team), nrTeamsPerPot))
                {
                    drawpot.Add(team);
                }
            }
            return drawpot;
        }

        private Dictionary<string, Team> GetTeamsInDraw()
        {
            var res = new Dictionary<string, Team>();
            foreach (var Pot in Pots)
            {
                foreach (var team in Pot.Value)
                {
                    res.Add(team, AvailableTeams[team]);
                }
            }
            return res;
        }

        private void AllocateTeam(string team, string drawnteam, int drawpotkey, int currentpotkey)
        {
            //add drawn team to current team
            if (TeamsInDraw[team].DrawnTeams.ContainsKey(drawpotkey))
                TeamsInDraw[team].DrawnTeams[drawpotkey].Add(drawnteam);
            else
                TeamsInDraw[team].DrawnTeams.Add(drawpotkey, new List<string> { drawnteam });
            TeamsInDraw[team].DrawnAssociations.Add(TeamsInDraw[drawnteam].Association);

            //add cuttent team to drawn team
            if (TeamsInDraw[drawnteam].DrawnTeams.ContainsKey(currentpotkey))
                TeamsInDraw[drawnteam].DrawnTeams[currentpotkey].Add(team);
            else
                TeamsInDraw[drawnteam].DrawnTeams.Add(currentpotkey, new List<string> { team });
            TeamsInDraw[drawnteam].DrawnAssociations.Add(TeamsInDraw[team].Association);
        }

        private void FindAndFillTeamWithOnlyOneOutcome()
        {
            bool filled = false;
            foreach (var tid in TeamsInDraw.Values)
            {
                foreach (var dp in tid.DrawPots)
                {
                    if (dp.Value.Count + tid.DrawnTeams[dp.Key].Count == nrTeamsPerPot && dp.Value.Count != 0)
                    {
                        foreach (var teamtoallocate in dp.Value)
                        {
                            AllocateTeam(tid.Name, teamtoallocate, dp.Key, GetPot(tid.Name));
                        }
                        filled = true;
                    }
                }
            }

            if (filled)
                SetDrawPots();
        }

        private int GetPot(string team)
        {
            foreach (var pot in Pots)
            {
                if (pot.Value.Contains(team))
                    return pot.Key;
            }
            return 0;
        }

        private bool VerifyDraw()
        {
            foreach (var team in TeamsInDraw)
            {
                foreach (var pot in team.Value.DrawnTeams)
                {
                    if (pot.Value.Count != nrTeamsPerPot)
                        return false;
                }
            }

            return true;
        }

        #endregion
    }
}
