using System.Collections.Generic;

namespace DrawSimulator
{
    public class Team
    {
        public string Name { get; set; }
        public string Association { get; set; }

        public List<string> ProhibitedTeams { get; private set; }
        public List<string> ProhibitedAssociations { get; private set; }

        public Dictionary<int, List<string>> DrawnTeams { get; set; }
        public Dictionary<int, List<string>> DrawPots { get; set; }

        public Team(string name, string association, List<string> prohibitedteams, List<string> prohibitedassociations)
        {
            Name = name;
            Association = association;
            ProhibitedTeams = prohibitedteams;
            ProhibitedAssociations = prohibitedassociations;
            DrawnTeams = new Dictionary<int, List<string>>();
            DrawPots = new Dictionary<int, List<string>>();
        }

        public bool ValidateTeam(Team team, int pot, int maxteamsperpot)
        {
            if (team.Name == Name)
                return false;

            if (team.Association == Association)
                return false;

            if (ProhibitedTeams.Contains(team.Name))
                return false;

            if (ProhibitedAssociations.Contains(team.Association))
                return false;

            if (DrawnTeams[pot].Count >= maxteamsperpot)
                return false;

            if (DrawnTeams[pot].Contains(team.Name))
                return false;

            return true;
        }

        public string DrawResultToString()
        {
            string res = Name;
            foreach (var pot in DrawnTeams)
            {
                res += "\nPot " + pot.Key + "(" + pot.Value.Count + ")" + ": ";
                foreach (var team in pot.Value)
                    res += team + ", ";
            }
            return res;
        }
    }
}
