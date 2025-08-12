using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DrawSimulator
{
    public class DrawManager
    {
        private const string teamsSaveFile = "AvailableTeams.json";
        private const string potsSaveFile = "Pots.json";
        public Dictionary<int, List<string>> Pots { get; private set; }
        public Dictionary<string, Team> AvailableTeams { get; private set; }

        private Dictionary<string, Team> TeamsInDraw;

        public DrawManager()
        {
            AvailableTeams = new Dictionary<string, Team>();
        }

        public List<string> RunDraw(bool allowfromownpot, int nrteamsperpot)
        {
            //remove the last draw
            foreach (var team in AvailableTeams)
            {
                team.Value.DrawnTeams.Clear();
                for (int x = 1; x < Pots.Count + 1; x++)
                    team.Value.DrawnTeams.Add(x, new List<string>());
            }

            Random rand = new Random();
            var results = new List<string>();
            TeamsInDraw = GetTeamsInDraw();
            SetDrawPots(nrteamsperpot);

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
                                }
                                var teamindex = rand.Next(0, drawpot.Count);
                                var drawnteam = drawpot[teamindex];
                                AllocateTeam(team, drawnteam, drawpotkey, Pot.Key, nrteamsperpot);
                            }

                            else
                                noroom = true;
                        }
                    }
                    results.Add(TeamsInDraw[team].DrawResultToString());
                }
            }
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
            SaveTeamsToJson();
        }

        public void RemoveTeam(string team)
        {
            AvailableTeams.Remove(team);
            SaveTeamsToJson();
        }

        public void RefreshPots(int nrpots)
        {
            Pots = new();
            for (int i = 1; i < nrpots + 1; i++)
                Pots.Add(i, new List<string>());
        }



        public void ReadTeamsFromJson()
        {
            if (!File.Exists(teamsSaveFile))
            {
                SaveTeamsToJson();
                return;
            }

            string input = File.ReadAllText(teamsSaveFile);
            var teams = JsonSerializer.Deserialize<List<Team>>(input, new JsonSerializerOptions { WriteIndented = true });
            foreach (var t in teams)
                AvailableTeams.Add(t.Name, t);
        }

        private void SaveTeamsToJson()
        {
            try
            {
                string output = JsonSerializer.Serialize(AvailableTeams.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(teamsSaveFile, output);
            }

            catch { return; }
        }

        public void ReadPotsFromJson()
        {
            if (!File.Exists(potsSaveFile))
            {
                SavePotsToJson();
                return;
            }

            string input = File.ReadAllText(potsSaveFile);
            Pots = JsonSerializer.Deserialize<Dictionary<int, List<string>>>(input, new JsonSerializerOptions { WriteIndented = true });
        }

        public void SavePotsToJson()
        {
            try
            {
                string output = JsonSerializer.Serialize(Pots, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(potsSaveFile, output);
            }

            catch { return; }
        }



        private List<string> GenerateDrawPot(string currentteam, int potkey, int nrteamsperpot)
        {
            var pot = Pots[potkey];
            var drawpot = new List<string>();
            foreach (var team in pot)
            {
                if (TeamsInDraw[team].ValidateTeam(TeamsInDraw[currentteam], GetPot(currentteam), nrteamsperpot)
                    && TeamsInDraw[currentteam].ValidateTeam(TeamsInDraw[team], GetPot(team), nrteamsperpot))
                {
                    drawpot.Add(team);
                }
            }
            return drawpot;
        }

        private void SetDrawPots(int nrTeamsPerPot)
        {
            foreach (var team in TeamsInDraw)
            {
                foreach (var drawpotkey in Pots.Keys)
                {
                    if (TeamsInDraw[team.Key].DrawPots.ContainsKey(drawpotkey))
                        TeamsInDraw[team.Key].DrawPots[drawpotkey] = GenerateDrawPot(team.Key, drawpotkey, nrTeamsPerPot);
                    else
                        TeamsInDraw[team.Key].DrawPots.Add(drawpotkey, GenerateDrawPot(team.Key, drawpotkey, nrTeamsPerPot));
                }
            }
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

        private void AllocateTeam(string team, string drawnteam, int drawpotkey, int currentpotkey, int nrteamsperpot)
        {
            if (TeamsInDraw[team].DrawnTeams[drawpotkey].Count == nrteamsperpot || TeamsInDraw[drawnteam].DrawnTeams[currentpotkey].Count == nrteamsperpot)
            {
                int x = -1;
            }
            //add drawn team to current team
            if (TeamsInDraw[team].DrawnTeams.ContainsKey(drawpotkey))
                TeamsInDraw[team].DrawnTeams[drawpotkey].Add(drawnteam);
            else
                TeamsInDraw[team].DrawnTeams.Add(drawpotkey, new List<string> { drawnteam });

            //add cuttent team to drawn team
            if (TeamsInDraw[drawnteam].DrawnTeams.ContainsKey(currentpotkey))
                TeamsInDraw[drawnteam].DrawnTeams[currentpotkey].Add(team);
            else
                TeamsInDraw[drawnteam].DrawnTeams.Add(currentpotkey, new List<string> { team });

            SetDrawPots(nrteamsperpot);
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
    }
}
