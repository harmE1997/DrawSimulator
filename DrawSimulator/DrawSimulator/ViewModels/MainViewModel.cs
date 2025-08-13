using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;

namespace DrawSimulator.ViewModels;

public class TeamField
{
    public string Team { get; set; }
    public string Association { get; set; }
}

public class MainViewModel : ViewModelBase
{
    private DrawManager drawManager;

    private List<TeamField> availableTeams;
    public List<TeamField> AvailableTeams { get => availableTeams; set => this.RaiseAndSetIfChanged(ref availableTeams, value); }

    private TeamField selectedAvailableTeam;
    public TeamField SelectedAvailableTeam { get => selectedAvailableTeam; set => this.RaiseAndSetIfChanged(ref selectedAvailableTeam, value); }

    private int currentPot = 1;
    public int CurrentPot { get => currentPot; set => this.RaiseAndSetIfChanged(ref currentPot, value); }

    private int nrPots;
    public int NrPots { get => nrPots; set { this.RaiseAndSetIfChanged(ref nrPots, value); UpdatePots(); } }

    private int nrTeamsPerPot = 1;
    public int NrTeamsPerPot { get => nrTeamsPerPot; set => this.RaiseAndSetIfChanged(ref nrTeamsPerPot, value); }

    private bool allowFromOwnPot = false;
    public bool AllowFromOwnPot { get => allowFromOwnPot; set => this.RaiseAndSetIfChanged(ref allowFromOwnPot, value); }


    private List<string> teamsInCurrentPot;
    public List<string> TeamsInCurrentPot { get => teamsInCurrentPot; set => this.RaiseAndSetIfChanged(ref teamsInCurrentPot, value); }

    private string currentPotAsString;
    public string CurrentPotAsString { get => currentPotAsString; set => this.RaiseAndSetIfChanged(ref currentPotAsString, value); }

    private string selectedPotTeam;
    public string SelectedPotTeam { get => selectedPotTeam; set => this.RaiseAndSetIfChanged(ref selectedPotTeam, value); }


    private string newTeamName;
    public string NewTeamName { get => newTeamName; set => this.RaiseAndSetIfChanged(ref newTeamName, value); }

    private string newTeamAssociation;
    public string NewTeamAssociation { get => newTeamAssociation; set => this.RaiseAndSetIfChanged(ref newTeamAssociation, value); }

    private string newTeamProhibitedTeams;
    public string NewTeamProhibitedTeams { get => newTeamProhibitedTeams; set => this.RaiseAndSetIfChanged(ref newTeamProhibitedTeams, value); }

    private string newTeamProhibitedAssociations;
    public string NewTeamProhibitedAssociations { get => newTeamProhibitedAssociations; set => this.RaiseAndSetIfChanged(ref newTeamProhibitedAssociations, value); }

    private string newAssociation;
    public string NewAssociation { get => newAssociation; set => this.RaiseAndSetIfChanged(ref newAssociation, value); }

    private string selectedAssociation;
    public string SelectedAssociation { get => selectedAssociation; set => this.RaiseAndSetIfChanged(ref selectedAssociation, value); }

    private List<string> availableAssociations;
    public List<string> AvailableAssociations { get => availableAssociations; set => this.RaiseAndSetIfChanged(ref availableAssociations, value); }

    private int nrAttempts;
    public int NrAttempts { get => nrAttempts; set => this.RaiseAndSetIfChanged(ref nrAttempts, value); }

    private bool classicDraw = false;
    public bool ClassicDraw { get => classicDraw; set => this.RaiseAndSetIfChanged(ref classicDraw, value); }

    private List<string> drawResults;
    public List<string> DrawResults { get => drawResults; set => this.RaiseAndSetIfChanged(ref drawResults, value); }

    public ReactiveCommand<Unit, Unit> cmdRunDraw { get; }
    public ReactiveCommand<Unit, Unit> cmdShowNextPot { get; }
    public ReactiveCommand<Unit, Unit> cmdShowPreviousPot { get; }
    public ReactiveCommand<Unit, Unit> cmdAddTeam { get; }
    public ReactiveCommand<Unit, Unit> cmdAddSelectedTeamToPot { get; }
    public ReactiveCommand<Unit, Unit> cmdRemoveSelectedTeam { get; }
    public ReactiveCommand<Unit, Unit> cmdRemoveTeamFromPot { get; }
    public ReactiveCommand<Unit, Unit> cmdClearPot { get; }
    public ReactiveCommand<Unit, Unit> cmdAddAssociation { get; }
    public ReactiveCommand<Unit, Unit> cmdRemoveAssociation { get; }

    public MainViewModel()
    {
        drawManager = new DrawManager();
        DrawResults = new List<string>();
        AvailableAssociations = new List<string>();
        AvailableAssociations = drawManager.ReadFromJson(DrawManager.AssociationsSaveFile, new List<string>());

        var teams = drawManager.ReadFromJson(DrawManager.TeamsSaveFile, new Dictionary<string, Team>());
        foreach (var t in teams)
            drawManager.AvailableTeams.Add(t.Value.Name, t.Value);

        SetAvailableTeams();

        var pots = drawManager.ReadFromJson(DrawManager.PotsSaveFile, new Dictionary<int, List<string>>() { { 1, new List<string>() } });
        if (pots.Count == 0)
            pots.Add(1, new List<string>());
        drawManager.Pots = pots;

        NrPots = drawManager.Pots.Count;
        drawManager.Pots = pots;

        if (NrPots == 0)
            NrPots = 4;

        TeamsInCurrentPot = drawManager.Pots[CurrentPot];
        currentPotAsString = "Pot " + CurrentPot.ToString();

        var nextPotCanExecute = this.WhenAnyValue(x => x.CurrentPot, (pot) => { return pot < NrPots && drawManager.Pots.ContainsKey(pot + 1); });
        var previousPotCanExecute = this.WhenAnyValue(x => x.CurrentPot, (pot) => { return pot > 1; });
        var addTeamCanExecute = this.WhenAnyValue(x => x.NewTeamName, x => x.NewTeamAssociation, (a, b) => { return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b); });
        var availableTeamsCommandsCanExecute = this.WhenAny(x => x.SelectedAvailableTeam, (team) => { return team != null; });
        var removeTeamFromPotCanExecute = this.WhenAnyValue(x => x.SelectedPotTeam, (team) => { return !string.IsNullOrEmpty(team); });
        var clearPotCommandCanExecute = this.WhenAnyValue(x => x.TeamsInCurrentPot, (teams) => { return teams.Count > 0; });
        var addAssociationCommandCanExecute = this.WhenAnyValue(x => x.NewAssociation, (association) => { return !string.IsNullOrEmpty(association); });
        var removeAssociationCommandCanExecute = this.WhenAnyValue(x => x.SelectedAssociation, (association) => { return !string.IsNullOrEmpty(association); });

        cmdRunDraw = ReactiveCommand.Create(RunDraw);
        cmdShowNextPot = ReactiveCommand.Create(NextPot, nextPotCanExecute);
        cmdShowPreviousPot = ReactiveCommand.Create(PreviousPot, previousPotCanExecute);
        cmdAddTeam = ReactiveCommand.Create(AddNewTeam, addTeamCanExecute);
        cmdAddSelectedTeamToPot = ReactiveCommand.Create(AddTeamToPot, availableTeamsCommandsCanExecute);
        cmdRemoveSelectedTeam = ReactiveCommand.Create(RemoveSelectedTeam, availableTeamsCommandsCanExecute);
        cmdRemoveTeamFromPot = ReactiveCommand.Create(RemoveTeamFromPot, removeTeamFromPotCanExecute);
        cmdClearPot = ReactiveCommand.Create(ClearPot, clearPotCommandCanExecute);
        cmdAddAssociation = ReactiveCommand.Create(AddAssociation, addAssociationCommandCanExecute);
        cmdRemoveAssociation = ReactiveCommand.Create(RemoveAssociation, removeAssociationCommandCanExecute);
    }

    private async void RunDraw()
    {
        int counter = 0;
        var res = new List<string>();
        if (!drawManager.VerifyPots())
        {
            PopupManager.ShowMessage("Cannot perform draw. Pots invalid");
            return;
        }

        while (res.Count == 0)
        {
            counter++;
            NrAttempts = counter;
            res = await drawManager.RunDraw(AllowFromOwnPot, NrTeamsPerPot, ClassicDraw);
        }

        DrawResults = res;
    }

    private void AddNewTeam()
    {
        drawManager.AddNewTeam(NewTeamName, NewTeamAssociation, NewTeamProhibitedTeams, NewTeamProhibitedAssociations);
        SetAvailableTeams();
    }

    private void AddTeamToPot()
    {
        drawManager.Pots[CurrentPot].Add(SelectedAvailableTeam.Team);
        TeamsInCurrentPot = new();
        TeamsInCurrentPot = drawManager.Pots[CurrentPot];
        TeamsInCurrentPot.Sort();
        drawManager.SaveToJson(drawManager.Pots, DrawManager.PotsSaveFile);
    }

    private void RemoveTeamFromPot()
    {
        if (drawManager.Pots[CurrentPot].Count > 1)
        {
            drawManager.Pots[CurrentPot].Remove(SelectedPotTeam);
            TeamsInCurrentPot = new();
            TeamsInCurrentPot = drawManager.Pots[CurrentPot];
        }

        else
        {
            drawManager.Pots[CurrentPot] = new();
            TeamsInCurrentPot = new();
        }

        drawManager.SaveToJson(drawManager.Pots, DrawManager.PotsSaveFile);
    }

    private void ClearPot()
    {
        drawManager.Pots[CurrentPot] = new();
        TeamsInCurrentPot = new();
        drawManager.SaveToJson(drawManager.Pots, DrawManager.PotsSaveFile);
    }

    private void RemoveSelectedTeam()
    {
        if (TeamsInCurrentPot.Contains(SelectedAvailableTeam.Team))
            RemoveTeamFromPot();

        SelectedPotTeam = SelectedAvailableTeam.Team;
        drawManager.RemoveTeam(SelectedAvailableTeam.Team);
        SetAvailableTeams();
    }

    private void NextPot()
    {
        CurrentPot++;
        CurrentPotAsString = "Pot " + CurrentPot.ToString();
        TeamsInCurrentPot = drawManager.Pots[CurrentPot];
    }

    private void PreviousPot()
    {
        CurrentPot--;
        CurrentPotAsString = "Pot " + CurrentPot.ToString();
        TeamsInCurrentPot = drawManager.Pots[CurrentPot];
    }

    private void AddAssociation()
    {
        var associations = AvailableAssociations;
        associations.Add(NewAssociation);
        associations.Sort();
        AvailableAssociations = new();
        AvailableAssociations = associations;
        drawManager.SaveToJson(AvailableAssociations, DrawManager.AssociationsSaveFile);
    }

    private void RemoveAssociation()
    {
        if (AvailableAssociations.Count > 1)
        {
            var associations = AvailableAssociations;
            associations.Remove(SelectedAssociation);
            associations.Sort();
            AvailableAssociations = new();
            AvailableAssociations = associations;
        }

        else
        {
            AvailableAssociations = new();
        }

        drawManager.SaveToJson(AvailableAssociations, DrawManager.AssociationsSaveFile);
    }

    private void UpdatePots()
    {
        CurrentPot = 1;
        drawManager.RefreshPots(NrPots);
        TeamsInCurrentPot = drawManager.Pots[CurrentPot];
    }

    private void SetAvailableTeams()
    {
        var teamfields = new List<TeamField>();
        foreach (var team in drawManager.AvailableTeams)
        {
            teamfields.Add(new TeamField() { Team = team.Key, Association = team.Value.Association });
        }

        AvailableTeams = teamfields;
    }
}
