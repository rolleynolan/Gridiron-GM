using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Services;
using GridironGM.GameCore.Utilities;

public partial class DashboardController : Control
{
    private enum NativeStartupState
    {
        Unknown = 0,
        Ready = 1,
        MissingAutosave = 2,
        CorruptAutosave = 3,
    }

    private const bool DEBUG_DASHBOARD = true;
    private const int LEAGUE_TAB_INDEX = 1;
    private const int LEAGUE_HISTORY_SUBTAB_INDEX = 4;
    private const int ROSTER_TAB_INDEX = 2;
    private const int CONTINUE_MAX_DAYS = 14;
    private const int REQUEST_TIMEOUT_MS = 5000;
    private const int SIM_UNTIL_TIMEOUT_MS = 30000;
    private static bool _printedFirstPlayerDebug = false;
    [Export]
    public bool DebugToolsVisibleByDefault { get; set; } = false;
    private Label _serverStatus;
    private Label _calendarTitle;
    private Label _calendarText;
    private Control _mainTabs;
    private Control _overviewTabPanel;
    private Control _leagueTabPanel;
    private Control _rosterTabPanel;
    private Button _btnOverviewTab;
    private Button _btnLeagueTab;
    private Button _btnRosterTab;
    private Label _lblFrontOfficeHeader;
    private Label _lblUserTeam;
    private Label _lblGameStatus;
    private Label _lblGameNext;
    private Label _continueStatus;
    private Control _debugPanel;
    private Label _debugOutputLabel;
    private RichTextLabel _stateDump;
    private Button _btnContinue;
    private Button _btnInbox;
    private Button _btnLeagueShortcut;
    private Button _btnRosterShortcut;
    private Button _btnSaveGame;
    private CheckButton _btnToggleDebug;
    private Button _btnRefresh;
    private Button _btnAdvanceDay;
    private Button _btnNewGame;
    private Button _btnResetSave;
    private Button _btnSaveNativeGame;
    private Button _btnLoadNativeGame;
    private Button _btnRunGameCoreSmokeTest;
    private OptionButton _simUntilSelect;
    private Button _btnSimUntil;
    private Button _btnColumns;
    private Control _startupPanel;
    private Label _lblStartupWarning;
    private Label _lblStartupStatus;
    private Button _btnStartupContinue;
    private Button _btnStartupLoadGame;
    private Button _btnStartupNewGame;
    private Button _btnStartupExit;
    private ConfirmationDialog _newGameConfirmDialog;
    private Button _btnSetUserTeam;
    private AcceptDialog _newGameTeamPicker;
    private ItemList _teamPickList;
    private Label _lblPickTeamText;
    private Label _lblPickTeamHint;
    private PopupMenu _popupColumns;
    private Label _lblPlayerHeader;
    private RichTextLabel _rtlScoutSummary;
    private RichTextLabel _rtlScoutReport;
    private Container _tagsRow;

    // NEW: team/roster UI
    private ItemList _teamList;
    private Label _rosterSummary;
    private Button _btnRosterViewMode;
    private Button _btnDepthChartViewMode;
    private HSplitContainer _rosterSplit;
    private LineEdit _rosterSearch;
    private OptionButton _posFilter;
    private Button _btnClearFilters;
    private Tree _rosterTree;
    private Control _depthChartPanel;
    private Label _depthChartSummary;
    private Button _btnAutoFillDepthChart;
    private Button _btnDepthChartMoveUp;
    private Button _btnDepthChartMoveDown;
    private Button _btnDepthChartSetStarter;
    private Label _depthChartActionStatus;
    private Label _depthChartSelectionStatus;
    private Tree _depthChartTree;
    private RichTextLabel _rtlTeamSummary;
    private Label _lblRecentResultsHeader;
    private RichTextLabel _overviewRecentResults;
    private Label _overviewActionHeader;
    private Label _overviewActionTitle;
    private Label _overviewActionSuggested;
    private RichTextLabel _overviewActionBody;
    private RichTextLabel _overviewNextEventSummary;
    private Control _overviewPlayoffPanel;
    private Label _overviewPlayoffHeader;
    private RichTextLabel _overviewPlayoffSummary;
    private Button _overviewActionButton;
    private Control _gameDayPopup;
    private Label _lblGameDayWeek;
    private Label _lblGameDayMatchup;
    private Label _lblGameDayVenue;
    private Label _lblGameDayRecords;
    private Label _lblGameDayStatus;
    private Button _btnGameDaySim;
    private Button _btnGameDayWatch;
    private Button _btnGameDayCancel;
    private Control _postGameRecapPopup;
    private Label _lblPostGameScore;
    private Label _lblPostGameWinner;
    private Label _lblPostGameInfo;
    private Label _lblPostGameSummary;
    private Label _lblPostGameStatus;
    private Button _btnPostGameBoxScore;
    private Button _btnPostGameClose;
    private Control _boxScorePopup;
    private Label _lblBoxScorePopupInfo;
    private Label _lblBoxScorePopupScore;
    private Label _lblBoxScorePopupStatus;
    private Tree _boxScorePopupTeamStatsTree;
    private Tree _boxScorePopupQuarterTree;
    private Button _btnBoxScorePopupClose;
    private Tree _standingsTree;
    private RichTextLabel _overviewStandingsSnapshot;
    private VBoxContainer _resultsListPanel;
    private ItemList _resultsList;
    private VBoxContainer _boxScorePanel;
    private Label _boxScoreHeader;
    private Tree _boxScoreQuarterTree;
    private Tree _boxScoreTeamStatsTree;
    private ItemList _boxScoreLeadersList;
    private Button _btnBoxScoreBack;
    private Tree _scheduleList;
    private Label _lblScheduleActionStatus;
    private Button _btnScheduleAction;
    private Tree _injuriesTree;
    private TabContainer _leagueHubTabs;
    private ItemList _historySeasonList;
    private RichTextLabel _historyDetailText;
    private OptionButton _resultsWeekSelect;
    private Button _btnHubRefresh;

    private readonly List<RosterColumn> _columns = new();
    private readonly Dictionary<string, bool> _columnVisibility = new();
    private Godot.Collections.Array _currentRoster = new();
    private readonly List<PlayerRow> _rosterRows = new();
    private readonly Dictionary<string, Godot.Collections.Dictionary> _playerDetailsById = new();
    private readonly Dictionary<string, Godot.Collections.Array> _teamRosterCache = new();
    private readonly Dictionary<string, Dictionary<string, Godot.Collections.Dictionary>> _teamPlayerDetailsCache = new();
    private int _teamSelectionVersion = 0;
    private string _currentTeamId = "";
    private string _userTeamId = "";
    private string _sortColumnId = "";
    private bool _sortAscending = true;
    private string _rosterSearchText = "";
    private string _posFilterValue = "All";
    private bool _suppressTeamListEvents = false;
    private bool _suppressRosterFilterEvents = false;
    private bool _depthChartViewActive = false;
    private bool _depthChartRequestBusy = false;
    private bool _dashboardRefreshPendingFromDepthChartEdit = false;
    private Godot.Collections.Array _inboxMessages = new();
    private string _selectedDepthChartPosition = "";
    private string _selectedDepthChartPlayerId = "";
    private string _selectedDepthChartPlayerName = "";
    private string _selectedInboxMessageId = "";
    private Godot.Collections.Dictionary _selectedInboxActionItem = null;
    private string _selectedSimGameId = "";
    private Godot.Collections.Array _resultsGames = new();
    private Godot.Collections.Array _scheduleGames = new();
    private readonly List<string> _availableResultsWeekKeys = new();
    private readonly Dictionary<string, string> _resultsWeekLabels = new();
    private readonly HashSet<string> _completedResultsWeekKeys = new();
    private readonly Dictionary<string, Godot.Collections.Dictionary> _gameCache = new();
    private int _resultsSelectionVersion = 0;
    private bool _suppressResultsWeekEvents = false;
    private int _currentWeek = 1;
    private int _maxWeek = 18;
    private string _selectedResultsWeekKey = "";
    private string _gmName = "User GM";
    private string _gmRole = "General Manager";
    private string _gmTeamLabel = "(unknown)";
    private int? _gmReputation = null;
    private int? _gmJobSecurity = null;
    private readonly List<LeagueHistorySeasonDto> _leagueHistorySeasons = new();
    private bool _suppressHistorySelectionEvents = false;
    private int? _selectedHistorySeasonYear = null;
    private string _dashboardTeamName = "";
    private string _dashboardTeamRecord = "0-0";
    private int? _dashboardRosterSize = null;
    private int? _dashboardInjuryCount = null;
    private string _dashboardCapRoom = "N/A";
    private string _inboxEmptyDetailMessage = "No urgent messages.";
    private Godot.Collections.Dictionary _dashboardTeam = new();
    private Godot.Collections.Dictionary _dashboardCalendar = new();
    private Godot.Collections.Dictionary _dashboardNextGame = new();
    private Godot.Collections.Dictionary _activeGameDayGame = new();
    private Godot.Collections.Array _dashboardRecentResults = new();
    private Godot.Collections.Dictionary _dashboardPlayoffBracket = new();
    private Godot.Collections.Dictionary _latestGameResult = null;
    private Godot.Collections.Dictionary _selectedScheduleGame = null;
    private bool _restorePostGameRecapAfterBoxScore = false;

    private static readonly string[] PosFilterOptions =
    {
        "All",
        "QB",
        "RB",
        "WR",
        "TE",
        "LT",
        "LG",
        "C",
        "RG",
        "RT",
        "OL",
        "EDGE",
        "DT",
        "DL",
        "LB",
        "CB",
        "S",
        "DB",
        "K",
        "P"
    };

    private GameCoreContext _nativeGameCoreContext;
    private GameCoreSaveService _nativeGameCoreSaveService;
    private RosterService _nativeRosterService;
    private DepthChartService _nativeDepthChartService;
    private ScheduleService _nativeScheduleService;
    private StandingsService _nativeStandingsService;
    private DashboardService _nativeDashboardService;
    private ContinueService _nativeContinueService;
    private GameDayService _nativeGameDayService;

    // NEW: store team dicts from /state_summary so we can map selection -> team_id
    private Godot.Collections.Array _teams = new();
    private readonly Dictionary<string, string> _teamDisplayById = new();
    private readonly Dictionary<string, string> _teamShortById = new();
    private readonly List<string> _teamPickIndexToId = new();
    private bool _awaitingNewGameTeamPick = false;
    private bool _handledNewGameTeamPick = false;
    private int _currentMainTab = 0;
    private string _pendingNativeStatusMessage = "";
    private NativeStartupState _nativeStartupState = NativeStartupState.Unknown;

    private T GetNodeOrWarn<T>(string path, string missingMessage = null) where T : Node
    {
        var node = GetNodeOrNull<T>(path);
        if (node == null)
        {
            var message = string.IsNullOrWhiteSpace(missingMessage)
                ? $"UI node not found at {path}; some dashboard features may be unavailable."
                : missingMessage;
            GD.PrintErr(message);
        }
        return node;
    }

    public override async void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--gamecore-smoke-test", StringComparer.Ordinal))
        {
            var smokeResult = await Task.Run(() => GameCoreSmokeTest.Run(GetTeamSeedPath()));
            foreach (var step in smokeResult.Steps)
                GD.Print($"[GameCore smoke] {step}");

            if (!smokeResult.Ok)
                GD.PushError($"[GameCore smoke] {smokeResult.Message}");

            GetTree().Quit(smokeResult.Ok ? 0 : 1);
            return;
        }

        var window = GetWindow();
        if (window != null)
            window.MinSize = new Vector2I(1152, 648);

        // Existing nodes
        _serverStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/ContinueBlock/ServerStatus");
        _calendarTitle = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/HeaderPanel/HeaderRow/CalendarBlock/CalendarTitle");
        _calendarText = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/HeaderPanel/HeaderRow/CalendarBlock/CalendarText");
        _mainTabs = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/MainTabs");
        _overviewTabPanel = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab", "OverviewTab content not found; dashboard tab navigation will be incomplete.");
        _leagueTabPanel = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab", "LeagueTab content not found; dashboard tab navigation will be incomplete.");
        _rosterTabPanel = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab", "RosterTab content not found; dashboard tab navigation will be incomplete.");
        _btnOverviewTab = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/TabButtonRow/BtnOverviewTab", "OverviewTab button not found; dashboard tab navigation will be unavailable.");
        _btnLeagueTab = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/TabButtonRow/BtnLeagueTab", "LeagueTab button not found; dashboard tab navigation will be unavailable.");
        _btnRosterTab = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/TabButtonRow/BtnRosterTab", "RosterTab button not found; dashboard tab navigation will be unavailable.");
        _lblFrontOfficeHeader = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/HeaderPanel/HeaderRow/FrontOfficeBlock/LblFrontOfficeHeader");
        _lblUserTeam = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/HeaderPanel/HeaderRow/FrontOfficeBlock/LblUserTeam");
        _lblGameStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/HeaderPanel/HeaderRow/GameBlock/GameStatus");
        _lblGameNext = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/HeaderPanel/HeaderRow/GameBlock/GameNext");
        _continueStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/ContinueBlock/ContinueStatus");
        _debugPanel = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/DebugPanel");
        _debugOutputLabel = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugOutputLabel");
        _stateDump = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/DebugPanel/StateDump");

        _btnContinue = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/ActionButtonRow/BtnContinue");
        _btnInbox = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/ActionButtonRow/BtnInbox");
        _btnLeagueShortcut = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/ActionButtonRow/BtnLeagueShortcut");
        _btnRosterShortcut = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/ActionButtonRow/BtnRosterShortcut");
        _simUntilSelect = GetNodeOrWarn<OptionButton>("AppMargin/MainPadding/MainLayout/ActionButtonRow/SimUntilSelect", "SimUntilSelect not found; skipping Sim Until binding.");
        _btnSimUntil = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/ActionButtonRow/BtnSimUntil", "BtnSimUntil not found; skipping Sim Until binding.");
        _btnSaveGame = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/ActionButtonRow/BtnSaveGame");
        _btnToggleDebug = GetNodeOrWarn<CheckButton>("AppMargin/MainPadding/MainLayout/DebugToggleRow/BtnToggleDebug");
        _btnRefresh = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnRefresh");
        _btnAdvanceDay = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnAdvanceDay");
        _btnNewGame = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnNewGame");
        _btnResetSave = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnResetSave");
        _btnSaveNativeGame = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnSaveNativeGame");
        _btnLoadNativeGame = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnLoadNativeGame");
        _btnRunGameCoreSmokeTest = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnRunGameCoreSmokeTest");
        _btnColumns = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/ActionButtonRow/BtnColumns", "BtnColumns not found; skipping columns menu binding.");
        _popupColumns = GetNodeOrWarn<PopupMenu>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/PopupColumns");
        _lblPlayerHeader = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/PlayerReportPanel/LblPlayerHeader");
        _rtlScoutSummary = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/PlayerReportPanel/RtlScoutSummary");
        _rtlScoutReport = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/PlayerReportPanel/ReportScroll/RtlScoutReport");
        _tagsRow = GetNodeOrWarn<Container>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/PlayerReportPanel/TagsRow");
        _rtlTeamSummary = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewLeftColumn/TeamSummaryPanel/TeamSummaryMargin/TeamSummaryContent/RtlTeamSummary");
        _lblRecentResultsHeader = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewLeftColumn/RecentResultsPanel/RecentResultsMargin/RecentResultsContent/LblRecentResultsHeader");
        _overviewRecentResults = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewLeftColumn/RecentResultsPanel/RecentResultsMargin/RecentResultsContent/RecentResultsScroll/OverviewRecentResults");
        _overviewActionHeader = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewRightColumn/ActionRequiredPanel/ActionRequiredMargin/ActionRequiredContent/LblOverviewActionHeader");
        _overviewActionTitle = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewRightColumn/ActionRequiredPanel/ActionRequiredMargin/ActionRequiredContent/OverviewActionTitle");
        _overviewActionSuggested = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewRightColumn/ActionRequiredPanel/ActionRequiredMargin/ActionRequiredContent/OverviewActionSuggested");
        _overviewActionBody = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewRightColumn/ActionRequiredPanel/ActionRequiredMargin/ActionRequiredContent/OverviewActionBody");
        _overviewNextEventSummary = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewRightColumn/NextEventPanel/NextEventMargin/NextEventContent/OverviewNextEventSummary");
        _overviewPlayoffPanel = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/PlayoffPicturePanel");
        _overviewPlayoffHeader = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/PlayoffPicturePanel/PlayoffPictureMargin/PlayoffPictureContent/LblPlayoffPictureHeader");
        _overviewPlayoffSummary = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/PlayoffPicturePanel/PlayoffPictureMargin/PlayoffPictureContent/PlayoffPictureScroll/OverviewPlayoffSummary");
        _overviewActionButton = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewRightColumn/ActionRequiredPanel/ActionRequiredMargin/ActionRequiredContent/OverviewActionButton");
        _gameDayPopup = GetNodeOrNull<Control>("GameDayPopup");
        _lblGameDayWeek = GetNodeOrNull<Label>("GameDayPopup/CenterWrap/Panel/Margin/Content/LblGameDayWeek");
        _lblGameDayMatchup = GetNodeOrNull<Label>("GameDayPopup/CenterWrap/Panel/Margin/Content/LblGameDayMatchup");
        _lblGameDayVenue = GetNodeOrNull<Label>("GameDayPopup/CenterWrap/Panel/Margin/Content/LblGameDayVenue");
        _lblGameDayRecords = GetNodeOrNull<Label>("GameDayPopup/CenterWrap/Panel/Margin/Content/LblGameDayRecords");
        _lblGameDayStatus = GetNodeOrNull<Label>("GameDayPopup/CenterWrap/Panel/Margin/Content/LblGameDayStatus");
        _btnGameDaySim = GetNodeOrNull<Button>("GameDayPopup/CenterWrap/Panel/Margin/Content/ButtonRow/BtnGameDaySim");
        _btnGameDayWatch = GetNodeOrNull<Button>("GameDayPopup/CenterWrap/Panel/Margin/Content/ButtonRow/BtnGameDayWatch");
        _btnGameDayCancel = GetNodeOrNull<Button>("GameDayPopup/CenterWrap/Panel/Margin/Content/ButtonRow/BtnGameDayCancel");
        _postGameRecapPopup = GetNodeOrNull<Control>("PostGameRecapPopup");
        _lblPostGameScore = GetNodeOrNull<Label>("PostGameRecapPopup/CenterWrap/Panel/Margin/Content/LblPostGameScore");
        _lblPostGameWinner = GetNodeOrNull<Label>("PostGameRecapPopup/CenterWrap/Panel/Margin/Content/LblPostGameWinner");
        _lblPostGameInfo = GetNodeOrNull<Label>("PostGameRecapPopup/CenterWrap/Panel/Margin/Content/LblPostGameInfo");
        _lblPostGameSummary = GetNodeOrNull<Label>("PostGameRecapPopup/CenterWrap/Panel/Margin/Content/LblPostGameSummary");
        _lblPostGameStatus = GetNodeOrNull<Label>("PostGameRecapPopup/CenterWrap/Panel/Margin/Content/LblPostGameStatus");
        _btnPostGameBoxScore = GetNodeOrNull<Button>("PostGameRecapPopup/CenterWrap/Panel/Margin/Content/ButtonRow/BtnPostGameBoxScore");
        _btnPostGameClose = GetNodeOrNull<Button>("PostGameRecapPopup/CenterWrap/Panel/Margin/Content/ButtonRow/BtnPostGameClose");
        _boxScorePopup = GetNodeOrNull<Control>("BoxScorePopup");
        _lblBoxScorePopupInfo = GetNodeOrNull<Label>("BoxScorePopup/CenterWrap/Panel/Margin/Content/LblBoxScorePopupInfo");
        _lblBoxScorePopupScore = GetNodeOrNull<Label>("BoxScorePopup/CenterWrap/Panel/Margin/Content/LblBoxScorePopupScore");
        _lblBoxScorePopupStatus = GetNodeOrNull<Label>("BoxScorePopup/CenterWrap/Panel/Margin/Content/LblBoxScorePopupStatus");
        _boxScorePopupTeamStatsTree = GetNodeOrNull<Tree>("BoxScorePopup/CenterWrap/Panel/Margin/Content/BoxScorePopupTeamStatsTree");
        _boxScorePopupQuarterTree = GetNodeOrNull<Tree>("BoxScorePopup/CenterWrap/Panel/Margin/Content/BoxScorePopupQuarterTree");
        _btnBoxScorePopupClose = GetNodeOrNull<Button>("BoxScorePopup/CenterWrap/Panel/Margin/Content/ButtonRow/BtnBoxScorePopupClose");
        if (_gameDayPopup == null)
            GD.PrintErr("Game Day popup is missing from scene.");
        if (_postGameRecapPopup == null)
            GD.PrintErr("Post-game recap popup is missing from scene.");
        if (_boxScorePopup == null)
            GD.PrintErr("Box score popup is missing from scene.");
        _startupPanel = GetNodeOrWarn<Control>("StartupPanel");
        _lblStartupWarning = GetNodeOrWarn<Label>("StartupPanel/CenterWrap/Panel/Margin/Content/LblStartupWarning");
        _lblStartupStatus = GetNodeOrWarn<Label>("StartupPanel/CenterWrap/Panel/Margin/Content/LblStartupStatus");
        _btnStartupContinue = GetNodeOrWarn<Button>("StartupPanel/CenterWrap/Panel/Margin/Content/StartupButtonRow/BtnStartupContinue");
        _btnStartupLoadGame = GetNodeOrWarn<Button>("StartupPanel/CenterWrap/Panel/Margin/Content/StartupButtonRow/BtnStartupLoadGame");
        _btnStartupNewGame = GetNodeOrWarn<Button>("StartupPanel/CenterWrap/Panel/Margin/Content/StartupButtonRow/BtnStartupNewGame");
        _btnStartupExit = GetNodeOrWarn<Button>("StartupPanel/CenterWrap/Panel/Margin/Content/StartupButtonRow/BtnStartupExit");
        _newGameConfirmDialog = GetNodeOrWarn<ConfirmationDialog>("NewGameConfirmDialog");
        _newGameTeamPicker = GetNodeOrWarn<AcceptDialog>("NewGameTeamPicker");
        _teamPickList = GetNodeOrWarn<ItemList>("NewGameTeamPicker/PickerContent/TeamPickList");
        _lblPickTeamText = GetNodeOrWarn<Label>("NewGameTeamPicker/PickerContent/LblPickTeamText");
        _lblPickTeamHint = GetNodeOrWarn<Label>("NewGameTeamPicker/PickerContent/LblPickTeamHint");

        // NEW nodes (make sure you added these nodes under MainTabs)
        _teamList = GetNodeOrWarn<ItemList>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/TeamList");
        _btnSetUserTeam = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/BtnSetUserTeam");
        _rosterSummary = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSummary");
        _btnRosterViewMode = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterModeRow/BtnRosterViewMode");
        _btnDepthChartViewMode = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterModeRow/BtnDepthChartViewMode");
        _rosterSplit = GetNodeOrWarn<HSplitContainer>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit");
        _rosterSearch = GetNodeOrWarn<LineEdit>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/RosterPane/FilterRow/RosterSearch");
        _posFilter = GetNodeOrWarn<OptionButton>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/RosterPane/FilterRow/PosFilter");
        _btnClearFilters = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/RosterPane/FilterRow/BtnClearFilters");
        _rosterTree = GetNodeOrWarn<Tree>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/RosterPane/RosterTree");
        _depthChartPanel = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel");
        _depthChartSummary = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartSummary");
        _btnAutoFillDepthChart = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartActionRow/BtnAutoFillDepthChart");
        _btnDepthChartMoveUp = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartActionRow/BtnDepthChartMoveUp");
        _btnDepthChartMoveDown = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartActionRow/BtnDepthChartMoveDown");
        _btnDepthChartSetStarter = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartActionRow/BtnDepthChartSetStarter");
        _depthChartActionStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartActionRow/DepthChartActionStatus");
        _depthChartSelectionStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartSelectionStatus");
        _depthChartTree = GetNodeOrWarn<Tree>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartTree");
        _standingsTree = GetNodeOrWarn<Tree>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/StandingsTab/StandingsTree");
        _overviewStandingsSnapshot = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/OverviewContentMargin/OverviewContent/OverviewRow/OverviewLeftColumn/StandingsPanel/StandingsMargin/StandingsContent/OverviewStandingsSnapshot");
        _resultsListPanel = GetNodeOrWarn<VBoxContainer>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ResultsTab/ResultsListPanel");
        _resultsList = GetNodeOrWarn<ItemList>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ResultsTab/ResultsListPanel/ResultsList");
        _boxScorePanel = GetNodeOrWarn<VBoxContainer>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ResultsTab/BoxScorePanel");
        _boxScoreHeader = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ResultsTab/BoxScorePanel/BoxScoreHeaderRow/LblBoxScoreHeader");
        _boxScoreQuarterTree = GetNodeOrWarn<Tree>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ResultsTab/BoxScorePanel/BoxScoreQuarterTree");
        _boxScoreTeamStatsTree = GetNodeOrWarn<Tree>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ResultsTab/BoxScorePanel/BoxScoreTeamStatsTree");
        _boxScoreLeadersList = GetNodeOrWarn<ItemList>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ResultsTab/BoxScorePanel/BoxScoreLeadersList");
        _btnBoxScoreBack = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ResultsTab/BoxScorePanel/BoxScoreHeaderRow/BtnBoxScoreBack");
        _scheduleList = GetNodeOrWarn<Tree>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ScheduleTab/ScheduleList");
        _lblScheduleActionStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ScheduleTab/ScheduleActionRow/LblScheduleActionStatus");
        _btnScheduleAction = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ScheduleTab/ScheduleActionRow/BtnScheduleAction");
        _injuriesTree = GetNodeOrWarn<Tree>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/Injuries/InjuriesTree");
        _leagueHubTabs = GetNodeOrWarn<TabContainer>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs");
        _historySeasonList = GetNodeOrWarn<ItemList>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/HistoryTab/HistorySplit/HistorySeasonListPanel/HistorySeasonList");
        _historyDetailText = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/HistoryTab/HistorySplit/HistoryDetailPanel/HistoryDetailScroll/HistoryDetailText");
        _resultsWeekSelect = GetNodeOrWarn<OptionButton>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubControls/ResultsWeekSelect");
        _btnHubRefresh = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubControls/BtnHubRefresh");

        if (_btnRefresh != null)
            _btnRefresh.Pressed += async () => await RefreshAll();
        if (_btnAdvanceDay != null)
            _btnAdvanceDay.Pressed += async () => await AdvanceDay();
        if (_btnNewGame != null)
            _btnNewGame.Pressed += async () => await NewGame();
        if (_btnResetSave != null)
            _btnResetSave.Pressed += async () => await ResetSave();
        if (_btnSaveNativeGame != null)
            _btnSaveNativeGame.Pressed += async () => await SaveNativeGame();
        if (_btnLoadNativeGame != null)
            _btnLoadNativeGame.Pressed += async () => await LoadNativeGame();
        if (_btnRunGameCoreSmokeTest != null)
            _btnRunGameCoreSmokeTest.Pressed += async () => await RunGameCoreSmokeTestAsync();
        if (_btnContinue != null)
            _btnContinue.Pressed += async () => await ContinueUntilPause();
        if (_btnInbox != null)
            _btnInbox.Pressed += async () => await SelectMainTab(0);
        if (_btnLeagueShortcut != null)
            _btnLeagueShortcut.Pressed += async () => await SelectMainTab(1);
        if (_btnRosterShortcut != null)
            _btnRosterShortcut.Pressed += async () => await SelectMainTab(ROSTER_TAB_INDEX);
        if (_btnSaveGame != null)
            _btnSaveGame.Pressed += async () => await SaveNativeGame();
        if (_btnToggleDebug != null)
            _btnToggleDebug.Toggled += OnDebugToggleToggled;
        if (_btnSimUntil != null)
            _btnSimUntil.Pressed += async () => await SimUntilSelectedMilestone();
        if (_btnColumns != null && _popupColumns != null)
            _btnColumns.Pressed += OnColumnsPressed;
        if (_btnSetUserTeam != null)
            _btnSetUserTeam.Pressed += async () => await SetUserTeamFromSelection();
        if (_btnRosterViewMode != null)
            _btnRosterViewMode.Pressed += async () => await SetRosterViewMode(false);
        if (_btnDepthChartViewMode != null)
            _btnDepthChartViewMode.Pressed += async () => await SetRosterViewMode(true);
        if (_btnAutoFillDepthChart != null)
            _btnAutoFillDepthChart.Pressed += async () => await AutoFillDepthChart();
        if (_btnDepthChartMoveUp != null)
            _btnDepthChartMoveUp.Pressed += async () => await UpdateDepthChart("move_up");
        if (_btnDepthChartMoveDown != null)
            _btnDepthChartMoveDown.Pressed += async () => await UpdateDepthChart("move_down");
        if (_btnDepthChartSetStarter != null)
            _btnDepthChartSetStarter.Pressed += async () => await UpdateDepthChart("set_starter");
        if (_newGameTeamPicker != null)
        {
            _newGameTeamPicker.Confirmed += async () => await OnNewGameTeamPickerConfirmed();
            _newGameTeamPicker.CloseRequested += async () => await OnNewGameTeamPickerCanceled();
        }
        if (_btnStartupContinue != null)
            _btnStartupContinue.Pressed += async () => await ContinueNativeStartup();
        if (_btnStartupLoadGame != null)
            _btnStartupLoadGame.Pressed += async () => await LoadNativeGame();
        if (_btnStartupNewGame != null)
            _btnStartupNewGame.Pressed += async () => await NewGame();
        if (_btnStartupExit != null)
            _btnStartupExit.Pressed += OnStartupExitPressed;
        if (_newGameConfirmDialog != null)
            _newGameConfirmDialog.Confirmed += async () => await ConfirmNativeNewGame();

        // When user clicks a team, load roster
        if (_teamList != null)
        {
            _teamList.ItemSelected += async (long index) =>
            {
                if (_suppressTeamListEvents)
                    return;
                await OnTeamSelected((int)index);
            };
        }
        if (_popupColumns != null)
            _popupColumns.IdPressed += OnColumnMenuIdPressed;
        if (_rosterTree != null)
        {
            _rosterTree.ColumnTitleClicked += OnRosterColumnTitleClicked;
            _rosterTree.ItemSelected += () => OnRosterItemSelected(_rosterTree.GetSelected());
        }
        if (_depthChartTree != null)
            _depthChartTree.ItemSelected += () => OnDepthChartItemSelected(_depthChartTree.GetSelected());
        if (_rosterSplit != null)
            _rosterSplit.Dragged += OnRosterSplitDragged;
        if (_rosterSearch != null)
            _rosterSearch.TextChanged += OnRosterSearchTextChanged;
        if (_posFilter != null)
            _posFilter.ItemSelected += OnPosFilterItemSelected;
        if (_btnClearFilters != null)
            _btnClearFilters.Pressed += OnClearFiltersPressed;
        if (_overviewActionButton != null)
            _overviewActionButton.Pressed += async () => await OnInboxPrimaryActionPressed();
        if (_btnGameDayCancel != null)
            _btnGameDayCancel.Pressed += CloseGameDayPopup;
        if (_btnGameDayWatch != null)
            _btnGameDayWatch.Pressed += OnWatchGamePressed;
        if (_btnGameDaySim != null)
            _btnGameDaySim.Pressed += async () => await OnGameDaySimPressed();
        if (_btnPostGameBoxScore != null)
            _btnPostGameBoxScore.Pressed += OnPostGameBoxScorePressed;
        if (_btnPostGameClose != null)
            _btnPostGameClose.Pressed += async () => await ClosePostGameRecapPopupAsync();
        if (_btnBoxScorePopupClose != null)
            _btnBoxScorePopupClose.Pressed += OnBoxScorePopupClosePressed;
        if (_resultsWeekSelect != null)
            _resultsWeekSelect.ItemSelected += OnResultsWeekSelected;
        if (_resultsList != null)
            _resultsList.ItemSelected += async (long index) => await OnResultSelected(index);
        if (_scheduleList != null)
            _scheduleList.ItemSelected += OnScheduleItemSelected;
        if (_historySeasonList != null)
            _historySeasonList.ItemSelected += OnHistorySeasonSelected;
        if (_btnScheduleAction != null)
            _btnScheduleAction.Pressed += async () => await OnScheduleActionPressed();
        if (_btnBoxScoreBack != null)
            _btnBoxScoreBack.Pressed += OnBoxScoreBack;
        if (_btnHubRefresh != null)
            _btnHubRefresh.Pressed += async () => await RefreshLeagueHub();

        UpdateRosterViewModeUi();
        if (_btnOverviewTab != null)
            _btnOverviewTab.Pressed += async () => await SelectMainTab(0);
        if (_btnLeagueTab != null)
            _btnLeagueTab.Pressed += async () => await SelectMainTab(1);
        if (_btnRosterTab != null)
            _btnRosterTab.Pressed += async () => await SelectMainTab(ROSTER_TAB_INDEX);

        if (_btnColumns != null && string.IsNullOrWhiteSpace(_btnColumns.Text))
            _btnColumns.Text = "Columns";

        SetupSimUntilOptions();
        SetupPosFilterItems();
        LoadRosterFilters();
        ApplyDebugPanelVisibility(DebugToolsVisibleByDefault);
        UpdateDepthChartSelectionLabel();
        UpdateDepthChartEditButtons();
        SetupRosterColumns();
        SetupStandingsTree();
        SetupInjuriesTree();
        SetupBoxScoreTrees();
        SetupScheduleTree();
        SetupHistoryView();
        ConfigureBoxScoreTree(_boxScorePopupQuarterTree);
        ConfigureBoxScoreTree(_boxScorePopupTeamStatsTree);
        SetupResultsWeekOptions(new List<string>(), "");
        SetReportPlaceholder("Select a player to view the scout report.");
        LoadRosterSplitOffset();
        ClearInboxDetail();
        UpdateNativeSourceStatus();
        UpdateNativeSaveLoadButtons();
        UpdateContinueButtonAvailability();
        ShowStandingsMessage("Standings: loading...");
        ShowResultsMessage("Results: loading...");
        ShowScheduleMessage("Select a team to view schedule.");
        ShowInjuriesMessage("Select a team to view injuries.");
        ShowHistoryMessage("No completed seasons yet.");
        CloseGameDayPopup();
        HideBoxScorePopup();
        SetMainTab(0);

        await EnsureNativeGameCoreAndRefresh();
    }

    private async Task EnsureNativeGameCoreAndRefresh()
    {
        var loadedNativeState = await EnsureNativeStartupState();
        if (!loadedNativeState)
            return;

        await RefreshAll();
    }

    private void SetServerError(string message)
    {
        if (_serverStatus == null || string.IsNullOrWhiteSpace(message))
            return;

        var clean = CleanStatusMessage(message, "Unable to connect to server.");
        _serverStatus.Text = $"Status: {clean}";
    }

    private void SetStateDumpText(string text, bool append = false)
    {
        if (_stateDump == null)
            return;

        if (append)
            _stateDump.Text += text;
        else
            _stateDump.Text = text;
    }

    private void SetDebugOutputStatus(string text)
    {
        if (_debugOutputLabel == null)
            return;

        _debugOutputLabel.Text = string.IsNullOrWhiteSpace(text) ? "Debug Output" : text;
    }

    private static bool IsNativeRuntimeSource() => true;

    private void UpdateNativeSourceStatus()
    {
        SetDebugOutputStatus("Runtime: C# GameCore");
        UpdateNativeSaveLoadButtons();
    }

    private void UpdateNativeSaveLoadButtons()
    {
        var nativeEnabled = IsNativeRuntimeSource();
        var hasActiveNativeLeague = _nativeGameCoreContext?.ActiveLeague != null;
        if (_btnSaveNativeGame != null)
            _btnSaveNativeGame.Disabled = !nativeEnabled;
        if (_btnLoadNativeGame != null)
            _btnLoadNativeGame.Disabled = !nativeEnabled;
        if (_btnSaveGame != null)
            _btnSaveGame.Disabled = nativeEnabled ? !hasActiveNativeLeague : false;
    }

    private async Task<bool> EnsureNativeStartupState()
    {
        if (!IsNativeRuntimeSource())
        {
            HideStartupPanel();
            _nativeStartupState = NativeStartupState.Unknown;
            return true;
        }

        if (_nativeGameCoreContext?.ActiveLeague != null)
        {
            _nativeStartupState = NativeStartupState.Ready;
            HideStartupPanel();
            UpdateNativeSaveLoadButtons();
            return true;
        }

        var loadResult = GetNativeGameCoreSaveService().Load();
        if (loadResult.Ok && loadResult.League != null)
        {
            EnsureNativeGameCoreServices();
            _nativeGameCoreContext.ActiveLeague = loadResult.League;
            _nativeStartupState = NativeStartupState.Ready;
            HideStartupPanel();
            _pendingNativeStatusMessage = "Loaded native save.";
            UpdateNativeSaveLoadButtons();
            return true;
        }

        _nativeStartupState = loadResult.SaveMissing
            ? NativeStartupState.MissingAutosave
            : NativeStartupState.CorruptAutosave;
        SetPrimaryStatus(loadResult.SaveMissing ? "No native save found." : "Unable to load native save.");
        if (!string.IsNullOrWhiteSpace(loadResult.Message))
            SetStateDumpText(loadResult.Message);
        ShowStartupPanel(loadResult);
        UpdateNativeSaveLoadButtons();
        await Task.CompletedTask;
        return false;
    }

    private void ShowStartupPanel(GameCoreLoadResult autosaveResult)
    {
        if (_startupPanel != null)
            _startupPanel.Visible = true;

        var saveService = GetNativeGameCoreSaveService();
        var hasAutosave = saveService.SaveExists();
        var hasNamedSave = saveService.SaveExists(GameCoreSaveService.NamedSaveFileName);
        var hasAnySave = hasAutosave || hasNamedSave;
        var corruptAutosave = autosaveResult != null && !autosaveResult.Ok && !autosaveResult.SaveMissing;

        if (_lblStartupWarning != null)
        {
            _lblStartupWarning.Visible = corruptAutosave;
            _lblStartupWarning.Text = corruptAutosave
                ? "Unable to load native save."
                : "";
        }

        if (_lblStartupStatus != null)
        {
            if (corruptAutosave)
                _lblStartupStatus.Text = "The autosave could not be loaded. Start a new game or try loading an existing native save.";
            else if (!hasAnySave)
                _lblStartupStatus.Text = "No native save found. Start a new game to begin.";
            else
                _lblStartupStatus.Text = "Start a new game or load an existing native save.";
        }

        if (_btnStartupContinue != null)
        {
            _btnStartupContinue.Disabled = !hasAutosave;
            _btnStartupContinue.Text = corruptAutosave ? "Try Load Autosave Again" : "Continue / Load Autosave";
        }

        if (_btnStartupLoadGame != null)
        {
            _btnStartupLoadGame.Disabled = !hasAnySave;
            _btnStartupLoadGame.Text = hasAnySave ? "Load Game" : "Load Game (No Save Found)";
        }
    }

    private void HideStartupPanel()
    {
        if (_startupPanel != null)
            _startupPanel.Visible = false;
    }

    private void OnStartupExitPressed()
    {
        GetTree().Quit();
    }

    private void SetPrimaryStatus(string message)
    {
        if (_continueStatus == null)
            return;

        _continueStatus.Text = string.IsNullOrWhiteSpace(message)
            ? "Status: Ready"
            : $"Status: {message}";
    }

    private void SetContinueButtonBusy(bool isBusy)
    {
        if (_btnContinue == null)
            return;

        if (isBusy)
        {
            _btnContinue.Disabled = true;
            _btnContinue.Text = "Simulating...";
            return;
        }

        UpdateContinueButtonAvailability();
    }

    private void UpdateContinueButtonAvailability()
    {
        if (_btnContinue == null)
            return;

        _btnContinue.Text = "Continue";
        _btnContinue.TooltipText = "";
        _btnContinue.Disabled = false;
    }

    private void OnDebugToggleToggled(bool toggledOn)
    {
        ApplyDebugPanelVisibility(toggledOn);
    }

    private void ApplyDebugPanelVisibility(bool visible)
    {
        if (_debugPanel != null)
        {
            _debugPanel.Visible = visible;
            if (_debugPanel.GetParent() is Container container)
                container.QueueSort();
        }
        if (_btnToggleDebug != null)
        {
            _btnToggleDebug.SetPressedNoSignal(visible);
            _btnToggleDebug.Text = visible ? "Hide Debug Tools" : "Show Debug Tools";
        }
    }

    private async Task RunGameCoreSmokeTestAsync()
    {
        if (_btnRunGameCoreSmokeTest == null)
            return;

        _btnRunGameCoreSmokeTest.Disabled = true;
        SetDebugOutputStatus("Running C# GameCore smoke test...");
        SetStateDumpText("Running C# GameCore smoke test...");

        try
        {
            var result = await Task.Run(() => GameCoreSmokeTest.Run(GetTeamSeedPath()));
            var statusMessage = result.Ok
                ? "C# GameCore smoke test passed."
                : $"C# GameCore smoke test failed: {InlineMessage(result.Message)}";

            SetDebugOutputStatus(statusMessage);
            SetStateDumpText(BuildSmokeTestOutput(result, statusMessage));
        }
        catch (Exception ex)
        {
            var message = $"C# GameCore smoke test failed: {InlineMessage(ex.Message)}";
            SetDebugOutputStatus(message);
            SetStateDumpText(message);
        }
        finally
        {
            _btnRunGameCoreSmokeTest.Disabled = false;
        }
    }

    private static string BuildSmokeTestOutput(GameCoreSmokeTestResult result, string statusMessage)
    {
        if (result == null)
            return statusMessage;

        var lines = new List<string> { statusMessage };
        if (result.Steps != null && result.Steps.Count > 0)
            lines.AddRange(result.Steps);

        return string.Join("\n", lines);
    }

    private static string CleanStatusMessage(string message, string fallback)
    {
        var clean = InlineMessage(message);
        if (string.IsNullOrWhiteSpace(clean))
            return fallback;

        if (clean.StartsWith("/", StringComparison.Ordinal))
        {
            var separatorIndex = clean.IndexOf(": ", StringComparison.Ordinal);
            if (separatorIndex >= 0 && separatorIndex + 2 < clean.Length)
                clean = clean.Substring(separatorIndex + 2);
        }

        return clean;
    }

    private static string BuildApiUrl(string path) => path;

    private async Task<(int status, string body)> GetWithTimeoutAsync(string path, int timeoutMs)
    {
        await Task.CompletedTask;
        return (0, "The retired Python backend is unavailable in the C# runtime.");
    }

    private async Task<(int status, string body)> PostWithTimeoutAsync(string path, string json, int timeoutMs)
    {
        await Task.CompletedTask;
        return (0, "The retired Python backend is unavailable in the C# runtime.");
    }

    private static string InlineMessage(string message, int maxLength = 240)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "";
        var normalized = message.Replace("\r", " ").Replace("\n", " ").Trim();
        if (normalized.Length <= maxLength)
            return normalized;
        return normalized.Substring(0, maxLength) + "...";
    }

    private static string GetBodyHead(string body, int maxLength = 400)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "";

        var normalized = body.Trim();
        if (normalized.Length > maxLength)
            normalized = normalized.Substring(0, maxLength);

        return normalized.Replace("\r", "\\r").Replace("\n", "\\n");
    }

    private string SummarizeRequestError(string path, int status, string body)
    {
        var url = BuildApiUrl(path);
        if (!string.IsNullOrWhiteSpace(body))
        {
            var normalized = InlineMessage(body);
            if (!string.IsNullOrWhiteSpace(url) &&
                normalized.Contains(url, StringComparison.OrdinalIgnoreCase))
                return normalized;

            return string.IsNullOrWhiteSpace(url) ? normalized : $"{url}: {normalized}";
        }

        if (status > 0)
            return string.IsNullOrWhiteSpace(url) ? $"HTTP {status}" : $"{url}: HTTP {status}";

        return string.IsNullOrWhiteSpace(url) ? "Request failed" : $"{url}: Request failed";
    }

    private async Task RefreshAll()
    {
        await RefreshHealth();
        var hasDashboardState = await RefreshDashboardState();
        if (!hasDashboardState)
            return;
        await RefreshStateSummary();
        await RefreshInbox();
        await RefreshLeagueHub();
        if (IsRosterTabActive())
            await RefreshRosterTab();
        if (!string.IsNullOrWhiteSpace(_pendingNativeStatusMessage))
        {
            SetPrimaryStatus(_pendingNativeStatusMessage);
            _pendingNativeStatusMessage = "";
        }
        else
        {
            SetPrimaryStatus("Dashboard refreshed.");
        }

        UpdateNativeSaveLoadButtons();
    }

    private async Task<bool> RefreshDashboardState()
    {
        if (_calendarTitle != null)
            _calendarTitle.Text = "Season";
        if (_calendarText != null)
            _calendarText.Text = "State: loading...";
        if (_lblGameStatus != null)
            _lblGameStatus.Text = "Schedule: loading...";
        if (_lblGameNext != null)
            _lblGameNext.Text = "Next: loading...";

        if (IsNativeRuntimeSource())
            return RefreshNativeDashboardState();

        var (status, body) = await GetWithTimeoutAsync("/dashboard_state", REQUEST_TIMEOUT_MS);
        if (status < 200 || status >= 300)
        {
            var summary = SummarizeRequestError("/dashboard_state", status, body);
            ApplyDashboardUnavailableState(summary);
            SetStateDumpText(body);
            SetServerError(summary);
            return false;
        }

        return ApplyDashboardStatePayload(body);
    }

    private bool RefreshNativeDashboardState()
    {
        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeDashboardService.GetDashboardState();
            if (response == null || !response.Ok || response.Dashboard == null)
            {
                var error = response?.Error;
                ApplyDashboardUnavailableState(string.IsNullOrWhiteSpace(error)
                    ? "Native dashboard is unavailable."
                    : error);
                SetStateDumpText(string.IsNullOrWhiteSpace(error)
                    ? "Native dashboard is unavailable."
                    : $"Native dashboard unavailable: {error}");
                return false;
            }

            ApplyDashboardState(BuildDashboardDictionary(response.Dashboard));
            SetStateDumpText("Native dashboard refreshed.");
            return true;
        }
        catch (Exception ex)
        {
            var error = $"Native dashboard failed: {InlineMessage(ex.Message)}";
            ApplyDashboardUnavailableState(error);
            SetStateDumpText(error);
            return false;
        }
    }

    private async Task RefreshHealth()
    {
        if (_serverStatus != null)
            _serverStatus.Text = "Runtime: C# GameCore";
        await Task.CompletedTask;
    }

    // UPDATED: now uses /state_summary (small payload)
    private async Task RefreshStateSummary()
    {
        if (_calendarTitle != null)
            _calendarTitle.Text = "Season";
        if (_calendarText != null)
            _calendarText.Text = "State: loading...";
        if (_lblGameStatus != null)
            _lblGameStatus.Text = "Schedule: loading...";
        if (_lblGameNext != null)
            _lblGameNext.Text = "Next: loading...";
        if (IsNativeRuntimeSource())
        {
            var summary = BuildNativeStateSummaryDictionary();
            ApplyStateSummary(summary);
            RenderFrontOfficeLabel();
            await RefreshDashboardState();
            return;
        }

        var (status, body) = await GetWithTimeoutAsync("/state_summary", REQUEST_TIMEOUT_MS);

        if (status < 200 || status >= 300)
        {
            var summary = SummarizeRequestError("/state_summary", status, body);
            if (_calendarTitle != null)
                _calendarTitle.Text = "Season";
            if (_calendarText != null)
                _calendarText.Text = $"State: ERROR - {summary}";
            if (_lblGameStatus != null)
                _lblGameStatus.Text = "Schedule unavailable";
            if (_lblGameNext != null)
                _lblGameNext.Text = "Next: unavailable";
            SetStateDumpText(body);
            SetServerError(summary);
            return;
        }

        var ok = ApplyStateSummaryPayload(body);
        if (ok)
        {
            await RefreshDashboardState();
            await RefreshFrontOfficeContext();
        }
    }

    private bool ApplyStateSummaryPayload(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            if (_calendarTitle != null)
                _calendarTitle.Text = "Season";
            if (_calendarText != null)
                _calendarText.Text = "Calendar: (unparsed)";
            if (_lblGameStatus != null)
                _lblGameStatus.Text = "Schedule unavailable";
            if (_lblGameNext != null)
                _lblGameNext.Text = "Next: unavailable";
            return false;
        }

        SetStateDumpText(body.Length > 8000 ? body.Substring(0, 8000) + "\n\n...(truncated)" : body);

        var parsed = Json.ParseString(body);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            if (_calendarTitle != null)
                _calendarTitle.Text = "Season";
            if (_calendarText != null)
                _calendarText.Text = "Calendar: (unparsed)";
            if (_lblGameStatus != null)
                _lblGameStatus.Text = "Schedule unavailable";
            if (_lblGameNext != null)
                _lblGameNext.Text = "Next: unavailable";
            return false;
        }

        var dict = parsed.AsGodotDictionary();
        ApplyStateSummary(dict);
        return true;
    }

    private bool ApplyDashboardStatePayload(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            ApplyDashboardUnavailableState("No active league loaded.");
            return false;
        }

        var parsed = Json.ParseString(body);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            ApplyDashboardUnavailableState("No active league loaded.");
            return false;
        }

        var payload = parsed.AsGodotDictionary();
        var ok = GetBoolValue(GetFirstNonNil(payload, "ok", "success"), false);
        if (!ok)
        {
            var error = FmtString(GetFirstNonNil(payload, "error", "message", "detail"), "No active league loaded.");
            ApplyDashboardUnavailableState(string.IsNullOrWhiteSpace(error) ? "No active league loaded." : error);
            return false;
        }

        var dashboard = TryExtractObject(payload, "dashboard");
        if (dashboard == null)
        {
            ApplyDashboardUnavailableState("No active league loaded.");
            return false;
        }

        ApplyDashboardState(dashboard);
        return true;
    }

    private void ApplyDashboardState(Godot.Collections.Dictionary dashboard)
    {
        var team = TryExtractObject(dashboard, "team");
        var calendar = TryExtractObject(dashboard, "calendar");
        var nextGame = TryExtractObject(dashboard, "next_game", "nextGame");
        var teamStatus = TryExtractObject(dashboard, "team_status", "teamStatus");
        var actionItems = TryExtractArray(dashboard, "action_items", "actionItems");
        var recentResults = TryExtractArray(dashboard, "recent_results", "recentResults");
        var playoffBracket = TryExtractObject(dashboard, "playoff_bracket", "playoffBracket");
        var playoffSummary = FmtString(GetFirstNonNil(dashboard, "playoff_summary_text", "playoffSummaryText"), "");
        var seasonCompletionSummary = TryExtractObject(dashboard, "season_completion_summary", "seasonCompletionSummary");

        _dashboardTeam = team ?? new Godot.Collections.Dictionary();
        _dashboardCalendar = calendar ?? new Godot.Collections.Dictionary();
        _dashboardNextGame = nextGame ?? new Godot.Collections.Dictionary();
        _dashboardRecentResults = recentResults ?? new Godot.Collections.Array();
        _dashboardPlayoffBracket = playoffBracket ?? new Godot.Collections.Dictionary();

        var year = calendar != null ? FmtInt(GetFirstNonNil(calendar, "year"), "?") : "?";
        var weekNumber = calendar != null ? GetIntValue(GetFirstNonNil(calendar, "week"), 0) : 0;
        var week = weekNumber > 0 ? weekNumber.ToString(CultureInfo.InvariantCulture) : "?";
        var phase = calendar != null ? FmtString(GetFirstNonNil(calendar, "phase"), "") : "";
        var weekLabel = calendar != null ? FmtString(GetFirstNonNil(calendar, "week_label", "weekLabel"), "") : "";
        var currentDate = calendar != null ? FmtString(GetFirstNonNil(calendar, "current_date", "currentDate"), "") : "";
        var dayOfWeek = calendar != null ? FmtString(GetFirstNonNil(calendar, "day_of_week", "dayOfWeek"), "") : "";
        var dateText = FormatCalendarDate(dayOfWeek, currentDate);
        var headline = !string.IsNullOrWhiteSpace(weekLabel)
            ? weekLabel
            : weekNumber > 0
                ? string.IsNullOrWhiteSpace(phase)
                    ? $"Week {week}"
                    : $"Week {week} - {phase}"
                : string.IsNullOrWhiteSpace(phase)
                    ? "Season in progress"
                    : phase;
        var detailLine = !string.IsNullOrWhiteSpace(dateText)
            ? $"{headline} - {dateText}"
            : headline;

        if (_calendarTitle != null)
            _calendarTitle.Text = $"{year} Season";
        if (_calendarText != null)
            _calendarText.Text = detailLine;

        var opponentAbbr = nextGame != null ? FmtString(GetFirstNonNil(nextGame, "opponent_abbreviation"), "") : "";
        var opponentName = nextGame != null ? FmtString(GetFirstNonNil(nextGame, "opponent"), "") : "";
        var homeAway = nextGame != null ? FmtString(GetFirstNonNil(nextGame, "home_away", "homeAway"), "") : "";
        var gameWeek = nextGame != null ? FmtString(GetFirstNonNil(nextGame, "week"), "") : "";
        var gameType = nextGame != null ? FmtString(GetFirstNonNil(nextGame, "game_type", "gameType"), "") : "";
        var headerOpponentLabel = nextGame != null ? FmtString(GetFirstNonNil(nextGame, "header_opponent_label", "headerOpponentLabel"), "") : "";
        var headerNextLabel = nextGame != null ? FmtString(GetFirstNonNil(nextGame, "header_next_label", "headerNextLabel"), "") : "";
        var nextOpponent = !string.IsNullOrWhiteSpace(opponentAbbr) ? opponentAbbr : opponentName;

        if (_lblGameStatus != null)
        {
            if (!string.IsNullOrWhiteSpace(headerOpponentLabel))
            {
                _lblGameStatus.Text = headerOpponentLabel;
            }
            else if (string.IsNullOrWhiteSpace(nextOpponent))
            {
                _lblGameStatus.Text = "No upcoming game";
            }
            else
            {
                _lblGameStatus.Text = homeAway.Equals("home", StringComparison.OrdinalIgnoreCase)
                    ? $"Next opponent: {nextOpponent} (home)"
                    : $"Next opponent: {nextOpponent} (away)";
            }
        }

        if (_lblGameNext != null)
        {
            if (!string.IsNullOrWhiteSpace(headerNextLabel))
            {
                _lblGameNext.Text = headerNextLabel;
            }
            else if (string.IsNullOrWhiteSpace(nextOpponent))
            {
                _lblGameNext.Text = "Next: unavailable";
            }
            else
            {
                var typeText = string.IsNullOrWhiteSpace(gameType) ? "" : $"{HumanizeStatus(gameType)} ";
                var weekText = string.IsNullOrWhiteSpace(gameWeek) ? "" : $"Week {gameWeek}";
                var details = $"{typeText}{weekText}".Trim();
                _lblGameNext.Text = string.IsNullOrWhiteSpace(details)
                    ? $"Next: {nextOpponent}"
                    : $"Next: {details} vs {nextOpponent}";
            }
        }

        var teamLabel = team != null ? FmtString(GetFirstNonNil(team, "abbreviation"), "") : "";
        var teamName = team != null ? FmtString(GetFirstNonNil(team, "name"), "") : "";
        var record = team != null ? FmtString(GetFirstNonNil(team, "record"), "0-0") : "0-0";
        _dashboardTeamName = teamName;
        _dashboardTeamRecord = record;
        if (!string.IsNullOrWhiteSpace(teamLabel))
            _gmTeamLabel = teamLabel;
        else if (!string.IsNullOrWhiteSpace(teamName))
            _gmTeamLabel = teamName;
        _dashboardRosterSize = teamStatus != null
            ? GetIntValue(GetFirstNonNil(teamStatus, "roster_size", "rosterSize"), 0)
            : 0;
        _dashboardInjuryCount = teamStatus != null
            ? GetIntValue(GetFirstNonNil(teamStatus, "injuries"), 0)
            : 0;
        _dashboardCapRoom = FormatDashboardCapRoom(teamStatus);
        RenderFrontOfficeLabel();
        RenderOverviewSnapshotCards();
        RenderPlayoffPicture(ComposeOverviewPlayoffSummary(playoffSummary, seasonCompletionSummary), _dashboardPlayoffBracket);
        _inboxMessages = ConvertDashboardActionItems(actionItems);
        UpdateInboxList();
        UpdateContinueButtonAvailability();
    }

    private void ApplyDashboardUnavailableState(string message)
    {
        var fallback = string.IsNullOrWhiteSpace(message) ? "No active league loaded." : message;
        if (_calendarTitle != null)
            _calendarTitle.Text = "Season";
        if (_calendarText != null)
            _calendarText.Text = fallback;
        if (_lblGameStatus != null)
            _lblGameStatus.Text = fallback;
        if (_lblGameNext != null)
            _lblGameNext.Text = "Next: unavailable";
        _dashboardTeamName = "";
        _dashboardTeamRecord = "0-0";
        _dashboardRosterSize = null;
        _dashboardInjuryCount = null;
        _dashboardCapRoom = "N/A";
        _dashboardTeam = new Godot.Collections.Dictionary();
        _dashboardCalendar = new Godot.Collections.Dictionary();
        _dashboardNextGame = new Godot.Collections.Dictionary();
        _dashboardRecentResults = new Godot.Collections.Array();
        _dashboardPlayoffBracket = new Godot.Collections.Dictionary();
        if (_teamList != null)
            _teamList.Clear();
        _teams.Clear();
        _teamDisplayById.Clear();
        _teamShortById.Clear();
        RenderFrontOfficeLabel();
        RenderOverviewSnapshotCards();
        RenderPlayoffPicture("Playoff bracket not generated yet.", null);
        _inboxMessages = new Godot.Collections.Array();
        UpdateInboxList();
        UpdateContinueButtonAvailability();
    }

    private Godot.Collections.Dictionary BuildDashboardDictionary(DashboardDto dashboard)
    {
        dashboard ??= new DashboardDto();

        var result = new Godot.Collections.Dictionary
        {
            {
                "team", new Godot.Collections.Dictionary
                {
                    { "name", dashboard.Team?.Name ?? "" },
                    { "abbreviation", dashboard.Team?.Abbreviation ?? "" },
                    { "record", dashboard.Team?.Record ?? "0-0" },
                }
            },
            {
                "calendar", new Godot.Collections.Dictionary
                {
                    { "year", dashboard.Calendar?.Year ?? 0 },
                    { "week", dashboard.Calendar?.Week ?? 0 },
                    { "absolute_week", dashboard.Calendar?.AbsoluteWeek ?? 0 },
                    { "phase_week", dashboard.Calendar?.PhaseWeek ?? 0 },
                    { "phase", dashboard.Calendar?.Phase ?? "" },
                    { "current_date", dashboard.Calendar?.CurrentDate ?? "" },
                    { "day_of_week", dashboard.Calendar?.DayOfWeek ?? "" },
                    { "week_label", dashboard.Calendar?.WeekLabel ?? "" },
                }
            },
            {
                "next_game", new Godot.Collections.Dictionary
                {
                    { "opponent", dashboard.NextGame?.Opponent ?? "" },
                    { "opponent_abbreviation", dashboard.NextGame?.OpponentAbbreviation ?? "" },
                    { "home_away", dashboard.NextGame?.HomeAway ?? "" },
                    { "week", dashboard.NextGame?.Week ?? 0 },
                    { "absolute_week", dashboard.NextGame?.AbsoluteWeek ?? 0 },
                    { "phase_week", dashboard.NextGame?.PhaseWeek ?? 0 },
                    { "phase", dashboard.NextGame?.Phase ?? "" },
                    { "game_type", dashboard.NextGame?.GameType ?? "" },
                    { "game_id", dashboard.NextGame?.GameId ?? "" },
                    { "week_label", dashboard.NextGame?.WeekLabel ?? "" },
                    { "header_opponent_label", dashboard.NextGame?.HeaderOpponentLabel ?? "" },
                    { "header_next_label", dashboard.NextGame?.HeaderNextLabel ?? "" },
                }
            },
            {
                "team_status", new Godot.Collections.Dictionary
                {
                    { "roster_size", dashboard.TeamStatus?.RosterSize ?? 0 },
                    { "injuries", dashboard.TeamStatus?.Injuries ?? 0 },
                    { "cap_room", dashboard.TeamStatus?.CapRoom ?? "" },
                }
            },
            { "playoff_bracket", BuildPlayoffBracketDictionary(dashboard.PlayoffBracket) },
            { "playoff_summary_text", dashboard.PlayoffSummaryText ?? "" },
            { "season_completion_summary", BuildSeasonCompletionSummaryDictionary(dashboard.SeasonCompletionSummary) },
            { "action_items", BuildDashboardActionItemsArray(dashboard.ActionItems) },
            { "recent_results", BuildDashboardRecentResultsArray(dashboard.RecentResults) },
        };

        return result;
    }

    private static Godot.Collections.Dictionary BuildSeasonCompletionSummaryDictionary(SeasonCompletionSummaryDto summary)
    {
        summary ??= new SeasonCompletionSummaryDto();
        return new Godot.Collections.Dictionary
        {
            { "is_available", summary.IsAvailable },
            { "completed_phase_label", summary.CompletedPhaseLabel ?? "" },
            { "champion_team_name", summary.ChampionTeamName ?? "" },
            { "runner_up_team_name", summary.RunnerUpTeamName ?? "" },
            { "championship_result_line", summary.ChampionshipResultLine ?? "" },
        };
    }

    private static string ComposeOverviewPlayoffSummary(string playoffSummary, Godot.Collections.Dictionary seasonCompletionSummary)
    {
        var hasCompletionSummary = seasonCompletionSummary != null
            && GetBoolValue(GetFirstNonNil(seasonCompletionSummary, "is_available"), false);
        if (!hasCompletionSummary)
            return playoffSummary;

        var completedPhaseLabel = FmtString(GetFirstNonNil(seasonCompletionSummary, "completed_phase_label", "completedPhaseLabel"), "Season Complete");
        var championTeamName = FmtString(GetFirstNonNil(seasonCompletionSummary, "champion_team_name", "championTeamName"), "");
        var runnerUpTeamName = FmtString(GetFirstNonNil(seasonCompletionSummary, "runner_up_team_name", "runnerUpTeamName"), "");
        var championshipResultLine = FmtString(GetFirstNonNil(seasonCompletionSummary, "championship_result_line", "championshipResultLine"), "");

        var lines = new List<string> { completedPhaseLabel };
        if (!string.IsNullOrWhiteSpace(championTeamName))
            lines.Add($"League Champion: {championTeamName}");
        if (!string.IsNullOrWhiteSpace(runnerUpTeamName))
            lines.Add($"Runner-Up: {runnerUpTeamName}");
        if (!string.IsNullOrWhiteSpace(championshipResultLine))
            lines.Add($"League Championship: {championshipResultLine}");

        var summary = string.Join("\n", lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        return string.IsNullOrWhiteSpace(playoffSummary)
            ? summary
            : $"{summary}\n\n{playoffSummary}";
    }

    private Godot.Collections.Array BuildDashboardActionItemsArray(System.Collections.Generic.IEnumerable<ActionItemDto> items)
    {
        var array = new Godot.Collections.Array();
        if (items == null)
            return array;

        foreach (var item in items)
        {
            array.Add(new Godot.Collections.Dictionary
            {
                { "type", item?.Type ?? "" },
                { "title", item?.Title ?? "Action Required" },
                { "description", item?.Description ?? "" },
                { "primary_action", item?.PrimaryAction ?? "" },
            });
        }

        return array;
    }

    private Godot.Collections.Dictionary BuildPlayoffBracketDictionary(PlayoffBracketDto bracket)
    {
        bracket ??= new PlayoffBracketDto();
        var result = new Godot.Collections.Dictionary
        {
            { "season_year", bracket.SeasonYear },
            { "generated_from_absolute_week", bracket.GeneratedFromAbsoluteWeek },
            { "generated_at_phase_label", bracket.GeneratedAtPhaseLabel ?? "" },
            { "conference_brackets", new Godot.Collections.Array() },
            { "league_championship_round", BuildPlayoffRoundDictionary(bracket.LeagueChampionshipRound) },
        };

        var conferenceBrackets = (Godot.Collections.Array)result["conference_brackets"];
        foreach (var conferenceBracket in bracket.ConferenceBrackets ?? new System.Collections.Generic.List<PlayoffConferenceBracketDto>())
        {
            var conferenceDict = new Godot.Collections.Dictionary
            {
                { "conference", conferenceBracket?.Conference ?? "" },
                { "seeds", new Godot.Collections.Array() },
                { "rounds", new Godot.Collections.Array() },
            };

            var seeds = (Godot.Collections.Array)conferenceDict["seeds"];
            foreach (var seed in conferenceBracket?.Seeds ?? new System.Collections.Generic.List<PlayoffSeedDto>())
            {
                seeds.Add(new Godot.Collections.Dictionary
                {
                    { "seed", seed?.Seed ?? 0 },
                    { "team_id", seed?.TeamId ?? "" },
                    { "team_name", seed?.TeamName ?? "" },
                    { "conference", seed?.Conference ?? "" },
                    { "division", seed?.Division ?? "" },
                    { "is_division_winner", seed?.IsDivisionWinner ?? false },
                    { "wins", seed?.Wins ?? 0 },
                    { "losses", seed?.Losses ?? 0 },
                    { "ties", seed?.Ties ?? 0 },
                    { "win_percentage", seed?.WinPercentage ?? 0.0 },
                    { "point_differential", seed?.PointDifferential ?? 0 },
                    { "points_for", seed?.PointsFor ?? 0 },
                });
            }

            var rounds = (Godot.Collections.Array)conferenceDict["rounds"];
            foreach (var round in conferenceBracket?.Rounds ?? new System.Collections.Generic.List<PlayoffRoundDto>())
                rounds.Add(BuildPlayoffRoundDictionary(round));

            conferenceBrackets.Add(conferenceDict);
        }

        return result;
    }

    private Godot.Collections.Dictionary BuildPlayoffRoundDictionary(PlayoffRoundDto round)
    {
        var roundDict = new Godot.Collections.Dictionary
        {
            { "round", round?.Round ?? "" },
            { "games", new Godot.Collections.Array() },
        };

        var games = (Godot.Collections.Array)roundDict["games"];
        foreach (var game in round?.Games ?? new System.Collections.Generic.List<PlayoffGameDto>())
        {
            games.Add(new Godot.Collections.Dictionary
            {
                { "round", game?.Round ?? "" },
                { "conference", game?.Conference ?? "" },
                { "home_seed", game?.HomeSeed ?? 0 },
                { "away_seed", game?.AwaySeed ?? 0 },
                { "home_team_id", game?.HomeTeamId ?? "" },
                { "away_team_id", game?.AwayTeamId ?? "" },
                { "home_team_name", game?.HomeTeamName ?? "" },
                { "away_team_name", game?.AwayTeamName ?? "" },
                { "status", game?.Status ?? "" },
                { "winner_team_id", game?.WinnerTeamId ?? "" },
            });
        }

        return roundDict;
    }

    private void RenderPlayoffPicture(string summaryText, Godot.Collections.Dictionary playoffBracket)
    {
        if (_overviewPlayoffHeader != null)
            _overviewPlayoffHeader.Text = "Playoff Picture";

        if (_overviewPlayoffSummary == null)
            return;

        var hasBracket = HasPlayoffBracket(playoffBracket);
        var hasProvidedSummary = !string.IsNullOrWhiteSpace(summaryText);
        var summary = hasProvidedSummary
            ? summaryText.Trim()
            : hasBracket
                ? BuildPlayoffSummaryFromDictionary(playoffBracket)
                : "Playoff bracket not generated yet.";

        GD.Print($"Overview playoff summary render: source={(hasProvidedSummary ? "dashboard" : hasBracket ? "fallback_bracket" : "no_bracket")}, length={summary.Length}");

        if (_overviewPlayoffPanel != null)
        {
            _overviewPlayoffPanel.Visible = hasBracket;
            _overviewPlayoffPanel.CustomMinimumSize = new Vector2(0, hasBracket ? 260 : 0);
        }

        _overviewPlayoffSummary.Clear();
        _overviewPlayoffSummary.Text = summary;
        _overviewPlayoffSummary.Visible = hasBracket;
        _overviewPlayoffSummary.FitContent = true;
        _overviewPlayoffSummary.CustomMinimumSize = new Vector2(0, hasBracket ? 188 : 0);
        _overviewPlayoffSummary.SizeFlagsVertical = Control.SizeFlags.Fill;
        _overviewPlayoffSummary.QueueRedraw();
        if (hasBracket)
            _overviewPlayoffSummary.ScrollToLine(0);
    }

    private void RenderOverviewSnapshotCards()
    {
        RenderTeamSummaryCard();
        RenderRecentResultsCard();
        RenderNextEventCard();
    }

    private void RenderTeamSummaryCard()
    {
        if (_rtlTeamSummary == null)
            return;

        var teamAbbr = FmtString(GetFirstNonNil(_dashboardTeam, "abbreviation"), "");
        var teamName = FmtString(GetFirstNonNil(_dashboardTeam, "name"), "");
        var record = FmtString(GetFirstNonNil(_dashboardTeam, "record"), _dashboardTeamRecord ?? "0-0");
        var displayName = !string.IsNullOrWhiteSpace(teamAbbr)
            ? string.IsNullOrWhiteSpace(teamName) ? teamAbbr : $"{teamAbbr} - {teamName}"
            : string.IsNullOrWhiteSpace(teamName) ? "No team selected" : teamName;
        var rosterText = _dashboardRosterSize.HasValue ? _dashboardRosterSize.Value.ToString(CultureInfo.InvariantCulture) : "N/A";
        var injuryText = _dashboardInjuryCount.HasValue ? _dashboardInjuryCount.Value.ToString(CultureInfo.InvariantCulture) : "N/A";

        _rtlTeamSummary.Text =
            $"{displayName}\n" +
            $"Record: {record}\n" +
            $"Cap Room: {_dashboardCapRoom}\n" +
            $"Roster: {rosterText}   Injuries: {injuryText}";
    }

    private void RenderRecentResultsCard()
    {
        if (_lblRecentResultsHeader != null)
            _lblRecentResultsHeader.Text = _dashboardRecentResults != null && _dashboardRecentResults.Count > 0
                ? "Recent Results"
                : "Next Game";

        if (_overviewRecentResults == null)
            return;

        var lines = new List<string>();
        if (_dashboardRecentResults != null)
        {
            for (var i = 0; i < Math.Min(_dashboardRecentResults.Count, 3); i++)
            {
                var resultVar = (Variant)_dashboardRecentResults[i];
                if (!TryGetDictionary(resultVar, out var result))
                    continue;

                var weekLabel = FmtString(GetFirstNonNil(result, "week_label", "weekLabel"), "");
                var summary = FmtString(GetFirstNonNil(result, "summary"), "");
                if (string.IsNullOrWhiteSpace(summary))
                    summary = FormatGameSummary(result, "");
                lines.Add(string.IsNullOrWhiteSpace(weekLabel) ? summary : $"{weekLabel}: {summary}");
            }
        }

        if (lines.Count == 0)
        {
            var nextLabel = FmtString(GetFirstNonNil(_dashboardNextGame, "header_next_label", "headerNextLabel"), "");
            var opponentLabel = FmtString(GetFirstNonNil(_dashboardNextGame, "header_opponent_label", "headerOpponentLabel"), "");
            var opponent = FmtString(GetFirstNonNil(_dashboardNextGame, "opponent_abbreviation", "opponentAbbreviation", "opponent"), "TBD");
            var homeAway = FmtString(GetFirstNonNil(_dashboardNextGame, "home_away", "homeAway"), "");

            if (!string.IsNullOrWhiteSpace(nextLabel))
                lines.Add(nextLabel);
            if (!string.IsNullOrWhiteSpace(opponentLabel))
                lines.Add(opponentLabel);
            else
                lines.Add(string.Equals(homeAway, "home", StringComparison.OrdinalIgnoreCase)
                    ? $"Home vs {opponent}"
                    : string.Equals(homeAway, "away", StringComparison.OrdinalIgnoreCase)
                        ? $"Away at {opponent}"
                        : $"Opponent: {opponent}");
        }

        _overviewRecentResults.Text = lines.Count == 0
            ? "No recent results available."
            : string.Join("\n\n", lines);
    }

    private void RenderNextEventCard()
    {
        if (_overviewNextEventSummary == null)
            return;

        var nextLabel = FmtString(GetFirstNonNil(_dashboardNextGame, "header_next_label", "headerNextLabel"), "");
        var opponentLabel = FmtString(GetFirstNonNil(_dashboardNextGame, "header_opponent_label", "headerOpponentLabel"), "");
        var weekLabel = FmtString(GetFirstNonNil(_dashboardNextGame, "week_label", "weekLabel"), "");
        var opponent = FmtString(GetFirstNonNil(_dashboardNextGame, "opponent_abbreviation", "opponentAbbreviation", "opponent"), "TBD");
        var homeAway = FmtString(GetFirstNonNil(_dashboardNextGame, "home_away", "homeAway"), "");

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(nextLabel))
            lines.Add(nextLabel);
        if (!string.IsNullOrWhiteSpace(weekLabel) && !string.Equals(weekLabel, nextLabel, StringComparison.OrdinalIgnoreCase))
            lines.Add(weekLabel);
        if (!string.IsNullOrWhiteSpace(opponentLabel))
        {
            lines.Add(opponentLabel);
        }
        else if (!string.IsNullOrWhiteSpace(opponent))
        {
            lines.Add(string.Equals(homeAway, "home", StringComparison.OrdinalIgnoreCase)
                ? $"Home vs {opponent}"
                : string.Equals(homeAway, "away", StringComparison.OrdinalIgnoreCase)
                    ? $"Away at {opponent}"
                    : $"Opponent: {opponent}");
        }

        _overviewNextEventSummary.Text = lines.Count == 0
            ? "No upcoming event available."
            : string.Join("\n", lines);
    }

    private static bool HasPlayoffBracket(Godot.Collections.Dictionary playoffBracket)
    {
        if (playoffBracket == null)
            return false;

        var conferenceBrackets = TryExtractArray(playoffBracket, "conference_brackets", "conferenceBrackets");
        return conferenceBrackets != null && conferenceBrackets.Count > 0;
    }

    private static string BuildPlayoffSummaryFromDictionary(Godot.Collections.Dictionary playoffBracket)
    {
        if (playoffBracket == null)
            return "Playoff bracket not generated yet.";

        var conferenceBrackets = TryExtractArray(playoffBracket, "conference_brackets", "conferenceBrackets");
        if (conferenceBrackets == null || conferenceBrackets.Count == 0)
            return "Playoff bracket not generated yet.";

        var lines = new List<string>();
        for (var conferenceIndex = 0; conferenceIndex < conferenceBrackets.Count; conferenceIndex++)
        {
            var conferenceVar = (Variant)conferenceBrackets[conferenceIndex];
            if (!TryGetDictionary(conferenceVar, out var conferenceDict))
                continue;

            var conferenceName = FmtString(GetFirstNonNil(conferenceDict, "conference"), "Conference");
            if (lines.Count > 0)
                lines.Add("");
            lines.Add(conferenceName);

            var seeds = TryExtractArray(conferenceDict, "seeds") ?? new Godot.Collections.Array();
            var seedRows = new SortedDictionary<int, string>();
            for (var seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
            {
                var seedVar = (Variant)seeds[seedIndex];
                if (!TryGetDictionary(seedVar, out var seedDict))
                    continue;

                var seedNumber = GetIntValue(GetFirstNonNil(seedDict, "seed"), 0);
                var teamName = FmtString(GetFirstNonNil(seedDict, "team_name", "teamName"), "TBD");
                if (seedNumber > 0)
                    seedRows[seedNumber] = $"{seedNumber}. {teamName}";
            }

            foreach (var seedRow in seedRows)
                lines.Add(seedRow.Key == 1 ? $"{seedRow.Value} - BYE" : seedRow.Value);

            var rounds = TryExtractArray(conferenceDict, "rounds") ?? new Godot.Collections.Array();
            for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
            {
                var roundVar = (Variant)rounds[roundIndex];
                if (!TryGetDictionary(roundVar, out var roundDict))
                    continue;

                var roundName = FmtString(GetFirstNonNil(roundDict, "round"), "Round");
                var games = TryExtractArray(roundDict, "games");
                if (games == null || games.Count == 0)
                    continue;

                lines.Add(roundName);
                var gameRows = new List<(int HomeSeed, int AwaySeed, string Text)>();
                for (var gameIndex = 0; gameIndex < games.Count; gameIndex++)
                {
                    var gameVar = (Variant)games[gameIndex];
                    if (!TryGetDictionary(gameVar, out var gameDict))
                        continue;

                    var homeSeed = GetIntValue(GetFirstNonNil(gameDict, "home_seed", "homeSeed"), 0);
                    var awaySeed = GetIntValue(GetFirstNonNil(gameDict, "away_seed", "awaySeed"), 0);
                    var homeTeam = FmtString(GetFirstNonNil(gameDict, "home_team_name", "homeTeamName"), "TBD");
                    var awayTeam = FmtString(GetFirstNonNil(gameDict, "away_team_name", "awayTeamName"), "TBD");
                    var winnerTeamId = FmtString(GetFirstNonNil(gameDict, "winner_team_id", "winnerTeamId"), "");
                    var status = FmtString(GetFirstNonNil(gameDict, "status"), "");
                    var matchup = homeSeed > 0 && awaySeed > 0
                        ? $"{homeSeed}. {homeTeam} vs {awaySeed}. {awayTeam}"
                        : $"{homeTeam} vs {awayTeam}";
                    if (!string.IsNullOrWhiteSpace(winnerTeamId) || string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
                        matchup = $"{matchup} (completed)";
                    gameRows.Add((homeSeed, awaySeed, matchup));
                }

                foreach (var gameRow in gameRows.OrderBy(row => row.HomeSeed).ThenBy(row => row.AwaySeed))
                    lines.Add(gameRow.Text);
            }
        }

        if (TryGetDictionary(GetFirstNonNil(playoffBracket, "league_championship_round", "leagueChampionshipRound"), out var championshipRound))
        {
            var championshipGames = TryExtractArray(championshipRound, "games");
            if (championshipGames != null && championshipGames.Count > 0)
            {
                lines.Add("");
                lines.Add(FmtString(GetFirstNonNil(championshipRound, "round"), "League Championship"));
                for (var gameIndex = 0; gameIndex < championshipGames.Count; gameIndex++)
                {
                    var gameVar = (Variant)championshipGames[gameIndex];
                    if (!TryGetDictionary(gameVar, out var gameDict))
                        continue;

                    var homeTeam = FmtString(GetFirstNonNil(gameDict, "home_team_name", "homeTeamName"), "TBD");
                    var awayTeam = FmtString(GetFirstNonNil(gameDict, "away_team_name", "awayTeamName"), "TBD");
                    lines.Add($"{homeTeam} vs {awayTeam}");
                }
            }
        }

        return lines.Count == 0 ? "Playoff bracket not generated yet." : string.Join("\n", lines);
    }

    private Godot.Collections.Array BuildDashboardRecentResultsArray(System.Collections.Generic.IEnumerable<RecentResultDto> items)
    {
        var array = new Godot.Collections.Array();
        if (items == null)
            return array;

        foreach (var item in items)
        {
            array.Add(new Godot.Collections.Dictionary
            {
                { "game_id", item?.GameId ?? "" },
                { "week", item?.Week ?? 0 },
                { "absolute_week", item?.AbsoluteWeek ?? 0 },
                { "phase_week", item?.PhaseWeek ?? 0 },
                { "phase", item?.Phase ?? "" },
                { "game_type", item?.GameType ?? "" },
                { "week_label", item?.WeekLabel ?? "" },
                { "home_team", item?.HomeTeam ?? "" },
                { "away_team", item?.AwayTeam ?? "" },
                { "home_score", item?.HomeScore ?? 0 },
                { "away_score", item?.AwayScore ?? 0 },
                { "winner", item?.Winner ?? "" },
                { "summary", item?.Summary ?? "" },
            });
        }

        return array;
    }

    private void ResetDashboardPreviewUiState()
    {
        ClearInboxDetail();
        _activeGameDayGame = new Godot.Collections.Dictionary();
        _latestGameResult = null;
        _restorePostGameRecapAfterBoxScore = false;
        if (_lblGameDayStatus != null)
            _lblGameDayStatus.Text = "";
        if (_lblPostGameStatus != null)
            _lblPostGameStatus.Text = "";
        if (_lblBoxScorePopupStatus != null)
            _lblBoxScorePopupStatus.Text = "";
        CloseGameDayPopup();
        HideBoxScorePopup();
        HidePostGameRecapPopup();
    }

    private void ApplyStateSummary(Godot.Collections.Dictionary dict)
    {
        if (dict == null)
        {
            if (_calendarTitle != null)
                _calendarTitle.Text = "Season";
            if (_calendarText != null)
                _calendarText.Text = "Calendar: (unparsed)";
            if (_lblGameStatus != null)
                _lblGameStatus.Text = "Schedule unavailable";
            if (_lblGameNext != null)
                _lblGameNext.Text = "Next: unavailable";
            return;
        }

        if (dict.ContainsKey("calendar"))
        {
            var cal = (Godot.Collections.Dictionary)dict["calendar"];

            var year = FmtInt(GetFirstNonNil(cal, "season_year"), "?");
            var date = FormatCalendarDate(
                FmtString(GetFirstNonNil(cal, "day_of_week"), ""),
                FmtString(GetFirstNonNil(cal, "current_date"), "?")
            );
            var weekLabel = FmtString(GetFirstNonNil(cal, "week_label"), "?");
            var gameStatus = BuildScheduleStatusLine(dict);
            var gameNext = BuildNextScheduleLine(dict);

            if (_calendarTitle != null)
                _calendarTitle.Text = $"{year} Season";

            if (_calendarText != null)
        // Preserve the compact top-bar layout while the native dashboard is refreshed.
                // _calendarText.Text = $"{year} Season\n{weekLabel}\n{date}\n\n{scheduleLine}";
                _calendarText.Text = $"{weekLabel} - {date}";
            if (_lblGameStatus != null)
                _lblGameStatus.Text = gameStatus;
            if (_lblGameNext != null)
                _lblGameNext.Text = gameNext;
        }
        else
        {
            if (_calendarTitle != null)
                _calendarTitle.Text = "Season";
            if (_calendarText != null)
                _calendarText.Text = "Calendar: (missing)";
            if (_lblGameStatus != null)
                _lblGameStatus.Text = "No user game today";
            if (_lblGameNext != null)
                _lblGameNext.Text = "Next: unavailable";
        }

        UpdateWeekInfoFromStateSummary(dict);
        UpdateUserTeamIdFromStateSummary(dict);
        if (!string.IsNullOrWhiteSpace(_userTeamId))
            _currentTeamId = _userTeamId;

        // Populate team list
        if (_teamList != null)
            _teamList.Clear();
        _teams.Clear();
        _teamDisplayById.Clear();
        _teamShortById.Clear();

        if (dict.ContainsKey("league"))
        {
            var league = (Godot.Collections.Dictionary)dict["league"];
            if (league.ContainsKey("teams"))
            {
                var teamsArr = (Godot.Collections.Array)league["teams"];
                foreach (var t in teamsArr)
                {
                    var team = (Godot.Collections.Dictionary)t;
                    _teams.Add(team);

                    var abbr = FmtString(GetFirstNonNil(team, "abbreviation", "abbr", "short_name"), "??");
                    var teamName = FmtString(GetFirstNonNil(team, "team_name", "name", "nickname"), "");
                    var city = FmtString(GetFirstNonNil(team, "city", "location"), "");

                    var display = $"{abbr} - {city} {teamName}".Trim();
                    if (_teamList != null)
                        _teamList.AddItem(display);

                    if (team.ContainsKey("id"))
                    {
                        var id = team["id"].ToString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            _teamDisplayById[id] = display;

                            var shortLabel = (!string.IsNullOrWhiteSpace(abbr) && !string.Equals(abbr, "??", StringComparison.Ordinal))
                                ? abbr
                                : teamName;
                            if (!string.IsNullOrWhiteSpace(shortLabel))
                                _teamShortById[id] = shortLabel;
                        }
                    }
                }
            }
        }

        UpdateUserTeamLabelFromStateSummary(dict);

        // Clear roster until a team is selected
        ShowRosterMessage("Select the Roster tab to load roster.");
        SetReportPlaceholder("Select a player to view the scout report.");
    }

    private Godot.Collections.Dictionary BuildNativeStateSummaryDictionary()
    {
        EnsureNativeGameCoreServices();
        var league = GetOrCreateNativeGameCoreContext().ActiveLeague;
        var summary = new Godot.Collections.Dictionary();
        if (league == null)
            return summary;

        var todayGame = _nativeGameDayService?.GetCurrentUserGame();
        var nextGame = _nativeScheduleService?.GetNextUserGame(league);
        var teamEntries = new Godot.Collections.Array();
        foreach (var team in league.Teams)
        {
            teamEntries.Add(new Godot.Collections.Dictionary
            {
                ["id"] = team.TeamId ?? "",
                ["abbreviation"] = team.Abbreviation ?? "",
                ["team_name"] = team.Name ?? "",
                ["city"] = "",
            });
        }

        var calendar = league.Calendar;
        var currentDate = calendar?.CurrentDate ?? "";
        var dayOfWeek = "";
        if (DateTime.TryParse(currentDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            dayOfWeek = parsedDate.ToString("dddd", CultureInfo.InvariantCulture);

        summary["calendar"] = new Godot.Collections.Dictionary
        {
            ["season_year"] = league.SeasonYear,
            ["current_date"] = currentDate,
            ["day_of_week"] = dayOfWeek,
            ["week"] = calendar?.PhaseWeek ?? 0,
            ["current_week"] = calendar?.PhaseWeek ?? 0,
            ["absolute_week"] = calendar?.AbsoluteWeek ?? 0,
            ["phase"] = calendar?.Phase ?? ScheduleService.GetPhaseForWeek(calendar?.Week ?? 1),
            ["total_weeks"] = LeagueBootstrapService.TotalSeasonWeeks,
            ["week_label"] = calendar?.WeekLabel ?? ScheduleService.BuildCalendarWeekLabel(calendar?.Week ?? 1),
            ["phase_week"] = calendar?.PhaseWeek ?? ScheduleService.GetPhaseWeek(calendar?.Week ?? 1),
        };
        summary["league"] = new Godot.Collections.Dictionary
        {
            ["teams"] = teamEntries,
            ["total_weeks"] = LeagueBootstrapService.TotalSeasonWeeks,
        };
        summary["user_team_id"] = league.UserTeamId ?? "";
        summary["user_team_abbr"] = GameCoreStateHelper.ResolveTeam(league)?.Abbreviation ?? "";
        if (todayGame != null)
        {
            summary["user_team_game_today"] = new Godot.Collections.Dictionary
            {
                ["label"] = BuildNativeScheduleLabel(todayGame, league.UserTeamId),
            };
        }
        if (nextGame != null)
        {
            summary["user_team_next_game"] = new Godot.Collections.Dictionary
            {
                ["label"] = BuildNativeScheduleLabel(nextGame, league.UserTeamId),
            };
        }

        return summary;
    }

    private string BuildNativeScheduleLabel(GridironGM.GameCore.Models.ScheduledGame game, string focusTeamId)
    {
        if (game == null)
            return "";

        var league = GetOrCreateNativeGameCoreContext().ActiveLeague;
        var opponent = GameCoreStateHelper.ResolveOpponent(league, game, focusTeamId);
        var opponentLabel = opponent?.Abbreviation ?? opponent?.Name ?? "TBD";
        var isHome = string.Equals(game.HomeTeamId, focusTeamId, StringComparison.OrdinalIgnoreCase);
        var weekLabel = string.IsNullOrWhiteSpace(game.WeekLabel)
            ? ScheduleService.BuildGameWeekLabel(game.GameType, game.AbsoluteWeek > 0 ? game.AbsoluteWeek : game.Week, game.PhaseWeek)
            : game.WeekLabel;
        return $"{weekLabel} {(isHome ? "vs" : "@")} {opponentLabel}";
    }

    private async Task OnMainTabChanged(int tabIndex)
    {
        SetMainTab(tabIndex);
        if (tabIndex == 0)
            await RefreshDashboardIfPending();
        if (tabIndex == ROSTER_TAB_INDEX)
            await RefreshRosterTab();
    }

    private async Task SelectMainTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= 3)
            return;

        if (_currentMainTab == tabIndex)
        {
            SetMainTab(tabIndex);
            if (tabIndex == 0)
                await RefreshDashboardIfPending();
            if (tabIndex == ROSTER_TAB_INDEX)
                await RefreshRosterTab();
            return;
        }

        await OnMainTabChanged(tabIndex);
    }

    private async Task OpenLeagueHistoryTabAsync()
    {
        await SelectMainTab(LEAGUE_TAB_INDEX);
        if (_leagueHubTabs != null && _leagueHubTabs.GetTabCount() > LEAGUE_HISTORY_SUBTAB_INDEX)
            _leagueHubTabs.CurrentTab = LEAGUE_HISTORY_SUBTAB_INDEX;
    }

    private void SetMainTab(int activeTab)
    {
        _currentMainTab = activeTab;

        if (_overviewTabPanel != null)
            _overviewTabPanel.Visible = activeTab == 0;
        if (_leagueTabPanel != null)
            _leagueTabPanel.Visible = activeTab == 1;
        if (_rosterTabPanel != null)
            _rosterTabPanel.Visible = activeTab == ROSTER_TAB_INDEX;

        UpdateMainTabButtons(activeTab);
    }

    private void UpdateMainTabButtons(int activeTab)
    {
        if (_btnOverviewTab != null)
            _btnOverviewTab.ButtonPressed = activeTab == 0;
        if (_btnLeagueTab != null)
            _btnLeagueTab.ButtonPressed = activeTab == 1;
        if (_btnRosterTab != null)
            _btnRosterTab.ButtonPressed = activeTab == ROSTER_TAB_INDEX;
    }

    private async Task RefreshFrontOfficeContext()
    {
        if (IsNativeRuntimeSource())
        {
            RenderFrontOfficeLabel();
            return;
        }

        var (profileStatus, profileBody) = await GetWithTimeoutAsync("/gm_profile", REQUEST_TIMEOUT_MS);
        if (profileStatus >= 200 && profileStatus < 300)
            ApplyGmProfilePayload(profileBody);
        else
            RenderFrontOfficeLabel();
    }

    private void ApplyGmProfilePayload(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            RenderFrontOfficeLabel();
            return;
        }

        var parsed = Json.ParseString(body);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            RenderFrontOfficeLabel();
            return;
        }

        var dict = parsed.AsGodotDictionary();
        var gm = TryExtractObject(dict, "gm", "profile");
        if (gm == null)
        {
            RenderFrontOfficeLabel();
            return;
        }

        _gmName = FmtString(GetFirstNonNil(gm, "name", "gm_name"), _gmName);
        _gmRole = FmtString(GetFirstNonNil(gm, "current_role", "role"), _gmRole);
        var teamId = FmtString(GetFirstNonNil(gm, "current_team_id", "team_id"), _userTeamId);
        if (!string.IsNullOrWhiteSpace(teamId))
            _userTeamId = teamId;

        var reputationText = FmtString(GetFirstNonNil(gm, "reputation"), "");
        var jobSecurityText = FmtString(GetFirstNonNil(gm, "job_security"), "");
        if (int.TryParse(reputationText, out var reputation))
            _gmReputation = reputation;
        if (int.TryParse(jobSecurityText, out var jobSecurity))
            _gmJobSecurity = jobSecurity;

        _gmTeamLabel = ResolveFrontOfficeTeamLabel(_userTeamId);
        RenderFrontOfficeLabel();
    }

    private void RenderFrontOfficeLabel()
    {
        if (_lblFrontOfficeHeader == null || _lblUserTeam == null)
            return;

        var gmName = string.IsNullOrWhiteSpace(_gmName) ? "User GM" : _gmName;
        var role = string.IsNullOrWhiteSpace(_gmRole) ? "General Manager" : _gmRole;
        var team = string.IsNullOrWhiteSpace(_gmTeamLabel) ? "(unknown)" : _gmTeamLabel;
        var teamText = string.IsNullOrWhiteSpace(_dashboardTeamName) ? team : _dashboardTeamName;
        var detail = $"{role} - Team: {teamText}";

        if (!string.IsNullOrWhiteSpace(_dashboardTeamRecord))
            detail += $" ({_dashboardTeamRecord})";
        if (_dashboardRosterSize.HasValue)
            detail += $" - Roster {_dashboardRosterSize.Value}";
        if (_dashboardInjuryCount.HasValue)
            detail += $" - Injuries {_dashboardInjuryCount.Value}";
        if (!string.IsNullOrWhiteSpace(_dashboardCapRoom))
            detail += $" - Cap {_dashboardCapRoom}";

        if (_gmReputation.HasValue)
            detail += $" - Rep {_gmReputation.Value}";
        if (_gmJobSecurity.HasValue)
            detail += $" - Security {_gmJobSecurity.Value}";

        _lblFrontOfficeHeader.Text = $"GM: {gmName}";
        _lblUserTeam.Text = detail;
    }

    private string ResolveFrontOfficeTeamLabel(string teamId)
    {
        var abbr = ResolveTeamAbbrFromId(teamId);
        if (!string.IsNullOrWhiteSpace(abbr))
            return abbr;
        return string.IsNullOrWhiteSpace(teamId) ? "(unknown)" : teamId;
    }

    private void ResetClientCachesForNewGame()
    {
        _currentTeamId = "";
        _userTeamId = "";
        _gmName = "User GM";
        _gmRole = "General Manager";
        _gmTeamLabel = "(unknown)";
        _gmReputation = null;
        _gmJobSecurity = null;
        _dashboardTeamName = "";
        _dashboardTeamRecord = "0-0";
        _dashboardRosterSize = null;
        _dashboardInjuryCount = null;
        _dashboardCapRoom = "N/A";
        RenderFrontOfficeLabel();
        _currentRoster = new Godot.Collections.Array();
        _playerDetailsById.Clear();
        _teamRosterCache.Clear();
        _teamPlayerDetailsCache.Clear();
        _gameCache.Clear();
        _teamShortById.Clear();
        _selectedInboxMessageId = "";
        _selectedSimGameId = "";
        _scheduleGames = new Godot.Collections.Array();
        _selectedScheduleGame = null;
        _activeGameDayGame = new Godot.Collections.Dictionary();
        _latestGameResult = null;
        _restorePostGameRecapAfterBoxScore = false;
        _teamPickIndexToId.Clear();
        _awaitingNewGameTeamPick = false;
        _handledNewGameTeamPick = false;
        HideBoxScorePopup();
        HidePostGameRecapPopup();
    }

    private async Task OnTeamSelected(int index)
    {
        if (index < 0 || index >= _teams.Count)
            return;

        var selectionVersion = ++_teamSelectionVersion;
        var team = (Godot.Collections.Dictionary)_teams[index];
        if (!team.ContainsKey("id"))
            return;

        var teamId = team["id"].ToString();
        _currentTeamId = teamId;

        var scheduleTask = RefreshScheduleAsync(teamId, selectionVersion);
        var injuryTask = RefreshInjuryReportAsync(teamId, selectionVersion);

        if (IsNativeRuntimeSource())
        {
            try
            {
                await RefreshRosterTab();
            }
            finally
            {
                await scheduleTask;
                await injuryTask;
            }

            return;
        }

        try
        {
            ShowRosterMessage("Loading roster...");
            SetReportPlaceholder("Loading roster...");

            if (_teamRosterCache.TryGetValue(teamId, out var cachedRoster)
                && _teamPlayerDetailsCache.TryGetValue(teamId, out var cachedDetails))
            {
                if (selectionVersion != _teamSelectionVersion)
                    return;
                _currentRoster = cachedRoster;
                _playerDetailsById.Clear();
                foreach (var kvp in cachedDetails)
                    _playerDetailsById[kvp.Key] = kvp.Value;
                BuildRosterTree();
                SetReportPlaceholder("Select a player to view the scout report.");
                return;
            }

            var (status, body) = await GetWithTimeoutAsync($"/team/{teamId}/roster?include_details=1", REQUEST_TIMEOUT_MS);
            if (selectionVersion != _teamSelectionVersion)
                return;
            if (status < 200 || status >= 300)
            {
                ShowRosterMessage($"ERROR {status}");
                SetStateDumpText(body);
                return;
            }

            var parsed = Json.ParseString(body);
            if (selectionVersion != _teamSelectionVersion)
                return;
            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                ShowRosterMessage("Roster: (unparsed)");
                return;
            }

            var rosterPayload = parsed.AsGodotDictionary();
            if (selectionVersion != _teamSelectionVersion)
                return;

            if (!rosterPayload.ContainsKey("roster"))
            {
                ShowRosterMessage("No roster key in payload");
                return;
            }

            var roster = (Godot.Collections.Array)rosterPayload["roster"];
            if (selectionVersion != _teamSelectionVersion)
                return;
            _currentRoster = roster;
            BuildPlayerDetailsMap(roster);
            _teamRosterCache[teamId] = roster;
            _teamPlayerDetailsCache[teamId] = new Dictionary<string, Godot.Collections.Dictionary>(_playerDetailsById);
            BuildRosterTree();
            SetReportPlaceholder("Select a player to view the scout report.");

            // Optional: show IR + Practice Squad counts in debug
            var irCount = rosterPayload.ContainsKey("ir_list")
                ? ((Godot.Collections.Array)rosterPayload["ir_list"]).Count
                : 0;

            var psCount = rosterPayload.ContainsKey("practice_squad")
                ? ((Godot.Collections.Array)rosterPayload["practice_squad"]).Count
                : 0;

            SetStateDumpText($"\n\nRoster loaded. IR={irCount}, PS={psCount}", append: true);
        }
        finally
        {
            await scheduleTask;
            await injuryTask;
        }
    }

    private static string BuildScheduleStatusLine(Godot.Collections.Dictionary state)
    {
        var userTeamGameToday = TryExtractObject(state, "user_team_game_today", "userTeamGameToday");
        if (userTeamGameToday != null)
        {
            var label = CleanScheduleGameLabel(FmtString(GetFirstNonNil(userTeamGameToday, "label"), ""));
            return string.IsNullOrWhiteSpace(label) ? "User game today" : label;
        }

        return "No user game today";
    }

    private static string BuildNextScheduleLine(Godot.Collections.Dictionary state)
    {
        var leagueGamesTodayCount = GetIntValue(TryExtract(state, "league_games_today_count", "leagueGamesTodayCount"), 0);
        var userTeamNextGame = TryExtractObject(state, "user_team_next_game", "userTeamNextGame");
        var nextGameLabel = userTeamNextGame != null
            ? FmtString(GetFirstNonNil(userTeamNextGame, "label"), "")
            : "";

        if (!string.IsNullOrWhiteSpace(nextGameLabel))
            return $"Next User Game: {CleanScheduleGameLabel(nextGameLabel)}";

        if (leagueGamesTodayCount > 0)
            return $"League games today: {leagueGamesTodayCount}";

        return "Next: no upcoming user game";
    }

    private static string FormatCalendarDate(string dayOfWeek, string isoDate)
    {
        if (DateTime.TryParseExact(
            isoDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate))
        {
            var day = string.IsNullOrWhiteSpace(dayOfWeek)
                ? parsedDate.ToString("dddd", CultureInfo.InvariantCulture)
                : dayOfWeek.Trim();
            return $"{day}, {parsedDate.ToString("MMM", CultureInfo.InvariantCulture)} {parsedDate.Day}, {parsedDate.Year}";
        }

        if (string.IsNullOrWhiteSpace(isoDate))
            return string.IsNullOrWhiteSpace(dayOfWeek) ? "Week 1, Day 1" : dayOfWeek;

        return string.IsNullOrWhiteSpace(dayOfWeek) ? isoDate : $"{dayOfWeek}, {isoDate}";
    }

    private static string CleanScheduleGameLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "";

        var cleaned = label.Trim();
        return System.Text.RegularExpressions.Regex.Replace(cleaned, @",\s+\d{4}(?=\s+[-\u2014]\s+)", "");
    }

    private async Task AdvanceDay()
    {
        _btnAdvanceDay.Disabled = true;
        var (status, body) = await PostWithTimeoutAsync("/advance_day", "{}", REQUEST_TIMEOUT_MS);
        _btnAdvanceDay.Disabled = false;

        if (status < 200 || status >= 300)
        {
            SetStateDumpText(body);
            return;
        }

        await RefreshStateSummary();
        await RefreshLeagueHub();
    }

    private async Task NewGame()
    {
        if (IsNativeRuntimeSource())
        {
            if (HasExistingNativeSave())
            {
                if (_newGameConfirmDialog != null)
                    _newGameConfirmDialog.PopupCentered();
                else
                    await ConfirmNativeNewGame();
            }
            else
            {
                await ConfirmNativeNewGame();
            }
            return;
        }

        SetNewGameButtonsDisabled(true);
        var (status, body) = await PostWithTimeoutAsync("/new_game", "{}", REQUEST_TIMEOUT_MS);

        if (status < 200 || status >= 300)
        {
            SetStateDumpText(body);
            SetNewGameButtonsDisabled(false);
            return;
        }

        ResetClientCachesForNewGame();
        if (_teamList != null)
            _teamList.Clear();
        _teams.Clear();
        _teamDisplayById.Clear();

        await RefreshStateSummaryForNewGame();
        PrepareNewGameTeamPicker();
        ShowNewGameTeamPicker();
    }

    private bool HasExistingNativeSave()
    {
        var saveService = GetNativeGameCoreSaveService();
        return saveService.SaveExists() || saveService.SaveExists(GameCoreSaveService.NamedSaveFileName);
    }

    private async Task ConfirmNativeNewGame()
    {
        SetNewGameButtonsDisabled(true);
        try
        {
            await StartFreshNativeLeague();
        }
        finally
        {
            SetNewGameButtonsDisabled(false);
        }
    }

    private async Task StartFreshNativeLeague()
    {
        EnsureNativeGameCoreServices();
        _nativeGameCoreContext.ActiveLeague = null;
        new LeagueBootstrapService(_nativeGameCoreContext).CreateTestLeague(GetTeamSeedPath());
        _nativeStartupState = NativeStartupState.Ready;
        ResetDashboardPreviewUiState();
        ResetClientCachesForNewGame();
        await RefreshAll();
        PrepareNewGameTeamPicker();
        ShowNewGameTeamPicker();
        SetPrimaryStatus("Choose a franchise to begin.");
    }

    private static string GetTeamSeedPath()
        => ProjectSettings.GlobalizePath("res://Assets/data_seed/teams.json");

    private async Task<bool> RefreshStateSummaryForNewGame()
    {
        if (_calendarTitle != null)
            _calendarTitle.Text = "Season";
        if (_calendarText != null)
            _calendarText.Text = "State: loading...";
        if (_lblGameStatus != null)
            _lblGameStatus.Text = "Schedule: loading...";
        if (_lblGameNext != null)
            _lblGameNext.Text = "Next: loading...";
        var (status, body) = await GetWithTimeoutAsync("/state_summary", REQUEST_TIMEOUT_MS);

        if (status < 200 || status >= 300)
        {
            var summary = SummarizeRequestError("/state_summary", status, body);
            if (_calendarTitle != null)
                _calendarTitle.Text = "Season";
            if (_calendarText != null)
                _calendarText.Text = $"State: ERROR - {summary}";
            if (_lblGameStatus != null)
                _lblGameStatus.Text = "Schedule unavailable";
            if (_lblGameNext != null)
                _lblGameNext.Text = "Next: unavailable";
            SetStateDumpText(body);
            SetServerError(summary);
            return false;
        }

        var ok = ApplyStateSummaryPayload(body);
        if (ok)
        {
            await RefreshDashboardState();
            await RefreshFrontOfficeContext();
        }
        return ok;
    }

    private void PrepareNewGameTeamPicker()
    {
        _teamPickIndexToId.Clear();
        if (_teamPickList == null)
            return;

        _teamPickList.Clear();

        if (_teams == null || _teams.Count == 0)
        {
            _teamPickList.AddItem("(error) No teams");
            _teamPickList.Select(0);
            return;
        }

        for (var i = 0; i < _teams.Count; i++)
        {
            var team = (Godot.Collections.Dictionary)_teams[i];
            var teamId = team.ContainsKey("id") ? team["id"].ToString() : "";
            if (string.IsNullOrWhiteSpace(teamId))
                continue;

            var display = BuildTeamPickDisplay(team);
            _teamPickIndexToId.Add(teamId);
            var abbreviation = team.ContainsKey("abbreviation") ? team["abbreviation"].ToString() : "";
            _teamPickList.AddItem(display, LoadTeamLogo(abbreviation));
        }

        if (_teamPickIndexToId.Count == 0)
            _teamPickList.AddItem("(error) No teams");

        if (_teamPickList.ItemCount > 0)
            _teamPickList.Select(0);
    }

    private void ShowNewGameTeamPicker()
    {
        if (_newGameTeamPicker == null)
        {
            SetNewGameButtonsDisabled(false);
            return;
        }

        _awaitingNewGameTeamPick = true;
        _handledNewGameTeamPick = false;
        _newGameTeamPicker.PopupCenteredRatio(0.4f);
    }

    private async Task OnNewGameTeamPickerConfirmed()
    {
        await FinalizeNewGameTeamPick(false);
    }

    private async Task OnNewGameTeamPickerCanceled()
    {
        await FinalizeNewGameTeamPick(true);
    }

    private async Task FinalizeNewGameTeamPick(bool forceFirst)
    {
        if (!_awaitingNewGameTeamPick || _handledNewGameTeamPick)
            return;

        _handledNewGameTeamPick = true;
        _awaitingNewGameTeamPick = false;

        if (_newGameTeamPicker != null && _newGameTeamPicker.Visible)
            _newGameTeamPicker.Hide();

        var teamId = ResolveTeamIdFromTeamPicker(forceFirst);
        if (string.IsNullOrWhiteSpace(teamId))
        {
            _gmTeamLabel = "(error)";
            RenderFrontOfficeLabel();
            SetNewGameButtonsDisabled(false);
            return;
        }

        try
        {
            await SetUserTeamForNewGame(teamId);
        }
        finally
        {
            SetNewGameButtonsDisabled(false);
        }
    }

    private async Task SetUserTeamForNewGame(string teamId)
    {
        if (IsNativeRuntimeSource())
        {
            EnsureNativeGameCoreServices();
            var league = _nativeGameCoreContext?.ActiveLeague;
            var team = league?.Teams.FirstOrDefault(candidate =>
                string.Equals(candidate.TeamId, teamId, StringComparison.OrdinalIgnoreCase));
            if (team == null)
            {
                _gmTeamLabel = "(error)";
                RenderFrontOfficeLabel();
                SetPrimaryStatus("Unable to select that franchise.");
                return;
            }

            league.UserTeamId = team.TeamId;
            ResetClientCachesForNewGame();
            var saveResult = await SaveCurrentNativeGame(
                GameCoreSaveService.NamedSaveFileName,
                $"Franchise started with {team.Name}.",
                autosaveToo: true);
            if (!saveResult.Ok)
                return;

            HideStartupPanel();
            await RefreshAll();
            await TrySelectTeamInRoster(team.TeamId);
            SetPrimaryStatus($"Franchise started with {team.Name}.");
            return;
        }

        var payload = new Godot.Collections.Dictionary
        {
            { "team_id", teamId }
        };
        var json = Json.Stringify(payload);
        var (status, body) = await PostWithTimeoutAsync("/set_user_team", json, REQUEST_TIMEOUT_MS);

        if (status < 200 || status >= 300)
        {
            SetStateDumpText(body);
            _gmTeamLabel = "(error)";
            RenderFrontOfficeLabel();
            return;
        }

        await RefreshStateSummary();
        await RefreshInbox();
        await TrySelectTeamInRoster(teamId);
        await RefreshLeagueHub();
        if (IsRosterTabActive())
            await RefreshRosterTab();
    }

    private async Task<bool> TrySelectTeamInRoster(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return false;

        if (_teams == null || _teams.Count == 0)
            return false;

        for (var i = 0; i < _teams.Count; i++)
        {
            var team = (Godot.Collections.Dictionary)_teams[i];
            var id = team.ContainsKey("id") ? team["id"].ToString() : "";
            if (!string.Equals(id, teamId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (_teamList != null)
            {
                _suppressTeamListEvents = true;
                _teamList.Select(i);
                _suppressTeamListEvents = false;
            }

            _currentTeamId = teamId;
            return true;
        }

        return false;
    }

    private string ResolveTeamIdFromTeamPicker(bool forceFirst)
    {
        if (_teamPickIndexToId.Count == 0)
            return "";

        var selectedIndex = 0;
        if (!forceFirst && _teamPickList != null)
        {
            var selected = _teamPickList.GetSelectedItems();
            if (selected != null && selected.Length > 0)
                selectedIndex = (int)selected[0];
        }

        if (selectedIndex < 0 || selectedIndex >= _teamPickIndexToId.Count)
            selectedIndex = 0;

        return _teamPickIndexToId[selectedIndex];
    }

    private static string BuildTeamPickDisplay(Godot.Collections.Dictionary team)
    {
        if (team == null)
            return "Team";

        var abbr = team.ContainsKey("abbreviation") ? team["abbreviation"].ToString() : "";
        var teamName = team.ContainsKey("team_name") ? team["team_name"].ToString() : "";
        var city = team.ContainsKey("city") ? team["city"].ToString() : "";

        var name = $"{city} {teamName}".Trim();
        if (string.IsNullOrWhiteSpace(name))
            return string.IsNullOrWhiteSpace(abbr) ? "Team" : abbr;

        return string.IsNullOrWhiteSpace(abbr) ? name : $"{name} ({abbr})";
    }

    private static Texture2D LoadTeamLogo(string abbreviation)
    {
        if (string.IsNullOrWhiteSpace(abbreviation))
            return null;

        return ResourceLoader.Load<Texture2D>($"res://Assets/team_logos/{abbreviation.Trim().ToUpperInvariant()}.png");
    }

    private void SetNewGameButtonsDisabled(bool disabled)
    {
        if (_btnNewGame != null)
            _btnNewGame.Disabled = disabled;
        if (_btnStartupNewGame != null)
            _btnStartupNewGame.Disabled = disabled;
    }

    private async Task ResetSave()
    {
        if (IsNativeRuntimeSource())
        {
            if (_btnResetSave != null)
                _btnResetSave.Disabled = true;

            try
            {
                var saveService = GetNativeGameCoreSaveService();
                saveService.Delete();
                saveService.Delete(GameCoreSaveService.NamedSaveFileName);
                EnsureNativeGameCoreServices();
                _nativeGameCoreContext.ActiveLeague = null;
                new LeagueBootstrapService(_nativeGameCoreContext).CreateTestLeague(GetTeamSeedPath());
                _nativeStartupState = NativeStartupState.Ready;
                ResetDashboardPreviewUiState();
                ResetClientCachesForNewGame();
                HideStartupPanel();
                await RefreshAll();
                SetPrimaryStatus("Native save reset. Started new native league.");
            }
            finally
            {
                if (_btnResetSave != null)
                    _btnResetSave.Disabled = false;
            }

            return;
        }

        _btnResetSave.Disabled = true;
        var (status, body) = await PostWithTimeoutAsync("/reset_save", "{}", REQUEST_TIMEOUT_MS);
        _btnResetSave.Disabled = false;

        if (status < 200 || status >= 300)
        {
            SetStateDumpText(body);
            return;
        }

        ResetClientCachesForNewGame();
        if (!ApplyStateSummaryPayload(body))
        {
            await RefreshAll();
            return;
        }
        await RefreshFrontOfficeContext();

        SetPrimaryStatus("Save reset.");

        GD.Print("Reset save OK");

        await RefreshInbox();
        await RefreshLeagueHub();
        if (IsRosterTabActive())
            await RefreshRosterTab();
    }

    private async Task SetUserTeamFromSelection()
    {
        if (_btnSetUserTeam != null)
            _btnSetUserTeam.Disabled = true;

        if (string.IsNullOrWhiteSpace(_currentTeamId))
        {
            _gmTeamLabel = "(select team)";
            RenderFrontOfficeLabel();
            if (_btnSetUserTeam != null)
                _btnSetUserTeam.Disabled = false;
            return;
        }

        var payload = new Godot.Collections.Dictionary
        {
            { "team_id", _currentTeamId }
        };
        var json = Json.Stringify(payload);
        var (status, body) = await PostWithTimeoutAsync("/set_user_team", json, REQUEST_TIMEOUT_MS);
        if (_btnSetUserTeam != null)
            _btnSetUserTeam.Disabled = false;

        if (status < 200 || status >= 300)
        {
            SetStateDumpText(body);
            _gmTeamLabel = "(error)";
            RenderFrontOfficeLabel();
            return;
        }

        await RefreshStateSummary();
        await RefreshInbox();
        if (IsRosterTabActive())
            await RefreshRosterTab();
    }

    private bool IsRosterTabActive()
    {
        return _currentMainTab == ROSTER_TAB_INDEX;
    }

    private async Task SetRosterViewMode(bool showDepthChart)
    {
        _depthChartViewActive = showDepthChart;
        UpdateRosterViewModeUi();
        if (IsRosterTabActive())
            await RefreshRosterTab();
    }

    private void UpdateRosterViewModeUi()
    {
        if (_btnRosterViewMode != null)
            _btnRosterViewMode.ButtonPressed = !_depthChartViewActive;
        if (_btnDepthChartViewMode != null)
            _btnDepthChartViewMode.ButtonPressed = _depthChartViewActive;
        if (_rosterSplit != null)
            _rosterSplit.Visible = !_depthChartViewActive;
        if (_depthChartPanel != null)
            _depthChartPanel.Visible = _depthChartViewActive;
    }

    private async Task RefreshRosterTab()
    {
        UpdateRosterViewModeUi();
        if (_depthChartViewActive)
        {
            await RefreshDepthChartView();
            return;
        }

        if (IsNativeRuntimeSource())
        {
            RefreshNativeRosterTab();
            return;
        }

        SetRosterSummaryPlaceholder();
        ShowRosterMessage("Loading roster...");

        var (status, body) = await GetWithTimeoutAsync("/team_roster", REQUEST_TIMEOUT_MS);
        if (status < 200 || status >= 300)
        {
            ClearRosterTab("Roster unavailable");
            SetStateDumpText(body);
            return;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            ClearRosterTab("Roster unavailable");
            return;
        }

        var parsed = Json.ParseString(body);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            ClearRosterTab("Roster unavailable");
            return;
        }

        var payload = parsed.AsGodotDictionary();
        var okVar = GetFirstNonNil(payload, "ok", "success");
        if (!IsNil(okVar) && !GetBoolValue(okVar, true))
        {
            var error = FmtString(GetFirstNonNil(payload, "error", "message", "detail"), "Roster unavailable");
            ClearRosterTab(string.IsNullOrWhiteSpace(error) ? "Roster unavailable" : error);
            return;
        }

        RenderRosterSnapshot(payload);
    }

    private async Task RefreshDepthChartView()
    {
        if (IsNativeRuntimeSource())
        {
            RefreshNativeDepthChartView();
            return;
        }

        SetDepthChartPlaceholder();

        var (status, body) = await GetWithTimeoutAsync("/team_depth_chart", REQUEST_TIMEOUT_MS);
        if (status < 200 || status >= 300)
        {
            ClearDepthChartView("Depth chart unavailable");
            SetStateDumpText(body);
            return;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            ClearDepthChartView("Depth chart unavailable");
            return;
        }

        var parsed = Json.ParseString(body);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            ClearDepthChartView("Depth chart unavailable");
            return;
        }

        var payload = parsed.AsGodotDictionary();
        var okVar = GetFirstNonNil(payload, "ok", "success");
        if (!IsNil(okVar) && !GetBoolValue(okVar, true))
        {
            var error = FmtString(GetFirstNonNil(payload, "error", "message", "detail"), "Depth chart unavailable");
            ClearDepthChartView(string.IsNullOrWhiteSpace(error) ? "Depth chart unavailable" : error);
            return;
        }

        RenderDepthChartSnapshot(payload);
    }

    private async Task AutoFillDepthChart()
    {
        if (_btnAutoFillDepthChart == null || _depthChartRequestBusy)
            return;

        SetDepthChartRequestBusy(true, "Auto-filling...");
        SetDepthChartActionStatus("Auto-filling...");

        if (IsNativeRuntimeSource())
        {
            try
            {
                EnsureNativeGameCoreServices();
                var response = _nativeDepthChartService.AutoFillDepthChart(ResolveNativeRosterDepthChartTeamId());
                if (response == null || !response.Ok)
                {
                    var error = string.IsNullOrWhiteSpace(response?.Error) ? "Unable to auto-fill depth chart." : response.Error;
                    SetDepthChartActionStatus(error);
                    SetPrimaryStatus(error);
                    return;
                }

                RenderDepthChartSnapshot(ConvertDepthChartResponseToPayload(response));
                await SaveNativeAutosave("Native autosave updated.");
                await RefreshDashboardState();
                await RefreshInbox();
                await RefreshLeagueHub();
                _dashboardRefreshPendingFromDepthChartEdit = false;
                SetDepthChartActionStatus("Depth chart auto-filled.");
                SetPrimaryStatus("Depth chart auto-filled.");
            }
            catch (Exception ex)
            {
                var nativeError = $"Native C# auto-fill failed: {InlineMessage(ex.Message)}";
                SetDepthChartActionStatus("Unable to auto-fill depth chart.");
                SetPrimaryStatus(nativeError);
            }
            finally
            {
                SetDepthChartRequestBusy(false);
            }

            return;
        }

        var request = new Godot.Collections.Dictionary();
        if (!string.IsNullOrWhiteSpace(_currentTeamId))
            request["team_id"] = _currentTeamId;

        var (status, body) = await PostWithTimeoutAsync("/auto_fill_depth_chart", Json.Stringify(request), REQUEST_TIMEOUT_MS);
        if (status < 200 || status >= 300)
        {
            var summary = SummarizeRequestError("/auto_fill_depth_chart", status, body);
            SetDepthChartActionStatus("Unable to auto-fill depth chart.");
            SetPrimaryStatus(summary);
            SetStateDumpText(body);
            SetDepthChartRequestBusy(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            SetDepthChartActionStatus("Unable to auto-fill depth chart.");
            SetPrimaryStatus("Unable to auto-fill depth chart.");
            SetDepthChartRequestBusy(false);
            return;
        }

        var parsed = Json.ParseString(body);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            SetDepthChartActionStatus("Unable to auto-fill depth chart.");
            SetPrimaryStatus("Unable to auto-fill depth chart.");
            SetDepthChartRequestBusy(false);
            return;
        }

        var payload = parsed.AsGodotDictionary();
        var ok = GetBoolValue(GetFirstNonNil(payload, "ok", "success"), false);
        if (!ok)
        {
            var error = FmtString(GetFirstNonNil(payload, "error", "message", "detail"), "Unable to auto-fill depth chart.");
            var cleanError = string.IsNullOrWhiteSpace(error) ? "Unable to auto-fill depth chart." : error;
            SetDepthChartActionStatus(cleanError);
            SetPrimaryStatus(cleanError);
            SetDepthChartRequestBusy(false);
            return;
        }

        var message = FmtString(GetFirstNonNil(payload, "message"), "Depth chart auto-filled.");
        var depthChart = TryExtractObject(payload, "depth_chart", "depthChart");
        if (depthChart != null)
            RenderDepthChartSnapshot(depthChart);
        else
            await RefreshDepthChartView();

        await RefreshDashboardState();
        await RefreshInbox();
        _dashboardRefreshPendingFromDepthChartEdit = false;
        SetDepthChartActionStatus(message);
        SetPrimaryStatus(message);
        SetDepthChartRequestBusy(false);
    }

    private async Task RefreshDashboardIfPending()
    {
        if (!_dashboardRefreshPendingFromDepthChartEdit)
            return;

        var refreshed = await RefreshDashboardState();
        if (refreshed)
            await RefreshInbox();
        _dashboardRefreshPendingFromDepthChartEdit = false;
    }

    private GameCoreContext GetOrCreateNativeGameCoreContext()
    {
        _nativeGameCoreContext ??= new GameCoreContext();
        return _nativeGameCoreContext;
    }

    private GameCoreSaveService GetNativeGameCoreSaveService()
        => _nativeGameCoreSaveService ??= new GameCoreSaveService();

    private void EnsureNativeGameCoreServices()
    {
        var context = GetOrCreateNativeGameCoreContext();
        _nativeRosterService ??= new RosterService(context);
        _nativeDepthChartService ??= new DepthChartService(context);
        _nativeScheduleService ??= new ScheduleService(context);
        _nativeStandingsService ??= new StandingsService(context);
        _nativeDashboardService ??= new DashboardService(context);
        _nativeContinueService ??= new ContinueService(context);
        _nativeGameDayService ??= new GameDayService(context);
    }

    private async Task<GameCoreSaveResult> SaveCurrentNativeGame(string saveName, string successMessage, bool autosaveToo)
    {
        if (!IsNativeRuntimeSource())
        {
            return new GameCoreSaveResult
            {
                Ok = false,
                Message = "Native save/load is only available in Native C# GameCore.",
            };
        }

        EnsureNativeGameCoreServices();
        var saveService = GetNativeGameCoreSaveService();
        var saveResult = saveService.Save(_nativeGameCoreContext, saveName);
        if (!saveResult.Ok)
        {
            SetPrimaryStatus("Unable to save native game.");
            SetDebugOutputStatus("Native save failed.");
            SetStateDumpText(saveResult.Message);
            return saveResult;
        }

        if (autosaveToo)
        {
            var autosaveResult = saveService.Save(_nativeGameCoreContext);
            if (!autosaveResult.Ok)
            {
                SetPrimaryStatus("Unable to save native game.");
                SetDebugOutputStatus("Native autosave failed.");
                SetStateDumpText(autosaveResult.Message);
                return autosaveResult;
            }
        }

        var statusMessage = string.IsNullOrWhiteSpace(successMessage) ? saveResult.Message : successMessage;
        SetPrimaryStatus(statusMessage);
        SetDebugOutputStatus(statusMessage);
        SetStateDumpText(statusMessage);
        await Task.CompletedTask;
        return saveResult;
    }

    private async Task<bool> SaveNativeAutosave(string successMessage)
    {
        var result = await SaveCurrentNativeGame(null, successMessage, autosaveToo: false);
        return result.Ok;
    }

    private async Task SaveNativeGame()
    {
        if (_btnSaveNativeGame != null)
            _btnSaveNativeGame.Disabled = true;
        if (_btnSaveGame != null)
            _btnSaveGame.Disabled = true;

        try
        {
            await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Native game saved.", autosaveToo: true);
        }
        finally
        {
            UpdateNativeSaveLoadButtons();
        }
    }

    private async Task LoadNativeGame()
    {
        if (_btnLoadNativeGame != null)
            _btnLoadNativeGame.Disabled = true;
        if (_btnStartupLoadGame != null)
            _btnStartupLoadGame.Disabled = true;

        try
        {
            await LoadNativeGameInternal(preferNamedSave: true, successMessage: "Native game loaded.");
        }
        finally
        {
            UpdateNativeSaveLoadButtons();
            RefreshStartupPanelButtons();
        }
    }

    private async Task ContinueNativeStartup()
    {
        if (!IsNativeRuntimeSource())
            return;

        if (_btnStartupContinue != null)
            _btnStartupContinue.Disabled = true;

        try
        {
            await LoadNativeGameInternal(preferNamedSave: false, successMessage: "Loaded native save.");
        }
        finally
        {
            RefreshStartupPanelButtons();
        }
    }

    private async Task LoadNativeGameInternal(bool preferNamedSave, string successMessage)
    {
        EnsureNativeGameCoreServices();

        GameCoreLoadResult loadResult;
        if (preferNamedSave)
        {
            loadResult = GetNativeGameCoreSaveService().Load(GameCoreSaveService.NamedSaveFileName);
            if (loadResult.SaveMissing)
                loadResult = GetNativeGameCoreSaveService().Load();
        }
        else
        {
            loadResult = GetNativeGameCoreSaveService().Load();
        }

        if (!loadResult.Ok || loadResult.League == null)
        {
            _nativeStartupState = loadResult.SaveMissing
                ? NativeStartupState.MissingAutosave
                : NativeStartupState.CorruptAutosave;
            _nativeGameCoreContext.ActiveLeague = null;
            SetPrimaryStatus(loadResult.SaveMissing ? "No native save found." : "Unable to load native save.");
            SetStateDumpText(loadResult.Message);
            ShowStartupPanel(loadResult);
            return;
        }

        _nativeGameCoreContext.ActiveLeague = loadResult.League;
        _nativeStartupState = NativeStartupState.Ready;
        ResetDashboardPreviewUiState();
        ResetClientCachesForNewGame();
        HideStartupPanel();
        _pendingNativeStatusMessage = successMessage;
        await RefreshAll();
        SetPrimaryStatus(successMessage);
    }

    private void RefreshStartupPanelButtons()
    {
        if (_startupPanel == null || !_startupPanel.Visible)
            return;

        var autosaveResult = _nativeStartupState == NativeStartupState.CorruptAutosave
            ? new GameCoreLoadResult { Ok = false, SaveMissing = false, Message = "Unable to load native save." }
            : null;
        ShowStartupPanel(autosaveResult);
    }

    private string ResolveNativeRosterDepthChartTeamId()
    {
        EnsureNativeGameCoreServices();

        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league?.Teams == null || league.Teams.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(_currentTeamId))
        {
            foreach (var team in league.Teams)
            {
                if (string.Equals(team.TeamId, _currentTeamId, StringComparison.OrdinalIgnoreCase))
                    return team.TeamId;
            }
        }

        if (!string.IsNullOrWhiteSpace(_userTeamId))
        {
            foreach (var team in league.Teams)
            {
                if (string.Equals(team.TeamId, _userTeamId, StringComparison.OrdinalIgnoreCase))
                    return team.TeamId;
            }
        }

        return null;
    }

    private void RefreshNativeRosterTab()
    {
        SetRosterSummaryPlaceholder();
        ShowRosterMessage("Loading roster...");

        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeRosterService.GetTeamRoster(ResolveNativeRosterDepthChartTeamId());
            if (response == null || !response.Ok)
            {
                ClearRosterTab(string.IsNullOrWhiteSpace(response?.Error) ? "Roster unavailable" : response.Error);
                return;
            }

            RenderRosterSnapshot(ConvertRosterResponseToPayload(response));
        }
        catch (Exception ex)
        {
            ClearRosterTab("Roster unavailable");
            SetPrimaryStatus($"Native C# roster failed: {InlineMessage(ex.Message)}");
        }
    }

    private void RefreshNativeDepthChartView()
    {
        SetDepthChartPlaceholder();

        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeDepthChartService.GetTeamDepthChart(ResolveNativeRosterDepthChartTeamId());
            if (response == null || !response.Ok)
            {
                ClearDepthChartView(string.IsNullOrWhiteSpace(response?.Error) ? "Depth chart unavailable" : response.Error);
                return;
            }

            RenderDepthChartSnapshot(ConvertDepthChartResponseToPayload(response));
        }
        catch (Exception ex)
        {
            ClearDepthChartView("Depth chart unavailable");
            SetPrimaryStatus($"Native C# depth chart failed: {InlineMessage(ex.Message)}");
        }
    }

    private Godot.Collections.Dictionary ConvertRosterResponseToPayload(TeamRosterResponse response)
    {
        var payload = new Godot.Collections.Dictionary
        {
            ["ok"] = response?.Ok ?? false,
            ["error"] = response?.Error ?? "",
            ["team"] = ConvertTeamIdentityToDictionary(response?.Team),
            ["roster_status"] = new Godot.Collections.Dictionary
            {
                ["is_valid"] = response?.RosterStatus?.IsValid ?? false,
                ["roster_size"] = response?.RosterStatus?.RosterSize ?? 0,
                ["roster_limit"] = response?.RosterStatus?.RosterLimit ?? 0,
                ["required_cuts"] = response?.RosterStatus?.RequiredCuts ?? 0,
                ["open_slots"] = response?.RosterStatus?.OpenSlots ?? 0,
                ["injured_count"] = response?.RosterStatus?.InjuredCount ?? 0,
                ["issues"] = ConvertStringListToArray(response?.RosterStatus?.Issues),
            },
            ["position_counts"] = new Godot.Collections.Array(),
            ["players"] = new Godot.Collections.Array(),
        };

        var positionCounts = (Godot.Collections.Array)payload["position_counts"];
        if (response?.PositionCounts != null)
        {
            foreach (var count in response.PositionCounts)
            {
                positionCounts.Add(new Godot.Collections.Dictionary
                {
                    ["position"] = count?.Position ?? "",
                    ["count"] = count?.Count ?? 0,
                });
            }
        }

        var players = (Godot.Collections.Array)payload["players"];
        if (response?.Players != null)
        {
            foreach (var player in response.Players)
            {
                players.Add(new Godot.Collections.Dictionary
                {
                    ["player_id"] = player?.PlayerId ?? "",
                    ["name"] = player?.Name ?? "",
                    ["position"] = player?.Position ?? "",
                    ["overall"] = player?.Overall ?? 0,
                    ["age"] = player?.Age ?? 0,
                    ["status"] = player?.Status ?? "",
                    ["injury"] = player?.Injury ?? "",
                    ["depth_role"] = player?.DepthRole ?? "",
                });
            }
        }

        return payload;
    }

    private Godot.Collections.Dictionary ConvertDepthChartResponseToPayload(TeamDepthChartResponse response)
    {
        var payload = new Godot.Collections.Dictionary
        {
            ["ok"] = response?.Ok ?? false,
            ["error"] = response?.Error ?? "",
            ["team"] = ConvertTeamIdentityToDictionary(response?.Team),
            ["depth_chart_status"] = new Godot.Collections.Dictionary
            {
                ["is_valid"] = response?.DepthChartStatus?.IsValid ?? false,
                ["issues"] = ConvertStringListToArray(response?.DepthChartStatus?.Issues),
            },
            ["positions"] = new Godot.Collections.Array(),
        };

        var positions = (Godot.Collections.Array)payload["positions"];
        if (response?.Positions != null)
        {
            foreach (var position in response.Positions)
            {
                var positionRow = new Godot.Collections.Dictionary
                {
                    ["position"] = position?.Position ?? "",
                    ["required_starters"] = position?.RequiredStarters ?? 0,
                    ["players"] = new Godot.Collections.Array(),
                };

                var players = (Godot.Collections.Array)positionRow["players"];
                if (position?.Players != null)
                {
                    foreach (var player in position.Players)
                    {
                        players.Add(new Godot.Collections.Dictionary
                        {
                            ["player_id"] = player?.PlayerId ?? "",
                            ["name"] = player?.Name ?? "",
                            ["overall"] = player?.Overall ?? 0,
                            ["role"] = player?.Role ?? "",
                            ["status"] = player?.Status ?? "",
                            ["injury"] = player?.Injury ?? "",
                        });
                    }
                }

                positions.Add(positionRow);
            }
        }

        return payload;
    }

    private void RefreshNativeStandingsView()
    {
        ShowStandingsMessage("Standings: loading...");

        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeStandingsService.GetStandings();
            if (response == null || !response.Ok)
            {
                ShowStandingsMessage(string.IsNullOrWhiteSpace(response?.Error) ? "Unable to load standings." : response.Error);
                return;
            }

            PopulateStandingsTree(ConvertStandingsResponseToArray(response));
        }
        catch (Exception ex)
        {
            ShowStandingsMessage("Unable to load standings.");
            SetPrimaryStatus($"Native C# standings failed: {InlineMessage(ex.Message)}");
        }
    }

    private void RefreshNativeScheduleView(string teamId, int selectionVersion = -1)
    {
        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;

        ShowScheduleMessage("Schedule: loading...");

        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeScheduleService.GetTeamSchedule(ResolveNativeScheduleTeamId(teamId));
            if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
                return;

            if (response == null || !response.Ok)
            {
                ShowScheduleMessage(string.IsNullOrWhiteSpace(response?.Error) ? "Unable to load schedule." : response.Error);
                return;
            }

            PopulateScheduleList(ConvertScheduleResponseToArray(response), teamId);
        }
        catch (Exception ex)
        {
            ShowScheduleMessage("Unable to load schedule.");
            SetPrimaryStatus($"Native C# schedule failed: {InlineMessage(ex.Message)}");
        }
    }

    private string ResolveNativeScheduleTeamId(string requestedTeamId)
    {
        EnsureNativeGameCoreServices();
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league?.Teams == null || league.Teams.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(requestedTeamId))
        {
            foreach (var team in league.Teams)
            {
                if (string.Equals(team.TeamId, requestedTeamId, StringComparison.OrdinalIgnoreCase))
                    return team.TeamId;
            }
        }

        return ResolveNativeRosterDepthChartTeamId();
    }

    private Godot.Collections.Array ConvertStandingsResponseToArray(StandingsResponse response)
    {
        var standings = new Godot.Collections.Array();
        if (response?.Standings == null)
            return standings;

        foreach (var row in response.Standings)
        {
            standings.Add(new Godot.Collections.Dictionary
            {
                ["team_id"] = row?.TeamId ?? "",
                ["abbreviation"] = row?.Abbreviation ?? "",
                ["team_name"] = row?.TeamName ?? "",
                ["wins"] = row?.Wins ?? 0,
                ["losses"] = row?.Losses ?? 0,
                ["ties"] = row?.Ties ?? 0,
                ["points_for"] = row?.PointsFor ?? 0,
                ["points_against"] = row?.PointsAgainst ?? 0,
                ["win_pct"] = row?.WinPct ?? 0d,
                ["division"] = row?.Division ?? "",
                ["conference"] = row?.Conference ?? "",
            });
        }

        return standings;
    }

    private Godot.Collections.Array ConvertScheduleResponseToArray(TeamScheduleResponse response)
    {
        var schedule = new Godot.Collections.Array();
        if (response?.Schedule == null)
            return schedule;

        foreach (var game in response.Schedule)
        {
            schedule.Add(new Godot.Collections.Dictionary
            {
                ["game_id"] = game?.GameId ?? "",
                ["week"] = game?.Week ?? 0,
                ["absolute_week"] = game?.AbsoluteWeek ?? 0,
                ["phase_week"] = game?.PhaseWeek ?? 0,
                ["phase"] = game?.Phase ?? "",
                ["display_week"] = game?.DisplayWeek ?? "",
                ["game_type"] = game?.GameType ?? "",
                ["week_label"] = game?.WeekLabel ?? "",
                ["opponent"] = game?.Opponent ?? "",
                ["home_away"] = game?.HomeAway ?? "",
                ["status"] = game?.Status ?? "",
                ["home_team"] = game?.HomeTeam ?? "",
                ["away_team"] = game?.AwayTeam ?? "",
                ["home_score"] = game?.HomeScore.HasValue == true ? game.HomeScore.Value : "",
                ["away_score"] = game?.AwayScore.HasValue == true ? game.AwayScore.Value : "",
                ["winner"] = game?.Winner ?? "",
            });
        }

        return schedule;
    }

    private static Godot.Collections.Dictionary ConvertTeamIdentityToDictionary(TeamIdentityDto team)
    {
        return new Godot.Collections.Dictionary
        {
            ["team_id"] = team?.TeamId ?? "",
            ["name"] = team?.Name ?? "",
            ["abbreviation"] = team?.Abbreviation ?? "",
        };
    }

    private static Godot.Collections.Array ConvertStringListToArray(IEnumerable<string> values)
    {
        var output = new Godot.Collections.Array();
        if (values == null)
            return output;

        foreach (var value in values)
            output.Add(value ?? "");

        return output;
    }

    private void ApplyNativeContinueStatus(ContinueResultDto result)
    {
        var stopReason = result?.StopReason ?? "";
        var daysAdvanced = result?.DaysAdvanced ?? 0;
        var message = string.IsNullOrWhiteSpace(stopReason)
            ? $"Advanced {daysAdvanced} day(s)."
            : $"Paused: {FormatContinueStopReason(stopReason)}";
        if (string.Equals(stopReason, "max_days_reached", StringComparison.OrdinalIgnoreCase))
            message += $" after {daysAdvanced} day(s).";

        _inboxEmptyDetailMessage = string.Equals(stopReason, "game_day", StringComparison.OrdinalIgnoreCase)
            ? "Game day reached."
            : "No urgent messages.";

        if (_continueStatus != null)
            _continueStatus.Text = message;
    }

    private Godot.Collections.Dictionary ConvertNativeGameDayState(GameDayStateDto game)
    {
        return new Godot.Collections.Dictionary
        {
            ["game_id"] = game?.GameId ?? "",
            ["week"] = game?.Week ?? 0,
            ["absolute_week"] = game?.AbsoluteWeek ?? 0,
            ["phase_week"] = game?.PhaseWeek ?? 0,
            ["phase"] = game?.Phase ?? "",
            ["game_type"] = game?.GameType ?? "",
            ["week_label"] = game?.WeekLabel ?? "",
            ["home_team"] = game?.HomeTeam ?? "",
            ["away_team"] = game?.AwayTeam ?? "",
            ["opponent"] = game?.Opponent ?? "",
            ["opponent_abbreviation"] = game?.OpponentAbbreviation ?? "",
            ["home_away"] = game?.HomeAway ?? "",
            ["status"] = game?.Status ?? "",
        };
    }

    private Godot.Collections.Dictionary BuildNativeGameResultDictionary(GameResultDto result)
    {
        var payload = new Godot.Collections.Dictionary
        {
            ["game_id"] = result?.GameId ?? "",
            ["week"] = result?.Week ?? 0,
            ["absolute_week"] = result?.AbsoluteWeek ?? 0,
            ["phase_week"] = result?.PhaseWeek ?? 0,
            ["phase"] = result?.Phase ?? "",
            ["game_type"] = result?.GameType ?? "",
            ["week_label"] = result?.WeekLabel ?? "",
            ["home_team"] = result?.HomeTeam ?? "",
            ["away_team"] = result?.AwayTeam ?? "",
            ["home_score"] = result?.HomeScore ?? 0,
            ["away_score"] = result?.AwayScore ?? 0,
            ["winner"] = result?.Winner ?? "",
            ["summary"] = result?.Summary ?? "",
        };

        var boxScore = new Godot.Collections.Dictionary
        {
            ["final"] = new Godot.Collections.Dictionary
            {
                ["away"] = result?.AwayScore ?? 0,
                ["home"] = result?.HomeScore ?? 0,
            },
            ["quarter_scores"] = new Godot.Collections.Dictionary
            {
                ["away"] = new Godot.Collections.Array { 0, 0, 0, result?.AwayScore ?? 0 },
                ["home"] = new Godot.Collections.Array { 0, 0, 0, result?.HomeScore ?? 0 },
            },
        };

        var teamStats = new Godot.Collections.Dictionary();
        if (result?.BoxScore != null)
        {
            foreach (var pair in result.BoxScore)
            {
                if (pair.Value is Dictionary<string, int> stats)
                {
                    foreach (var stat in stats)
                        teamStats[stat.Key] = stat.Value;
                }
                else if (pair.Value != null && string.Equals(pair.Key, "final", StringComparison.OrdinalIgnoreCase))
                {
                    boxScore["final_text"] = pair.Value.ToString() ?? "";
                }
            }
        }
        boxScore["team_stats"] = teamStats;
        payload["box_score"] = boxScore;
        return payload;
    }

    private bool TryShowNativeGameResult(string gameId, string fallbackError, string statusText)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            return false;

        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeGameDayService.GetGameResult(gameId);
            if (response?.Ok != true || response.Result == null)
                return false;

            var result = BuildNativeGameResultDictionary(response.Result);
            _gameCache[gameId] = result.Duplicate(true);
            ShowPostGameRecapFromResult(result, statusText);
            SetPrimaryStatus(statusText);
            return true;
        }
        catch (Exception ex)
        {
            SetPrimaryStatus($"{fallbackError} {InlineMessage(ex.Message)}");
            return false;
        }
    }

    private void RefreshNativeResultsView(string weekKey)
    {
        EnsureNativeGameCoreServices();
        var league = GetOrCreateNativeGameCoreContext().ActiveLeague;
        if (league == null)
        {
            ShowResultsMessage("Results unavailable.");
            return;
        }

        var weekKeys = new List<string>();
        _resultsWeekLabels.Clear();
        foreach (var result in league.Results)
        {
            var key = BuildNativeResultWeekKey(
                result.GameType,
                result.AbsoluteWeek > 0 ? result.AbsoluteWeek : result.Week);
            if (string.IsNullOrWhiteSpace(key) || weekKeys.Contains(key))
                continue;
            weekKeys.Add(key);
            _resultsWeekLabels[key] = string.IsNullOrWhiteSpace(result.WeekLabel)
                ? FormatWeekKeyHeader(key)
                : result.WeekLabel;
        }

        weekKeys.Sort(CompareWeekKeys);
        _availableResultsWeekKeys.Clear();
        _availableResultsWeekKeys.AddRange(weekKeys);
        _completedResultsWeekKeys.Clear();
        foreach (var key in weekKeys)
            _completedResultsWeekKeys.Add(key);

        var selectedKey = string.IsNullOrWhiteSpace(weekKey)
            ? GetPreferredResultsWeekKey(weekKeys, weekKeys)
            : weekKey;
        SetupResultsWeekOptions(weekKeys, selectedKey);

        var results = new Godot.Collections.Array();
        foreach (var result in league.Results)
        {
            var key = BuildNativeResultWeekKey(
                result.GameType,
                result.AbsoluteWeek > 0 ? result.AbsoluteWeek : result.Week);
            if (!string.IsNullOrWhiteSpace(selectedKey) && !string.Equals(key, selectedKey, StringComparison.OrdinalIgnoreCase))
                continue;
            var payload = BuildNativeGameResultDictionary(GameCoreStateHelper.ToGameResultDto(result));
            results.Add(payload);
            var gameId = FmtString(GetFirstNonNil(payload, "game_id"), "");
            if (!string.IsNullOrWhiteSpace(gameId))
                _gameCache[gameId] = payload.Duplicate(true);
        }

        PopulateResultsList(results);
    }

    private string BuildNativeResultWeekKey(string gameType, int week)
    {
        var season = (gameType ?? "").Trim().ToLowerInvariant();
        season = season switch
        {
            "preseason" => "preseason",
            "regular_season" => "regular",
            "playoffs" => "playoffs",
            "postseason" => "playoffs",
            _ => string.IsNullOrWhiteSpace(season) ? "regular" : season,
        };
        return $"{season}:{week}";
    }

    internal static int CompareWeekKeys(string left, string right)
    {
        (var leftSeasonRank, var leftWeek) = ParseWeekKeyParts(left);
        (var rightSeasonRank, var rightWeek) = ParseWeekKeyParts(right);
        var seasonCompare = leftSeasonRank.CompareTo(rightSeasonRank);
        if (seasonCompare != 0)
            return seasonCompare;
        return leftWeek.CompareTo(rightWeek);
    }

    private static (int seasonRank, int week) ParseWeekKeyParts(string weekKey)
    {
        if (string.IsNullOrWhiteSpace(weekKey))
            return (int.MaxValue, int.MaxValue);

        var parts = weekKey.Split(':', 2);
        var seasonRank = parts[0].Trim().ToLowerInvariant() switch
        {
            "preseason" => 0,
            "regular" => 1,
            "playoffs" => 2,
            _ => 3,
        };
        var week = 0;
        if (parts.Length == 2)
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out week);
        return (seasonRank, week);
    }

    private void RefreshNativeInjuryReport(string teamId, int selectionVersion)
    {
        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;

        ShowInjuriesMessage("Injury report: loading...");
        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeRosterService.GetTeamRoster(ResolveNativeScheduleTeamId(teamId));
            if (response == null || !response.Ok)
            {
                ShowInjuriesMessage(string.IsNullOrWhiteSpace(response?.Error) ? "Injury report unavailable." : response.Error);
                return;
            }

            var entries = new Godot.Collections.Array();
            if (response.Players != null)
            {
                foreach (var player in response.Players)
                {
                    if (string.IsNullOrWhiteSpace(player?.Injury))
                        continue;
                    entries.Add(new Godot.Collections.Dictionary
                    {
                        ["name"] = player?.Name ?? "",
                        ["position"] = player?.Position ?? "",
                        ["injury_status"] = player?.Status ?? "",
                        ["injury_name"] = player?.Injury ?? "",
                        ["return_date"] = "",
                        ["days_remaining"] = "",
                        ["ir"] = string.Equals(player?.Status, "ir", StringComparison.OrdinalIgnoreCase),
                    });
                }
            }

            PopulateInjuryTree(entries);
        }
        catch (Exception ex)
        {
            ShowInjuriesMessage("Injury report unavailable.");
            SetPrimaryStatus($"Native injuries failed: {InlineMessage(ex.Message)}");
        }
    }

    private async Task RefreshHistoryAsync()
    {
        if (IsNativeRuntimeSource())
        {
            RefreshNativeHistoryView();
            await Task.CompletedTask;
            return;
        }

        ShowHistoryMessage("League history is only available in native mode.");
        await Task.CompletedTask;
    }

    private void RefreshNativeHistoryView()
    {
        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeDashboardService.GetLeagueHistory();
            if (response == null || !response.Ok)
            {
                ShowHistoryMessage(string.IsNullOrWhiteSpace(response?.Error) ? "League history unavailable." : response.Error);
                return;
            }

            PopulateHistoryView(response.Seasons ?? new List<LeagueHistorySeasonDto>());
        }
        catch (Exception ex)
        {
            ShowHistoryMessage("League history unavailable.");
            SetPrimaryStatus($"Native league history failed: {InlineMessage(ex.Message)}");
        }
    }

    private void RenderRosterSnapshot(Godot.Collections.Dictionary payload)
    {
        if (_rosterTree == null)
            return;

        var team = TryExtractObject(payload, "team");
        var rosterStatus = TryExtractObject(payload, "roster_status");
        var positionCounts = TryExtractArray(payload, "position_counts");
        var players = TryExtractArray(payload, "players") ?? new Godot.Collections.Array();

        _currentTeamId = SafeString(team, "team_id", _currentTeamId);
        var teamAbbr = SafeString(team, "abbreviation", "");
        var teamName = SafeString(team, "name", "");
        var teamLabel = string.IsNullOrWhiteSpace(teamAbbr) && string.IsNullOrWhiteSpace(teamName)
            ? "-"
            : $"{teamAbbr} {teamName}".Trim();
        var rosterStatusLabel = GetBoolValue(GetFirstNonNil(rosterStatus, "is_valid"), true) ? "Valid" : "Invalid";
        var issueText = FormatRosterIssues(TryExtractArray(rosterStatus, "issues"));
        var positionCountText = FormatPositionCounts(positionCounts);

        if (_rosterSummary != null)
        {
            _rosterSummary.Text =
                $"Team: {teamLabel} | Status: {rosterStatusLabel} | Players: {SafeIntDisplay(rosterStatus, "roster_size")}/{SafeIntDisplay(rosterStatus, "roster_limit")} | Cuts: {SafeIntDisplay(rosterStatus, "required_cuts")} | Injured: {SafeIntDisplay(rosterStatus, "injured_count")}";
            if (!string.IsNullOrWhiteSpace(issueText))
                _rosterSummary.Text += $"\nIssues: {issueText}";
            if (!string.IsNullOrWhiteSpace(positionCountText))
                _rosterSummary.Text += $"\nPositions: {positionCountText}";
        }

        _currentRoster = players;
        BuildPlayerDetailsMap(players);
        ConfigureRosterTreeForCompactView();
        SetReportPlaceholder("Select a player to view roster details.");
    }

    private void ClearRosterTab(string message)
    {
        _currentRoster = new Godot.Collections.Array();
        _playerDetailsById.Clear();
        SetRosterSummaryPlaceholder();
        ShowRosterMessage(message);
        SetReportPlaceholder(message);
    }

    private void SetRosterSummaryPlaceholder()
    {
        if (_rosterSummary != null)
        {
            _rosterSummary.Text = "Team: - | Status: - | Players: -/- | Cuts: - | Injured: -";
        }
    }

    private void SetDepthChartPlaceholder()
    {
        if (_depthChartSummary != null)
            _depthChartSummary.Text = "Team: - | Depth Chart: loading...";
        SetDepthChartActionStatus("");
        SetDepthChartRequestBusy(false);
        if (_depthChartTree != null)
        {
            _depthChartTree.Clear();
            _depthChartTree.HideRoot = true;
            _depthChartTree.ColumnTitlesVisible = true;
            _depthChartTree.Columns = 6;
            _depthChartTree.SetColumnTitle(0, "#");
            _depthChartTree.SetColumnTitle(1, "Name");
            _depthChartTree.SetColumnTitle(2, "OVR");
            _depthChartTree.SetColumnTitle(3, "Role");
            _depthChartTree.SetColumnTitle(4, "Status");
            _depthChartTree.SetColumnTitle(5, "Injury");
            var root = _depthChartTree.CreateItem();
            var item = _depthChartTree.CreateItem(root);
            item.SetText(1, "Loading depth chart...");
        }
    }

    private void ClearDepthChartView(string message)
    {
        ClearDepthChartSelection();
        if (_depthChartSummary != null)
            _depthChartSummary.Text = $"Team: - | Depth Chart: {message}";
        SetDepthChartActionStatus("");
        SetDepthChartRequestBusy(false);
        if (_depthChartTree != null)
        {
            _depthChartTree.Clear();
            _depthChartTree.HideRoot = true;
            _depthChartTree.ColumnTitlesVisible = true;
            _depthChartTree.Columns = 6;
            _depthChartTree.SetColumnTitle(0, "#");
            _depthChartTree.SetColumnTitle(1, "Name");
            _depthChartTree.SetColumnTitle(2, "OVR");
            _depthChartTree.SetColumnTitle(3, "Role");
            _depthChartTree.SetColumnTitle(4, "Status");
            _depthChartTree.SetColumnTitle(5, "Injury");
            var root = _depthChartTree.CreateItem();
            var item = _depthChartTree.CreateItem(root);
            item.SetText(1, message);
        }
    }

    private void RenderDepthChartSnapshot(Godot.Collections.Dictionary payload)
    {
        var team = TryExtractObject(payload, "team");
        var status = TryExtractObject(payload, "depth_chart_status", "depthChartStatus");
        var positions = TryExtractArray(payload, "positions") ?? new Godot.Collections.Array();
        _currentTeamId = SafeString(team, "team_id", _currentTeamId);
        var teamAbbr = SafeString(team, "abbreviation", "");
        var teamName = SafeString(team, "name", "");
        var teamLabel = string.IsNullOrWhiteSpace(teamAbbr) && string.IsNullOrWhiteSpace(teamName)
            ? "-"
            : $"{teamAbbr} {teamName}".Trim();
        var valid = GetBoolValue(GetFirstNonNil(status, "is_valid", "isValid"), true);
        var issues = TryExtractArray(status, "issues");
        var issueText = FormatRosterIssues(issues);

        if (_depthChartSummary != null)
        {
            _depthChartSummary.Text = $"Team: {teamLabel} | Depth Chart: {(valid ? "Valid" : "Invalid")}";
            if (!valid)
                _depthChartSummary.Text += $"\nIssues: {(string.IsNullOrWhiteSpace(issueText) ? "Needs attention." : issueText)}";
        }

        if (_depthChartTree == null)
            return;

        _depthChartTree.Clear();
        _depthChartTree.HideRoot = true;
        _depthChartTree.ColumnTitlesVisible = true;
        _depthChartTree.Columns = 6;
        _depthChartTree.SelectMode = Tree.SelectModeEnum.Row;
        _depthChartTree.SetColumnTitle(0, "#");
        _depthChartTree.SetColumnTitle(1, "Name");
        _depthChartTree.SetColumnTitle(2, "OVR");
        _depthChartTree.SetColumnTitle(3, "Role");
        _depthChartTree.SetColumnTitle(4, "Status");
        _depthChartTree.SetColumnTitle(5, "Injury");

        var root = _depthChartTree.CreateItem();
        var selectedStillExists = false;
        for (var i = 0; i < positions.Count; i++)
        {
            if (!TryGetDictionary((Variant)positions[i], out var positionRow))
                continue;

            var position = SafeString(positionRow, "position", "UNK");
            var requiredStarters = SafeIntDisplay(positionRow, "required_starters", "requiredStarters", fallback: "0");
            var header = _depthChartTree.CreateItem(root);
            header.SetText(1, $"{position} (Starters: {requiredStarters})");
            for (var column = 0; column < 6; column++)
                header.SetSelectable(column, false);

            var players = TryExtractArray(positionRow, "players") ?? new Godot.Collections.Array();
            if (players.Count == 0)
            {
                var emptyItem = _depthChartTree.CreateItem(header);
                emptyItem.SetText(1, "No players available");
                continue;
            }

            for (var playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                if (!TryGetDictionary((Variant)players[playerIndex], out var player))
                    continue;

                var row = _depthChartTree.CreateItem(header);
                var playerName = SafeString(player, "name", "Unknown Player");
                var playerId = SafeString(player, "player_id", "");
                row.SetText(0, (playerIndex + 1).ToString(CultureInfo.InvariantCulture));
                row.SetText(1, playerName);
                row.SetText(2, SafeIntDisplay(player, "overall", fallback: "-"));
                row.SetText(3, SafeString(player, "role", "Backup"));
                row.SetText(4, HumanizeStatus(SafeString(player, "status", "active")));
                row.SetText(5, SafeString(player, "injury", "Healthy"));
                row.SetMetadata(
                    0,
                    new Godot.Collections.Dictionary
                    {
                        ["position"] = position,
                        ["player_id"] = playerId,
                        ["name"] = playerName,
                    }
                );
                if (
                    !string.IsNullOrWhiteSpace(playerId)
                    && string.Equals(playerId, _selectedDepthChartPlayerId, StringComparison.Ordinal)
                )
                {
                    row.Select(0);
                    selectedStillExists = true;
                    _selectedDepthChartPosition = position;
                    _selectedDepthChartPlayerName = playerName;
                }
            }
        }

        if (!selectedStillExists)
            ClearDepthChartSelection();
        else
            UpdateDepthChartSelectionLabel();
    }

    private void SetDepthChartRequestBusy(bool isBusy, string autoFillButtonText = null)
    {
        _depthChartRequestBusy = isBusy;
        UpdateDepthChartEditButtons();

        if (_btnAutoFillDepthChart == null)
            return;

        _btnAutoFillDepthChart.Disabled = isBusy;
        _btnAutoFillDepthChart.Text = isBusy && !string.IsNullOrWhiteSpace(autoFillButtonText)
            ? autoFillButtonText
            : "Auto-Fill Depth Chart";
    }

    private void SetDepthChartActionStatus(string message)
    {
        if (_depthChartActionStatus == null)
            return;

        _depthChartActionStatus.Text = string.IsNullOrWhiteSpace(message) ? "" : message;
    }

    private void ClearDepthChartSelection()
    {
        _selectedDepthChartPosition = "";
        _selectedDepthChartPlayerId = "";
        _selectedDepthChartPlayerName = "";
        UpdateDepthChartSelectionLabel();
        UpdateDepthChartEditButtons();
    }

    private void UpdateDepthChartSelectionLabel()
    {
        if (_depthChartSelectionStatus == null)
            return;

        _depthChartSelectionStatus.Text =
            string.IsNullOrWhiteSpace(_selectedDepthChartPlayerId) || string.IsNullOrWhiteSpace(_selectedDepthChartPosition)
                ? "Selected: none"
                : $"Selected: {_selectedDepthChartPosition} - {_selectedDepthChartPlayerName}";
    }

    private void UpdateDepthChartEditButtons()
    {
        var canEdit =
            !_depthChartRequestBusy
            && !string.IsNullOrWhiteSpace(_selectedDepthChartPosition)
            && !string.IsNullOrWhiteSpace(_selectedDepthChartPlayerId);

        if (_btnDepthChartMoveUp != null)
            _btnDepthChartMoveUp.Disabled = !canEdit;
        if (_btnDepthChartMoveDown != null)
            _btnDepthChartMoveDown.Disabled = !canEdit;
        if (_btnDepthChartSetStarter != null)
            _btnDepthChartSetStarter.Disabled = !canEdit;
    }

    private void OnDepthChartItemSelected(TreeItem selected)
    {
        if (selected == null)
        {
            ClearDepthChartSelection();
            return;
        }

        var metadata = selected.GetMetadata(0);
        if (!TryGetDictionary(metadata, out var rowData))
        {
            ClearDepthChartSelection();
            return;
        }

        var position = SafeString(rowData, "position", "");
        var playerId = SafeString(rowData, "player_id", "");
        if (string.IsNullOrWhiteSpace(position) || string.IsNullOrWhiteSpace(playerId))
        {
            ClearDepthChartSelection();
            return;
        }

        _selectedDepthChartPosition = position;
        _selectedDepthChartPlayerId = playerId;
        _selectedDepthChartPlayerName = SafeString(rowData, "name", "Unknown Player");
        UpdateDepthChartSelectionLabel();
        UpdateDepthChartEditButtons();
    }

    private async Task UpdateDepthChart(string action)
    {
        if (_depthChartRequestBusy)
            return;

        if (string.IsNullOrWhiteSpace(_selectedDepthChartPosition) || string.IsNullOrWhiteSpace(_selectedDepthChartPlayerId))
        {
            const string selectMessage = "Select a depth chart player first.";
            SetDepthChartActionStatus(selectMessage);
            SetPrimaryStatus(selectMessage);
            UpdateDepthChartEditButtons();
            return;
        }

        SetDepthChartRequestBusy(true);
        SetDepthChartActionStatus("Updating depth chart...");

        if (IsNativeRuntimeSource())
        {
            try
            {
                EnsureNativeGameCoreServices();
                var response = _nativeDepthChartService.UpdateDepthChart(
                    action,
                    _selectedDepthChartPosition,
                    _selectedDepthChartPlayerId,
                    ResolveNativeRosterDepthChartTeamId());

                if (response == null || !response.Ok)
                {
                    var error = string.IsNullOrWhiteSpace(response?.Error) ? "Unable to update depth chart." : response.Error;
                    SetDepthChartActionStatus(error);
                    SetPrimaryStatus(error);
                    return;
                }

                RenderDepthChartSnapshot(ConvertDepthChartResponseToPayload(response));
                await SaveNativeAutosave("Native autosave updated.");
                await RefreshDashboardState();
                await RefreshInbox();
                await RefreshLeagueHub();
                _dashboardRefreshPendingFromDepthChartEdit = false;
                SetDepthChartActionStatus("Depth chart updated.");
                SetPrimaryStatus("Depth chart updated.");
            }
            catch (Exception ex)
            {
                var nativeError = $"Native C# depth chart update failed: {InlineMessage(ex.Message)}";
                SetDepthChartActionStatus("Unable to update depth chart.");
                SetPrimaryStatus(nativeError);
            }
            finally
            {
                SetDepthChartRequestBusy(false);
            }

            return;
        }

        var request = new Godot.Collections.Dictionary
        {
            ["position"] = _selectedDepthChartPosition,
            ["player_id"] = _selectedDepthChartPlayerId,
            ["action"] = action,
        };
        if (!string.IsNullOrWhiteSpace(_currentTeamId))
            request["team_id"] = _currentTeamId;

        var (status, body) = await PostWithTimeoutAsync("/update_depth_chart", Json.Stringify(request), REQUEST_TIMEOUT_MS);
        if (status < 200 || status >= 300)
        {
            var summary = SummarizeRequestError("/update_depth_chart", status, body);
            SetDepthChartActionStatus("Unable to update depth chart.");
            SetPrimaryStatus(summary);
            SetStateDumpText(body);
            SetDepthChartRequestBusy(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            SetDepthChartActionStatus("Unable to update depth chart.");
            SetPrimaryStatus("Unable to update depth chart.");
            SetDepthChartRequestBusy(false);
            return;
        }

        var parsed = Json.ParseString(body);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            SetDepthChartActionStatus("Unable to update depth chart.");
            SetPrimaryStatus("Unable to update depth chart.");
            SetDepthChartRequestBusy(false);
            return;
        }

        var payload = parsed.AsGodotDictionary();
        var ok = GetBoolValue(GetFirstNonNil(payload, "ok", "success"), false);
        if (!ok)
        {
            var error = FmtString(GetFirstNonNil(payload, "error", "message", "detail"), "Unable to update depth chart.");
            var cleanError = string.IsNullOrWhiteSpace(error) ? "Unable to update depth chart." : error;
            SetDepthChartActionStatus(cleanError);
            SetPrimaryStatus(cleanError);
            SetDepthChartRequestBusy(false);
            return;
        }

        var message = FmtString(GetFirstNonNil(payload, "message"), "Depth chart updated.");
        var depthChart = TryExtractObject(payload, "depth_chart", "depthChart");
        if (depthChart != null)
            RenderDepthChartSnapshot(depthChart);
        else
            await RefreshDepthChartView();

        _dashboardRefreshPendingFromDepthChartEdit = true;
        SetDepthChartActionStatus(message);
        SetPrimaryStatus(message);
        SetDepthChartRequestBusy(false);
    }

    private void ConfigureRosterTreeForCompactView()
    {
        if (_rosterTree == null)
            return;

        _rosterTree.HideRoot = true;
        _rosterTree.ColumnTitlesVisible = true;
        _rosterTree.SelectMode = Tree.SelectModeEnum.Row;
        ApplyColumnVisibility();
    }

    private string FormatPlayerRow(Godot.Collections.Dictionary player)
    {
        var jersey = SafeIntDisplay(player, "jersey_number", "jersey", fallback: "-");
        var name = SafeString(player, new[] { "name", "player_name", "full_name" }, "Unknown Player");
        var age = SafeIntDisplay(player, "age", fallback: "-");
        var overall = SafeIntDisplay(player, "overall", "ovr", fallback: "-");
        var pot = SafeIntDisplay(player, "pot", "potential", "pot_rating", fallback: "-");
        var status = FormatCompactPlayerStatus(player);
        return $"#{jersey} {name} - Age {age} - OVR {overall} - POT {pot} - {status}";
    }

    private static string FormatRosterIssues(Godot.Collections.Array issues)
    {
        if (issues == null || issues.Count == 0)
            return "";

        var parts = new List<string>();
        for (var i = 0; i < issues.Count; i++)
        {
            var text = FmtString((Variant)issues[i], "").Trim();
            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(text);
        }

        return string.Join(" ", parts);
    }

    private static string FormatPositionCounts(Godot.Collections.Array positionCounts)
    {
        if (positionCounts == null || positionCounts.Count == 0)
            return "";

        var sortedRows = new List<Godot.Collections.Dictionary>();
        for (var i = 0; i < positionCounts.Count; i++)
        {
            if (TryGetDictionary((Variant)positionCounts[i], out var row))
                sortedRows.Add(row);
        }

        sortedRows.Sort((left, right) => FootballPositionOrder.Compare(
            SafeString(left, "position", ""),
            SafeString(right, "position", "")));

        var parts = new List<string>();
        for (var i = 0; i < sortedRows.Count; i++)
        {
            var row = sortedRows[i];
            var position = SafeString(row, "position", "");
            var count = SafeIntDisplay(row, "count");
            if (!string.IsNullOrWhiteSpace(position))
                parts.Add($"{position} {count}");
        }

        return string.Join(" | ", parts);
    }

    private static string FormatCompactPlayerStatus(Godot.Collections.Dictionary player)
    {
        if (player == null)
            return "Healthy";

        if (GetBoolValue(GetFirstNonNil(player, "on_injured_reserve", "ir"), false))
            return "IR";

        var injury = FmtString(GetFirstNonNil(player, "injury_status", "status", "injury"), "healthy").Trim();
        if (string.IsNullOrWhiteSpace(injury))
            injury = "healthy";

        var bucket = FmtString(GetFirstNonNil(player, "roster_bucket"), "").Trim();
        if (string.Equals(bucket, "practice_squad", StringComparison.OrdinalIgnoreCase))
            return $"{HumanizeStatus(injury)} (PS)";

        return HumanizeStatus(injury);
    }

    private static string HumanizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "Healthy";
        if (string.Equals(status, "ir", StringComparison.OrdinalIgnoreCase))
            return "IR";

        var normalized = status.Replace('_', ' ').Trim().ToLowerInvariant();
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    private static string SafeString(Godot.Collections.Dictionary dict, string key, string fallback = "-")
        => SafeString(dict, new[] { key }, fallback);

    private static string SafeString(Godot.Collections.Dictionary dict, IEnumerable<string> keys, string fallback = "-")
    {
        if (dict == null)
            return fallback;

        Variant value = default;
        foreach (var key in keys)
        {
            value = GetFirstNonNil(dict, key);
            if (!IsNil(value))
                break;
        }

        var text = FmtString(value, "").Trim();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static string SafeIntDisplay(Godot.Collections.Dictionary dict, string key, string fallback = "-")
        => SafeIntDisplay(dict, new[] { key }, fallback);

    private static string SafeIntDisplay(Godot.Collections.Dictionary dict, string key, string alternateKey, string fallback = "-")
        => SafeIntDisplay(dict, new[] { key, alternateKey }, fallback);

    private static string SafeIntDisplay(Godot.Collections.Dictionary dict, string key, string alternateKey, string thirdKey, string fallback = "-")
        => SafeIntDisplay(dict, new[] { key, alternateKey, thirdKey }, fallback);

    private static string SafeIntDisplay(Godot.Collections.Dictionary dict, IEnumerable<string> keys, string fallback = "-")
    {
        if (dict == null)
            return fallback;

        Variant value = default;
        foreach (var key in keys)
        {
            value = GetFirstNonNil(dict, key);
            if (!IsNil(value))
                break;
        }

        if (IsNil(value))
            return fallback;

        var parsed = GetIntValue(value, int.MinValue);
        return parsed == int.MinValue ? fallback : parsed.ToString(CultureInfo.InvariantCulture);
    }

    private async Task ContinueUntilPause()
    {
        if (IsNativeRuntimeSource())
        {
            SetContinueButtonBusy(true);
            SetPrimaryStatus("Simulating season...");
            try
            {
                EnsureNativeGameCoreServices();
                var response = _nativeContinueService.Continue(CONTINUE_MAX_DAYS);
                if (response == null || !response.Ok)
                {
                    var error = string.IsNullOrWhiteSpace(response?.Error) ? "Continue failed." : response.Error;
                    SetPrimaryStatus(error);
                    return;
                }

                ApplyNativeContinueStatus(response.Result);
                if (response.Result != null && response.Result.Advanced)
                    await SaveNativeAutosave("Native autosave updated.");
                await RefreshDashboardState();
                await RefreshStateSummary();
                await RefreshInbox();
                await RefreshLeagueHub();
            }
            catch (Exception ex)
            {
                SetPrimaryStatus($"Native continue failed: {InlineMessage(ex.Message)}");
            }
            finally
            {
                SetContinueButtonBusy(false);
            }
            return;
        }

        SetContinueButtonBusy(true);
        SetPrimaryStatus("Simulating season...");
        var payload = new Godot.Collections.Dictionary
        {
            { "max_days", CONTINUE_MAX_DAYS }
        };
        var json = Json.Stringify(payload);
        var (status, body) = await PostWithTimeoutAsync("/continue", json, REQUEST_TIMEOUT_MS);
        SetContinueButtonBusy(false);

        if (status < 200 || status >= 300)
        {
            SetStateDumpText(body);
            SetPrimaryStatus($"Continue failed (HTTP {status}).");
            return;
        }

        if (!UpdateContinueStatus(body))
            return;
        await RefreshDashboardState();
        await RefreshStateSummary();
        await RefreshInbox();
        await RefreshLeagueHub();
    }

    private void SetupSimUntilOptions()
    {
        if (_simUntilSelect == null)
            return;

        _simUntilSelect.Clear();
        var nativeMilestones = new[] { 1, 5, LeagueBootstrapService.RegularSeasonWeeks };
        var addedWeeks = new HashSet<int>();
        foreach (var milestone in nativeMilestones)
        {
            if (milestone <= 0 || !addedWeeks.Add(milestone))
                continue;
            _simUntilSelect.AddItem($"Regular Season Week {milestone}", milestone);
        }
        _simUntilSelect.AddItem("Playoffs", 1001);
        _simUntilSelect.AddItem("Offseason Pending", 1002);
        _simUntilSelect.AddItem("Free Agency Pending", 1003);
        _simUntilSelect.AddItem("Draft Pending", 1004);
        _simUntilSelect.AddItem("Training Camp Pending", 1005);
        if (_simUntilSelect.ItemCount > 0)
            _simUntilSelect.Select(0);
    }

    private Godot.Collections.Dictionary BuildSimUntilPayload()
    {
        var selectedId = _simUntilSelect != null ? _simUntilSelect.GetSelectedId() : 1;
        if (selectedId == 1001)
        {
            return new Godot.Collections.Dictionary
            {
                { "target_type", "playoffs_start" }
            };
        }
        if (selectedId == 1002)
        {
            return new Godot.Collections.Dictionary
            {
                { "target_type", "offseason_start" }
            };
        }
        if (selectedId == 1003)
        {
            return new Godot.Collections.Dictionary
            {
                { "target_type", "free_agency" }
            };
        }
        if (selectedId == 1004)
        {
            return new Godot.Collections.Dictionary
            {
                { "target_type", "draft" }
            };
        }
        if (selectedId == 1005)
        {
            return new Godot.Collections.Dictionary
            {
                { "target_type", "training_camp" }
            };
        }
        return new Godot.Collections.Dictionary
        {
            { "target_type", "regular_season_week" },
            { "target_week", selectedId }
        };
    }

    private async Task SimUntilSelectedMilestone()
    {
        if (_btnSimUntil != null)
            _btnSimUntil.Disabled = true;
        SetPrimaryStatus("Simulating to selected milestone...");

        if (IsNativeRuntimeSource())
        {
            try
            {
                await RunNativeSimUntilSelectedMilestone();
            }
            finally
            {
                if (_btnSimUntil != null)
                    _btnSimUntil.Disabled = false;
            }

            return;
        }

        var json = Json.Stringify(BuildSimUntilPayload());
        var (status, body) = await PostWithTimeoutAsync("/sim_until", json, SIM_UNTIL_TIMEOUT_MS);

        if (_btnSimUntil != null)
            _btnSimUntil.Disabled = false;

        if (status < 200 || status >= 300)
        {
            SetStateDumpText(body);
            SetPrimaryStatus($"Sim Until failed (HTTP {status}).");
            return;
        }

        UpdateSimUntilStatus(body);
        await RefreshStateSummary();
        await RefreshInbox();
        await RefreshLeagueHub();
    }

    private async Task RunNativeSimUntilSelectedMilestone()
    {
        EnsureNativeGameCoreServices();
        var selectedId = _simUntilSelect != null ? _simUntilSelect.GetSelectedId() : 1;
        var league = GetOrCreateNativeGameCoreContext().ActiveLeague;
        if (league == null)
        {
            SetPrimaryStatus("No active league loaded.");
            return;
        }

        var resultsBefore = league.Results?.Count ?? 0;
        var (targetType, targetWeek) = GetNativeSimUntilTarget(selectedId);
        var response = _nativeContinueService.ContinueUntil(targetType, targetWeek, CONTINUE_MAX_DAYS, maxIterations: 256);
        if (response == null || !response.Ok)
        {
            var error = string.IsNullOrWhiteSpace(response?.Error) ? "Sim Until failed." : response.Error;
            SetPrimaryStatus(error);
            return;
        }

        var lastResult = response.Result;
        if (lastResult != null && lastResult.Advanced)
            await SaveNativeAutosave("Native autosave updated.");

        ApplyNativeContinueStatus(lastResult);
        await RefreshDashboardState();
        await RefreshStateSummary();
        await RefreshInbox();
        await RefreshLeagueHub();

        var gamesSimulated = lastResult != null && lastResult.GamesSimulated > 0
            ? lastResult.GamesSimulated
            : Math.Max(0, (league.Results?.Count ?? 0) - resultsBefore);
        var targetLabel = GetNativeSimUntilTargetLabel(selectedId);
        var stopReason = lastResult?.StopReason ?? "";
        if (string.Equals(stopReason, "reached_requested_week", StringComparison.OrdinalIgnoreCase)
            || string.Equals(stopReason, "reached_playoffs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(stopReason, "reached_offseason", StringComparison.OrdinalIgnoreCase)
            || string.Equals(stopReason, "reached_free_agency", StringComparison.OrdinalIgnoreCase)
            || string.Equals(stopReason, "reached_draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(stopReason, "reached_training_camp", StringComparison.OrdinalIgnoreCase))
        {
            SetPrimaryStatus($"{targetLabel} reached ({gamesSimulated} games, {lastResult?.WeeksAdvanced ?? 0} weeks).");
            return;
        }

        if (string.Equals(stopReason, "postseason_pending", StringComparison.OrdinalIgnoreCase))
        {
            SetPrimaryStatus($"Paused at postseason pending ({gamesSimulated} games).");
            return;
        }

        if (lastResult != null && !string.IsNullOrWhiteSpace(stopReason))
        {
            SetPrimaryStatus($"Paused: {FormatContinueStopReason(stopReason)}");
            return;
        }

        SetPrimaryStatus($"Sim Until complete ({gamesSimulated} games).");
    }

    private static (string TargetType, int TargetWeek) GetNativeSimUntilTarget(int selectedId)
    {
        return selectedId switch
        {
            1001 => ("playoffs_start", 0),
            1002 => ("offseason_start", 0),
            1003 => ("free_agency", 0),
            1004 => ("draft", 0),
            1005 => ("training_camp", 0),
            _ => ("regular_season_week", selectedId),
        };
    }

    private bool HasReachedNativeSimUntilTarget(GridironGM.GameCore.Models.LeagueState league, int selectedId)
    {
        if (league?.Calendar == null)
            return false;

        if (selectedId == 1001)
            return string.Equals(ScheduleService.GetPhaseForWeek(league.Calendar.Week), ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ScheduleService.GetPhaseForWeek(league.Calendar.Week), ScheduleService.SeasonCompletePhase, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ScheduleService.GetPhaseForWeek(league.Calendar.Week), ScheduleService.OffseasonPendingPhase, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ScheduleService.GetPhaseForWeek(league.Calendar.Week), "Offseason", StringComparison.OrdinalIgnoreCase);
        if (selectedId == 1002)
            return ScheduleService.IsOffseasonPlaceholderPhase(ScheduleService.GetPhaseForWeek(league.Calendar.Week))
                || string.Equals(ScheduleService.GetPhaseForWeek(league.Calendar.Week), "Offseason", StringComparison.OrdinalIgnoreCase);
        if (selectedId == 1003)
            return HasReachedOffseasonPlaceholderTarget(league, ScheduleService.FreeAgencyPendingPhase);
        if (selectedId == 1004)
            return HasReachedOffseasonPlaceholderTarget(league, ScheduleService.DraftPendingPhase);
        if (selectedId == 1005)
            return HasReachedOffseasonPlaceholderTarget(league, ScheduleService.TrainingCampPendingPhase);

        var phase = ScheduleService.GetPhaseForWeek(league.Calendar.Week);
        var phaseWeek = ScheduleService.GetPhaseWeek(league.Calendar.Week);
        if (!string.Equals(phase, "Regular Season", StringComparison.OrdinalIgnoreCase))
            return false;

        return phaseWeek >= selectedId;
    }

    private static string GetNativeSimUntilTargetLabel(int selectedId)
    {
        return selectedId switch
        {
            1001 => "Playoffs",
            1002 => "Offseason Pending",
            1003 => "Free Agency Pending",
            1004 => "Draft Pending",
            1005 => "Training Camp Pending",
            _ => $"Regular Season Week {selectedId}",
        };
    }

    private void UpdateSimUntilStatus(string body)
    {
        var parsed = Json.ParseString(body);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            SetPrimaryStatus("Sim Until complete.");
            return;
        }

        var dict = parsed.AsGodotDictionary();
        var ok = GetBoolValue(GetFirstNonNil(dict, "ok"), false);
        if (!ok)
        {
            var error = FmtString(GetFirstNonNil(dict, "error"), "failed");
            SetPrimaryStatus(CleanStatusMessage(error, "Sim Until failed."));
            SetStateDumpText(body);
            return;
        }

        var games = GetIntValue(GetFirstNonNil(dict, "games_simulated", "gamesSimulated"), 0);
        var stoppedAt = TryExtractObject(dict, "stopped_at", "stoppedAt");
        var label = stoppedAt != null
            ? FmtString(GetFirstNonNil(stoppedAt, "week_label", "weekLabel"), "")
            : "";
        SetPrimaryStatus(string.IsNullOrWhiteSpace(label)
            ? $"Sim Until complete ({games} games)."
            : $"{label} reached ({games} games).");
    }

    private async Task RefreshInbox()
    {
        UpdateInboxList();
        await Task.CompletedTask;
    }

    private async Task RefreshLeagueHub()
    {
        await RefreshStandingsAsync();
        await RefreshResultsAsync(GetSelectedResultsWeekKey());
        await RefreshScheduleAsync(_currentTeamId);
        await RefreshInjuryReportAsync(_currentTeamId);
        await RefreshHistoryAsync();
    }

    private async Task RefreshStandingsAsync()
    {
        if (IsNativeRuntimeSource())
        {
            RefreshNativeStandingsView();
            return;
        }

        ShowStandingsMessage("Standings: loading...");
        var (status, body) = await GetWithTimeoutAsync("/standings", REQUEST_TIMEOUT_MS);
        if (status < 200 || status >= 300)
        {
            var summary = SummarizeRequestError("/standings", status, body);
            ShowStandingsMessage($"(error) {summary}");
            SetStateDumpText(body);
            SetServerError(summary);
            return;
        }

        var parsed = Json.ParseString(body);
        if (parsed.VariantType == Variant.Type.Dictionary)
        {
            var payload = parsed.AsGodotDictionary();
            var ok = GetBoolValue(GetFirstNonNil(payload, "ok", "success"), true);
            if (!ok)
            {
                var error = CleanStatusMessage(
                    FmtString(GetFirstNonNil(payload, "error", "message", "detail"), "Unable to load standings."),
                    "Unable to load standings.");
                ShowStandingsMessage(error);
                return;
            }
        }
        var standings = ExtractStandingsArray(parsed);
        if (standings == null)
        {
            SetStateDumpText(body);
            ShowStandingsMessage("(error) invalid standings payload");
            return;
        }

        PopulateStandingsTree(standings);
    }

    private async Task RefreshResultsAsync(string weekKey)
    {
        if (IsNativeRuntimeSource())
        {
            RefreshNativeResultsView(weekKey);
            await Task.CompletedTask;
            return;
        }

        var selectedIndex = _resultsWeekSelect != null ? _resultsWeekSelect.Selected : -1;
        var requestedLabel = string.IsNullOrWhiteSpace(weekKey) ? "auto" : weekKey;
        GD.Print($"Results refresh week={requestedLabel} (selectedIndex={selectedIndex})");
        ShowResultsMessage("Results: loading...");
        var path = string.IsNullOrWhiteSpace(weekKey)
            ? "/results"
            : $"/results?week_key={Uri.EscapeDataString(weekKey)}";
        var (status, body) = await GetWithTimeoutAsync(path, REQUEST_TIMEOUT_MS);
        if (status < 200 || status >= 300)
        {
            var summary = SummarizeRequestError(path, status, body);
            ShowResultsMessage($"(error) {summary}");
            SetStateDumpText(body);
            return;
        }

        var parsed = Json.ParseString(body);
        Godot.Collections.Dictionary payload = null;
        if (parsed.VariantType == Variant.Type.Dictionary)
            payload = parsed.AsGodotDictionary();
        var results = ExtractArrayPayload(parsed, "results", "games", "matchups");
        if (results == null)
        {
            ShowResultsMessage("(error)");
            return;
        }

        var payloadWeekKey = "";
        var availableWeekKeys = new List<string>();
        var availableWeekLabels = new List<string>();
        var completedWeekKeys = new List<string>();
        var completedWeekLabels = new List<string>();
        if (payload != null)
        {
            payloadWeekKey = FmtString(GetFirstNonNil(payload, "week_key", "selected_week_key", "selectedWeekKey"), "");
            availableWeekKeys = ParseStringList(TryExtractArray(payload, "available_week_keys", "availableWeekKeys"));
            availableWeekLabels = ParseStringList(TryExtractArray(payload, "available_week_labels", "availableWeekLabels"));
            completedWeekKeys = ParseStringList(TryExtractArray(payload, "completed_week_keys", "completedWeekKeys"));
            completedWeekLabels = ParseStringList(TryExtractArray(payload, "completed_week_labels", "completedWeekLabels"));
        }

        _availableResultsWeekKeys.Clear();
        _availableResultsWeekKeys.AddRange(availableWeekKeys);
        _resultsWeekLabels.Clear();
        for (var i = 0; i < availableWeekKeys.Count; i++)
        {
            var key = availableWeekKeys[i];
            var label = i < availableWeekLabels.Count ? availableWeekLabels[i] : "";
            if (!string.IsNullOrWhiteSpace(key))
                _resultsWeekLabels[key] = label;
        }
        _completedResultsWeekKeys.Clear();
        for (var i = 0; i < completedWeekKeys.Count; i++)
            _completedResultsWeekKeys.Add(completedWeekKeys[i]);
        SetupResultsWeekOptions(availableWeekKeys, payloadWeekKey);
        if (!string.IsNullOrWhiteSpace(payloadWeekKey))
            _selectedResultsWeekKey = payloadWeekKey;

        PopulateResultsList(results);
    }

    private async Task RefreshScheduleAsync(string teamId, int selectionVersion = -1)
    {
        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;

        if (string.IsNullOrWhiteSpace(teamId))
        {
            ShowScheduleMessage("Select a team to view schedule.");
            return;
        }

        if (IsNativeRuntimeSource())
        {
            RefreshNativeScheduleView(teamId, selectionVersion);
            return;
        }

        ShowScheduleMessage("Schedule: loading...");
        var (status, body) = await GetWithTimeoutAsync($"/team_schedule?team_id={teamId}", REQUEST_TIMEOUT_MS);
        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;
        if (status < 200 || status >= 300)
        {
            ShowScheduleMessage($"(error) HTTP {status}");
            SetStateDumpText(body);
            return;
        }

        var parsed = Json.ParseString(body);
        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;
        if (parsed.VariantType == Variant.Type.Dictionary)
        {
            var payload = parsed.AsGodotDictionary();
            var ok = GetBoolValue(GetFirstNonNil(payload, "ok", "success"), true);
            if (!ok)
            {
                var error = CleanStatusMessage(
                    FmtString(GetFirstNonNil(payload, "error", "message", "detail"), "Unable to load schedule."),
                    "Unable to load schedule.");
                ShowScheduleMessage(error);
                return;
            }
        }
        var schedule = ExtractArrayPayload(parsed, "schedule", "games", "matchups");
        if (schedule == null)
        {
            ShowScheduleMessage("(error)");
            return;
        }

        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;
        PopulateScheduleList(schedule, teamId);
    }

    private async Task RefreshInjuryReportAsync(string teamId, int selectionVersion = -1)
    {
        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;

        if (string.IsNullOrWhiteSpace(teamId))
        {
            ShowInjuriesMessage("Select a team to view injuries.");
            return;
        }

        if (IsNativeRuntimeSource())
        {
            RefreshNativeInjuryReport(teamId, selectionVersion);
            await Task.CompletedTask;
            return;
        }

        ShowInjuriesMessage("Injury report: loading...");
        var (status, body) = await GetWithTimeoutAsync($"/injury_report?team_id={teamId}", REQUEST_TIMEOUT_MS);
        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;
        if (status < 200 || status >= 300)
        {
            ShowInjuriesMessage($"(error) HTTP {status}");
            SetStateDumpText(body);
            return;
        }

        var parsed = Json.ParseString(body);
        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;
        var injuries = ExtractArrayPayload(parsed, "entries", "injuries", "injury_report");
        if (injuries == null)
        {
            ShowInjuriesMessage("(error)");
            return;
        }

        if (selectionVersion > 0 && selectionVersion != _teamSelectionVersion)
            return;
        PopulateInjuryTree(injuries);
    }

    private void OnResultsWeekSelected(long index)
    {
        if (_suppressResultsWeekEvents)
            return;

        var itemIndex = (int)index;
        if (itemIndex < 0 || itemIndex >= _resultsWeekSelect.ItemCount)
            return;

        var weekKey = GetResultsWeekKeyFromIndex(itemIndex);
        _selectedResultsWeekKey = weekKey ?? "";
        _ = RefreshResultsAsync(_selectedResultsWeekKey);
    }

    private async Task OnInboxPrimaryActionPressed()
    {
        var selectedMessage = _selectedInboxActionItem;
        if (selectedMessage == null && !string.IsNullOrWhiteSpace(_selectedInboxMessageId))
        {
            selectedMessage = FindInboxMessage(_selectedInboxMessageId);
            _selectedInboxActionItem = selectedMessage;
        }

        if (selectedMessage == null)
        {
            SetPrimaryStatus("Select an inbox item first.");
            return;
        }

        if (IsGameDayMessage(selectedMessage))
        {
            if (!OpenGameDayPopupFromDashboardData())
                SetPrimaryStatus("Unable to open matchup popup.");
            return;
        }

        if (IsRosterInvalidMessage(selectedMessage))
        {
            _depthChartViewActive = false;
            await SelectMainTab(ROSTER_TAB_INDEX);
            return;
        }

        if (IsDepthChartInvalidMessage(selectedMessage))
        {
            _depthChartViewActive = true;
            await SelectMainTab(ROSTER_TAB_INDEX);
            return;
        }

        if (IsPostseasonPendingMessage(selectedMessage))
        {
            await ContinueUntilPause();
            return;
        }

        if (IsSeasonCompleteMessage(selectedMessage))
        {
            await SelectMainTab(LEAGUE_TAB_INDEX);
            return;
        }

        if (IsOffseasonPendingMessage(selectedMessage))
        {
            var type = FmtString(GetFirstNonNil(selectedMessage, "type"), "");
            if (string.Equals(type, ScheduleService.TrainingCampPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
            {
                SetPrimaryStatus("Training camp systems are not implemented yet.");
                return;
            }

            await ContinueUntilPause();
            return;
        }

        SetPrimaryStatus("This action is not available yet.");
        await Task.CompletedTask;
    }

    private async Task SimSelectedGame()
    {
        if (IsGameDayMessage(_selectedInboxActionItem))
        {
            await OnInboxPrimaryActionPressed();
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedSimGameId))
            return;

        if (_overviewActionButton != null)
            _overviewActionButton.Disabled = true;
        var payload = new Godot.Collections.Dictionary
        {
            { "game_id", _selectedSimGameId }
        };
        var json = Json.Stringify(payload);
        var (status, body) = await PostWithTimeoutAsync("/simulate_user_game", json, REQUEST_TIMEOUT_MS);
        if (_overviewActionButton != null)
            _overviewActionButton.Disabled = false;

        if (status < 200 || status >= 300)
        {
            SetStateDumpText(body);
            return;
        }

        await RefreshStateSummary();
        await RefreshInbox();
        await RefreshLeagueHub();
    }

    private bool OpenGameDayPopupFromDashboardData()
    {
        if (IsNativeRuntimeSource())
        {
            EnsureNativeGameCoreServices();
            var response = _nativeGameDayService.GetCurrentGameDayState();
            if (response?.Ok == true && response.Game != null)
            {
                _activeGameDayGame = ConvertNativeGameDayState(response.Game);
                return OpenGameDayPopup(_activeGameDayGame);
            }
        }

        _activeGameDayGame = _dashboardNextGame?.Duplicate(true) ?? new Godot.Collections.Dictionary();
        return OpenGameDayPopup(_activeGameDayGame);
    }

    private bool OpenGameDayPopupFromScheduleRow(Godot.Collections.Dictionary game)
    {
        if (game == null)
        {
            SetPrimaryStatus("Unable to open matchup popup.");
            return false;
        }

        var matchup = game.Duplicate(true);
        var teamName = FmtString(GetFirstNonNil(_dashboardTeam, "name"), "");
        var teamAbbr = FmtString(GetFirstNonNil(_dashboardTeam, "abbreviation"), "");
        var fallbackTeam = !string.IsNullOrWhiteSpace(teamAbbr) ? teamAbbr : teamName;
        var homeTeam = FmtString(GetFirstNonNil(game, "home_team"), "");
        var awayTeam = FmtString(GetFirstNonNil(game, "away_team"), "");
        var opponent = FmtString(GetFirstNonNil(game, "opponent"), "");
        var homeAway = FmtString(GetFirstNonNil(game, "home_away"), "").Trim().ToLowerInvariant();

        matchup["opponent"] = opponent;
        matchup["opponent_abbreviation"] = opponent;
        matchup["home_away"] = homeAway;
        matchup["game_type"] = FmtString(GetFirstNonNil(game, "game_type"), "");
        matchup["week"] = GetIntValue(GetFirstNonNil(game, "week"), 0);
        if (string.IsNullOrWhiteSpace(homeTeam))
            homeTeam = homeAway == "home" ? fallbackTeam : opponent;
        if (string.IsNullOrWhiteSpace(awayTeam))
            awayTeam = homeAway == "away" ? fallbackTeam : opponent;
        matchup["home_team"] = homeTeam;
        matchup["away_team"] = awayTeam;
        if (string.IsNullOrWhiteSpace(FmtString(GetFirstNonNil(matchup, "game_id"), "")))
            matchup["game_id"] = FmtString(GetFirstNonNil(_dashboardNextGame, "game_id"), "");

        _activeGameDayGame = matchup;
        return OpenGameDayPopup(matchup);
    }

    private bool OpenGameDayPopup(Godot.Collections.Dictionary matchup)
    {
        if (_gameDayPopup == null)
        {
            SetPrimaryStatus("Game Day popup is missing from scene.");
            return false;
        }

        var teamName = FmtString(GetFirstNonNil(_dashboardTeam, "name"), "");
        var teamAbbr = FmtString(GetFirstNonNil(_dashboardTeam, "abbreviation"), "");
        var opponentName = FmtString(GetFirstNonNil(matchup, "opponent"), "");
        var opponentAbbr = FmtString(GetFirstNonNil(matchup, "opponent_abbreviation"), "");
        var weekValue = FmtString(GetFirstNonNil(matchup, "week"), "");
        if (string.IsNullOrWhiteSpace(weekValue))
            weekValue = FmtString(GetFirstNonNil(_dashboardCalendar, "week"), "Unknown week");

        var gameType = FmtString(GetFirstNonNil(matchup, "game_type"), "");
        if (string.IsNullOrWhiteSpace(gameType))
            gameType = FmtString(GetFirstNonNil(_dashboardCalendar, "phase"), "Game");

        var homeAway = FmtString(GetFirstNonNil(matchup, "home_away"), "");
        var teamRecord = FmtString(GetFirstNonNil(_dashboardTeam, "record"), "Record unavailable");
        var displayTeam = !string.IsNullOrWhiteSpace(teamName) ? teamName : (string.IsNullOrWhiteSpace(teamAbbr) ? "Your Team" : teamAbbr);
        var displayOpponent = !string.IsNullOrWhiteSpace(opponentName) ? opponentName : (!string.IsNullOrWhiteSpace(opponentAbbr) ? opponentAbbr : "Unknown opponent");
        var displayGameType = HumanizeStatus(gameType);
        var displayWeek = string.IsNullOrWhiteSpace(weekValue) || string.Equals(weekValue, "Unknown week", StringComparison.OrdinalIgnoreCase)
            ? displayGameType
            : $"{displayGameType} Week {weekValue}";
        var venueText = homeAway switch
        {
            "home" => "Home Game",
            "away" => "Away Game",
            "vs" => "Home Game",
            "@" => "Away Game",
            _ => "Venue unavailable",
        };
        var recordLabel = !string.IsNullOrWhiteSpace(teamRecord)
            ? $"{displayTeam}: {teamRecord} | Opponent: Record unavailable"
            : "Record unavailable";
        var hasCompactData =
            !string.IsNullOrWhiteSpace(teamName) ||
            !string.IsNullOrWhiteSpace(teamAbbr) ||
            !string.IsNullOrWhiteSpace(opponentName) ||
            !string.IsNullOrWhiteSpace(opponentAbbr);

        if (_lblGameDayWeek != null)
            _lblGameDayWeek.Text = displayWeek;
        if (_lblGameDayMatchup != null)
            _lblGameDayMatchup.Text = $"{displayTeam} vs {displayOpponent}";
        if (_lblGameDayVenue != null)
            _lblGameDayVenue.Text = venueText;
        if (_lblGameDayRecords != null)
            _lblGameDayRecords.Text = recordLabel;
        if (_lblGameDayStatus != null)
            _lblGameDayStatus.Text = "Game ready.";
        if (_btnGameDaySim != null)
            _btnGameDaySim.Disabled = false;

        _gameDayPopup.Visible = true;
        SetPrimaryStatus(hasCompactData ? "Viewing matchup." : "No matchup data available.");
        return true;
    }

    private void CloseGameDayPopup()
    {
        if (_gameDayPopup != null)
            _gameDayPopup.Visible = false;
        if (_btnGameDaySim != null)
            _btnGameDaySim.Disabled = false;
    }

    private void OnWatchGamePressed()
    {
        if (_lblGameDayStatus != null)
            _lblGameDayStatus.Text = "Watch Game is coming later.";
        SetPrimaryStatus("Watch Game is coming later.");
    }

    private async Task OnGameDaySimPressed()
    {
        if (IsNativeRuntimeSource())
        {
            var nativeGameId = FmtString(GetFirstNonNil(_activeGameDayGame, "game_id"), "");
            if (string.IsNullOrWhiteSpace(nativeGameId))
                nativeGameId = FmtString(GetFirstNonNil(_dashboardNextGame, "game_id"), "");
            if (_btnGameDaySim != null)
                _btnGameDaySim.Disabled = true;
            if (_lblGameDayStatus != null)
                _lblGameDayStatus.Text = "Simulating game...";
            SetPrimaryStatus("Simulating current game...");
            try
            {
                EnsureNativeGameCoreServices();
                var response = _nativeGameDayService.SimulateCurrentUserGame(nativeGameId);
                if (response?.Ok != true || response.Result == null)
                {
                    var error = string.IsNullOrWhiteSpace(response?.Error) ? "Sim Game failed." : response.Error;
                    if (_lblGameDayStatus != null)
                        _lblGameDayStatus.Text = error;
                    SetPrimaryStatus(error);
                    return;
                }

                CloseGameDayPopup();
                _activeGameDayGame = new Godot.Collections.Dictionary();
                ShowPostGameRecapFromResult(BuildNativeGameResultDictionary(response.Result));
                await SaveNativeAutosave("Native autosave updated.");
                SetPrimaryStatus("Game complete.");
                await RefreshDashboardState();
                await RefreshStateSummary();
                await RefreshInbox();
                await RefreshLeagueHub();
            }
            catch (Exception ex)
            {
                if (_lblGameDayStatus != null)
                    _lblGameDayStatus.Text = "Unable to complete game.";
                SetPrimaryStatus($"Native Sim Game failed: {InlineMessage(ex.Message)}");
            }
            finally
            {
                if (_btnGameDaySim != null)
                    _btnGameDaySim.Disabled = false;
            }
            return;
        }

        var gameId = FmtString(GetFirstNonNil(_activeGameDayGame, "game_id"), "");
        if (string.IsNullOrWhiteSpace(gameId))
            gameId = FmtString(GetFirstNonNil(_dashboardNextGame, "game_id"), "");
        if (string.IsNullOrWhiteSpace(gameId))
        {
            if (_lblGameDayStatus != null)
                _lblGameDayStatus.Text = "Sim Game is coming soon.";
            SetPrimaryStatus("Sim Game is coming soon.");
            return;
        }

        if (_btnGameDaySim != null)
            _btnGameDaySim.Disabled = true;
        if (_lblGameDayStatus != null)
            _lblGameDayStatus.Text = "Simulating game...";
        SetPrimaryStatus("Simulating current game...");

        var payload = new Godot.Collections.Dictionary
        {
            { "game_id", gameId }
        };
        var json = Json.Stringify(payload);
        var (status, body) = await PostWithTimeoutAsync("/simulate_user_game", json, REQUEST_TIMEOUT_MS);

        if (_btnGameDaySim != null)
            _btnGameDaySim.Disabled = false;

        if (status < 200 || status >= 300)
        {
            if (_lblGameDayStatus != null)
                _lblGameDayStatus.Text = "Unable to complete game.";
            SetPrimaryStatus($"Sim Game failed (HTTP {status}).");
            return;
        }

        var result = ParseCompactResult(body, "Sim Game failed.", out var errorMessage);
        if (!string.IsNullOrWhiteSpace(errorMessage) || result == null || result.Count == 0)
        {
            if (_lblGameDayStatus != null)
                _lblGameDayStatus.Text = string.IsNullOrWhiteSpace(errorMessage) ? "Sim Game failed." : errorMessage;
            SetPrimaryStatus(string.IsNullOrWhiteSpace(errorMessage) ? "Sim Game failed." : errorMessage);
            return;
        }

        CloseGameDayPopup();
        _activeGameDayGame = new Godot.Collections.Dictionary();
        ShowPostGameRecapFromResult(result);
        SetPrimaryStatus("Game complete.");
    }

    private async Task OpenCompletedScheduleGameAsync(Godot.Collections.Dictionary game)
    {
        var gameId = GetGameId(game);
        if (string.IsNullOrWhiteSpace(gameId))
        {
            SetPrimaryStatus("No game result is available.");
            if (_lblScheduleActionStatus != null)
                _lblScheduleActionStatus.Text = "No game result is available.";
            return;
        }

        if (IsNativeRuntimeSource())
        {
            if (TryShowNativeGameResult(gameId, "Game result not found.", "Loaded game result."))
            {
                SetPrimaryStatus("Viewing game recap.");
                if (_lblScheduleActionStatus != null)
                    _lblScheduleActionStatus.Text = "Completed game loaded.";
            }
            else
            {
                SetPrimaryStatus("Unable to load game result.");
                if (_lblScheduleActionStatus != null)
                    _lblScheduleActionStatus.Text = "Unable to load game result.";
            }
            return;
        }

        var encodedGameId = Uri.EscapeDataString(gameId);
        var (status, body) = await GetWithTimeoutAsync($"/game_result?game_id={encodedGameId}", REQUEST_TIMEOUT_MS);
        if (status < 200 || status >= 300)
        {
            SetPrimaryStatus("Unable to load game result.");
            if (_lblScheduleActionStatus != null)
                _lblScheduleActionStatus.Text = "Unable to load game result.";
            return;
        }

        var result = ParseCompactResult(body, "Unable to load game result.", out var errorMessage);
        if (!string.IsNullOrWhiteSpace(errorMessage) || result == null || result.Count == 0)
        {
            var clean = string.IsNullOrWhiteSpace(errorMessage) ? "Game result not found." : errorMessage;
            SetPrimaryStatus(clean);
            if (_lblScheduleActionStatus != null)
                _lblScheduleActionStatus.Text = clean;
            return;
        }

        ShowPostGameRecapFromResult(result, "Loaded game result.");
        SetPrimaryStatus("Viewing game recap.");
        if (_lblScheduleActionStatus != null)
            _lblScheduleActionStatus.Text = "Completed game loaded.";
    }

    private Godot.Collections.Dictionary ParseCompactResult(string body, string fallbackError, out string errorMessage)
    {
        errorMessage = "";
        var fallback = new Godot.Collections.Dictionary();
        if (string.IsNullOrWhiteSpace(body))
            return fallback;

        var parsed = Json.ParseString(body);
        if (parsed.VariantType != Variant.Type.Dictionary)
            return fallback;

        var payload = parsed.AsGodotDictionary();
        var okValue = GetFirstNonNil(payload, "ok", "success");
        if (!IsNil(okValue) && !GetBoolValue(okValue, true))
        {
            errorMessage = CleanStatusMessage(
                FmtString(GetFirstNonNil(payload, "error", "message", "detail"), fallbackError),
                fallbackError);
            return fallback;
        }

        return TryExtractObject(payload, "result") ?? payload;
    }

    private void ShowPostGameRecapFromResult(Godot.Collections.Dictionary result, string statusText = "")
    {
        _latestGameResult = result?.Duplicate(true);
        PopulatePostGameRecap(result);
        if (_lblPostGameStatus != null)
            _lblPostGameStatus.Text = statusText ?? "";
        ShowPostGameRecapPopup();
    }

    private void PopulatePostGameRecap(Godot.Collections.Dictionary result)
    {
        result ??= new Godot.Collections.Dictionary();

        var homeTeam = FmtString(GetFirstNonNil(result, "home_team", "home", "home_abbr"), "Unknown home team");
        var awayTeam = FmtString(GetFirstNonNil(result, "away_team", "away", "away_abbr"), "Unknown away team");
        var homeScore = FmtInt(GetFirstNonNil(result, "home_score", "home_points", "home_pts"), "-");
        var awayScore = FmtInt(GetFirstNonNil(result, "away_score", "away_points", "away_pts"), "-");
        var winner = FmtString(GetFirstNonNil(result, "winner", "winner_id"), "");
        if (string.IsNullOrWhiteSpace(winner))
            winner = "TBD";

        var gameInfo = BuildCompactGameInfoLine(result);
        var summary = FmtString(GetFirstNonNil(result, "summary", "summary_text"), "Game complete.");
        if (string.IsNullOrWhiteSpace(summary))
            summary = "Game complete.";

        if (_lblPostGameScore != null)
            _lblPostGameScore.Text = $"{homeTeam} {homeScore} - {awayTeam} {awayScore}";
        if (_lblPostGameWinner != null)
            _lblPostGameWinner.Text = $"Winner: {winner}";
        if (_lblPostGameInfo != null)
            _lblPostGameInfo.Text = gameInfo;
        if (_lblPostGameSummary != null)
            _lblPostGameSummary.Text = summary;
        if (_lblPostGameStatus != null && string.IsNullOrWhiteSpace(_lblPostGameStatus.Text))
            _lblPostGameStatus.Text = "";
    }

    private void ShowPostGameRecapPopup()
    {
        if (_postGameRecapPopup != null)
            _postGameRecapPopup.Visible = true;
    }

    private void HidePostGameRecapPopup()
    {
        if (_postGameRecapPopup != null)
            _postGameRecapPopup.Visible = false;
    }

    private void OnPostGameBoxScorePressed()
    {
        if (_latestGameResult == null || _latestGameResult.Count == 0)
        {
            if (_lblPostGameStatus != null)
                _lblPostGameStatus.Text = "No box score is available yet.";
            SetPrimaryStatus("No box score is available yet.");
            return;
        }

        ShowBoxScoreFromResult(_latestGameResult);
        SetPrimaryStatus("Viewing box score.");
    }

    private async Task ClosePostGameRecapPopupAsync()
    {
        if (_btnPostGameClose != null)
            _btnPostGameClose.Disabled = true;

        _restorePostGameRecapAfterBoxScore = false;
        HideBoxScorePopup();
        HidePostGameRecapPopup();
        await RefreshDashboardState();

        if (_btnPostGameClose != null)
            _btnPostGameClose.Disabled = false;

        SetPrimaryStatus("Dashboard refreshed.");
    }

    private static string BuildCompactGameInfoLine(Godot.Collections.Dictionary result)
    {
        var weekLabel = FmtString(GetFirstNonNil(result, "week_label", "weekLabel"), "");
        if (!string.IsNullOrWhiteSpace(weekLabel))
            return weekLabel;

        var gameType = HumanizeStatus(FmtString(GetFirstNonNil(result, "game_type", "season_type", "season_phase"), ""));
        var week = FmtInt(GetFirstNonNil(result, "week", "season_week", "calendar_week"), "");
        if (string.IsNullOrWhiteSpace(gameType))
            return "Game complete";
        return string.IsNullOrWhiteSpace(week) ? gameType : $"{gameType} Week {week}";
    }

    private void PopulateLatestGameBoxScorePopup(Godot.Collections.Dictionary result)
    {
        result ??= new Godot.Collections.Dictionary();

        var homeTeam = FmtString(GetFirstNonNil(result, "home_team", "home", "home_abbr"), "Unknown home team");
        var awayTeam = FmtString(GetFirstNonNil(result, "away_team", "away", "away_abbr"), "Unknown away team");
        var homeScore = FmtInt(GetFirstNonNil(result, "home_score", "home_points", "home_pts"), "-");
        var awayScore = FmtInt(GetFirstNonNil(result, "away_score", "away_points", "away_pts"), "-");

        if (_lblBoxScorePopupInfo != null)
            _lblBoxScorePopupInfo.Text = BuildCompactGameInfoLine(result);
        if (_lblBoxScorePopupScore != null)
            _lblBoxScorePopupScore.Text = $"{homeTeam} {homeScore} - {awayTeam} {awayScore}";
        if (_lblBoxScorePopupStatus != null)
            _lblBoxScorePopupStatus.Text = "";

        if (!TryResolveBoxScoreObjects(result, out _, out var boxScore))
            boxScore = result;

        PopulateBoxScoreQuarterTree(
            _boxScorePopupQuarterTree,
            boxScore,
            awayTeam,
            homeTeam,
            awayScore,
            homeScore,
            useDefaultQuarterRows: true);
        PopulateBoxScoreTeamStatsTree(
            _boxScorePopupTeamStatsTree,
            boxScore,
            awayTeam,
            homeTeam,
            useCompactStatRows: true);
    }

    private void ShowBoxScoreFromResult(Godot.Collections.Dictionary result)
    {
        PopulateLatestGameBoxScorePopup(result);
        _restorePostGameRecapAfterBoxScore = _postGameRecapPopup != null && _postGameRecapPopup.Visible;
        if (_restorePostGameRecapAfterBoxScore)
            HidePostGameRecapPopup();
        ShowBoxScorePopup();
    }

    private void ShowBoxScorePopup()
    {
        if (_boxScorePopup != null)
            _boxScorePopup.Visible = true;
    }

    private void HideBoxScorePopup()
    {
        if (_boxScorePopup != null)
            _boxScorePopup.Visible = false;
    }

    private void OnBoxScorePopupClosePressed()
    {
        HideBoxScorePopup();
        if (_restorePostGameRecapAfterBoxScore)
            ShowPostGameRecapPopup();
        _restorePostGameRecapAfterBoxScore = false;
        SetPrimaryStatus("Closed box score.");
    }

    private async Task AcknowledgeSelectedMessage()
    {
        if (string.IsNullOrWhiteSpace(_selectedInboxMessageId))
            return;

        if (_overviewActionButton != null)
            _overviewActionButton.Disabled = true;
        var payload = new Godot.Collections.Dictionary
        {
            { "message_id", _selectedInboxMessageId }
        };
        var teamId = GetAcknowledgeTeamId();
        if (!string.IsNullOrWhiteSpace(teamId))
            payload["team_id"] = teamId;
        var json = Json.Stringify(payload);
        var (status, body) = await PostWithTimeoutAsync("/inbox/mark_read", json, REQUEST_TIMEOUT_MS);
        if (_overviewActionButton != null)
            _overviewActionButton.Disabled = false;

        if (status < 200 || status >= 300)
        {
            SetStateDumpText(body);
            return;
        }

        var parsed = Json.ParseString(body);
        if (parsed.VariantType == Variant.Type.Dictionary)
        {
            var dict = parsed.AsGodotDictionary();
            var okVar = GetFirstNonNil(dict, "ok", "success");
            if (!IsNil(okVar) && !GetBoolValue(okVar, true))
            {
                var message = FmtString(GetFirstNonNil(dict, "error", "message", "detail"), "");
                if (string.IsNullOrWhiteSpace(message))
                    message = body;
                SetStateDumpText($"Acknowledge failed (ok=false): {message}");
                return;
            }
        }

        await RefreshInbox();
    }

    private bool UpdateContinueStatus(string responseBody)
    {
        var parsed = Json.ParseString(responseBody);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            SetPrimaryStatus("Continue finished.");
            return false;
        }

        var dict = parsed.AsGodotDictionary();
        var ok = GetBoolValue(GetFirstNonNil(dict, "ok"), true);
        if (!ok)
        {
            var error = FmtString(GetFirstNonNil(dict, "error"), "Continue failed.");
            SetPrimaryStatus(CleanStatusMessage(error, "Continue failed."));
            SetStateDumpText(responseBody);
            return false;
        }

        var result = TryExtractObject(dict, "result");
        var data = result ?? dict;
        var stopReason = FmtString(GetFirstNonNil(data, "stop_reason", "reason"), "");
        var daysAdvanced = GetIntValue(GetFirstNonNil(data, "days_advanced"), 0);

        var message = string.IsNullOrWhiteSpace(stopReason)
            ? $"Advanced {daysAdvanced} day(s)."
            : $"Paused: {FormatContinueStopReason(stopReason)}";
        if (string.Equals(stopReason, "max_days_reached", StringComparison.OrdinalIgnoreCase))
            message += $" after {daysAdvanced} day(s).";

        _inboxEmptyDetailMessage = string.Equals(stopReason, "game_day", StringComparison.OrdinalIgnoreCase)
            ? "Game day reached."
            : "No urgent messages.";

        if (_continueStatus != null)
            _continueStatus.Text = message;
        return true;
    }

    private string FormatContinueStopReason(string stopReason)
    {
        return stopReason switch
        {
            "game_day" => "Game day reached",
            "week_advanced" => "Week advanced",
            "season_phase_changed" => "Season phase changed",
            "reached_requested_week" => "Requested week reached",
            "reached_playoffs" => "Playoffs reached",
            "reached_offseason" => "Offseason Pending reached",
            "reached_free_agency" => "Free Agency Pending reached",
            "reached_draft" => "Draft Pending reached",
            "reached_training_camp" => "Training Camp Pending reached",
            "offseason_pending" => "Offseason pending",
            "staff_carousel_pending" => "Staff Carousel pending",
            "retirement_pending" => "Retirement pending",
            "exclusive_negotiation_pending" => "Exclusive Negotiation pending",
            "franchise_tag_pending" => "Franchise Tag pending",
            "league_year_pending" => "League Year pending",
            "free_agency_pending" => "Free Agency pending",
            "draft_prep_pending" => "Draft Prep pending",
            "draft_pending" => "Draft pending",
            "rookie_signing_pending" => "Rookie Signing pending",
            "training_camp_pending" => "Training Camp pending",
            "postseason_pending" => "Postseason pending",
            "required_user_action" => "Required user action",
            "roster_invalid" => "Roster invalid",
            "depth_chart_invalid" => "Depth chart invalid",
            "max_days_reached" => "Max days reached",
            "max_iterations_reached" => "Iteration limit reached",
            "user_stop_requested" => "Stop requested",
            "no_active_league" => "No active league loaded",
            _ => string.IsNullOrWhiteSpace(stopReason) ? "Simulation paused" : stopReason.Replace('_', ' ')
        };
    }

    private static string GetPotValue(Godot.Collections.Dictionary player)
    {
        var value = GetFirstNonNil(player, "pot", "potential", "pot_rating");
        return FmtInt(value, "?");
    }

    private static int GetPotValueInt(Godot.Collections.Dictionary player)
    {
        var value = GetFirstNonNil(player, "pot", "potential", "pot_rating");
        return GetIntValue(value, 0);
    }

    private static int GetOverallValue(Godot.Collections.Dictionary player)
    {
        var value = GetFirstNonNil(player, "overall", "ovr");
        return GetIntValue(value, 0);
    }

    private static int GetAgeValue(Godot.Collections.Dictionary player)
    {
        return player.ContainsKey("age") ? GetIntValue((Variant)player["age"], 0) : 0;
    }

    private static string GetPlayerId(Godot.Collections.Dictionary player)
    {
        if (player.ContainsKey("player_id"))
            return player["player_id"].ToString();
        return player.ContainsKey("id") ? player["id"].ToString() : "";
    }

    private static string GetCompactRosterStatus(Godot.Collections.Dictionary player)
    {
        var status = FmtString(GetFirstNonNil(player, "status"), "").Trim().ToLowerInvariant();
        return status switch
        {
            "active" => "Active",
            "ir" => "IR",
            "practice_squad" => "Practice Squad",
            _ => string.IsNullOrWhiteSpace(status) ? "Active" : HumanizeStatus(status)
        };
    }

    private static string GetCompactRosterInjury(Godot.Collections.Dictionary player)
    {
        var injury = FmtString(GetFirstNonNil(player, "injury"), "").Trim();
        return string.IsNullOrWhiteSpace(injury) ? "Healthy" : injury;
    }

    private static bool IsNil(Variant value)
    {
        return value.VariantType == Variant.Type.Nil;
    }

    private static Variant GetFirstNonNil(Godot.Collections.Dictionary player, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (player.ContainsKey(key))
            {
                var value = (Variant)player[key];
                if (!IsNil(value))
                    return value;
            }
        }

        return default;
    }

    private static Variant TryExtract(Godot.Collections.Dictionary obj, params string[] keys)
    {
        if (obj == null || keys == null || keys.Length == 0)
            return default;

        foreach (var key in keys)
        {
            if (obj.ContainsKey(key))
            {
                var value = (Variant)obj[key];
                if (!IsNil(value))
                    return value;
            }
        }

        return default;
    }

    private static Godot.Collections.Array TryExtractArray(Godot.Collections.Dictionary obj, params string[] keys)
    {
        var value = TryExtract(obj, keys);
        return TryGetArray(value, out var array) ? array : null;
    }

    private static Godot.Collections.Dictionary TryExtractObject(Godot.Collections.Dictionary obj, params string[] keys)
    {
        var value = TryExtract(obj, keys);
        return TryGetDictionary(value, out var dict) ? dict : null;
    }

    private static int GetIntValue(Variant value, int fallback)
    {
        if (IsNil(value))
            return fallback;

        if (value.VariantType == Variant.Type.Int)
            return value.AsInt32();

        if (value.VariantType == Variant.Type.Float)
            return (int)Math.Round(value.AsDouble());

        if (value.VariantType == Variant.Type.String)
        {
            var str = value.AsString();
            if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                return parsedInt;
            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble))
                return (int)Math.Round(parsedDouble);
        }

        return fallback;
    }

    private static float GetFloatValue(Variant value, float fallback)
    {
        if (IsNil(value))
            return fallback;

        if (value.VariantType == Variant.Type.Float)
            return (float)value.AsDouble();

        if (value.VariantType == Variant.Type.Int)
            return value.AsInt32();

        if (value.VariantType == Variant.Type.String)
        {
            var str = value.AsString();
            if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFloat))
                return parsedFloat;
        }

        return fallback;
    }

    private static bool TryGetDictionary(Variant value, out Godot.Collections.Dictionary dict)
    {
        if (value.VariantType == Variant.Type.Dictionary)
        {
            dict = value.AsGodotDictionary();
            return true;
        }

        dict = null;
        return false;
    }

    private static bool TryGetArray(Variant value, out Godot.Collections.Array array)
    {
        if (value.VariantType == Variant.Type.Array)
        {
            array = value.AsGodotArray();
            return true;
        }

        array = null;
        return false;
    }

    private static bool GetBoolValue(Variant value, bool fallback)
    {
        if (IsNil(value))
            return fallback;

        if (value.VariantType == Variant.Type.Bool)
            return value.AsBool();

        if (value.VariantType == Variant.Type.Int)
            return value.AsInt32() != 0;

        if (value.VariantType == Variant.Type.Float)
            return Math.Abs(value.AsDouble()) > 0.0001;

        if (value.VariantType == Variant.Type.String)
        {
            var str = value.AsString();
            if (bool.TryParse(str, out var parsedBool))
                return parsedBool;
            if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                return parsedInt != 0;
        }

        return fallback;
    }

    private static string FmtInt(Variant value, string fallback = "?")
    {
        if (IsNil(value))
            return fallback;

        if (value.VariantType == Variant.Type.Int)
            return value.AsInt32().ToString();

        if (value.VariantType == Variant.Type.Float)
            return ((int)Math.Round(value.AsDouble())).ToString();

        if (value.VariantType == Variant.Type.String)
        {
            var str = value.AsString();
            if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                return parsedInt.ToString();
            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble))
                return ((int)Math.Round(parsedDouble)).ToString();
        }

        return fallback;
    }

    private static string FmtString(Variant value, string fallback = "")
    {
        if (IsNil(value))
            return fallback;

        if (value.VariantType == Variant.Type.String)
            return value.AsString();

        return value.ToString();
    }

    private static string FormatDashboardCapRoom(Godot.Collections.Dictionary teamStatus)
    {
        if (teamStatus == null)
            return "N/A";

        var capRoom = GetFirstNonNil(teamStatus, "cap_room", "capRoom");
        if (IsNil(capRoom))
            return "N/A";

        if (capRoom.VariantType == Variant.Type.Int)
            return capRoom.AsInt64().ToString("N0", CultureInfo.InvariantCulture);

        if (capRoom.VariantType == Variant.Type.Float)
            return capRoom.AsDouble().ToString("N0", CultureInfo.InvariantCulture);

        var text = FmtString(capRoom, "");
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed.ToString("N0", CultureInfo.InvariantCulture);

        return string.IsNullOrWhiteSpace(text) ? "N/A" : text;
    }

    private static string FormatStatValue(object value)
    {
        if (value == null)
            return "";

        if (value is Variant variant)
        {
            if (variant.VariantType == Variant.Type.Nil)
                return "";
            if (variant.VariantType == Variant.Type.Int)
                return variant.AsInt32().ToString();
            if (variant.VariantType == Variant.Type.Float)
                return FormatStatFloat(variant.AsDouble());
            if (variant.VariantType == Variant.Type.String)
                return variant.AsString();
            return variant.ToString();
        }

        if (value is float f)
            return FormatStatFloat(f);
        if (value is double d)
            return FormatStatFloat(d);
        if (value is decimal dec)
            return FormatStatFloat((double)dec);

        return value.ToString();
    }

    private static string FormatStatFloat(double value)
    {
        if (Math.Abs(value - Math.Round(value)) < 1e-9)
            return ((int)Math.Round(value)).ToString();

        return value.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string FmtClock(Variant value)
    {
        if (IsNil(value))
            return "";

        if (value.VariantType == Variant.Type.String)
            return value.AsString();

        if (value.VariantType == Variant.Type.Dictionary)
        {
            var dict = value.AsGodotDictionary();
            var date = dict.ContainsKey("current_date") ? dict["current_date"].ToString() : "";
            var time = dict.ContainsKey("current_time") ? dict["current_time"].ToString() : "";
            if (string.IsNullOrWhiteSpace(time) && dict.ContainsKey("hour"))
            {
                var hour = dict["hour"].ToString();
                if (!string.IsNullOrWhiteSpace(hour))
                    time = $"{hour}:00";
            }

            if (!string.IsNullOrWhiteSpace(date) && !string.IsNullOrWhiteSpace(time))
                return $"{date} {time}";
            if (!string.IsNullOrWhiteSpace(date))
                return date;
            if (!string.IsNullOrWhiteSpace(time))
                return time;
        }

        return value.ToString();
    }

    private static string DebugVariant(Variant value)
    {
        if (IsNil(value))
            return "Nil";

        return $"{value} ({value.VariantType})";
    }

    private void UpdateWeekInfoFromStateSummary(Godot.Collections.Dictionary dict)
    {
        if (dict == null)
            return;

        var weekValue = default(Variant);
        var maxWeekValue = default(Variant);

        if (dict.ContainsKey("calendar"))
        {
            var calendarVar = (Variant)dict["calendar"];
            if (TryGetDictionary(calendarVar, out var calendar))
            {
                weekValue = GetFirstNonNil(calendar, "current_week", "week", "week_num", "current_week_number");
                maxWeekValue = GetFirstNonNil(calendar, "total_weeks", "weeks", "week_count", "max_week");
            }
        }

        if (IsNil(weekValue))
            weekValue = GetFirstNonNil(dict, "current_week", "week", "week_num");

        if (IsNil(maxWeekValue) && dict.ContainsKey("league"))
        {
            var leagueVar = (Variant)dict["league"];
            if (TryGetDictionary(leagueVar, out var league))
                maxWeekValue = GetFirstNonNil(league, "total_weeks", "weeks", "week_count", "max_week");
        }

        var week = GetIntValue(weekValue, _currentWeek);
        if (week > 0)
            _currentWeek = week;

        var maxWeek = GetIntValue(maxWeekValue, _maxWeek);
        if (maxWeek <= 0)
            maxWeek = _maxWeek;

        if (_currentWeek > maxWeek)
            maxWeek = _currentWeek;

        _maxWeek = maxWeek;
    }

    private void UpdateUserTeamIdFromStateSummary(Godot.Collections.Dictionary dict)
    {
        if (dict == null)
        {
            _userTeamId = "";
            return;
        }

        var teamVar = GetFirstNonNil(dict, "user_team_id", "user_team", "userTeamId");
        if (!IsNil(teamVar) && teamVar.VariantType == Variant.Type.Dictionary && TryGetDictionary(teamVar, out var teamDict))
            teamVar = GetFirstNonNil(teamDict, "id", "team_id");

        if (IsNil(teamVar) && dict.ContainsKey("time_engine"))
        {
            var engineVar = (Variant)dict["time_engine"];
            if (TryGetDictionary(engineVar, out var engineDict))
                teamVar = GetFirstNonNil(engineDict, "user_team_id", "team_id");
        }

        if (IsNil(teamVar) && dict.ContainsKey("user"))
        {
            var userVar = (Variant)dict["user"];
            if (TryGetDictionary(userVar, out var userDict))
                teamVar = GetFirstNonNil(userDict, "team_id", "user_team_id", "teamId", "team");
        }

        if (IsNil(teamVar) && dict.ContainsKey("profile"))
        {
            var profileVar = (Variant)dict["profile"];
            if (TryGetDictionary(profileVar, out var profileDict))
                teamVar = GetFirstNonNil(profileDict, "team_id", "user_team_id", "teamId", "team");
        }

        _userTeamId = FmtString(teamVar, "");
    }

    private void UpdateUserTeamLabelFromStateSummary(Godot.Collections.Dictionary dict)
    {
        if (dict == null)
        {
            _gmTeamLabel = "(unknown)";
            RenderFrontOfficeLabel();
            return;
        }

        var abbrVar = GetFirstNonNil(dict, "user_team_abbr", "userTeamAbbr");
        if (!IsNil(abbrVar) && abbrVar.VariantType == Variant.Type.Dictionary && TryGetDictionary(abbrVar, out var abbrDict))
            abbrVar = GetFirstNonNil(abbrDict, "abbreviation", "abbr", "short_name");

        var abbr = FmtString(abbrVar, "");
        if (string.IsNullOrWhiteSpace(abbr) && dict.ContainsKey("user_team"))
        {
            var userVar = (Variant)dict["user_team"];
            if (TryGetDictionary(userVar, out var userDict))
                abbr = FmtString(GetFirstNonNil(userDict, "abbreviation", "abbr", "short_name"), "");
        }

        if (string.IsNullOrWhiteSpace(abbr))
            abbr = ResolveTeamAbbrFromId(_userTeamId);

        _gmTeamLabel = string.IsNullOrWhiteSpace(abbr) ? "(unknown)" : abbr;
        RenderFrontOfficeLabel();
    }

    private string ResolveTeamAbbrFromId(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return "";

        for (var i = 0; i < _teams.Count; i++)
        {
            var team = (Godot.Collections.Dictionary)_teams[i];
            var id = FmtString(GetFirstNonNil(team, "id", "team_id"), "");
            if (!string.Equals(id, teamId, StringComparison.OrdinalIgnoreCase))
                continue;
            return FmtString(GetFirstNonNil(team, "abbreviation", "abbr", "short_name"), "");
        }

        return "";
    }

    private string GetAcknowledgeTeamId()
    {
        if (!string.IsNullOrWhiteSpace(_userTeamId))
            return _userTeamId;

        return _currentTeamId ?? "";
    }

    private void SetupStandingsTree()
    {
        ConfigureStandingsTree(_standingsTree);
        ConfigureOverviewStandingsSnapshot();
    }

    private void ConfigureOverviewStandingsSnapshot()
    {
        if (_overviewStandingsSnapshot == null)
            return;

        _overviewStandingsSnapshot.BbcodeEnabled = false;
        _overviewStandingsSnapshot.FitContent = true;
        _overviewStandingsSnapshot.ScrollActive = false;
        _overviewStandingsSnapshot.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _overviewStandingsSnapshot.CustomMinimumSize = new Vector2(0, 108);
    }

    private void SetupInjuriesTree()
    {
        if (_injuriesTree == null)
            return;

        _injuriesTree.HideRoot = true;
        _injuriesTree.ColumnTitlesVisible = true;
        _injuriesTree.Columns = 7;
        _injuriesTree.SetColumnTitle(0, "Name");
        _injuriesTree.SetColumnExpand(0, true);
        _injuriesTree.SetColumnCustomMinimumWidth(0, 170);
        _injuriesTree.SetColumnTitle(1, "Pos");
        _injuriesTree.SetColumnCustomMinimumWidth(1, 44);
        _injuriesTree.SetColumnTitle(2, "Status");
        _injuriesTree.SetColumnCustomMinimumWidth(2, 78);
        _injuriesTree.SetColumnTitle(3, "Injury");
        _injuriesTree.SetColumnCustomMinimumWidth(3, 120);
        _injuriesTree.SetColumnTitle(4, "Return");
        _injuriesTree.SetColumnCustomMinimumWidth(4, 78);
        _injuriesTree.SetColumnTitle(5, "Days Left");
        _injuriesTree.SetColumnCustomMinimumWidth(5, 62);
        _injuriesTree.SetColumnTitle(6, "IR");
        _injuriesTree.SetColumnCustomMinimumWidth(6, 34);
    }

    private void SetupBoxScoreTrees()
    {
        ConfigureBoxScoreTree(_boxScoreQuarterTree);
        ConfigureBoxScoreTree(_boxScoreTeamStatsTree);
    }

    private void SetupHistoryView()
    {
        if (_historyDetailText == null)
            return;

        _historyDetailText.BbcodeEnabled = false;
        _historyDetailText.FitContent = true;
        _historyDetailText.ScrollActive = false;
        _historyDetailText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
    }

    private void SetupScheduleTree()
    {
        if (_scheduleList == null)
            return;

        _scheduleList.HideRoot = true;
        _scheduleList.ColumnTitlesVisible = true;
        _scheduleList.Columns = 5;
        _scheduleList.SetColumnTitle(0, "Status");
        _scheduleList.SetColumnCustomMinimumWidth(0, 96);
        _scheduleList.SetColumnTitle(1, "Matchup");
        _scheduleList.SetColumnExpand(1, true);
        _scheduleList.SetColumnCustomMinimumWidth(1, 180);
        _scheduleList.SetColumnTitle(2, "Week");
        _scheduleList.SetColumnCustomMinimumWidth(2, 148);
        _scheduleList.SetColumnTitle(3, "Result");
        _scheduleList.SetColumnCustomMinimumWidth(3, 126);
        _scheduleList.SetColumnTitle(4, "Action");
        _scheduleList.SetColumnCustomMinimumWidth(4, 108);
        _scheduleList.AllowReselect = true;
    }

    private static void ConfigureBoxScoreTree(Tree tree)
    {
        if (tree == null)
            return;

        tree.HideRoot = true;
        tree.ColumnTitlesVisible = true;
    }

    private static void ConfigureStandingsTree(Tree tree)
    {
        if (tree == null)
            return;

        tree.HideRoot = true;
        tree.ColumnTitlesVisible = true;
        tree.Columns = 5;
        tree.SetColumnTitle(0, "Team");
        tree.SetColumnExpand(0, true);
        tree.SetColumnCustomMinimumWidth(0, 168);
        tree.SetColumnTitle(1, "W-L-T");
        tree.SetColumnCustomMinimumWidth(1, 70);
        tree.SetColumnTitle(2, "PF");
        tree.SetColumnCustomMinimumWidth(2, 44);
        tree.SetColumnTitle(3, "PA");
        tree.SetColumnCustomMinimumWidth(3, 44);
        tree.SetColumnTitle(4, "Win %");
        tree.SetColumnCustomMinimumWidth(4, 62);
    }

    private void SetupResultsWeekOptions(List<string> availableWeekKeys, string selectedWeekKey)
    {
        if (_resultsWeekSelect == null)
            return;

        _suppressResultsWeekEvents = true;
        var previousWeek = GetSelectedResultsWeekKey();
        _resultsWeekSelect.Clear();

        if (availableWeekKeys == null || availableWeekKeys.Count == 0)
        {
            _suppressResultsWeekEvents = false;
            return;
        }

        var popup = _resultsWeekSelect.GetPopup();
        for (var i = 0; i < availableWeekKeys.Count; i++)
        {
            var weekKey = availableWeekKeys[i];
            if (string.IsNullOrWhiteSpace(weekKey))
                continue;
            var label = GetResultsWeekLabel(weekKey);
            _resultsWeekSelect.AddItem(label);
            var index = _resultsWeekSelect.ItemCount - 1;
            if (popup != null)
                popup.SetItemMetadata(index, weekKey);
        }

        var targetWeek = !string.IsNullOrWhiteSpace(selectedWeekKey) ? selectedWeekKey : previousWeek;
        if (string.IsNullOrWhiteSpace(targetWeek) || !availableWeekKeys.Contains(targetWeek))
            targetWeek = GetPreferredResultsWeekKey(availableWeekKeys, _completedResultsWeekKeys);

        var targetIndex = FindResultsWeekIndex(targetWeek);
        if (targetIndex < 0 && _resultsWeekSelect.ItemCount > 0)
            targetIndex = 0;

        if (targetIndex >= 0)
            _resultsWeekSelect.Select(targetIndex);

        if (targetIndex >= 0)
            _selectedResultsWeekKey = GetResultsWeekKeyFromIndex(targetIndex);

        _suppressResultsWeekEvents = false;
    }

    private string GetSelectedResultsWeekKey()
    {
        if (_resultsWeekSelect != null && _resultsWeekSelect.ItemCount > 0)
        {
            var selectedIndex = _resultsWeekSelect.Selected;
            if (selectedIndex >= 0 && selectedIndex < _resultsWeekSelect.ItemCount)
            {
                var selectedKey = GetResultsWeekKeyFromIndex(selectedIndex);
                if (!string.IsNullOrWhiteSpace(selectedKey))
                    return selectedKey;
            }
        }

        if (!string.IsNullOrWhiteSpace(_selectedResultsWeekKey)
            && (_availableResultsWeekKeys.Count == 0 || _availableResultsWeekKeys.Contains(_selectedResultsWeekKey)))
            return _selectedResultsWeekKey;

        if (_availableResultsWeekKeys.Count > 0)
            return GetPreferredResultsWeekKey(_availableResultsWeekKeys, _completedResultsWeekKeys);

        return "";
    }

    internal static string GetPreferredResultsWeekKey(
        System.Collections.Generic.IEnumerable<string> availableWeekKeys,
        System.Collections.Generic.IEnumerable<string> completedWeekKeys)
    {
        var available = (availableWeekKeys ?? System.Linq.Enumerable.Empty<string>())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (available.Count == 0)
            return "";

        var completed = (completedWeekKeys ?? System.Linq.Enumerable.Empty<string>())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(key => available.Contains(key, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var candidates = completed.Count > 0 ? completed : available;
        candidates.Sort(CompareWeekKeys);
        return candidates[candidates.Count - 1];
    }

    private int FindResultsWeekIndex(string weekKey)
    {
        if (_resultsWeekSelect == null)
            return -1;

        for (var i = 0; i < _resultsWeekSelect.ItemCount; i++)
        {
            var key = GetResultsWeekKeyFromIndex(i);
            if (!string.IsNullOrWhiteSpace(key)
                && string.Equals(key, weekKey, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private string GetResultsWeekKeyFromIndex(int index)
    {
        if (_resultsWeekSelect == null)
            return "";
        var popup = _resultsWeekSelect.GetPopup();
        if (popup == null)
            return "";
        var meta = popup.GetItemMetadata(index);
        return FmtString(meta, "");
    }

    private string GetResultsWeekLabel(string weekKey)
    {
        if (string.IsNullOrWhiteSpace(weekKey))
            return "";
        if (_resultsWeekLabels.TryGetValue(weekKey, out var label)
            && !string.IsNullOrWhiteSpace(label))
            return label;
        return FormatWeekKeyLabel(weekKey);
    }

    private void ShowStandingsMessage(string message)
    {
        ShowStandingsMessage(_standingsTree, message);
        ShowOverviewStandingsMessage(message);
    }

    private static void ShowStandingsMessage(Tree tree, string message)
    {
        if (tree == null)
            return;

        tree.Clear();
        var root = tree.CreateItem();
        var item = tree.CreateItem(root);
        item.SetText(0, message);
    }

    private void ShowOverviewStandingsMessage(string message)
    {
        if (_overviewStandingsSnapshot == null)
            return;

        _overviewStandingsSnapshot.Text = string.IsNullOrWhiteSpace(message) ? "Standings unavailable." : message;
    }

    private void ShowResultsMessage(string message)
    {
        if (_resultsList == null)
            return;

        _resultsList.Clear();
        _resultsList.AddItem(message);
        ShowResultsListPanel();
    }

    private void ShowResultsListPanel()
    {
        if (_resultsListPanel == null || _boxScorePanel == null)
            return;

        _resultsListPanel.Visible = true;
        _boxScorePanel.Visible = false;
    }

    private void ShowBoxScorePanel()
    {
        if (_resultsListPanel == null || _boxScorePanel == null)
            return;

        _resultsListPanel.Visible = false;
        _boxScorePanel.Visible = true;
    }

    private void ClearBoxScore()
    {
        if (_boxScoreHeader != null)
            _boxScoreHeader.Text = "Box Score";

        _boxScoreQuarterTree?.Clear();
        _boxScoreTeamStatsTree?.Clear();
        _boxScoreLeadersList?.Clear();
    }

    private void ShowScheduleMessage(string message)
    {
        ClearScheduleSelectionState();
        if (_scheduleList == null)
            return;

        _scheduleList.Clear();
        var root = _scheduleList.CreateItem();
        var item = _scheduleList.CreateItem(root);
        item.SetText(0, message);
        UpdateScheduleActionUi(null);
    }

    private void ClearScheduleSelectionState()
    {
        _scheduleGames = new Godot.Collections.Array();
        _selectedScheduleGame = null;
        if (_scheduleList != null)
        {
            _scheduleList.DeselectAll();
            _scheduleList.Clear();
        }
    }

    private void ShowInjuriesMessage(string message)
    {
        if (_injuriesTree == null)
            return;

        _injuriesTree.Clear();
        var root = _injuriesTree.CreateItem();
        var item = _injuriesTree.CreateItem(root);
        item.SetText(0, message);
    }

    private void ShowHistoryMessage(string message)
    {
        _leagueHistorySeasons.Clear();
        _selectedHistorySeasonYear = null;

        if (_historySeasonList != null)
        {
            _suppressHistorySelectionEvents = true;
            _historySeasonList.Clear();
            _historySeasonList.AddItem(string.IsNullOrWhiteSpace(message) ? "No completed seasons yet." : message);
            _historySeasonList.DeselectAll();
            _suppressHistorySelectionEvents = false;
        }

        if (_historyDetailText != null)
            _historyDetailText.Text = string.IsNullOrWhiteSpace(message) ? "No completed seasons yet." : message;
    }

    private void PopulateHistoryView(List<LeagueHistorySeasonDto> seasons)
    {
        _leagueHistorySeasons.Clear();
        if (seasons != null)
            _leagueHistorySeasons.AddRange(seasons.Where(season => season != null));

        if (_leagueHistorySeasons.Count == 0)
        {
            ShowHistoryMessage("No completed seasons yet.");
            return;
        }

        if (_historySeasonList == null)
            return;

        _suppressHistorySelectionEvents = true;
        _historySeasonList.Clear();
        for (var i = 0; i < _leagueHistorySeasons.Count; i++)
            _historySeasonList.AddItem(BuildHistorySeasonListLabel(_leagueHistorySeasons[i]));

        var selectedIndex = 0;
        if (_selectedHistorySeasonYear.HasValue)
        {
            var existingIndex = _leagueHistorySeasons.FindIndex(season => season.SeasonYear == _selectedHistorySeasonYear.Value);
            if (existingIndex >= 0)
                selectedIndex = existingIndex;
        }

        _historySeasonList.Select(selectedIndex);
        _historySeasonList.EnsureCurrentIsVisible();
        _suppressHistorySelectionEvents = false;
        RenderHistorySeasonByIndex(selectedIndex);
    }

    private void OnHistorySeasonSelected(long index)
    {
        if (_suppressHistorySelectionEvents)
            return;

        RenderHistorySeasonByIndex((int)index);
    }

    private void RenderHistorySeasonByIndex(int index)
    {
        if (index < 0 || index >= _leagueHistorySeasons.Count)
        {
            ShowHistoryMessage("No completed seasons yet.");
            return;
        }

        var season = _leagueHistorySeasons[index];
        _selectedHistorySeasonYear = season.SeasonYear;
        if (_historyDetailText != null)
            _historyDetailText.Text = BuildHistoryDetailText(season);
    }

    private static string BuildHistorySeasonListLabel(LeagueHistorySeasonDto season)
    {
        if (season == null)
            return "Unknown Season";

        var champion = string.IsNullOrWhiteSpace(season.ChampionTeamName) ? "Champion TBD" : season.ChampionTeamName;
        return $"{season.SeasonYear} - {champion}";
    }

    private string BuildHistoryDetailText(LeagueHistorySeasonDto season)
    {
        if (season == null)
            return "No completed seasons yet.";

        var lines = new List<string>
        {
            $"{season.SeasonYear} Season History",
            $"Completed: {FallbackText(season.CompletedPhaseLabel, "Season Complete")}",
            $"League Champion: {FallbackText(season.ChampionTeamName, "TBD")}",
            $"Runner-Up: {FallbackText(season.RunnerUpTeamName, "TBD")}",
            $"{FallbackText(season.ChampionshipGameLabel, "League Championship")}: {FallbackText(season.ChampionTeamName, "TBD")} {season.ChampionshipWinnerScore}, {FallbackText(season.RunnerUpTeamName, "TBD")} {season.ChampionshipRunnerUpScore}",
            $"Regular-season games: {season.TotalRegularSeasonGames}",
            $"Playoff games: {season.TotalPlayoffGames}",
        };

        if (!string.IsNullOrWhiteSpace(season.GeneratedAtLabel))
            lines.Add($"Archived: {season.GeneratedAtLabel}");

        lines.Add("");
        lines.Add("Champion Summary");
        lines.Add($"Winner: {FallbackText(season.ChampionTeamName, "TBD")}");
        lines.Add($"Runner-Up: {FallbackText(season.RunnerUpTeamName, "TBD")}");

        lines.Add("");
        lines.Add("Final Standings");
        if (season.TeamRecords == null || season.TeamRecords.Count == 0)
        {
            lines.Add("No team records available.");
        }
        else
        {
            var currentGroup = "";
            foreach (var record in season.TeamRecords)
            {
                var group = $"{FallbackText(record.Conference, "Conference")} - {FallbackText(record.Division, "Division")}";
                if (!string.Equals(currentGroup, group, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrWhiteSpace(currentGroup))
                        lines.Add("");
                    lines.Add(group);
                    currentGroup = group;
                }

                var abbr = string.IsNullOrWhiteSpace(record.Abbreviation) ? "" : $" ({record.Abbreviation})";
                lines.Add($"{record.TeamName}{abbr}: {FormatRecord(record.Wins, record.Losses, record.Ties)} | PF {record.PointsFor} | PA {record.PointsAgainst} | Win% {record.WinPercentage:0.000}");
            }
        }

        lines.Add("");
        lines.Add("Playoff Seeds");
        if (season.PlayoffSeeds == null || season.PlayoffSeeds.Count == 0)
        {
            lines.Add("No playoff seeds available.");
        }
        else
        {
            foreach (var conferenceGroup in season.PlayoffSeeds.GroupBy(seed => FallbackText(seed.Conference, "League"), StringComparer.OrdinalIgnoreCase))
            {
                lines.Add(conferenceGroup.Key);
                foreach (var seed in conferenceGroup.OrderBy(entry => entry.Seed))
                {
                    var divisionWinnerTag = seed.IsDivisionWinner ? " [Division Winner]" : "";
                    lines.Add($"#{seed.Seed} {FallbackText(seed.TeamName, "TBD")} ({FallbackText(seed.Division, "Division")}){divisionWinnerTag}");
                }
                lines.Add("");
            }

            if (lines.Count > 0 && lines[^1] == "")
                lines.RemoveAt(lines.Count - 1);
        }

        lines.Add("");
        lines.Add("Playoff Results");
        AppendHistoryRound(lines, season, "Wild Card");
        AppendHistoryRound(lines, season, "Divisional");
        AppendHistoryRound(lines, season, "Conference Championship");
        AppendHistoryRound(lines, season, "League Championship");

        return string.Join("\n", lines);
    }

    private static void AppendHistoryRound(List<string> lines, LeagueHistorySeasonDto season, string roundName)
    {
        lines.Add(roundName);
        var games = (season?.PlayoffResults ?? new List<LeagueHistoryPlayoffResultDto>())
            .Where(result => string.Equals(NormalizeHistoryRound(result.Round), roundName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(result => result.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(result => result.HomeTeamName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (games.Count == 0)
        {
            lines.Add("No results recorded.");
            lines.Add("");
            return;
        }

        foreach (var game in games)
        {
            var prefix = string.IsNullOrWhiteSpace(game.Conference) || string.Equals(roundName, "League Championship", StringComparison.OrdinalIgnoreCase)
                ? ""
                : $"{game.Conference}: ";
            lines.Add($"{prefix}{FallbackText(game.WinnerTeamName, "TBD")} {game.HomeScore}-{game.AwayScore} over {FallbackText(game.LoserTeamName, "TBD")} ({FallbackText(game.AwayTeamName, "TBD")} at {FallbackText(game.HomeTeamName, "TBD")})");
        }

        lines.Add("");
    }

    private static string NormalizeHistoryRound(string round)
    {
        return (round ?? "").Trim().ToLowerInvariant() switch
        {
            "wild card" => "Wild Card",
            "divisional" => "Divisional",
            "divisional round" => "Divisional",
            "conference championship" => "Conference Championship",
            "league championship" => "League Championship",
            _ => FallbackText(round, "Unknown Round"),
        };
    }

    private static string FormatRecord(int wins, int losses, int ties)
    {
        return ties > 0
            ? $"{wins}-{losses}-{ties}"
            : $"{wins}-{losses}";
    }

    private static string FallbackText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private Godot.Collections.Array ExtractArrayPayload(Variant parsed, params string[] keys)
    {
        if (parsed.VariantType == Variant.Type.Array)
            return parsed.AsGodotArray();

        if (parsed.VariantType == Variant.Type.Dictionary)
        {
            var dict = parsed.AsGodotDictionary();
            foreach (var key in keys)
            {
                if (!dict.ContainsKey(key))
                    continue;

                var arrayVar = (Variant)dict[key];
                if (TryGetArray(arrayVar, out var array))
                    return array;
            }
        }

        return null;
    }

    private Godot.Collections.Array ExtractStandingsArray(Variant parsed)
    {
        var direct = ExtractArrayPayload(parsed, "rows", "standings", "table", "records", "teams");
        if (direct != null)
            return direct;

        if (parsed.VariantType == Variant.Type.Dictionary)
        {
            var dict = parsed.AsGodotDictionary();
            if (dict.ContainsKey("divisions"))
            {
                var divisionsVar = (Variant)dict["divisions"];
                if (TryGetArray(divisionsVar, out var divisions))
                {
                    var combined = new Godot.Collections.Array();
                    for (var i = 0; i < divisions.Count; i++)
                    {
                        var divisionVar = (Variant)divisions[i];
                        var rows = ExtractArrayPayload(divisionVar, "rows", "standings", "teams", "records");
                        if (rows == null)
                            continue;

                        for (var j = 0; j < rows.Count; j++)
                            combined.Add(rows[j]);
                    }

                    return combined;
                }
            }
        }

        return null;
    }

    private void PopulateStandingsTree(Godot.Collections.Array standings)
    {
        PopulateStandingsTree(_standingsTree, standings);
        PopulateOverviewStandingsSnapshot(standings);
    }

    private void PopulateOverviewStandingsSnapshot(Godot.Collections.Array standings)
    {
        if (_overviewStandingsSnapshot == null)
            return;

        var snapshot = BuildOverviewStandingsSnapshot(standings);
        if (snapshot == null || snapshot.Count == 0)
        {
            _overviewStandingsSnapshot.Text = "No standings data yet.";
            return;
        }

        var lines = new List<string>();
        for (var i = 0; i < snapshot.Count; i++)
        {
            var rowVar = (Variant)snapshot[i];
            if (!TryGetDictionary(rowVar, out var record))
                continue;

            var teamName = GetStandingsTeamName(record);
            var wins = FmtInt(GetRecordValue(record, "wins", "w", "win"), "0");
            var losses = FmtInt(GetRecordValue(record, "losses", "l", "loss"), "0");
            var ties = FmtInt(GetRecordValue(record, "ties", "t", "tie"), "0");
            var pointsFor = FmtInt(GetRecordValue(record, "points_for", "pf"), "0");
            var pointsAgainst = FmtInt(GetRecordValue(record, "points_against", "pa"), "0");
            var pctVar = GetRecordValue(record, "win_pct", "pct", "win_percentage", "percentage");
            var pctValue = GetFloatValue(pctVar, -1f);
            var pctText = pctValue >= 0f ? pctValue.ToString("0.000", CultureInfo.InvariantCulture) : "0.000";
            lines.Add($"{teamName}   {wins}-{losses}-{ties}   PF {pointsFor} / PA {pointsAgainst}   {pctText}");
        }

        _overviewStandingsSnapshot.Text = lines.Count > 0
            ? string.Join("\n", lines)
            : "No standings data yet.";
    }

    private Godot.Collections.Array BuildOverviewStandingsSnapshot(Godot.Collections.Array standings)
    {
        var snapshot = new Godot.Collections.Array();
        if (standings == null || standings.Count == 0)
            return snapshot;

        var userTeamId = !string.IsNullOrWhiteSpace(_userTeamId) ? _userTeamId : _currentTeamId;
        Godot.Collections.Dictionary userRow = null;
        var division = "";
        var conference = "";

        for (var i = 0; i < standings.Count; i++)
        {
            var rowVar = (Variant)standings[i];
            if (!TryGetDictionary(rowVar, out var record))
                continue;

            var teamId = FmtString(GetFirstNonNil(record, "team_id", "teamId"), "");
            if (!string.Equals(teamId, userTeamId, StringComparison.OrdinalIgnoreCase))
                continue;

            userRow = record;
            division = FmtString(GetFirstNonNil(record, "division"), "");
            conference = FmtString(GetFirstNonNil(record, "conference"), "");
            break;
        }

        AddStandingsSnapshotRows(snapshot, standings, row =>
        {
            if (!TryGetDictionary(row, out var record))
                return false;
            return !string.IsNullOrWhiteSpace(division)
                && string.Equals(FmtString(GetFirstNonNil(record, "division"), ""), division, StringComparison.OrdinalIgnoreCase);
        });

        if (snapshot.Count < 5)
        {
            AddStandingsSnapshotRows(snapshot, standings, row =>
            {
                if (!TryGetDictionary(row, out var record))
                    return false;
                return !string.IsNullOrWhiteSpace(conference)
                    && string.Equals(FmtString(GetFirstNonNil(record, "conference"), ""), conference, StringComparison.OrdinalIgnoreCase);
            });
        }

        if (snapshot.Count < 5)
            AddStandingsSnapshotRows(snapshot, standings, _ => true);

        if (snapshot.Count == 0 && userRow != null)
            snapshot.Add(userRow);

        return snapshot;
    }

    private static void AddStandingsSnapshotRows(
        Godot.Collections.Array target,
        Godot.Collections.Array source,
        Func<Variant, bool> includeRow)
    {
        if (target == null || source == null || includeRow == null)
            return;

        for (var i = 0; i < source.Count && target.Count < 5; i++)
        {
            var rowVar = (Variant)source[i];
            if (!includeRow(rowVar))
                continue;
            if (!TryGetDictionary(rowVar, out var record))
                continue;

            var teamId = FmtString(GetFirstNonNil(record, "team_id", "teamId"), "");
            var alreadyIncluded = false;
            for (var existingIndex = 0; existingIndex < target.Count; existingIndex++)
            {
                var existingVar = (Variant)target[existingIndex];
                if (!TryGetDictionary(existingVar, out var existingRecord))
                    continue;
                var existingTeamId = FmtString(GetFirstNonNil(existingRecord, "team_id", "teamId"), "");
                if (string.Equals(existingTeamId, teamId, StringComparison.OrdinalIgnoreCase))
                {
                    alreadyIncluded = true;
                    break;
                }
            }

            if (!alreadyIncluded)
                target.Add(record);
        }
    }

    private void PopulateStandingsTree(Tree tree, Godot.Collections.Array standings)
    {
        if (tree == null)
            return;

        tree.Clear();
        var root = tree.CreateItem();

        if (standings == null || standings.Count == 0)
        {
            var emptyItem = tree.CreateItem(root);
            emptyItem.SetText(0, "No standings data.");
            return;
        }

        for (var i = 0; i < standings.Count; i++)
        {
            var rowVar = (Variant)standings[i];
            if (!TryGetDictionary(rowVar, out var record))
            {
                var errorItem = tree.CreateItem(root);
                errorItem.SetText(0, "(error)");
                continue;
            }

            var teamName = GetStandingsTeamName(record);
            var winsVar = GetRecordValue(record, "wins", "w", "win");
            var lossesVar = GetRecordValue(record, "losses", "l", "loss");
            var tiesVar = GetRecordValue(record, "ties", "t", "tie");
            var pointsForVar = GetRecordValue(record, "points_for", "pf");
            var pointsAgainstVar = GetRecordValue(record, "points_against", "pa");
            var pctVar = GetRecordValue(record, "win_pct", "pct", "win_percentage", "percentage");

            var winsText = FmtInt(winsVar, "?");
            var lossesText = FmtInt(lossesVar, "?");
            var tiesText = FmtInt(tiesVar, "0");
            var pointsForText = FmtInt(pointsForVar, "0");
            var pointsAgainstText = FmtInt(pointsAgainstVar, "0");
            var pctText = "";

            var pctValue = GetFloatValue(pctVar, -1f);
            if (pctValue >= 0f)
                pctText = pctValue.ToString("0.000", CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(pctText))
            {
                var wins = GetIntValue(winsVar, -1);
                var losses = GetIntValue(lossesVar, -1);
                var ties = GetIntValue(tiesVar, 0);
                var total = wins + losses + ties;
                if (wins >= 0 && losses >= 0 && total > 0)
                {
                    var pct = (wins + (0.5f * ties)) / total;
                    pctText = pct.ToString("0.000", CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrWhiteSpace(teamName))
                teamName = "Team";

            var item = tree.CreateItem(root);
            item.SetText(0, teamName);
            item.SetText(1, $"{(string.IsNullOrWhiteSpace(winsText) ? "?" : winsText)}-{(string.IsNullOrWhiteSpace(lossesText) ? "?" : lossesText)}-{(string.IsNullOrWhiteSpace(tiesText) ? "0" : tiesText)}");
            item.SetText(2, string.IsNullOrWhiteSpace(pointsForText) ? "0" : pointsForText);
            item.SetText(3, string.IsNullOrWhiteSpace(pointsAgainstText) ? "0" : pointsAgainstText);
            item.SetText(4, string.IsNullOrWhiteSpace(pctText) ? "?" : pctText);
        }
    }

    private void PopulateResultsList(Godot.Collections.Array results)
    {
        _resultsGames = results ?? new Godot.Collections.Array();
        if (_resultsList == null)
            return;

        _resultsList.Clear();
        ShowResultsListPanel();
        ClearBoxScore();
        if (results == null || results.Count == 0)
        {
            _resultsList.AddItem("No results.");
            return;
        }

        for (var i = 0; i < results.Count; i++)
        {
            var resultVar = (Variant)results[i];
            if (!TryGetDictionary(resultVar, out var game))
            {
                _resultsList.AddItem("(error)");
                continue;
            }

            var line = FormatGameSummary(game, "");
            if (string.IsNullOrWhiteSpace(line))
                line = "(error)";
            var index = _resultsList.AddItem(line);
            var gameId = GetGameId(game);
            var metadata = new Godot.Collections.Dictionary
            {
                { "index", i }
            };
            if (!string.IsNullOrWhiteSpace(gameId))
                metadata["game_id"] = gameId;
            _resultsList.SetItemMetadata(index, metadata);
        }
    }

    private async Task OnResultSelected(long index)
    {
        var itemIndex = (int)index;
        if (itemIndex < 0 || itemIndex >= _resultsList.ItemCount)
            return;

        var meta = _resultsList.GetItemMetadata(itemIndex);
        var selectionVersion = ++_resultsSelectionVersion;
        var gameId = "";
        var resultIndex = itemIndex;

        if (meta.VariantType == Variant.Type.Dictionary && TryGetDictionary(meta, out var metaDict))
        {
            if (metaDict.ContainsKey("game_id"))
                gameId = FmtString((Variant)metaDict["game_id"], "");
            if (metaDict.ContainsKey("index"))
                resultIndex = GetIntValue((Variant)metaDict["index"], itemIndex);
        }
        else
        {
            gameId = FmtString(meta, "");
            resultIndex = GetIntValue(meta, itemIndex);
        }

        if (string.IsNullOrWhiteSpace(gameId))
        {
            if (_resultsGames != null && resultIndex >= 0 && resultIndex < _resultsGames.Count)
            {
                var gameVar = (Variant)_resultsGames[resultIndex];
                if (TryGetDictionary(gameVar, out var fallbackGame))
                {
                    gameId = GetGameId(fallbackGame);
                    if (string.IsNullOrWhiteSpace(gameId))
                    {
                        ShowBoxScoreForGame(fallbackGame);
                        return;
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(gameId))
        {
            SetStateDumpText("Box score unavailable.");
            return;
        }

        if (_gameCache.TryGetValue(gameId, out var cachedGame))
        {
            ShowBoxScoreForGame(cachedGame);
            return;
        }

        if (IsNativeRuntimeSource())
        {
            if (TryShowNativeGameResult(gameId, "Unable to load game result.", "Loaded game result."))
                return;

            SetStateDumpText("Box score unavailable.");
            return;
        }

        ClearBoxScore();
        if (_boxScoreHeader != null)
            _boxScoreHeader.Text = "Box Score: loading...";
        ShowBoxScorePanel();

        var (status, body) = await GetWithTimeoutAsync($"/game/{gameId}", REQUEST_TIMEOUT_MS);
        if (selectionVersion != _resultsSelectionVersion)
            return;

        if (status < 200 || status >= 300)
        {
            SetStateDumpText($"Box score unavailable. HTTP {status}.");
            if (_boxScoreHeader != null)
                _boxScoreHeader.Text = "Box Score: (error)";
            ShowBoxScorePanel();
            return;
        }

        var loggedParseFailure = false;
        void LogBoxScoreParseFailure(Variant parsedPayload)
        {
            if (loggedParseFailure)
                return;

            loggedParseFailure = true;
            var keys = "(non-dict)";
            if (parsedPayload.VariantType == Variant.Type.Dictionary)
                keys = string.Join(", ", parsedPayload.AsGodotDictionary().Keys);
            var head = GetBodyHead(body, 400);
            GD.PrintErr($"BoxScore parse failed. status={status} keys={keys} body_head={head}");
        }

        var parsed = Json.ParseString(body);
        if (!TryExtractGamePayload(parsed, out var gamePayload))
        {
            LogBoxScoreParseFailure(parsed);
            SetStateDumpText("Box score unavailable.");
            if (_boxScoreHeader != null)
                _boxScoreHeader.Text = "Box Score: (error)";
            ShowBoxScorePanel();
            return;
        }

        _gameCache[gameId] = gamePayload;
        ShowBoxScoreForGame(gamePayload);
    }

    private void OnBoxScoreBack()
    {
        ShowResultsListPanel();
        if (_resultsList != null)
            _resultsList.DeselectAll();
        ClearBoxScore();
    }

    private void ShowBoxScoreForGame(Godot.Collections.Dictionary game)
    {
        ClearBoxScore();

        if (!TryResolveBoxScoreObjects(game, out var gameObj, out var boxScore))
        {
            if (_boxScoreHeader != null)
                _boxScoreHeader.Text = "Box Score: (missing)";
            ShowBoxScorePanel();
            return;
        }

        var awayVar = TryExtract(
            boxScore,
            "away_team",
            "away",
            "away_team_id",
            "awayTeamId",
            "away_id",
            "awayId",
            "away_teamId");
        if (IsNil(awayVar))
        {
            awayVar = TryExtract(
                gameObj,
                "away_team",
                "away",
                "away_team_id",
                "awayTeamId",
                "away_id",
                "awayId",
                "away_teamId");
        }

        var homeVar = TryExtract(
            boxScore,
            "home_team",
            "home",
            "home_team_id",
            "homeTeamId",
            "home_id",
            "homeId",
            "home_teamId");
        if (IsNil(homeVar))
        {
            homeVar = TryExtract(
                gameObj,
                "home_team",
                "home",
                "home_team_id",
                "homeTeamId",
                "home_id",
                "homeId",
                "home_teamId");
        }

        var awayAbbr = GetTeamAbbr(awayVar);
        var homeAbbr = GetTeamAbbr(homeVar);

        var (awayScore, homeScore) = GetFinalScores(gameObj, boxScore);
        var awayLabel = string.IsNullOrWhiteSpace(awayAbbr) ? "Away" : awayAbbr;
        var homeLabel = string.IsNullOrWhiteSpace(homeAbbr) ? "Home" : homeAbbr;
        if (_boxScoreHeader != null)
            _boxScoreHeader.Text = $"{awayLabel} {awayScore} @ {homeLabel} {homeScore}";

        PopulateBoxScoreQuarterTree(boxScore, awayLabel, homeLabel, awayScore, homeScore);
        PopulateBoxScoreTeamStatsTree(boxScore, awayLabel, homeLabel);
        PopulateBoxScoreLeaders(boxScore, awayLabel, homeLabel);

        ShowBoxScorePanel();
    }

    private void PopulateInjuryTree(Godot.Collections.Array entries)
    {
        if (_injuriesTree == null)
            return;

        _injuriesTree.Clear();
        var root = _injuriesTree.CreateItem();

        if (entries == null || entries.Count == 0)
        {
            var emptyItem = _injuriesTree.CreateItem(root);
            emptyItem.SetText(0, "No injuries.");
            return;
        }

        for (var i = 0; i < entries.Count; i++)
        {
            var entryVar = (Variant)entries[i];
            if (!TryGetDictionary(entryVar, out var entry))
            {
                var errorItem = _injuriesTree.CreateItem(root);
                errorItem.SetText(0, "(error)");
                continue;
            }

            var name = FmtString(GetFirstNonNil(entry, "name", "player_name", "player"), "");
            var pos = FmtString(GetFirstNonNil(entry, "position", "pos"), "");
            var status = FmtString(GetFirstNonNil(entry, "injury_status", "status"), "");
            var injury = FmtString(GetFirstNonNil(entry, "injury_name", "injury"), "");
            var returnDate = FmtString(GetFirstNonNil(entry, "injury_end_date", "return_date", "return"), "");
            var daysLeft = FmtInt(GetFirstNonNil(entry, "days_remaining", "days_left"), "");
            var onIr = GetBoolValue(GetFirstNonNil(entry, "on_injured_reserve", "ir"), false);
            var irText = onIr ? "Yes" : "";

            if (string.IsNullOrWhiteSpace(name))
                name = "Player";

            var item = _injuriesTree.CreateItem(root);
            item.SetText(0, name);
            item.SetText(1, pos);
            item.SetText(2, status);
            item.SetText(3, injury);
            item.SetText(4, returnDate);
            item.SetText(5, daysLeft);
            item.SetText(6, irText);
        }
    }

    private void PopulateScheduleList(Godot.Collections.Array games, string teamId)
    {
        _scheduleGames = games ?? new Godot.Collections.Array();
        _selectedScheduleGame = null;
        if (_scheduleList == null)
            return;

        _scheduleList.Clear();
        var root = _scheduleList.CreateItem();
        if (games == null || games.Count == 0)
        {
            var emptyItem = _scheduleList.CreateItem(root);
            emptyItem.SetText(0, "No schedule.");
            UpdateScheduleActionUi(null);
            return;
        }

        for (var i = 0; i < games.Count; i++)
        {
            var gameVar = (Variant)games[i];
            if (!TryGetDictionary(gameVar, out var game))
            {
                var errorItem = _scheduleList.CreateItem(root);
                errorItem.SetText(0, "(error)");
                errorItem.SetMetadata(0, i);
                continue;
            }

            var item = _scheduleList.CreateItem(root);
            item.SetMetadata(0, i);
            item.SetText(0, GetScheduleStatusLabel(game));
            item.SetText(1, BuildScheduleMatchupText(game, teamId));
            item.SetText(2, GetScheduleWeekText(game));
            item.SetText(3, GetScheduleResultText(game));
            item.SetText(4, GetScheduleActionLabel(game));
        }
        UpdateScheduleActionUi(null);
    }

    private static string GetScheduleStatusLabel(Godot.Collections.Dictionary game)
    {
        if (game == null)
            return "";

        var status = FmtString(GetFirstNonNil(game, "status"), "upcoming").Trim().ToLowerInvariant();
        return status switch
        {
            "final" => "Final",
            "game_day" => "Game Ready",
            _ => "Upcoming",
        };
    }

    private string BuildScheduleMatchupText(Godot.Collections.Dictionary game, string focusTeamId)
    {
        if (game == null)
            return "";

        var homeTeam = FmtString(GetFirstNonNil(game, "home_team", "home", "home_abbr"), "");
        var awayTeam = FmtString(GetFirstNonNil(game, "away_team", "away", "away_abbr"), "");
        var opponent = ResolveScheduleOpponent(game, focusTeamId, GetScheduleIsHome(game, focusTeamId));
        var homeAway = FmtString(GetFirstNonNil(game, "home_away", "homeAway"), "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(opponent))
            opponent = "Opponent";

        var status = FmtString(GetFirstNonNil(game, "status"), "upcoming").Trim().ToLowerInvariant();
        if (status == "final")
        {
            if (string.IsNullOrWhiteSpace(homeTeam))
                homeTeam = "HOME";
            if (string.IsNullOrWhiteSpace(awayTeam))
                awayTeam = "AWAY";
            return $"{homeTeam} vs {awayTeam}";
        }

        return homeAway == "away" || homeAway == "@"
            ? $"at {opponent}"
            : $"vs {opponent}";
    }

    private static string GetScheduleWeekText(Godot.Collections.Dictionary game)
    {
        if (game == null)
            return "";

        var weekLabel = FmtString(GetFirstNonNil(game, "week_label", "weekLabel"), "");
        if (!string.IsNullOrWhiteSpace(weekLabel))
            return weekLabel;

        var gameType = HumanizeStatus(FmtString(GetFirstNonNil(game, "game_type", "season_type"), ""));
        var weekValue = FmtString(GetFirstNonNil(game, "phase_week", "phaseWeek", "week", "season_week", "calendar_week"), "");
        return string.IsNullOrWhiteSpace(gameType)
            ? $"Week {weekValue}"
            : string.IsNullOrWhiteSpace(weekValue) ? gameType : $"{gameType} Week {weekValue}";
    }

    private static string GetScheduleResultText(Godot.Collections.Dictionary game)
    {
        if (game == null)
            return "";

        var status = FmtString(GetFirstNonNil(game, "status"), "upcoming").Trim().ToLowerInvariant();
        if (status == "final")
        {
            var homeScore = FmtInt(GetFirstNonNil(game, "home_score"), "-");
            var awayScore = FmtInt(GetFirstNonNil(game, "away_score"), "-");
            var homeTeam = FmtString(GetFirstNonNil(game, "home_team", "home", "home_abbr"), "HOME");
            var awayTeam = FmtString(GetFirstNonNil(game, "away_team", "away", "away_abbr"), "AWAY");
            return $"{homeTeam} {homeScore} - {awayTeam} {awayScore}";
        }

        return "-";
    }

    private static string GetScheduleActionLabel(Godot.Collections.Dictionary game)
    {
        if (game == null)
            return "";

        var status = FmtString(GetFirstNonNil(game, "status"), "upcoming").Trim().ToLowerInvariant();
        return status switch
        {
            "final" => "View Recap",
            "game_day" => "View Matchup",
            _ => "Preview later",
        };
    }

    private void OnScheduleItemSelected()
    {
        if (_scheduleList == null)
        {
            UpdateScheduleActionUi(null);
            return;
        }

        var selected = _scheduleList.GetSelected();
        if (selected == null)
        {
            UpdateScheduleActionUi(null);
            return;
        }

        var metadata = selected.GetMetadata(0);
        var scheduleIndex = GetIntValue(metadata, -1);
        if (scheduleIndex < 0 || _scheduleGames == null || scheduleIndex >= _scheduleGames.Count)
        {
            UpdateScheduleActionUi(null);
            return;
        }

        var gameVar = (Variant)_scheduleGames[scheduleIndex];
        if (!TryGetDictionary(gameVar, out var game))
        {
            UpdateScheduleActionUi(null);
            return;
        }

        _selectedScheduleGame = game;
        UpdateScheduleActionUi(game);
    }

    private void UpdateScheduleActionUi(Godot.Collections.Dictionary game)
    {
        var status = game != null ? FmtString(GetFirstNonNil(game, "status"), "upcoming").Trim().ToLowerInvariant() : "";

        if (_lblScheduleActionStatus != null)
        {
            _lblScheduleActionStatus.Text = status switch
            {
                "final" => "Completed game. Open the recap, then use Box Score from the recap popup.",
                "game_day" => "Current user game is ready. Open the matchup popup to sim the game.",
                "upcoming" => "Future matchup preview is coming later.",
                _ => "Select a game to view details.",
            };
        }

        if (_btnScheduleAction == null)
            return;

        _btnScheduleAction.Disabled = true;
        _btnScheduleAction.Text = "View";

        if (status == "final")
        {
            _btnScheduleAction.Disabled = false;
            _btnScheduleAction.Text = "View Recap";
        }
        else if (status == "game_day")
        {
            _btnScheduleAction.Disabled = false;
            _btnScheduleAction.Text = "View Matchup";
        }
        else if (status == "upcoming")
        {
            _btnScheduleAction.Disabled = true;
            _btnScheduleAction.Text = "Preview later";
        }
    }

    private async Task OnScheduleActionPressed()
    {
        var game = _selectedScheduleGame;
        if (game == null)
        {
            SetPrimaryStatus("Select a scheduled game first.");
            return;
        }

        var status = FmtString(GetFirstNonNil(game, "status"), "upcoming").Trim().ToLowerInvariant();
        if (status == "final")
        {
            await OpenCompletedScheduleGameAsync(game);
            return;
        }

        if (status == "game_day")
        {
            OpenGameDayPopupFromScheduleRow(game);
            return;
        }

        SetPrimaryStatus("Future matchup preview is coming later.");
    }

    private string GetStandingsTeamName(Godot.Collections.Dictionary record)
    {
        var teamVar = GetFirstNonNil(record, "team", "team_info");
        if (!IsNil(teamVar))
        {
            if (TryGetDictionary(teamVar, out var teamDict))
                return FormatTeamDisplay(teamDict);

            var rawTeam = FmtString(teamVar, "");
            if (!string.IsNullOrWhiteSpace(rawTeam))
                return rawTeam;
        }

        var teamId = FmtString(GetFirstNonNil(record, "team_id", "id"), "");
        var fromId = ResolveTeamNameFromId(teamId);
        if (!string.IsNullOrWhiteSpace(fromId))
            return fromId;

        var abbr = FmtString(GetFirstNonNil(record, "abbreviation", "abbr", "short_name"), "");
        var name = FmtString(GetFirstNonNil(record, "team_name", "name", "nickname"), "");
        var city = FmtString(GetFirstNonNil(record, "city", "location"), "");
        var combined = $"{city} {name}".Trim();
        if (!string.IsNullOrWhiteSpace(abbr))
            return string.IsNullOrWhiteSpace(combined) ? abbr : $"{abbr} - {combined}";

        return string.IsNullOrWhiteSpace(combined) ? "Team" : combined;
    }

    private static Variant GetRecordValue(Godot.Collections.Dictionary record, params string[] keys)
    {
        var value = GetFirstNonNil(record, keys);
        if (!IsNil(value))
            return value;

        if (record.ContainsKey("record"))
        {
            var recordVar = (Variant)record["record"];
            if (TryGetDictionary(recordVar, out var recordDict))
                return GetFirstNonNil(recordDict, keys);
        }

        return default;
    }

    private string ResolveTeamName(Variant teamVar)
    {
        if (IsNil(teamVar))
            return "";

        if (teamVar.VariantType == Variant.Type.Dictionary && TryGetDictionary(teamVar, out var teamDict))
            return FormatTeamDisplay(teamDict);

        var idOrName = FmtString(teamVar, "");
        if (string.IsNullOrWhiteSpace(idOrName))
            return "";

        var fromId = ResolveTeamNameFromId(idOrName);
        return string.IsNullOrWhiteSpace(fromId) ? idOrName : fromId;
    }

    private string ResolveTeamNameFromId(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return "";

        return _teamDisplayById.TryGetValue(teamId, out var display) ? display : "";
    }

    private static string FormatTeamDisplay(Godot.Collections.Dictionary team)
    {
        var abbr = FmtString(GetFirstNonNil(team, "abbreviation", "abbr", "short_name"), "");
        var name = FmtString(GetFirstNonNil(team, "team_name", "name", "nickname"), "");
        var city = FmtString(GetFirstNonNil(team, "city", "location"), "");
        var combined = $"{city} {name}".Trim();
        if (!string.IsNullOrWhiteSpace(abbr))
            return string.IsNullOrWhiteSpace(combined) ? abbr : $"{abbr} - {combined}";

        return string.IsNullOrWhiteSpace(combined) ? "Team" : combined;
    }

    private string FormatScheduleSummary(Godot.Collections.Dictionary game, string focusTeamId, ref bool loggedUnresolvedOpponent)
    {
        var headerText = FormatSeasonWeekHeader(game);
        var prefix = string.IsNullOrWhiteSpace(headerText) ? "" : $"{headerText}: ";

        var isHome = GetScheduleIsHome(game, focusTeamId);
        var opponent = ResolveScheduleOpponent(game, focusTeamId, isHome);
        if (string.IsNullOrWhiteSpace(opponent))
        {
            opponent = "UNKNOWN";
            if (!loggedUnresolvedOpponent)
            {
                if (ShouldLogScheduleOpponentUnresolved(game))
                {
                    LogScheduleOpponentUnresolved(game);
                    loggedUnresolvedOpponent = true;
                }
            }
        }

        var locationPrefix = isHome.HasValue ? (isHome.Value ? "vs " : "@ ") : "vs ";
        var line = $"{prefix}{locationPrefix}{opponent}".Trim();

        var homeScore = FmtInt(GetFirstNonNil(game, "home_score", "home_points", "home_pts", "score_home"), "");
        var awayScore = FmtInt(GetFirstNonNil(game, "away_score", "away_points", "away_pts", "score_away"), "");

        if (isHome.HasValue)
        {
            var leftScore = isHome.Value ? homeScore : awayScore;
            var rightScore = isHome.Value ? awayScore : homeScore;
            line = $"{line}{FormatScoreSuffix(leftScore, rightScore)}".Trim();
        }
        else
        {
            line = $"{line}{FormatScoreSuffix(awayScore, homeScore)}".Trim();
        }

        return line;
    }

    private bool? GetScheduleIsHome(Godot.Collections.Dictionary game, string focusTeamId)
    {
        if (game == null)
            return null;

        var homeFlag = ParseHomeAwayFlag(GetFirstNonNil(game, "is_home", "home", "isHome"));
        if (homeFlag.HasValue)
            return homeFlag;

        var homeAwayFlag = ParseHomeAwayFlag(GetFirstNonNil(game, "home_away", "homeAway"));
        if (homeAwayFlag.HasValue)
            return homeAwayFlag;

        if (!string.IsNullOrWhiteSpace(focusTeamId))
        {
            var homeVar = GetFirstNonNil(game, "home_team", "home_team_id", "homeTeamId", "home_id", "home_teamId");
            var awayVar = GetFirstNonNil(game, "away_team", "away_team_id", "awayTeamId", "away_id", "away_teamId");
            var homeId = GetTeamIdFromVariant(homeVar);
            var awayId = GetTeamIdFromVariant(awayVar);
            if (!string.IsNullOrWhiteSpace(homeId)
                && string.Equals(homeId, focusTeamId, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(awayId)
                && string.Equals(awayId, focusTeamId, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return null;
    }

    private static List<string> ParseStringList(Godot.Collections.Array values)
    {
        var parsed = new List<string>();
        if (values == null)
            return parsed;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < values.Count; i++)
        {
            var valueVar = (Variant)values[i];
            var text = FmtString(valueVar, "").Trim();
            if (string.IsNullOrWhiteSpace(text) || seen.Contains(text))
                continue;

            seen.Add(text);
            parsed.Add(text);
        }

        return parsed;
    }

    private static string FormatWeekKeyLabel(string weekKey)
    {
        if (string.IsNullOrWhiteSpace(weekKey))
            return "";

        var parts = weekKey.Split(':', 2);
        if (parts.Length == 2 && int.TryParse(parts[1], out var weekNum))
        {
            var season = parts[0].Trim().ToLowerInvariant();
            if (season == "preseason")
                return $"Pre W{weekNum}";
            if (season == "regular")
                return $"W{weekNum}";
            if (season == "postseason" || season == "playoffs")
                return $"Post W{weekNum}";
        }

        return weekKey;
    }

    private static string FormatWeekKeyHeader(string weekKey)
    {
        if (string.IsNullOrWhiteSpace(weekKey))
            return "";

        var parts = weekKey.Split(':', 2);
        if (parts.Length == 2 && int.TryParse(parts[1], out var weekNum))
        {
            var season = parts[0].Trim().ToLowerInvariant();
            if (season == "preseason")
                return $"Preseason Week {weekNum}";
            if (season == "regular")
                return $"Regular Season Week {weekNum}";
            if (season == "postseason" || season == "playoffs")
                return $"Postseason Week {weekNum}";
        }

        return weekKey;
    }

    private static bool? ParseHomeAwayFlag(Variant value)
    {
        if (IsNil(value))
            return null;

        if (value.VariantType == Variant.Type.Bool)
            return value.AsBool();
        if (value.VariantType == Variant.Type.Int)
            return value.AsInt32() != 0;
        if (value.VariantType == Variant.Type.Float)
            return Math.Abs(value.AsDouble()) > 0.0001;

        if (value.VariantType == Variant.Type.String)
        {
            var str = value.AsString();
            if (string.IsNullOrWhiteSpace(str))
                return null;
            if (bool.TryParse(str, out var parsedBool))
                return parsedBool;
            if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                return parsedInt != 0;

            var normalized = str.Trim().ToLowerInvariant();
            if (normalized == "home" || normalized == "h" || normalized == "vs")
                return true;
            if (normalized == "away" || normalized == "a" || normalized == "@")
                return false;
        }

        return null;
    }

    private string ResolveScheduleOpponent(Godot.Collections.Dictionary game, string focusTeamId, bool? isHome)
    {
        var opponent = FmtString(GetFirstNonNil(
            game,
            "opponent_abbr",
            "opponentAbbr",
            "opponent_abbreviation",
            "opponentAbbreviation"), "");
        if (!string.IsNullOrWhiteSpace(opponent))
            return opponent;

        opponent = FmtString(GetFirstNonNil(game, "opponent_name", "opponentName"), "");
        if (!string.IsNullOrWhiteSpace(opponent))
            return opponent;

        var opponentVar = GetFirstNonNil(game, "opponent");
        if (!IsNil(opponentVar) && opponentVar.VariantType == Variant.Type.String)
        {
            opponent = opponentVar.AsString();
            if (!string.IsNullOrWhiteSpace(opponent))
                return opponent;
        }

        var opponentIdVar = GetFirstNonNil(
            game,
            "opponent_id",
            "opponentId",
            "opponent_team_id",
            "opponentTeamId");
        if (!IsNil(opponentIdVar) && opponentIdVar.VariantType == Variant.Type.String)
        {
            var opponentId = opponentIdVar.AsString();
            opponent = ResolveTeamShortFromId(opponentId);
            if (!string.IsNullOrWhiteSpace(opponent))
                return opponent;
            if (LooksLikeTeamAbbr(opponentId))
                return opponentId;
        }

        var homeId = FmtString(GetFirstNonNil(game, "home_team_id", "homeTeamId"), "");
        var awayId = FmtString(GetFirstNonNil(game, "away_team_id", "awayTeamId"), "");
        if (string.IsNullOrWhiteSpace(homeId))
        {
            var homeVar = GetFirstNonNil(game, "home_team", "home_id", "homeTeamId", "home_teamId");
            homeId = GetTeamIdFromVariant(homeVar);
        }
        if (string.IsNullOrWhiteSpace(awayId))
        {
            var awayVar = GetFirstNonNil(game, "away_team", "away_id", "awayTeamId", "away_teamId");
            awayId = GetTeamIdFromVariant(awayVar);
        }

        if (!string.IsNullOrWhiteSpace(focusTeamId))
        {
            if (!string.IsNullOrWhiteSpace(homeId)
                && string.Equals(homeId, focusTeamId, StringComparison.OrdinalIgnoreCase))
            {
                opponent = ResolveTeamShortFromId(awayId);
                if (!string.IsNullOrWhiteSpace(opponent))
                    return opponent;
            }

            if (!string.IsNullOrWhiteSpace(awayId)
                && string.Equals(awayId, focusTeamId, StringComparison.OrdinalIgnoreCase))
            {
                opponent = ResolveTeamShortFromId(homeId);
                if (!string.IsNullOrWhiteSpace(opponent))
                    return opponent;
            }
        }

        if (isHome.HasValue)
        {
            var derivedOpponentId = isHome.Value ? awayId : homeId;
            opponent = ResolveTeamShortFromId(derivedOpponentId);
            if (!string.IsNullOrWhiteSpace(opponent))
                return opponent;
        }

        return "";
    }

    private void LogScheduleOpponentUnresolved(Godot.Collections.Dictionary game)
    {
        if (game == null)
            return;

        var keys = string.Join(", ", game.Keys);
        var item = InlineMessage(game.ToString(), 240);
        GD.PrintErr($"Schedule opponent unresolved. Keys={keys} item={item}");
    }

    private static bool ShouldLogScheduleOpponentUnresolved(Godot.Collections.Dictionary game)
    {
        if (game == null)
            return true;

        var opponentIdVar = GetFirstNonNil(
            game,
            "opponent_id",
            "opponentId",
            "opponent_team_id",
            "opponentTeamId");
        if (IsNil(opponentIdVar))
            return true;

        return opponentIdVar.VariantType != Variant.Type.String;
    }

    private static bool LooksLikeTeamAbbr(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed.Length < 2 || trimmed.Length > 4)
            return false;

        for (var i = 0; i < trimmed.Length; i++)
        {
            if (!char.IsLetter(trimmed[i]))
                return false;
        }

        return true;
    }

    private string ResolveTeamShortFromId(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return "";

        if (_teamShortById.TryGetValue(teamId, out var shortLabel)
            && !string.IsNullOrWhiteSpace(shortLabel))
            return shortLabel;

        if (_teamDisplayById.TryGetValue(teamId, out var display)
            && !string.IsNullOrWhiteSpace(display))
        {
            var dashIndex = display.IndexOf(" - ", StringComparison.Ordinal);
            if (dashIndex > 0)
            {
                var abbr = display.Substring(0, dashIndex).Trim();
                if (!string.IsNullOrWhiteSpace(abbr) && !string.Equals(abbr, "??", StringComparison.Ordinal))
                    return abbr;

                var namePart = display.Substring(dashIndex + 3).Trim();
                if (!string.IsNullOrWhiteSpace(namePart))
                    return namePart;
            }

            return display;
        }

        return "";
    }

    private string FormatSeasonWeekHeader(Godot.Collections.Dictionary game)
    {
        if (game == null)
            return "";

        var directWeekLabel = FmtString(GetFirstNonNil(game, "week_label", "weekLabel"), "");
        if (!string.IsNullOrWhiteSpace(directWeekLabel))
            return directWeekLabel;

        var seasonType = FmtString(GetFirstNonNil(game, "season_type", "seasonType", "season"), "");
        var seasonWeek = GetIntValue(GetFirstNonNil(game, "season_week", "seasonWeek"), 0);
        if (!string.IsNullOrWhiteSpace(seasonType) && seasonWeek > 0)
        {
            var normalized = seasonType.Trim().ToLowerInvariant();
            if (normalized == "preseason")
                return $"Preseason Week {seasonWeek}";
            if (normalized == "regular")
                return $"Regular Season Week {seasonWeek}";
            if (normalized == "postseason" || normalized == "playoffs")
                return $"Postseason Week {seasonWeek}";
            return $"{seasonType} Week {seasonWeek}";
        }

        var weekKey = FmtString(GetFirstNonNil(game, "week_key", "weekKey"), "");
        if (!string.IsNullOrWhiteSpace(weekKey))
            return FormatWeekKeyHeader(weekKey);

        var calendarWeek = FmtInt(GetFirstNonNil(game, "phase_week", "phaseWeek", "calendar_week", "calendarWeek", "week", "week_num", "week_number"), "");
        if (!string.IsNullOrWhiteSpace(calendarWeek))
            return $"Week {calendarWeek}";

        return "";
    }

    private string FormatGameSummary(Godot.Collections.Dictionary game, string focusTeamId)
    {
        var headerText = FormatSeasonWeekHeader(game);
        var homeVar = GetFirstNonNil(game, "home_team", "home", "home_team_id", "home_id", "home_teamId");
        var awayVar = GetFirstNonNil(game, "away_team", "away", "away_team_id", "away_id", "away_teamId");

        var homeName = ResolveTeamName(homeVar);
        var awayName = ResolveTeamName(awayVar);
        var homeScore = FmtInt(GetFirstNonNil(game, "home_score", "home_points", "home_pts", "score_home"), "");
        var awayScore = FmtInt(GetFirstNonNil(game, "away_score", "away_points", "away_pts", "score_away"), "");
        var status = FmtString(GetFirstNonNil(game, "status", "state"), "");
        var hasScores = !string.IsNullOrWhiteSpace(homeScore) || !string.IsNullOrWhiteSpace(awayScore);
        var isFinal = hasScores
            || string.Equals(status, "final", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase);
        string FormatSuffix(string leftScore, string rightScore)
        {
            return isFinal ? FormatScoreSuffix(leftScore, rightScore) : " (Scheduled)";
        }

        var prefix = string.IsNullOrWhiteSpace(headerText) ? "" : $"{headerText}: ";

        if (!string.IsNullOrWhiteSpace(focusTeamId))
        {
            var homeId = GetTeamIdFromVariant(homeVar);
            var awayId = GetTeamIdFromVariant(awayVar);

            if (!string.IsNullOrWhiteSpace(homeId)
                && string.Equals(homeId, focusTeamId, StringComparison.OrdinalIgnoreCase))
            {
                var opponent = string.IsNullOrWhiteSpace(awayName) ? "Opponent" : awayName;
                return $"{prefix}vs {opponent}{FormatSuffix(homeScore, awayScore)}".Trim();
            }

            if (!string.IsNullOrWhiteSpace(awayId)
                && string.Equals(awayId, focusTeamId, StringComparison.OrdinalIgnoreCase))
            {
                var opponent = string.IsNullOrWhiteSpace(homeName) ? "Opponent" : homeName;
                return $"{prefix}@ {opponent}{FormatSuffix(awayScore, homeScore)}".Trim();
            }
        }

        var awayLabel = string.IsNullOrWhiteSpace(awayName) ? "Away" : awayName;
        var homeLabel = string.IsNullOrWhiteSpace(homeName) ? "Home" : homeName;
        var matchup = $"{awayLabel} @ {homeLabel}".Trim();
        return $"{prefix}{matchup}{FormatSuffix(awayScore, homeScore)}".Trim();
    }

    private string GetGameId(Godot.Collections.Dictionary game)
    {
        var idVar = GetFirstNonNil(game, "game_id", "gameId", "id");
        return FmtString(idVar, "");
    }

    private bool TryResolveBoxScoreObjects(
        Godot.Collections.Dictionary root,
        out Godot.Collections.Dictionary gameObj,
        out Godot.Collections.Dictionary boxObj)
    {
        gameObj = null;
        boxObj = null;

        if (root == null)
            return false;

        gameObj = TryExtractObject(root, "game", "result", "matchup") ?? root;

        var nestedGame = TryExtractObject(gameObj, "game", "result", "matchup");
        if (nestedGame != null)
            gameObj = nestedGame;

        boxObj = TryExtractObject(root, "box_score", "boxScore", "boxscore", "box", "box_stats")
            ?? TryExtractObject(gameObj, "box_score", "boxScore", "boxscore", "box", "box_stats")
            ?? gameObj;

        if (boxObj == null)
            return false;

        if (!LooksLikeGameDict(gameObj) && !LooksLikeGameDict(boxObj))
            return false;

        return true;
    }

    private bool TryExtractGamePayload(Variant parsed, out Godot.Collections.Dictionary payload)
    {
        payload = null;

        if (parsed.VariantType != Variant.Type.Dictionary)
            return false;

        var root = parsed.AsGodotDictionary();
        if (TryResolveBoxScoreObjects(root, out _, out _))
        {
            payload = root;
            return true;
        }

        var data = TryExtractObject(root, "data");
        if (data != null && TryResolveBoxScoreObjects(data, out _, out _))
        {
            payload = data;
            return true;
        }

        var payloadWrapper = TryExtractObject(root, "payload");
        if (payloadWrapper != null && TryResolveBoxScoreObjects(payloadWrapper, out _, out _))
        {
            payload = payloadWrapper;
            return true;
        }

        return false;
    }

    private static bool LooksLikeGameDict(Godot.Collections.Dictionary dict)
    {
        return dict.ContainsKey("home_team")
            || dict.ContainsKey("away_team")
            || dict.ContainsKey("home")
            || dict.ContainsKey("away")
            || dict.ContainsKey("home_team_id")
            || dict.ContainsKey("homeTeamId")
            || dict.ContainsKey("home_id")
            || dict.ContainsKey("homeId")
            || dict.ContainsKey("away_team_id")
            || dict.ContainsKey("awayTeamId")
            || dict.ContainsKey("away_id")
            || dict.ContainsKey("awayId")
            || dict.ContainsKey("game_id")
            || dict.ContainsKey("gameId")
            || dict.ContainsKey("id")
            || dict.ContainsKey("home_score")
            || dict.ContainsKey("away_score")
            || dict.ContainsKey("homeScore")
            || dict.ContainsKey("awayScore")
            || dict.ContainsKey("box_score")
            || dict.ContainsKey("boxScore")
            || dict.ContainsKey("boxscore")
            || dict.ContainsKey("box")
            || dict.ContainsKey("box_stats")
            || dict.ContainsKey("score")
            || dict.ContainsKey("score_home")
            || dict.ContainsKey("score_away")
            || dict.ContainsKey("team_stats")
            || dict.ContainsKey("teamStats")
            || dict.ContainsKey("player_stats")
            || dict.ContainsKey("playerStats")
            || dict.ContainsKey("box_score_lines")
            || dict.ContainsKey("boxScoreLines")
            || dict.ContainsKey("quarter_scores")
            || dict.ContainsKey("quarters")
            || dict.ContainsKey("leaders")
            || dict.ContainsKey("final");
    }

    private static string FormatScoreSuffix(string leftScore, string rightScore)
    {
        if (string.IsNullOrWhiteSpace(leftScore) && string.IsNullOrWhiteSpace(rightScore))
            return "";

        var left = string.IsNullOrWhiteSpace(leftScore) ? "?" : leftScore;
        var right = string.IsNullOrWhiteSpace(rightScore) ? "?" : rightScore;
        return $" {left}-{right}";
    }

    private string GetTeamIdFromVariant(Variant teamVar)
    {
        if (IsNil(teamVar))
            return "";

        if (teamVar.VariantType == Variant.Type.Dictionary && TryGetDictionary(teamVar, out var teamDict))
            return FmtString(GetFirstNonNil(teamDict, "id", "team_id"), "");

        return FmtString(teamVar, "");
    }

    private string GetTeamAbbr(Variant teamVar)
    {
        if (IsNil(teamVar))
            return "";

        if (teamVar.VariantType == Variant.Type.Dictionary && TryGetDictionary(teamVar, out var teamDict))
        {
            var abbr = FmtString(GetFirstNonNil(teamDict, "abbreviation", "abbr", "short_name"), "");
            if (!string.IsNullOrWhiteSpace(abbr))
                return abbr;
        }

        var display = ResolveTeamName(teamVar);
        if (string.IsNullOrWhiteSpace(display))
            return "";

        var dashIndex = display.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIndex > 0)
            return display.Substring(0, dashIndex).Trim();

        return display;
    }

    private (string awayScore, string homeScore) GetFinalScores(Godot.Collections.Dictionary game, Godot.Collections.Dictionary boxScore)
    {
        var awayScoreVar = TryExtract(game, "away_score", "awayScore", "away_points", "away_pts", "score_away");
        var homeScoreVar = TryExtract(game, "home_score", "homeScore", "home_points", "home_pts", "score_home");

        var gameScoreDict = TryExtractObject(game, "score");
        if (gameScoreDict != null)
        {
            var nestedAway = TryExtract(gameScoreDict, "away", "away_score", "awayScore", "away_points");
            if (!IsNil(nestedAway))
                awayScoreVar = nestedAway;
            var nestedHome = TryExtract(gameScoreDict, "home", "home_score", "homeScore", "home_points");
            if (!IsNil(nestedHome))
                homeScoreVar = nestedHome;
        }

        var directAway = TryExtract(boxScore, "away_score", "awayScore", "away_points", "away_pts", "score_away");
        if (!IsNil(directAway))
            awayScoreVar = directAway;
        var directHome = TryExtract(boxScore, "home_score", "homeScore", "home_points", "home_pts", "score_home");
        if (!IsNil(directHome))
            homeScoreVar = directHome;

        var finalVar = TryExtract(boxScore, "final", "final_score");
        if (!IsNil(finalVar) && TryGetDictionary(finalVar, out var finalDict))
        {
            var finalAway = TryExtract(finalDict, "away", "away_score", "awayScore", "away_points");
            if (!IsNil(finalAway))
                awayScoreVar = finalAway;
            var finalHome = TryExtract(finalDict, "home", "home_score", "homeScore", "home_points");
            if (!IsNil(finalHome))
                homeScoreVar = finalHome;
        }

        var scoreDict = TryExtractObject(boxScore, "score");
        if (scoreDict != null)
        {
            var scoreAway = TryExtract(scoreDict, "away", "away_score", "awayScore", "away_points");
            if (!IsNil(scoreAway))
                awayScoreVar = scoreAway;
            var scoreHome = TryExtract(scoreDict, "home", "home_score", "homeScore", "home_points");
            if (!IsNil(scoreHome))
                homeScoreVar = scoreHome;
        }

        var awayScore = FmtInt(awayScoreVar, "");
        var homeScore = FmtInt(homeScoreVar, "");

        if (string.IsNullOrWhiteSpace(awayScore))
            awayScore = "?";
        if (string.IsNullOrWhiteSpace(homeScore))
            homeScore = "?";

        return (awayScore, homeScore);
    }

    private void PopulateBoxScoreQuarterTree(
        Godot.Collections.Dictionary boxScore,
        string awayLabel,
        string homeLabel,
        string awayScore,
        string homeScore)
    {
        PopulateBoxScoreQuarterTree(_boxScoreQuarterTree, boxScore, awayLabel, homeLabel, awayScore, homeScore);
    }

    private static void PopulateBoxScoreQuarterTree(
        Tree tree,
        Godot.Collections.Dictionary boxScore,
        string awayLabel,
        string homeLabel,
        string awayScore,
        string homeScore,
        bool useDefaultQuarterRows = true)
    {
        if (tree == null)
            return;

        tree.Clear();

        TryExtractQuarterScores(boxScore, out var awayQuarters, out var homeQuarters);

        var awayCount = awayQuarters?.Count ?? 0;
        var homeCount = homeQuarters?.Count ?? 0;
        var quarterCount = Math.Max(awayCount, homeCount);
        if (useDefaultQuarterRows)
            quarterCount = Math.Max(quarterCount, 4);
        if (quarterCount <= 0)
            quarterCount = 4;

        var columns = 2 + quarterCount;
        tree.Columns = columns;
        tree.SetColumnTitle(0, "Team");
        for (var i = 0; i < quarterCount; i++)
        {
            var label = i < 4 ? $"Q{i + 1}" : $"OT{i - 3}";
            tree.SetColumnTitle(i + 1, label);
        }
        tree.SetColumnTitle(columns - 1, "Final");

        var rootItem = tree.CreateItem();
        var awayItem = tree.CreateItem(rootItem);
        awayItem.SetText(0, awayLabel);
        for (var i = 0; i < quarterCount; i++)
            awayItem.SetText(i + 1, GetQuarterText(awayQuarters, i, "N/A"));
        awayItem.SetText(columns - 1, awayScore);

        var homeItem = tree.CreateItem(rootItem);
        homeItem.SetText(0, homeLabel);
        for (var i = 0; i < quarterCount; i++)
            homeItem.SetText(i + 1, GetQuarterText(homeQuarters, i, "N/A"));
        homeItem.SetText(columns - 1, homeScore);
    }

    private void PopulateBoxScoreTeamStatsTree(Godot.Collections.Dictionary boxScore, string awayLabel, string homeLabel)
    {
        PopulateBoxScoreTeamStatsTree(_boxScoreTeamStatsTree, boxScore, awayLabel, homeLabel);
    }

    private static void PopulateBoxScoreTeamStatsTree(
        Tree tree,
        Godot.Collections.Dictionary boxScore,
        string awayLabel,
        string homeLabel,
        bool useCompactStatRows = false)
    {
        if (tree == null)
            return;

        tree.Clear();
        tree.Columns = 3;
        tree.SetColumnTitle(0, "Stat");
        tree.SetColumnTitle(1, awayLabel);
        tree.SetColumnTitle(2, homeLabel);

        void AddBoxScoreStatsFallback(string message)
        {
            var root = tree.CreateItem();
            var item = tree.CreateItem(root);
            item.SetText(0, message);
        }

        var statsVar = TryExtract(boxScore, "team_stats", "teamStats", "team_statistics", "stats");
        if (IsNil(statsVar))
        {
            AddBoxScoreStatsFallback("(no stats)");
            return;
        }

        if (TryGetDictionary(statsVar, out var statsDict))
        {
            var rowsArray = TryExtractArray(statsDict, "rows");
            if (rowsArray != null)
                statsVar = rowsArray;

            var awayStats = TryExtractObject(statsDict, "away");
            var homeStats = TryExtractObject(statsDict, "home");

            if (awayStats != null || homeStats != null)
            {
                var rows = BuildBoxScoreStatRows(awayStats, homeStats, useCompactStatRows);
                if (rows.Count == 0)
                {
                    AddBoxScoreStatsFallback("(no stats)");
                    return;
                }

                var root = tree.CreateItem();
                foreach (var statRow in rows)
                {
                    var row = tree.CreateItem(root);
                    row.SetText(0, statRow.Label);
                    row.SetText(1, statRow.AwayValue);
                    row.SetText(2, statRow.HomeValue);
                }
                return;
            }
        }

        if (TryGetArray(statsVar, out var statsArray))
        {
            var root = tree.CreateItem();
            var added = false;
            for (var i = 0; i < statsArray.Count; i++)
            {
                var rowVar = (Variant)statsArray[i];
                if (!TryGetDictionary(rowVar, out var rowDict))
                    continue;

                var statName = FmtString(TryExtract(rowDict, "stat", "name", "label"), "Stat");
                var awayValue = FormatStatValue(TryExtract(rowDict, "away", "away_value", "away_stat"));
                var homeValue = FormatStatValue(TryExtract(rowDict, "home", "home_value", "home_stat"));

                var item = tree.CreateItem(root);
                item.SetText(0, statName);
                item.SetText(1, awayValue);
                item.SetText(2, homeValue);
                added = true;
            }
            if (!added)
                AddBoxScoreStatsFallback("(no stats)");
            return;
        }

        AddBoxScoreStatsFallback("(no stats)");
    }

    private void PopulateBoxScoreLeaders(Godot.Collections.Dictionary boxScore, string awayLabel, string homeLabel)
    {
        if (_boxScoreLeadersList == null)
            return;

        _boxScoreLeadersList.Clear();
        var linesArray = TryExtractArray(boxScore, "player_stats", "playerStats", "box_score_lines", "boxScoreLines");
        if (linesArray == null)
        {
            var linesDict = TryExtractObject(boxScore, "player_stats", "playerStats", "box_score_lines", "boxScoreLines");
            if (linesDict != null)
                linesArray = TryExtractArray(linesDict, "rows", "lines", "items");
        }

        if (linesArray != null)
        {
            var addedLines = AddBoxScoreLines(linesArray);
            if (addedLines > 0)
                return;
        }

        var leadersVar = TryExtract(boxScore, "leaders", "leader_stats", "stat_leaders", "leaders_list");
        if (IsNil(leadersVar) || !TryGetDictionary(leadersVar, out var leadersDict))
        {
            _boxScoreLeadersList.AddItem("(no stats)");
            return;
        }

        string LeaderValue(Variant value)
        {
            var text = FormatStatValue(value);
            return string.IsNullOrWhiteSpace(text) ? "-" : text;
        }

        string LeaderPlayerName(Variant value)
        {
            if (TryGetDictionary(value, out var playerDict))
            {
                var nameVar = GetFirstNonNil(playerDict, "name", "player", "full_name");
                return LeaderValue(nameVar);
            }

            return LeaderValue(value);
        }

        string FormatPassingLine(Variant entryVar)
        {
            if (!TryGetDictionary(entryVar, out var entryDict))
                return "Passing: -";

            var player = LeaderPlayerName(GetFirstNonNil(entryDict, "player", "name"));
            var comp = LeaderValue(GetFirstNonNil(entryDict, "comp", "completions"));
            var att = LeaderValue(GetFirstNonNil(entryDict, "att", "attempts"));
            var yards = LeaderValue(GetFirstNonNil(entryDict, "yards", "yds"));
            var td = LeaderValue(GetFirstNonNil(entryDict, "td", "tds"));
            var picks = LeaderValue(GetFirstNonNil(entryDict, "int", "ints", "interceptions"));
            return $"Passing: {player} ({comp}/{att}, {yards} yds, {td} TD, {picks} INT)";
        }

        string FormatRushingLine(Variant entryVar)
        {
            if (!TryGetDictionary(entryVar, out var entryDict))
                return "Rushing: -";

            var player = LeaderPlayerName(GetFirstNonNil(entryDict, "player", "name"));
            var yards = LeaderValue(GetFirstNonNil(entryDict, "yards", "yds"));
            var td = LeaderValue(GetFirstNonNil(entryDict, "td", "tds"));
            return $"Rushing: {player} ({yards} yds, {td} TD)";
        }

        string FormatReceivingLine(Variant entryVar)
        {
            if (!TryGetDictionary(entryVar, out var entryDict))
                return "Receiving: -";

            var player = LeaderPlayerName(GetFirstNonNil(entryDict, "player", "name"));
            var rec = LeaderValue(GetFirstNonNil(entryDict, "rec", "receptions"));
            var yards = LeaderValue(GetFirstNonNil(entryDict, "yards", "yds"));
            var td = LeaderValue(GetFirstNonNil(entryDict, "td", "tds"));
            return $"Receiving: {player} ({rec} rec, {yards} yds, {td} TD)";
        }

        void AddLeaderSection(string header, Variant sectionVar)
        {
            _boxScoreLeadersList.AddItem(header);
            if (!TryGetDictionary(sectionVar, out var sectionDict))
            {
                _boxScoreLeadersList.AddItem("Passing: -");
                _boxScoreLeadersList.AddItem("Rushing: -");
                _boxScoreLeadersList.AddItem("Receiving: -");
                return;
            }

            _boxScoreLeadersList.AddItem(FormatPassingLine(GetFirstNonNil(sectionDict, "passing", "pass", "passing_leader")));
            _boxScoreLeadersList.AddItem(FormatRushingLine(GetFirstNonNil(sectionDict, "rushing", "rush", "rushing_leader")));
            _boxScoreLeadersList.AddItem(FormatReceivingLine(GetFirstNonNil(sectionDict, "receiving", "receive", "receiving_leader", "rec")));
        }

        AddLeaderSection("Away Leaders", GetFirstNonNil(leadersDict, "away", "away_team", "away_leaders"));
        AddLeaderSection("Home Leaders", GetFirstNonNil(leadersDict, "home", "home_team", "home_leaders"));
    }

    private int AddBoxScoreLines(Godot.Collections.Array lines)
    {
        if (_boxScoreLeadersList == null || lines == null)
            return 0;

        var added = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            var entryVar = (Variant)lines[i];
            var line = FormatBoxScoreLine(entryVar);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            _boxScoreLeadersList.AddItem(line);
            added++;
        }

        return added;
    }

    private string FormatBoxScoreLine(Variant entryVar)
    {
        if (IsNil(entryVar))
            return "";

        if (entryVar.VariantType == Variant.Type.String)
            return entryVar.AsString();

        if (TryGetDictionary(entryVar, out var entryDict))
        {
            var line = FmtString(TryExtract(entryDict, "line", "stat_line", "summary", "text"), "");
            if (!string.IsNullOrWhiteSpace(line))
                return line;

            var playerVar = TryExtract(entryDict, "player", "name", "player_name", "full_name");
            var playerName = "";
            if (!IsNil(playerVar))
            {
                if (TryGetDictionary(playerVar, out var playerDict))
                    playerName = FmtString(TryExtract(playerDict, "name", "full_name", "player"), "");
                else
                    playerName = FmtString(playerVar, "");
            }

            var statParts = new List<string>();
            void AddStat(string label, params string[] keys)
            {
                var value = TryExtract(entryDict, keys);
                if (IsNil(value))
                    return;

                var text = FormatStatValue(value);
                if (string.IsNullOrWhiteSpace(text))
                    return;

                statParts.Add($"{label} {text}");
            }

            AddStat("Comp", "comp", "completions");
            AddStat("Att", "att", "attempts");
            AddStat("Yds", "yards", "yds", "pass_yards", "rush_yards", "rec_yards");
            AddStat("TD", "td", "tds", "touchdowns");
            AddStat("INT", "int", "ints", "interceptions");
            AddStat("Rec", "rec", "receptions");
            AddStat("Car", "carries", "rushes");

            if (!string.IsNullOrWhiteSpace(playerName) && statParts.Count > 0)
                return $"{playerName}: {string.Join(", ", statParts)}";
            if (!string.IsNullOrWhiteSpace(playerName))
                return playerName;

            return InlineMessage(entryDict.ToString(), 200);
        }

        return FormatStatValue(entryVar);
    }

    private static string GetQuarterText(Godot.Collections.Array quarters, int index)
    {
        return GetQuarterText(quarters, index, "");
    }

    private static string GetQuarterText(Godot.Collections.Array quarters, int index, string fallback)
    {
        if (quarters == null || index < 0 || index >= quarters.Count)
            return fallback;

        var value = (Variant)quarters[index];
        var text = FmtInt(value, "");
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static bool TryExtractQuarterScores(
        Godot.Collections.Dictionary boxScore,
        out Godot.Collections.Array awayQuarters,
        out Godot.Collections.Array homeQuarters)
    {
        awayQuarters = null;
        homeQuarters = null;

        var qVar = GetFirstNonNil(boxScore, "quarter_scores", "scoring_by_quarter", "quarters", "qtrs", "quarter_results");
        if (IsNil(qVar))
            return false;

        if (TryGetDictionary(qVar, out var qDict))
        {
            var awayVar = GetFirstNonNil(qDict, "away", "away_scores", "away_quarters", "away_q");
            var homeVar = GetFirstNonNil(qDict, "home", "home_scores", "home_quarters", "home_q");
            TryGetArray(awayVar, out awayQuarters);
            TryGetArray(homeVar, out homeQuarters);
            return awayQuarters != null || homeQuarters != null;
        }

        if (TryGetArray(qVar, out var quarters))
        {
            var awayList = new Godot.Collections.Array();
            var homeList = new Godot.Collections.Array();
            for (var i = 0; i < quarters.Count; i++)
            {
                var entryVar = (Variant)quarters[i];
                if (!TryGetDictionary(entryVar, out var entryDict))
                    continue;

                var awayValue = GetFirstNonNil(entryDict, "away", "away_score", "away_points", "a");
                var homeValue = GetFirstNonNil(entryDict, "home", "home_score", "home_points", "h");
                awayList.Add(awayValue);
                homeList.Add(homeValue);
            }

            awayQuarters = awayList;
            homeQuarters = homeList;
            return awayQuarters.Count > 0 || homeQuarters.Count > 0;
        }

        return false;
    }

    private sealed class BoxScoreStatRow
    {
        public BoxScoreStatRow(string label, string awayValue, string homeValue)
        {
            Label = label;
            AwayValue = awayValue;
            HomeValue = homeValue;
        }

        public string Label { get; }
        public string AwayValue { get; }
        public string HomeValue { get; }
    }

    private static List<BoxScoreStatRow> BuildBoxScoreStatRows(
        Godot.Collections.Dictionary awayStats,
        Godot.Collections.Dictionary homeStats,
        bool useCompactStatRows)
    {
        var rows = new List<BoxScoreStatRow>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCompactRow(string label, params string[] keys)
        {
            var awayValue = FormatStatValue(TryExtract(awayStats, keys));
            var homeValue = FormatStatValue(TryExtract(homeStats, keys));
            if (!useCompactStatRows && string.IsNullOrWhiteSpace(awayValue) && string.IsNullOrWhiteSpace(homeValue))
                return;

            rows.Add(new BoxScoreStatRow(
                label,
                string.IsNullOrWhiteSpace(awayValue) ? "N/A" : awayValue,
                string.IsNullOrWhiteSpace(homeValue) ? "N/A" : homeValue));

            foreach (var key in keys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    usedKeys.Add(key);
            }
        }

        AddCompactRow("Total Yards", "total_yards", "totalYards");
        AddCompactRow("Passing Yards", "passing_yards", "pass_yards", "passingYards");
        AddCompactRow("Rushing Yards", "rushing_yards", "rush_yards", "rushingYards");
        AddCompactRow("Turnovers", "turnovers", "turnover_count", "turnoverCount");
        AddCompactRow("First Downs", "first_downs", "firstDowns");
        AddCompactRow("Time of Possession", "time_of_possession", "timeOfPossession", "possession_time");

        if (!useCompactStatRows)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (awayStats != null)
            {
                foreach (var key in awayStats.Keys)
                    keys.Add(key.ToString());
            }
            if (homeStats != null)
            {
                foreach (var key in homeStats.Keys)
                    keys.Add(key.ToString());
            }

            foreach (var key in keys)
            {
                if (usedKeys.Contains(key))
                    continue;

                var awayValue = awayStats != null && awayStats.ContainsKey(key) ? FormatStatValue((Variant)awayStats[key]) : "";
                var homeValue = homeStats != null && homeStats.ContainsKey(key) ? FormatStatValue((Variant)homeStats[key]) : "";
                rows.Add(new BoxScoreStatRow(
                    HumanizeBoxScoreStatKey(key),
                    string.IsNullOrWhiteSpace(awayValue) ? "N/A" : awayValue,
                    string.IsNullOrWhiteSpace(homeValue) ? "N/A" : homeValue));
            }
        }

        return rows;
    }

    private static string HumanizeBoxScoreStatKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Stat";

        var normalized = key.Replace("_", " ").Trim().ToLowerInvariant();
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    private void SetupRosterColumns()
    {
        _columns.Clear();
        _columns.Add(new RosterColumn(
            id: "pos",
            title: "Pos",
            defaultVisible: true,
            width: 60,
            expand: false,
            getter: row => row.PositionDisplay,
            sortGetter: row => GetPositionSortOrder(row.Position),
            sortable: true));
        _columns.Add(new RosterColumn(
            id: "name",
            title: "Name",
            defaultVisible: true,
            width: 220,
            expand: true,
            getter: row => row.Name,
            sortGetter: row => row.Name,
            sortable: true));
        _columns.Add(new RosterColumn(
            id: "ovr",
            title: "OVR",
            defaultVisible: true,
            width: 60,
            expand: false,
            getter: row => row.Overall > 0 ? row.Overall.ToString() : "-",
            sortGetter: row => row.Overall,
            sortable: true));
        _columns.Add(new RosterColumn(
            id: "age",
            title: "Age",
            defaultVisible: true,
            width: 60,
            expand: false,
            getter: row => row.Age > 0 ? row.Age.ToString() : "-",
            sortGetter: row => row.Age,
            sortable: true));
        _columns.Add(new RosterColumn(
            id: "status",
            title: "Status",
            defaultVisible: true,
            width: 120,
            expand: false,
            getter: row => row.Status,
            sortGetter: row => row.Status,
            sortable: true));
        _columns.Add(new RosterColumn(
            id: "injury",
            title: "Injury",
            defaultVisible: true,
            width: 180,
            expand: true,
            getter: row => row.Injury,
            sortGetter: row => row.Injury,
            sortable: true));
        _columns.Add(new RosterColumn(
            id: "id",
            title: "Id",
            defaultVisible: false,
            width: 120,
            expand: false,
            getter: row => row.Id,
            sortGetter: row => row.Id,
            sortable: false));

        InitRosterTree();
        LoadColumnVisibility();
        ApplyColumnVisibility();
        PopulateColumnsMenu();
    }

    private void InitRosterTree()
    {
        if (_rosterTree == null)
            return;

        _rosterTree.HideRoot = true;
        _rosterTree.ColumnTitlesVisible = true;
        _rosterTree.SelectMode = Tree.SelectModeEnum.Row;
    }

    private void LoadColumnVisibility()
    {
        const int rosterColumnSchemaVersion = 2;
        _columnVisibility.Clear();
        foreach (var column in _columns)
            _columnVisibility[column.Id] = column.DefaultVisible;

        var config = new ConfigFile();
        if (config.Load("user://ui.cfg") != Error.Ok)
            return;

        var savedVersion = GetIntValue(config.GetValue("ui", "dashboard_roster_columns_version", 0), 0);
        if (savedVersion != rosterColumnSchemaVersion)
            return;

        var raw = config.GetValue("ui", "dashboard_roster_columns", "").AsString();
        if (string.IsNullOrWhiteSpace(raw))
            return;

        var visibleIds = raw.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var visibleSet = new HashSet<string>(visibleIds, StringComparer.OrdinalIgnoreCase);
        foreach (var column in _columns)
            _columnVisibility[column.Id] = visibleSet.Contains(column.Id);
    }

    private void SaveColumnVisibility()
    {
        const int rosterColumnSchemaVersion = 2;
        var visibleIds = new List<string>();
        foreach (var column in _columns)
        {
            if (_columnVisibility.TryGetValue(column.Id, out var visible) && visible)
                visibleIds.Add(column.Id);
        }

        var config = new ConfigFile();
        config.Load("user://ui.cfg");
        config.SetValue("ui", "dashboard_roster_columns_version", rosterColumnSchemaVersion);
        config.SetValue("ui", "dashboard_roster_columns", string.Join(",", visibleIds));
        config.Save("user://ui.cfg");
    }

    private void LoadRosterSplitOffset()
    {
        if (_rosterSplit == null)
            return;

        try
        {
            var config = new ConfigFile();
            if (config.Load("user://ui.cfg") != Error.Ok)
            {
                _rosterSplit.CallDeferred(nameof(ApplyDefaultRosterSplitOffset));
                return;
            }

            var raw = config.GetValue("ui", "dashboard_split_offset", -1);
            var offset = GetIntValue(raw, -1);
            if (offset <= 0)
            {
                _rosterSplit.CallDeferred(nameof(ApplyDefaultRosterSplitOffset));
                return;
            }

            _rosterSplit.SplitOffset = offset;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load roster split offset: {ex.Message}");
            _rosterSplit.CallDeferred(nameof(ApplyDefaultRosterSplitOffset));
        }
    }

    private void ApplyDefaultRosterSplitOffset()
    {
        if (_rosterSplit == null)
            return;

        var width = _rosterSplit.Size.X;
        if (width <= 0)
            return;

        var offset = (int)Math.Round(width * 0.65f);
        if (offset > 0)
            _rosterSplit.SplitOffset = offset;
    }

    private void SaveRosterSplitOffset(int offset)
    {
        if (offset <= 0)
            return;

        try
        {
            var config = new ConfigFile();
            config.Load("user://ui.cfg");
            config.SetValue("ui", "dashboard_split_offset", offset);
            config.Save("user://ui.cfg");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save roster split offset: {ex.Message}");
        }
    }

    private void SetupPosFilterItems()
    {
        if (_posFilter == null)
            return;

        var wasSuppressed = _suppressRosterFilterEvents;
        _suppressRosterFilterEvents = true;
        _posFilter.Clear();
        foreach (var option in PosFilterOptions)
            _posFilter.AddItem(option);
        _posFilter.Select(0);
        _posFilterValue = PosFilterOptions[0];
        _suppressRosterFilterEvents = wasSuppressed;
    }

    private void LoadRosterFilters()
    {
        _rosterSearchText = "";
        _posFilterValue = PosFilterOptions[0];

        try
        {
            var config = new ConfigFile();
            if (config.Load("user://ui.cfg") == Error.Ok)
            {
                _rosterSearchText = config.GetValue("ui", "dashboard_roster_search", "").AsString();
                _posFilterValue = config.GetValue("ui", "dashboard_roster_pos_filter", PosFilterOptions[0]).AsString();
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load roster filters: {ex.Message}");
        }

        _suppressRosterFilterEvents = true;
        if (_rosterSearch != null)
            _rosterSearch.Text = _rosterSearchText ?? "";
        if (_posFilter != null)
        {
            var index = GetPosFilterIndex(_posFilterValue);
            _posFilter.Select(index);
            _posFilterValue = PosFilterOptions[index];
        }
        _suppressRosterFilterEvents = false;
    }

    private void SaveRosterFilters()
    {
        try
        {
            var config = new ConfigFile();
            config.Load("user://ui.cfg");
            config.SetValue("ui", "dashboard_roster_search", _rosterSearchText ?? "");
            config.SetValue("ui", "dashboard_roster_pos_filter", _posFilterValue ?? PosFilterOptions[0]);
            config.Save("user://ui.cfg");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save roster filters: {ex.Message}");
        }
    }

    private Godot.Collections.Array ConvertDashboardActionItems(Godot.Collections.Array actionItems)
    {
        var messages = new Godot.Collections.Array();
        if (actionItems == null)
            return messages;

        for (var i = 0; i < actionItems.Count; i++)
        {
            var itemVar = (Variant)actionItems[i];
            if (!TryGetDictionary(itemVar, out var item))
                continue;

            var message = new Godot.Collections.Dictionary
            {
                { "id", $"{FmtString(GetFirstNonNil(item, "type"), "action")}_{i}" },
                { "type", FmtString(GetFirstNonNil(item, "type"), "") },
                { "title", FmtString(GetFirstNonNil(item, "title"), "Action Required") },
                { "message", FmtString(GetFirstNonNil(item, "description"), "") },
                { "severity", FmtString(GetFirstNonNil(item, "severity"), "info") },
                { "primary_action", FmtString(GetFirstNonNil(item, "primary_action", "primaryAction"), "") },
                { "requires_ack", false },
                { "read", false },
            };
            messages.Add(message);
        }
        return messages;
    }

    private void UpdateInboxList()
    {
        var selectedMessageId = _selectedInboxMessageId;
        if (_overviewActionHeader != null)
            _overviewActionHeader.Text = "Action Required";

        if (_inboxMessages == null || _inboxMessages.Count == 0)
        {
            ClearInboxDetail("No urgent actions.", _inboxEmptyDetailMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(selectedMessageId))
        {
            var selectedMessage = FindInboxMessage(selectedMessageId);
            if (selectedMessage != null)
            {
                _selectedInboxMessageId = selectedMessageId;
                _selectedInboxActionItem = selectedMessage;
                UpdateInboxDetail(selectedMessage);
                return;
            }
        }

        var firstVar = (Variant)_inboxMessages[0];
        if (!TryGetDictionary(firstVar, out var firstMessage))
        {
            ClearInboxDetail("No urgent actions.", _inboxEmptyDetailMessage);
            return;
        }

        _selectedInboxMessageId = GetMessageId(firstMessage);
        _selectedInboxActionItem = firstMessage;
        UpdateInboxDetail(firstMessage);
    }

    private Godot.Collections.Dictionary FindInboxMessage(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return null;

        for (var i = 0; i < _inboxMessages.Count; i++)
        {
            var messageVar = (Variant)_inboxMessages[i];
            if (!TryGetDictionary(messageVar, out var message))
                continue;

            var currentId = GetMessageId(message);
            if (string.Equals(currentId, messageId, StringComparison.OrdinalIgnoreCase))
                return message;
        }

        return null;
    }

    private bool TrySelectInboxMessage(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return false;

        var message = FindInboxMessage(messageId);
        if (message != null)
        {
            _selectedInboxMessageId = messageId;
            _selectedInboxActionItem = message;
            UpdateInboxDetail(message);
            return true;
        }

        return false;
    }

    private void UpdateInboxDetail(Godot.Collections.Dictionary message)
    {
        if (message == null)
        {
            ClearInboxDetail();
            return;
        }

        var subject = FormatOverviewActionSubject(GetMessageSubject(message));
        var severityPrefix = GetInboxSeverityPrefix(message);
        var subjectText = $"{severityPrefix}{subject}".Trim();
        if (_overviewActionTitle != null)
            _overviewActionTitle.Text = string.IsNullOrWhiteSpace(subjectText) ? "Message" : subjectText;
        if (_overviewActionHeader != null)
            _overviewActionHeader.Text = "Action Required";

        var body = GetMessageBody(message);
        var primaryAction = FmtString(GetFirstNonNil(message, "primary_action", "primaryAction"), "");
        if (_overviewActionBody != null)
        {
            _overviewActionBody.FitContent = true;
            _overviewActionBody.CustomMinimumSize = new Vector2(0, 72);
            _overviewActionBody.Text = string.IsNullOrWhiteSpace(body) ? "No message body available." : body;
        }
        if (_overviewActionSuggested != null)
        {
            _overviewActionSuggested.Visible = true;
            _overviewActionSuggested.Text = string.IsNullOrWhiteSpace(primaryAction)
                ? "Suggested action: review this item."
                : $"Suggested action: {primaryAction}";
        }

        _selectedSimGameId = "";

        if (_overviewActionButton != null)
        {
            var canUsePrimaryAction = IsGameDayMessage(message)
                || IsRosterInvalidMessage(message)
                || IsDepthChartInvalidMessage(message)
                || IsPostseasonPendingMessage(message)
                || IsSeasonCompleteMessage(message)
                || IsOffseasonPendingMessage(message);
            var primaryActionLabel = ResolveInboxPrimaryActionLabel(message);
            _overviewActionButton.Visible = canUsePrimaryAction;
            _overviewActionButton.Disabled = !canUsePrimaryAction;
            _overviewActionButton.Text = primaryActionLabel;
            _overviewActionButton.TooltipText = "";
        }
    }

    private string GetInboxSeverityPrefix(Godot.Collections.Dictionary message)
    {
        var severity = FmtString(GetFirstNonNil(message, "severity"), "info");
        return severity switch
        {
            "danger" => "!! ",
            "warning" => "! ",
            _ => "",
        };
    }

    private string ResolveInboxHeaderText()
    {
        if (_selectedInboxActionItem != null)
        {
            var selectedSubject = GetMessageSubject(_selectedInboxActionItem);
            if (!string.IsNullOrWhiteSpace(selectedSubject))
                return selectedSubject;
        }

        if (_inboxMessages != null && _inboxMessages.Count > 0)
        {
            var messageVar = (Variant)_inboxMessages[0];
            if (TryGetDictionary(messageVar, out var message))
            {
                var subject = GetMessageSubject(message);
                if (!string.IsNullOrWhiteSpace(subject))
                    return subject;
            }
        }

        return "Action Required";
    }

    private static bool IsGameDayMessage(Godot.Collections.Dictionary message)
    {
        if (message == null)
            return false;

        var type = FmtString(GetFirstNonNil(message, "type"), "");
        return string.Equals(type, "game_day", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRosterInvalidMessage(Godot.Collections.Dictionary message)
    {
        if (message == null)
            return false;

        var type = FmtString(GetFirstNonNil(message, "type"), "");
        return string.Equals(type, "roster_invalid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDepthChartInvalidMessage(Godot.Collections.Dictionary message)
    {
        if (message == null)
            return false;

        var type = FmtString(GetFirstNonNil(message, "type"), "");
        return string.Equals(type, "depth_chart_invalid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPostseasonPendingMessage(Godot.Collections.Dictionary message)
    {
        if (message == null)
            return false;

        var type = FmtString(GetFirstNonNil(message, "type"), "");
        return string.Equals(type, "postseason_pending", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSeasonCompleteMessage(Godot.Collections.Dictionary message)
    {
        if (message == null)
            return false;

        var type = FmtString(GetFirstNonNil(message, "type"), "");
        return string.Equals(type, "season_complete", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOffseasonPendingMessage(Godot.Collections.Dictionary message)
    {
        if (message == null)
            return false;

        var type = FmtString(GetFirstNonNil(message, "type"), "");
        return string.Equals(type, ScheduleService.OffseasonPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.StaffCarouselPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.RetirementPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.ExclusiveNegotiationPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.FranchiseTagPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.LeagueYearPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.FreeAgencyPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.DraftPrepPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.DraftPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.RookieSigningPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, ScheduleService.TrainingCampPendingPhaseKey, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveInboxPrimaryActionLabel(Godot.Collections.Dictionary message)
    {
        var configuredLabel = FmtString(GetFirstNonNil(message, "primary_action", "primaryAction"), "");
        if (!string.IsNullOrWhiteSpace(configuredLabel))
        {
            if (IsPostseasonPendingMessage(message) || IsSeasonCompleteMessage(message))
                return "Continue";
            if (IsOffseasonPendingMessage(message))
                return string.Equals(FmtString(GetFirstNonNil(message, "type"), ""), ScheduleService.TrainingCampPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
                    ? "Continue"
                    : configuredLabel;
            return configuredLabel;
        }

        if (IsGameDayMessage(message))
            return "View Matchup";
        if (IsRosterInvalidMessage(message))
            return "View Roster";
        if (IsDepthChartInvalidMessage(message))
            return "View Depth Chart";
        if (IsPostseasonPendingMessage(message))
            return "Continue";
        if (IsSeasonCompleteMessage(message))
            return "League";
        if (IsOffseasonPendingMessage(message))
            return "Continue";
        return "Primary Action";
    }

    private static bool HasReachedOffseasonPlaceholderTarget(GridironGM.GameCore.Models.LeagueState league, string targetPhase)
    {
        var currentPhase = ScheduleService.GetPhaseForWeek(league.Calendar.Week);
        var currentWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(currentPhase);
        var targetWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(targetPhase);
        return currentWeek > 0 && targetWeek > 0 && currentWeek >= targetWeek;
    }

    private static string FormatOverviewActionSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return "Message";

        const string prefix = "Action Required:";
        var cleaned = subject.Trim();
        if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? subject.Trim() : cleaned;
    }

    private void ClearInboxDetail(string subject = "No urgent actions.", string body = "")
    {
        _selectedInboxMessageId = "";
        _selectedInboxActionItem = null;
        _selectedSimGameId = "";
        if (_overviewActionHeader != null)
            _overviewActionHeader.Text = "Action Required";
        if (_overviewActionTitle != null)
            _overviewActionTitle.Text = subject;
        if (_overviewActionSuggested != null)
        {
            _overviewActionSuggested.Text = "";
            _overviewActionSuggested.Visible = false;
        }
        if (_overviewActionBody != null)
        {
            _overviewActionBody.FitContent = true;
            _overviewActionBody.CustomMinimumSize = new Vector2(0, 64);
            _overviewActionBody.Text = body;
        }
        if (_overviewActionButton != null)
        {
            _overviewActionButton.Visible = false;
            _overviewActionButton.Disabled = true;
            _overviewActionButton.Text = "Continue";
            _overviewActionButton.TooltipText = "";
        }
    }

    private string GetMessageId(Godot.Collections.Dictionary message)
    {
        var value = GetFirstNonNil(message, "id", "message_id");
        return FmtString(value, "");
    }

    private string GetMessageSubject(Godot.Collections.Dictionary message)
    {
        var value = GetFirstNonNil(message, "subject", "title", "headline");
        var subject = FmtString(value, "");
        return string.IsNullOrWhiteSpace(subject) ? "Message" : subject;
    }

    private string GetMessageBody(Godot.Collections.Dictionary message)
    {
        var value = GetFirstNonNil(message, "body", "message", "text", "content");
        return FmtString(value, "");
    }

    private string GetMessageTimestamp(Godot.Collections.Dictionary message)
    {
        var value = GetFirstNonNil(message, "timestamp", "created_at", "created", "time", "sent_at");
        return FmtString(value, "");
    }

    private bool GetMessageRequiresAck(Godot.Collections.Dictionary message)
    {
        var value = GetFirstNonNil(message, "requires_ack", "requires_acknowledge", "needs_ack");
        return GetBoolValue(value, false);
    }

    private bool IsMessageRead(Godot.Collections.Dictionary message)
    {
        var readValue = GetFirstNonNil(message, "is_read", "read", "read_at");
        if (!IsNil(readValue))
        {
            if (readValue.VariantType == Variant.Type.String)
            {
                var str = readValue.AsString();
                if (!string.IsNullOrWhiteSpace(str))
                    return true;
            }

            return GetBoolValue(readValue, false);
        }

        var unreadValue = GetFirstNonNil(message, "unread", "is_unread");
        if (!IsNil(unreadValue))
            return !GetBoolValue(unreadValue, false);

        return false;
    }

    private bool TryGetSimGameId(Godot.Collections.Dictionary message, out string gameId)
    {
        gameId = "";
        var actionsValue = GetFirstNonNil(message, "actions", "action", "available_actions");
        if (IsNil(actionsValue))
            return false;

        if (actionsValue.VariantType == Variant.Type.Array)
        {
            var actions = actionsValue.AsGodotArray();
            for (var i = 0; i < actions.Count; i++)
            {
                if (TryGetSimGameIdFromAction((Variant)actions[i], out gameId))
                    return true;
            }

            return false;
        }

        if (actionsValue.VariantType == Variant.Type.Dictionary)
            return TryGetSimGameIdFromAction(actionsValue, out gameId);

        return false;
    }

    private static bool IsSimGameAction(string actionType)
    {
        return string.Equals(actionType, "simulate_user_game", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionType, "sim_game", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionType, "simulate_game", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionType, "sim_game_user", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSimGameLabel(string label)
    {
        return !string.IsNullOrWhiteSpace(label)
            && label.IndexOf("Sim Game", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool TryGetSimGameIdFromAction(Variant actionVar, out string gameId)
    {
        gameId = "";
        if (!TryGetDictionary(actionVar, out var action))
            return false;

        var actionType = FmtString(GetFirstNonNil(action, "type", "action", "name"), "");
        var hasActionType = !string.IsNullOrWhiteSpace(actionType);
        var isSimGame = hasActionType
            ? IsSimGameAction(actionType)
            : IsSimGameLabel(FmtString(GetFirstNonNil(action, "label"), ""));
        if (!isSimGame)
            return false;

        var gameIdValue = GetFirstNonNil(action, "game_id", "gameId", "id");
        if (!IsNil(gameIdValue))
        {
            gameId = FmtString(gameIdValue, "");
            if (!string.IsNullOrWhiteSpace(gameId))
                return true;
        }

        if (action.ContainsKey("payload"))
        {
            var payloadVar = (Variant)action["payload"];
            if (TryGetDictionary(payloadVar, out var payload))
            {
                var payloadGameId = GetFirstNonNil(payload, "game_id", "gameId", "id");
                gameId = FmtString(payloadGameId, "");
                if (!string.IsNullOrWhiteSpace(gameId))
                    return true;
            }
        }

        if (action.ContainsKey("data"))
        {
            var dataVar = (Variant)action["data"];
            if (TryGetDictionary(dataVar, out var data))
            {
                var dataGameId = GetFirstNonNil(data, "game_id", "gameId", "id");
                gameId = FmtString(dataGameId, "");
                if (!string.IsNullOrWhiteSpace(gameId))
                    return true;
            }
        }

        return false;
    }

    private void ApplyRosterFilters()
    {
        var selectedPlayerId = GetSelectedPlayerId();
        BuildRosterTree();

        if (!string.IsNullOrWhiteSpace(selectedPlayerId) && TrySelectRosterPlayer(selectedPlayerId))
            return;

        _rosterTree.DeselectAll();
        SetReportPlaceholder("Select a player to view the scout report.");
    }

    private string GetSelectedPlayerId()
    {
        var selected = _rosterTree.GetSelected();
        if (selected == null)
            return "";

        var metadata = selected.GetMetadata(0);
        if (IsNil(metadata))
            return "";

        return metadata.VariantType == Variant.Type.String ? metadata.AsString() : metadata.ToString();
    }

    private bool TrySelectRosterPlayer(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return false;

        var root = _rosterTree.GetRoot();
        if (root == null)
            return false;

        var item = root.GetFirstChild();
        while (item != null)
        {
            var metadata = item.GetMetadata(0);
            if (!IsNil(metadata))
            {
                var currentId = metadata.VariantType == Variant.Type.String ? metadata.AsString() : metadata.ToString();
                if (string.Equals(currentId, playerId, StringComparison.OrdinalIgnoreCase))
                {
                    item.Select(0);
                    OnRosterItemSelected(item);
                    return true;
                }
            }

            item = item.GetNext();
        }

        return false;
    }

    private bool PassesRosterFilters(PlayerRow row)
    {
        var search = _rosterSearchText?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var name = row.Name ?? "";
            if (name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(_posFilterValue)
            && !string.Equals(_posFilterValue, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (!MatchesPosFilter(row.Position, _posFilterValue))
                return false;
        }

        return true;
    }

    private static bool MatchesPosFilter(string pos, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter) || string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(pos))
            return false;

        if (string.Equals(filter, "OL", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(pos, "LT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pos, "LG", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pos, "C", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pos, "RG", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pos, "RT", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(filter, "DL", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(pos, "DT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pos, "EDGE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pos, "DE", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(filter, "DB", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(pos, "CB", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pos, "S", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(filter, "EDGE", StringComparison.OrdinalIgnoreCase))
            return string.Equals(pos, "EDGE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pos, "DE", StringComparison.OrdinalIgnoreCase);

        return string.Equals(pos, filter, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetPositionSortOrder(string pos)
    {
        return FootballPositionOrder.GetSortOrder(pos);
    }

    private static int GetPosFilterIndex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        for (var i = 0; i < PosFilterOptions.Length; i++)
        {
            if (string.Equals(PosFilterOptions[i], value, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return 0;
    }

    private List<RosterColumn> GetVisibleColumns()
    {
        var visible = new List<RosterColumn>();
        foreach (var column in _columns)
        {
            var isVisible = _columnVisibility.TryGetValue(column.Id, out var visibleFlag)
                ? visibleFlag
                : column.DefaultVisible;
            if (isVisible)
                visible.Add(column);
        }

        return visible;
    }

    private void ApplyColumnVisibility()
    {
        if (_rosterTree == null)
            return;

        var visibleColumns = GetVisibleColumns();
        _rosterTree.Columns = visibleColumns.Count;

        for (var i = 0; i < visibleColumns.Count; i++)
        {
            var column = visibleColumns[i];
            _rosterTree.SetColumnTitle(i, column.Title);
            _rosterTree.SetColumnExpand(i, column.Expand);
            if (column.Width > 0)
                _rosterTree.SetColumnCustomMinimumWidth(i, column.Width);
        }

        BuildRosterTree();
    }

    private void PopulateColumnsMenu()
    {
        if (_popupColumns == null)
            return;

        _popupColumns.Clear();
        for (var i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            _popupColumns.AddCheckItem(column.Title, i);
            var visible = _columnVisibility.TryGetValue(column.Id, out var isVisible)
                ? isVisible
                : column.DefaultVisible;
            _popupColumns.SetItemChecked(i, visible);
        }
    }

    private void OnColumnsPressed()
    {
        if (_btnColumns == null || _popupColumns == null)
            return;

        PopulateColumnsMenu();
        var pos = (Vector2I)_btnColumns.GlobalPosition + new Vector2I(0, (int)_btnColumns.Size.Y);
        _popupColumns.Position = pos;
        _popupColumns.Popup();
    }

    private void OnRosterSplitDragged(long offset)
    {
        SaveRosterSplitOffset((int)offset);
    }

    private void OnRosterSearchTextChanged(string newText)
    {
        if (_suppressRosterFilterEvents)
            return;

        _rosterSearchText = newText ?? "";
        SaveRosterFilters();
        ApplyRosterFilters();
    }

    private void OnPosFilterItemSelected(long index)
    {
        if (_suppressRosterFilterEvents)
            return;

        var selection = (int)index;
        if (selection < 0 || selection >= PosFilterOptions.Length)
            selection = 0;

        _posFilterValue = PosFilterOptions[selection];
        SaveRosterFilters();
        ApplyRosterFilters();
    }

    private void OnClearFiltersPressed()
    {
        if (_suppressRosterFilterEvents)
            return;

        _suppressRosterFilterEvents = true;
        if (_rosterSearch != null)
            _rosterSearch.Text = "";
        if (_posFilter != null)
            _posFilter.Select(0);
        _suppressRosterFilterEvents = false;

        _rosterSearchText = "";
        _posFilterValue = PosFilterOptions[0];
        SaveRosterFilters();
        ApplyRosterFilters();
    }

    private void OnColumnMenuIdPressed(long id)
    {
        var columnIndex = (int)id;
        if (columnIndex < 0 || columnIndex >= _columns.Count)
            return;

        var column = _columns[columnIndex];
        var current = _columnVisibility.TryGetValue(column.Id, out var isVisible)
            ? isVisible
            : column.DefaultVisible;
        _columnVisibility[column.Id] = !current;

        ApplyColumnVisibility();
        PopulateColumnsMenu();
        SaveColumnVisibility();
    }

    private void OnRosterColumnTitleClicked(long column, long mouseButtonIndex)
    {
        var visibleColumns = GetVisibleColumns();
        if (column < 0 || column >= visibleColumns.Count)
            return;

        var columnDef = visibleColumns[(int)column];
        if (!columnDef.Sortable)
            return;

        if (_sortColumnId == columnDef.Id)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumnId = columnDef.Id;
            _sortAscending = true;
        }

        var selectedPlayerId = GetSelectedPlayerId();
        BuildRosterTree();
        if (!string.IsNullOrWhiteSpace(selectedPlayerId) && TrySelectRosterPlayer(selectedPlayerId))
            return;

        _rosterTree.DeselectAll();
        SetReportPlaceholder("Select a player to view the scout report.");
    }

    private void BuildRosterRows()
    {
        _rosterRows.Clear();
        if (_currentRoster == null)
            return;

        for (var i = 0; i < _currentRoster.Count; i++)
        {
            var player = (Godot.Collections.Dictionary)_currentRoster[i];
            var row = new PlayerRow(
                id: GetPlayerId(player),
                name: SafeString(player, new[] { "name", "player_name", "full_name" }, "Unknown Player"),
                position: SafeString(player, "position", "-"),
                age: GetAgeValue(player),
                overall: GetOverallValue(player),
                status: GetCompactRosterStatus(player),
                injury: GetCompactRosterInjury(player),
                source: player);
            _rosterRows.Add(row);
        }
    }

    private void BuildRosterTree()
    {
        if (_rosterTree == null)
            return;

        var visibleColumns = GetVisibleColumns();
        _rosterTree.Clear();
        var root = _rosterTree.CreateItem();

        if (_currentRoster == null || _currentRoster.Count == 0)
        {
            if (visibleColumns.Count > 0)
            {
                var emptyItem = _rosterTree.CreateItem(root);
                emptyItem.SetText(0, "No roster data.");
            }
            return;
        }

        BuildRosterRows();
        var players = new List<PlayerRow>(_rosterRows.Count);
        for (var i = 0; i < _rosterRows.Count; i++)
        {
            var player = _rosterRows[i];
            if (PassesRosterFilters(player))
                players.Add(player);
        }

        if (players.Count == 0)
        {
            if (visibleColumns.Count > 0)
            {
                var emptyItem = _rosterTree.CreateItem(root);
                emptyItem.SetText(0, "No roster data.");
            }
            return;
        }

        SortRoster(players);

        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];

            if (DEBUG_DASHBOARD && !_printedFirstPlayerDebug && i == 0)
            {
                var keys = string.Join(", ", player.Source.Keys);
                var potValue = player.Source.ContainsKey("pot") ? (Variant)player.Source["pot"] : default;
                var potentialValue = player.Source.ContainsKey("potential") ? (Variant)player.Source["potential"] : default;
                var potRatingValue = player.Source.ContainsKey("pot_rating") ? (Variant)player.Source["pot_rating"] : default;
                GD.Print($"Roster[0] keys: [{keys}] | pot={DebugVariant(potValue)} | potential={DebugVariant(potentialValue)} | pot_rating={DebugVariant(potRatingValue)}");
                _printedFirstPlayerDebug = true;
            }

            var item = _rosterTree.CreateItem(root);
            if (!string.IsNullOrWhiteSpace(player.Id) && visibleColumns.Count > 0)
                item.SetMetadata(0, player.Id);
            for (var colIndex = 0; colIndex < visibleColumns.Count; colIndex++)
            {
                var column = visibleColumns[colIndex];
                item.SetText(colIndex, column.Getter(player));
            }
        }
    }

    private void OnRosterItemSelected(TreeItem selected)
    {
        if (selected == null)
        {
            SetReportPlaceholder("Select a player to view the scout report.");
            return;
        }

        var metadata = selected.GetMetadata(0);
        if (IsNil(metadata))
        {
            SetReportPlaceholder("No report available.");
            return;
        }

        var playerId = metadata.VariantType == Variant.Type.String ? metadata.AsString() : metadata.ToString();
        if (string.IsNullOrWhiteSpace(playerId) || !_playerDetailsById.TryGetValue(playerId, out var player))
        {
            SetReportPlaceholder("No report available.");
            return;
        }

        UpdateReportPanel(player);
    }

    private void SortRoster(List<PlayerRow> players)
    {
        var sortedColumn = GetColumnById(_sortColumnId);
        if (sortedColumn != null && sortedColumn.Sortable && sortedColumn.SortGetter != null)
        {
            if (string.Equals(sortedColumn.Id, "pos", StringComparison.OrdinalIgnoreCase))
            {
                players.Sort((a, b) =>
                {
                    var aValue = GetPositionSortOrder(a.Position);
                    var bValue = GetPositionSortOrder(b.Position);
                    var comparison = aValue.CompareTo(bValue);
                    if (comparison == 0)
                        comparison = StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
                    return _sortAscending ? comparison : -comparison;
                });
                return;
            }

            players.Sort((a, b) =>
            {
                var aValue = sortedColumn.SortGetter(a);
                var bValue = sortedColumn.SortGetter(b);
                var comparison = CompareSortValues(aValue, bValue);
                return _sortAscending ? comparison : -comparison;
            });
            return;
        }

        players.Sort((a, b) =>
        {
            var positionCompare = FootballPositionOrder.Compare(a.Position, b.Position);
            if (positionCompare != 0)
                return positionCompare;

            var overallCompare = b.Overall.CompareTo(a.Overall);
            if (overallCompare != 0)
                return overallCompare;

            return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
        });
    }

    private RosterColumn GetColumnById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        foreach (var column in _columns)
        {
            if (string.Equals(column.Id, id, StringComparison.OrdinalIgnoreCase))
                return column;
        }

        return null;
    }

    private static int CompareSortValues(IComparable aValue, IComparable bValue)
    {
        if (aValue == null && bValue == null)
            return 0;
        if (aValue == null)
            return -1;
        if (bValue == null)
            return 1;

        if (aValue is string aString && bValue is string bString)
            return StringComparer.OrdinalIgnoreCase.Compare(aString, bString);

        return Comparer<IComparable>.Default.Compare(aValue, bValue);
    }

    private void ShowRosterMessage(string message)
    {
        if (_rosterTree == null)
            return;

        ConfigureRosterTreeForCompactView();
        _rosterTree.Clear();
        var root = _rosterTree.CreateItem();
        var item = _rosterTree.CreateItem(root);
        item.SetText(0, message);
    }

    private void BuildPlayerDetailsMap(Godot.Collections.Array roster)
    {
        _playerDetailsById.Clear();
        for (var i = 0; i < roster.Count; i++)
        {
            var player = (Godot.Collections.Dictionary)roster[i];
            var playerId = GetPlayerId(player);
            if (string.IsNullOrWhiteSpace(playerId))
                continue;
            _playerDetailsById[playerId] = player;
        }
    }

    private void UpdateReportPanel(Godot.Collections.Dictionary player)
    {
        var pos = GetString(player, "position");
        var name = GetString(player, "name");
        var age = GetAgeValue(player);

        var displayName = string.IsNullOrWhiteSpace(name) ? "Player" : name;
        var ageText = age > 0 ? age.ToString() : "?";
        var abilityLabel = GetAbilityLabel(GetOverallValue(player));
        var upsideLabel = GetUpsideLabel(GetPotValueInt(player));
        var confidenceValue = GetFirstNonNil(player, "confidence", "scout_confidence", "scouting_confidence");
        var confidence = FmtString(confidenceValue, "");
        if (string.IsNullOrWhiteSpace(confidence))
            confidence = "Med";

        var header = string.IsNullOrWhiteSpace(pos)
            ? $"{displayName}, Age {ageText} - {abilityLabel} | {upsideLabel} ({confidence})"
            : $"{displayName} ({pos}), Age {ageText} - {abilityLabel} | {upsideLabel} ({confidence})";

        if (_lblPlayerHeader != null)
            _lblPlayerHeader.Text = header;

        var summary = player.ContainsKey("scout_summary")
            ? FmtString((Variant)player["scout_summary"], "")
            : "";
        if (_rtlScoutSummary != null)
            _rtlScoutSummary.Text = string.IsNullOrWhiteSpace(summary) ? "No scout summary available." : summary;

        var report = player.ContainsKey("scout_report")
            ? FmtString((Variant)player["scout_report"], "")
            : "";
        if (_rtlScoutReport != null)
            _rtlScoutReport.Text = string.IsNullOrWhiteSpace(report) ? "No scout report available." : report;

        UpdateTags(player);
    }

    private void UpdateTags(Godot.Collections.Dictionary player)
    {
        if (_tagsRow == null)
            return;

        foreach (var child in _tagsRow.GetChildren())
            ((Node)child).QueueFree();

        if (!player.ContainsKey("tags"))
        {
            _tagsRow.Visible = false;
            return;
        }

        var tagsVariant = (Variant)player["tags"];
        if (tagsVariant.VariantType != Variant.Type.Array)
        {
            _tagsRow.Visible = false;
            return;
        }

        var tags = tagsVariant.AsGodotArray();
        if (tags.Count == 0)
        {
            _tagsRow.Visible = false;
            return;
        }

        for (var i = 0; i < tags.Count; i++)
        {
            var tagValue = (Variant)tags[i];
            var tagText = FmtString(tagValue, "");
            if (string.IsNullOrWhiteSpace(tagText))
                continue;

            var tagLabel = new Label
            {
                Text = tagText
            };
            _tagsRow.AddChild(tagLabel);
        }

        _tagsRow.Visible = _tagsRow.GetChildCount() > 0;
    }

    private void SetReportPlaceholder(string message)
    {
        if (_lblPlayerHeader != null)
            _lblPlayerHeader.Text = "Player Report";
        if (_rtlScoutSummary != null)
            _rtlScoutSummary.Text = message;
        if (_rtlScoutReport != null)
            _rtlScoutReport.Text = "";
        if (_tagsRow != null)
        {
            foreach (var child in _tagsRow.GetChildren())
                ((Node)child).QueueFree();
            _tagsRow.Visible = false;
        }
    }

    private static string GetString(Godot.Collections.Dictionary player, string key)
    {
        return player.ContainsKey(key) ? player[key].ToString() : "";
    }

    private static string GetAbilityLabel(int overall)
    {
        if (overall <= 0)
            return "?";
        if (overall < 55)
            return "Depth";
        if (overall <= 64)
            return "Backup";
        if (overall <= 74)
            return "Spot Starter";
        if (overall <= 82)
            return "Starter";
        if (overall <= 89)
            return "Pro Bowl";
        if (overall <= 94)
            return "All-Pro";
        return "Elite";
    }

    private static string GetUpsideLabel(int pot)
    {
        if (pot <= 0)
            return "?";
        if (pot < 55)
            return "Depth Upside";
        if (pot <= 64)
            return "Backup Upside";
        if (pot <= 74)
            return "Spot Starter Upside";
        if (pot <= 82)
            return "Starter Upside";
        if (pot <= 89)
            return "Pro Bowl Upside";
        if (pot <= 94)
            return "All-Pro Upside";
        return "Elite Upside";
    }

    private sealed class PlayerRow
    {
        public PlayerRow(string id, string name, string position, int age, int overall, string status, string injury, Godot.Collections.Dictionary source)
        {
            Id = id ?? "";
            Name = name ?? "";
            Position = position ?? "";
            Age = age;
            Overall = overall;
            Status = status ?? "";
            Injury = injury ?? "";
            Source = source;
        }

        public string Id { get; }
        public string Name { get; }
        public string Position { get; }
        public int Age { get; }
        public int Overall { get; }
        public string Status { get; }
        public string Injury { get; }
        public Godot.Collections.Dictionary Source { get; }

        public string PositionDisplay
            => string.Equals(Position, "DE", StringComparison.OrdinalIgnoreCase) ? "EDGE" : Position;
    }

    private sealed class RosterColumn
    {
        public RosterColumn(
            string id,
            string title,
            bool defaultVisible,
            int width,
            bool expand,
            Func<PlayerRow, string> getter,
            Func<PlayerRow, IComparable> sortGetter,
            bool sortable)
        {
            Id = id;
            Title = title;
            DefaultVisible = defaultVisible;
            Width = width;
            Expand = expand;
            Getter = getter;
            SortGetter = sortGetter;
            Sortable = sortable;
        }

        public string Id { get; }
        public string Title { get; }
        public bool DefaultVisible { get; }
        public int Width { get; }
        public bool Expand { get; }
        public Func<PlayerRow, string> Getter { get; }
        public Func<PlayerRow, IComparable> SortGetter { get; }
        public bool Sortable { get; }
    }

}
