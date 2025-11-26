using System.Collections.Generic;
using System.Linq;

namespace DrawSimulator
{
    public enum SpecialTreatAssociations
    {
        UEFA
    }

    public class Team
    {
        public string Name { get; set; }
        public string Association { get; set; }

        public List<string> ProhibitedTeams { get; private set; }
        public List<string> ProhibitedAssociations { get; private set; }

        public Dictionary<int, List<string>> DrawnTeams { get; set; }
        public List<string> DrawnAssociations { get; set; }

        public Dictionary<int, List<string>> DrawPots { get; set; }

        public Team(string name, string association, List<string> prohibitedteams, List<string> prohibitedassociations)
        {
            Name = name;
            Association = association;
            ProhibitedTeams = prohibitedteams;
            ProhibitedAssociations = prohibitedassociations;
            DrawnTeams = new Dictionary<int, List<string>>();
            DrawPots = new Dictionary<int, List<string>>();
            DrawnAssociations = new List<string>();
        }

        public bool ValidateTeam(Team team, int pot, int maxteamsperpot)
        {
            if (team.Name == Name)
                return false;

            if (team.Association == Association && Association != SpecialTreatAssociations.UEFA.ToString())
                return false;

            if (ProhibitedTeams.Contains(team.Name))
                return false;

            if (ProhibitedAssociations.Contains(team.Association))
                return false;

            if (DrawnTeams[pot].Count >= maxteamsperpot)
                return false;

            if (DrawnTeams[pot].Contains(team.Name))
                return false;

            if (team.Association == SpecialTreatAssociations.UEFA.ToString() && nrAssociationAppearances(team.Association) == 2)
                return false;

            if (team.Association != SpecialTreatAssociations.UEFA.ToString() && nrAssociationAppearances(team.Association) == 1)
                return false;

            return true;
        }

        private int nrAssociationAppearances(string association)
        {
            int appearances = 0;
            foreach (var a in DrawnAssociations)
            {
                if (a == association)
                    appearances++;
            }

            if (Association == SpecialTreatAssociations.UEFA.ToString() && association == SpecialTreatAssociations.UEFA.ToString())
                appearances++;

            return appearances;
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

        public string ProhibitedTeamsToString()
        {
            string res = "";
            foreach (var pt in ProhibitedTeams)
                res += pt + ", ";
            return res;
        }

        public string ProhibitedAssociationsToString()
        {
            string res = "";
            foreach (var pa in ProhibitedAssociations)
            {
                res += pa;
                if (pa != ProhibitedAssociations.Last())
                    res += ", ";
            }

            return res;
        }
    }
}
