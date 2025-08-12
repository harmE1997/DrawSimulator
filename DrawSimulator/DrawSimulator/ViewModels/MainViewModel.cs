using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace DrawSimulator.ViewModels;

public class MainViewModel : ViewModelBase
{
    private DrawManager drawManager;

    private List<string> availableTeams;
    public List<string> AvailableTeams { get => availableTeams; set => this.RaiseAndSetIfChanged(ref availableTeams, value); }

    private string selectedAvailableTeam;
    public string SelectedAvailableTeam { get => selectedAvailableTeam; set => this.RaiseAndSetIfChanged(ref selectedAvailableTeam, value); }

    private int currentPot = 1;
    public int CurrentPot { get => currentPot; set => this.RaiseAndSetIfChanged(ref currentPot, value); }

    private int nrPots = 4;
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


    private List<string> drawResults;
    public List<string> DrawResults { get => drawResults; set => this.RaiseAndSetIfChanged(ref drawResults, value); }

    public ReactiveCommand<Unit, Unit> cmdRunDraw { get; }
    public ReactiveCommand<Unit, Unit> cmdShowNextPot { get; }
    public ReactiveCommand<Unit, Unit> cmdShowPreviousPot { get; }
    public ReactiveCommand<Unit, Unit> cmdAddTeam { get; }
    public ReactiveCommand<Unit, Unit> cmdAddSelectedTeamToPot { get; }
    public ReactiveCommand<Unit, Unit> cmdRemoveSelectedTeam { get; }
    public ReactiveCommand<Unit, Unit> cmdRemoveTeamFromPot { get; }

    public MainViewModel()
    {
        drawManager = new DrawManager();
        UpdatePots();
        DrawResults = new List<string>();

        currentPotAsString = "Pot " + CurrentPot.ToString();

        var nextPotCanExecute = this.WhenAnyValue(x => x.CurrentPot, (pot) => { return pot < NrPots && drawManager.Pots.ContainsKey(pot + 1); });
        var previousPotCanExecute = this.WhenAnyValue(x => x.CurrentPot, (pot) => { return pot > 1; });
        var addTeamCanExecute = this.WhenAnyValue(x => x.NewTeamName, x => x.NewTeamAssociation, (a, b) => { return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b); });
        var availableTeamsCommandsCanExecute = this.WhenAnyValue(x => x.SelectedAvailableTeam, (team) => { return !string.IsNullOrEmpty(team); });
        var removeTeamFromPotCanExecute = this.WhenAnyValue(x => x.SelectedPotTeam, (team) => { return !string.IsNullOrEmpty(team); });

        cmdRunDraw = ReactiveCommand.Create(RunDraw);
        cmdShowNextPot = ReactiveCommand.Create(NextPot, nextPotCanExecute);
        cmdShowPreviousPot = ReactiveCommand.Create(PreviousPot, previousPotCanExecute);
        cmdAddTeam = ReactiveCommand.Create(AddNewTeam, addTeamCanExecute);
        cmdAddSelectedTeamToPot = ReactiveCommand.Create(AddTeamToPot, availableTeamsCommandsCanExecute);
        cmdRemoveSelectedTeam = ReactiveCommand.Create(RemoveSelectedTeam, availableTeamsCommandsCanExecute);
        cmdRemoveTeamFromPot = ReactiveCommand.Create(RemoveTeamFromPot, removeTeamFromPotCanExecute);

        drawManager.ReadTeamsFromJson();
        AvailableTeams = drawManager.AvailableTeams.Keys.ToList();
        AvailableTeams.Sort();

        drawManager.ReadPotsFromJson();
        TeamsInCurrentPot = drawManager.Pots[CurrentPot];
    }

    private void RunDraw()
    {
        var res = new List<string>();
        while (res.Count == 0)
            res = drawManager.RunDraw(AllowFromOwnPot, NrTeamsPerPot);
        DrawResults = res;
    }

    private void AddNewTeam()
    {
        drawManager.AddNewTeam(NewTeamName, NewTeamAssociation, NewTeamProhibitedTeams, NewTeamProhibitedAssociations);
        AvailableTeams = drawManager.AvailableTeams.Keys.ToList();
        AvailableTeams.Sort();
    }

    private void AddTeamToPot()
    {
        drawManager.Pots[CurrentPot].Add(SelectedAvailableTeam);
        TeamsInCurrentPot = new();
        TeamsInCurrentPot = drawManager.Pots[CurrentPot];
        TeamsInCurrentPot.Sort();
        drawManager.SavePotsToJson();
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

        drawManager.SavePotsToJson();
    }

    private void RemoveSelectedTeam()
    {
        if (TeamsInCurrentPot.Contains(SelectedAvailableTeam))
        {
            SelectedPotTeam = SelectedAvailableTeam;
            RemoveTeamFromPot();
        }
        AvailableTeams = drawManager.AvailableTeams.Keys.ToList();
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

    private void UpdatePots()
    {
        CurrentPot = 1;
        drawManager.RefreshPots(NrPots);
        TeamsInCurrentPot = drawManager.Pots[CurrentPot];
    }
}
