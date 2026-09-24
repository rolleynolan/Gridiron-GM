using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
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
    private static bool _printedFirstPlayerDebug = false;
    [Export]
    public bool DebugToolsVisibleByDefault { get; set; } = false;
    private Label _serverStatus;
    private Label _shellCalendar;
    private Label _shellTeam;
    private Label _shellTeamContext;
    private Label _shellRuntime;
    private Label _shellRecord;
    private TextureRect _shellTeamLogo;
    private TextureRect _railTeamLogo;
    private Button _shellInboxBadge;
    private Button _shellAdvance;
    private VBoxContainer _shellNavigation;
    private VBoxContainer _teamNavigationGroup;
    private VBoxContainer _financesNavigationGroup;
    private VBoxContainer _leagueNavigationGroup;
    private VBoxContainer _scoutingNavigationGroup;
    private VBoxContainer _tradeNavigationGroup;
    private AcceptDialog _collegeRankingsDialog;
    private Tree _collegeRankingsTree;
    private RichTextLabel _collegeTeamProfile;
    private LineEdit _collegeRankingsSearch;
    private OptionButton _collegeRankingsConference;
    private Label _collegeRankingsCount;
    private AcceptDialog _collegeLeadersDialog;
    private AcceptDialog _collegePostseasonProjectionsDialog;
    private AcceptDialog _collegeBigBoardsDialog;
    private AcceptDialog _collegeAwardsDialog;
    private AcceptDialog _collegeNewsDialog;
    private VBoxContainer _leagueStatsWorkspace;
    private OptionButton _leagueStatsCategory;
    private OptionButton _leagueStatsMeasure;
    private OptionButton _leagueStatsViewMode;
    private Tree _leagueStatsTree;
    private bool _leagueStatsWorkspaceActive;
    private VBoxContainer _leagueScheduleWorkspace;
    private OptionButton _leagueScheduleWeekPicker;
    private TabContainer _leagueScheduleTabs;
    private Tree _leagueWeekScheduleTree;
    private Tree _leaguePlayoffTree;
    private bool _leagueScheduleWorkspaceActive;
    private ScrollContainer _leagueNewsWorkspace;
    private VBoxContainer _leagueNewsGrid;
    private readonly List<LeagueNewsStory> _leagueNewsStories = new();
    private VBoxContainer _leaguePlayerSearchWorkspace;
    private LineEdit _leaguePlayerSearchText;
    private OptionButton _leaguePlayerPositionFilter;
    private OptionButton _leaguePlayerStatusFilter;
    private Label _leaguePlayerSearchCount;
    private Tree _leaguePlayerSearchTree;
    private int _leaguePlayerSearchPage;
    private VBoxContainer _leagueHistoryWorkspace;
    private OptionButton _leagueHistoryYearPicker;
    private TabContainer _leagueHistoryArchiveTabs;
    private VBoxContainer _leagueAwardsWorkspace;
    private readonly Dictionary<int, Button> _shellPrimaryNavigation = new();
    private Control _franchiseHome;
    private VBoxContainer _homeStandingsBody;
    private VBoxContainer _homeProfileBody;
    private VBoxContainer _homeNewsBody;
    private VBoxContainer _homeProspectsBody;
    private VBoxContainer _homeLeadersBody;
    private int _homeConferenceIndex;
    private int _homeProspectPanelIndex;
    private int _homeLeaderCategory;
    private bool _dashboardEditMode;
    private HBoxContainer _dashboardEditorBar;
    private OptionButton _dashboardTilePicker;
    private Label _dashboardEditHint;
    private VBoxContainer _dashboardTileGrid;
    private readonly List<string> _dashboardTileOrder = new() { "standings", "news", "profile", "prospects", "leaders" };
    private readonly HashSet<string> _dashboardHiddenTiles = new();
    private bool _dashboardFeaturedTileWide = true;
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
    private AcceptDialog _franchiseSettingsDialog;
    private RichTextLabel _franchiseProfileSummary;
    private Label _franchiseSaveStatus;
    private CheckButton _franchiseDeveloperToggle;
    private AcceptDialog _inboxDeskDialog;
    private OptionButton _inboxCategoryFilter;
    private ItemList _inboxQueueList;
    private Label _inboxDeskCount;
    private Label _inboxDeskSubject;
    private Label _inboxDeskContext;
    private RichTextLabel _inboxDeskBody;
    private Button _inboxDeskAction;
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
    private AcceptDialog _franchiseSetupDialog;
    private OptionButton _setupProfileSelect;
    private LineEdit _setupGmName;
    private SpinBox _setupNegotiation;
    private SpinBox _setupPlayerManagement;
    private SpinBox _setupScouting;
    private SpinBox _setupLeadership;
    private OptionButton _setupRosterSource;
    private OptionButton _setupOutfit;
    private Label _setupBudget;
    private Label _setupStatus;
    private List<GmProfile> _setupProfiles = new();
    private Button _btnFreeAgency;
    private Button _btnTrades;
    private AcceptDialog _marketDeskDialog;
    private Label _marketDeskSummary;
    private RichTextLabel _marketDeskHistory;
    private Label _marketDeskStatus;
    private AcceptDialog _tradeDialog;
    private AcceptDialog _tradeFinderDialog;
    private ItemList _tradeFinderAssets;
    private Tree _tradeFinderOffers;
    private OptionButton _tradeFinderRequestedPosition;
    private Label _tradeFinderSelectionStatus;
    private RichTextLabel _tradeFinderStatus;
    private Button _tradeFinderAcceptOffer;
    private Button _tradeFinderRejectOffer;
    private Button _tradeFinderWithdraw;
    private string _selectedTradeMarketOfferId = "";
    private readonly List<string> _tradeFinderSelectedAssets = new();
    private readonly List<string> _pendingTradeOfferAssets = new();
    private AcceptDialog _waiversDialog;
    private Tree _waiversTree;
    private Label _waiversSummary;
    private RichTextLabel _waiversStatus;
    private Button _btnClaimWaiverMarket;
    private OptionButton _waiverConditionalReleasePicker;
    private string _selectedWaiverMarketPlayerId = "";
    private AcceptDialog _leagueTransactionsDialog;
    private OptionButton _leagueTransactionsTypeFilter;
    private OptionButton _leagueTransactionsTeamFilter;
    private LineEdit _leagueTransactionsPlayerSearch;
    private Tree _leagueTransactionsTree;
    private RichTextLabel _leagueTransactionsStatus;
    private OptionButton _tradePartnerSelect;
    private ItemList _tradeOfferAssets;
    private ItemList _tradeRequestAssets;
    private RichTextLabel _tradePartnerEvaluation;
    private RichTextLabel _tradeRationale;
    private Label _tradeStatus;
    private AcceptDialog _freeAgencyDialog;
    private Label _freeAgencyCapSummary;
    private Tree _freeAgentList;
    private LineEdit _freeAgencySearch;
    private OptionButton _freeAgencyPositionFilter;
    private MenuButton _freeAgencyColumnsMenu;
    private PopupMenu _freeAgentContextMenu;
    private AcceptDialog _freeAgentNegotiationDialog;
    private RichTextLabel _freeAgentNegotiationContext;
    private int _freeAgentSortColumn;
    private bool _freeAgentSortDescending = true;
    private SpinBox _freeAgentAnnualOffer;
    private SpinBox _freeAgentGuaranteeOffer;
    private SpinBox _freeAgentYearsOffer;
    private OptionButton _freeAgentContractType;
    private Button _btnSubmitFreeAgentOffer;
    private Label _freeAgencyStatus;
    private string _selectedFreeAgentId = "";
    private Button _btnRosterManagement;
    private AcceptDialog _rosterManagementDialog;
    private ItemList _waiverClaimList;
    private ItemList _practiceSquadFreeAgentList;
    private ItemList _practiceSquadList;
    private SpinBox _practiceSquadAnnualOffer;
    private RichTextLabel _transactionHistoryText;
    private Label _rosterManagementStatus;
    private string _selectedWaiverPlayerId = "";
    private string _selectedPracticeSquadFreeAgentId = "";
    private string _selectedPracticeSquadPlayerId = "";
    private ConfirmationDialog _practiceSquadActiveSigningDialog;
    private RichTextLabel _practiceSquadActiveSigningDetails;
    private string _practiceSquadActiveSigningPlayerId = "";
    private Button _btnTrainingCamp;
    private AcceptDialog _trainingCampDialog;
    private Label _trainingCampStatus;
    private RichTextLabel _trainingCampReport;
    private RichTextLabel _trainingCampRoles;
    private AcceptDialog _trainingCampWeeklyReportDialog;
    private Label _trainingCampWeeklyReportSummary;
    private Tree _trainingCampWeeklyPositionTree;
    private Tree _trainingCampWeeklyBattleTree;
    private Tree _trainingCampWeeklyHealthTree;
    private Label _trainingCampWeeklyReportStatus;
    private AcceptDialog _trainingCampPlayerFocusDialog;
    private Tree _trainingCampPlayerFocusTree;
    private LineEdit _trainingCampPlayerFocusSearch;
    private OptionButton _trainingCampPlayerFocusPosition;
    private Label _trainingCampPlayerFocusSummary;
    private Label _trainingCampPlayerFocusStatus;
    private Button _trainingCampApplyPlayerFocus;
    private string _trainingCampSelectedFocusPlayerId = "";
    private AcceptDialog _trainingCampPositionFocusDialog;
    private Tree _trainingCampPositionFocusTree;
    private Label _trainingCampPositionFocusSummary;
    private Label _trainingCampPositionFocusStatus;
    private Button _trainingCampApplyPositionFocus;
    private string _trainingCampSelectedFocusPosition = "";
    private AcceptDialog _finalCutdownDialog;
    private Tree _finalCutdownTree;
    private Label _finalCutdownSummary;
    private Label _finalCutdownStatus;
    private Button _finalCutdownConfirmCuts;
    private ConfirmationDialog _finalCutdownBatchDialog;
    private RichTextLabel _finalCutdownBatchDetails;
    private readonly HashSet<string> _finalCutdownSelectedPlayerIds = new(StringComparer.OrdinalIgnoreCase);
    private Button _btnReleaseSelectedPlayer;
    private Button _btnWaiveSelectedPlayer;
    private Button _btnMoveSelectedPlayerToIr;
    private Button _btnOfferExtension;
    private Button _btnApplyFranchiseTag;
    private ConfirmationDialog _releaseDialog;
    private RichTextLabel _releaseDetails;
    private string _releasePlayerId = "";
    private PopupMenu _rosterContextMenu;
    private PopupMenu _rosterContractMenu;
    private AcceptDialog _extensionDialog;
    private Label _extensionPlayerLabel;
    private SpinBox _extensionAnnualOffer;
    private SpinBox _extensionGuaranteeOffer;
    private SpinBox _extensionYearsOffer;
    private Label _extensionStatus;
    private string _extensionPlayerId = "";
    private Button _btnDraftBoard;
    private AcceptDialog _udfaMarketDialog;
    private Tree _udfaMarketTree;
    private Label _udfaMarketSummary;
    private Label _udfaMarketStatus;
    private Button _udfaOfferContract;
    private Button _udfaInvite;
    private string _selectedUdfaPlayerId = "";
    private AcceptDialog _draftBoardDialog;
    private ItemList _draftProspectList;
    private RichTextLabel _draftProspectDetail;
    private LineEdit _draftSearch;
    private OptionButton _draftPositionFilter;
    private ItemList _draftOrderList;
    private ItemList _draftRecentPicks;
    private ItemList _teamDraftBoardList;
    private OptionButton _teamDraftBoardPositionFilter;
    private OptionButton _teamDraftBoardConfidenceFilter;
    private Label _teamDraftBoardFilterSummary;
    private OptionButton _teamDraftBoardTag;
    private LineEdit _teamDraftBoardTier;
    private LineEdit _teamDraftBoardNote;
    private Label _draftPickContext;
    private Label _draftOwnedPicks;
    private Label _draftStatus;
    private Button _btnStartDraft;
    private Button _btnMakeDraftPick;
    private Button _btnTradeCurrentDraftPick;
    private AcceptDialog _draftAnnouncementDialog;
    private TextureRect _draftAnnouncementLogo;
    private Label _draftAnnouncementKicker;
    private Label _draftAnnouncementHeadline;
    private RichTextLabel _draftAnnouncementBody;
    private CheckBox _draftShortAnnouncements;
    private readonly Queue<DraftClassRecapEntry> _pendingDraftAnnouncements = new();
    private bool _syncingDraftAnnouncementPreference;
    private string _selectedDraftProspectId = "";
    private PopupMenu _popupColumns;
    private Control _rosterPane;
    private Control _playerReportPanel;
    private Label _squadWorkspaceHeader;
    private Label _lblPlayerHeader;
    private Label _lblRosterEvaluation;
    private RichTextLabel _rtlPlayerStats;
    private RichTextLabel _rtlScoutSummary;
    private RichTextLabel _rtlScoutReport;
    private Container _tagsRow;
    private HBoxContainer _squadPlayerActions;

    // NEW: team/roster UI
    private ItemList _teamList;
    private Label _rosterSummary;
    private Button _btnRosterViewMode;
    private Button _btnDepthChartViewMode;
    private HSplitContainer _rosterSplit;
    private LineEdit _rosterSearch;
    private OptionButton _posFilter;
    private Button _btnClearFilters;
    private OptionButton _rosterStatusFilter;
    private Tree _rosterTree;
    private Control _depthChartPanel;
    private Label _depthChartSummary;
    private Button _btnAutoFillDepthChart;
    private Button _btnDepthChartSetStarter;
    private Button _btnDepthChartToggleLock;
    private Button _btnReturnToLiveGame;
    private Label _depthChartActionStatus;
    private Label _depthChartSelectionStatus;
    private Tree _depthChartTree;
    private DepthFieldDiagram _depthFieldDiagram;
    private Godot.Collections.Array _depthChartPositions = new();
    private Godot.Collections.Dictionary _depthChartPayload;
    private LineEdit _depthChartSearch;
    private int _depthChartUnitFilter;
    private VBoxContainer _developmentWorkspace;
    private Label _developmentWindowLabel;
    private LineEdit _developmentSearch;
    private OptionButton _developmentPositionFilter;
    private OptionButton _developmentTrendFilter;
    private OptionButton _developmentChangeFilter;
    private Tree _developmentTree;
    private readonly List<DevelopmentRow> _developmentRows = new();
    private string _developmentSortColumn = "player";
    private bool _developmentSortAscending = true;
    private ScrollContainer _injuriesWorkspace;
    private HBoxContainer _injuryPanelRow;
    private readonly Dictionary<string, Tree> _injuryPanelTrees = new(StringComparer.OrdinalIgnoreCase);
    private VBoxContainer _staffWorkspace;
    private Label _staffOrganizationLabel;
    private Tree _staffTree;
    private readonly List<StaffRow> _staffRows = new();
    private AcceptDialog _staffDetailDialog;
    private Label _staffDetailHeader;
    private RichTextLabel _staffDetailBody;
    private OptionButton _staffMarketPicker;
    private Button _staffChangeButton;
    private Label _staffChangeStatus;
    private string _selectedStaffRole = "";
    private string _selectedStaffCoachId = "";
    private VBoxContainer _teamHistoryWorkspace;
    private Label _teamHistoryOrganizationLabel;
    private TabContainer _teamHistoryTabs;
    private Tree _teamSeasonHistoryTree;
    private Tree _teamFinancialHistoryTree;
    private Tree _teamDraftHistoryTree;
    private Tree _teamTransactionHistoryTree;
    private Tree _teamStaffHistoryTree;
    private AcceptDialog _teamSeasonRecapDialog;
    private Label _teamSeasonRecapHeader;
    private RichTextLabel _teamSeasonRecapBody;
    private readonly List<TeamHistorySeasonRow> _teamHistorySeasons = new();
    private string _teamHistorySortColumn = "season";
    private bool _teamHistorySortAscending;
    private ScrollContainer _teamStandingsWorkspace;
    private HBoxContainer _teamStandingsPanels;
    private Label _teamStandingsContext;
    private Tree _teamDivisionStandingsTree;
    private Tree _teamConferencePlayoffTree;
    private VBoxContainer _teamStatsWorkspace;
    private OptionButton _teamStatsSeasonFilter;
    private OptionButton _teamStatsComparisonFilter;
    private HBoxContainer _teamStatsEditorBar;
    private OptionButton _teamStatsTilePicker;
    private VBoxContainer _teamStatsGrid;
    private Label _teamStatsEditHint;
    private bool _teamStatsEditMode;
    private bool _teamStatsViewActive;
    private VBoxContainer _teamFinancesWorkspace;
    private bool _teamFinancesViewActive;
    private VBoxContainer _contractsWorkspace;
    private Tree _contractsTree;
    private bool _contractsViewActive;
    private string _contractsSortColumn = "current";
    private bool _contractsSortAscending;
    private VBoxContainer _accountingWorkspace;
    private Tree _accountingLedgerTree;
    private RichTextLabel _accountingDetail;
    private bool _accountingViewActive;
    private VBoxContainer _practiceSquadWorkspace;
    private Tree _practiceSquadRosterTree;
    private Label _practiceSquadWorkspaceStatus;
    private bool _practiceSquadViewActive;
    private string _practiceSquadWorkspacePlayerId = "";
    private readonly List<string> _teamStatsTileOrder = new() { "totals", "league_rank", "leaders", "trend" };
    private readonly HashSet<string> _teamStatsHiddenTiles = new();
    private bool _teamStatsFeaturedWide = true;
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
    private LiveGameObserver _liveGameObserver;
    private PostGameHub _postGameHub;
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
    private Label _leagueContextSummary;
    private RichTextLabel _scheduleInspector;
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
    private readonly List<string> _rosterColumnOrder = new();
    private readonly Dictionary<string, int> _rosterColumnWidths = new();
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
    private string _lastRosterColumnId = "";
    private string _rosterSearchText = "";
    private string _posFilterValue = "All";
    private string _rosterStatusFilterValue = "All statuses";
    private bool _suppressTeamListEvents = false;
    private bool _suppressRosterFilterEvents = false;
    private bool _depthChartViewActive = false;
    private bool _developmentViewActive = false;
    private bool _injuriesViewActive = false;
    private bool _staffViewActive = false;
    private bool _teamHistoryViewActive = false;
    private bool _teamStandingsViewActive = false;
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
    private string _recordBookSummary = "";
    private HistoricalArchiveResponse _historicalArchive = new();
    private bool _suppressHistorySelectionEvents = false;
    private int? _selectedHistorySeasonYear = null;
    private string _dashboardTeamName = "";
    private string _dashboardTeamAbbreviation = "";
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
    private Godot.Collections.Dictionary _observedGameResult = null;
    private Godot.Collections.Dictionary _selectedScheduleGame = null;
    private bool _restorePostGameRecapAfterBoxScore = false;
    private bool _restoreLiveGameObserverAfterBoxScore = false;
    private bool _liveGameAdjustmentMode = false;

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
    private LiveGameSessionService _nativeLiveGameSessionService;

    // Native dashboard projection used to map roster selection to authoritative team ids.
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

    private void CreateWorkstationShell()
    {
        var navy = new Color("07121f");
        var slate = new Color("0d2031");
        var edge = new Color("254258");
        var ink = new Color("eee7d8");
        var muted = new Color("9cadb8");
        var green = new Color("4f9b55");

        var appMargin = GetNodeOrNull<Control>("AppMargin");
        if (appMargin != null)
            ApplyWorkstationTheme(appMargin, slate, edge, ink, muted, green);

        var legacyTabs = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/TabButtonRow");
        var legacyActions = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/ActionButtonRow");
        var legacyHeader = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/HeaderPanel");
        if (legacyTabs != null)
            legacyTabs.Visible = false;
        if (legacyActions != null)
            legacyActions.Visible = false;
        if (legacyHeader != null)
            legacyHeader.Visible = false;

        var topBar = new PanelContainer { Name = "WorkstationTopBar", MouseFilter = MouseFilterEnum.Ignore };
        topBar.SetAnchorsPreset(LayoutPreset.TopWide);
        topBar.OffsetLeft = 244;
        topBar.OffsetRight = -8;
        topBar.OffsetBottom = 92;
        topBar.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(navy, edge, 0, 1));
        AddChild(topBar);
        var topRow = new HFlowContainer();
        topRow.AddThemeConstantOverride("h_separation", 18);
        topRow.AddThemeConstantOverride("v_separation", 4);
        topBar.AddChild(topRow);
        _shellTeamLogo = CreateTeamLogoTexture(new Vector2(44, 44));
        _shellTeamLogo.TooltipText = "Controlled franchise logo";
        topRow.AddChild(_shellTeamLogo);
        var teamIdentity = new VBoxContainer { CustomMinimumSize = new Vector2(180, 0) };
        teamIdentity.AddThemeConstantOverride("separation", 0);
        _shellTeam = new Label { Text = "FRANCHISE" };
        _shellTeam.AddThemeColorOverride("font_color", ink);
        _shellTeam.AddThemeFontSizeOverride("font_size", 15);
        teamIdentity.AddChild(_shellTeam);
        _shellTeamContext = new Label { Text = "CONTROLLED TEAM" };
        _shellTeamContext.AddThemeColorOverride("font_color", muted);
        _shellTeamContext.AddThemeFontSizeOverride("font_size", 11);
        teamIdentity.AddChild(_shellTeamContext);
        topRow.AddChild(teamIdentity);
        _shellCalendar = new Label { Text = "LEAGUE CONTEXT LOADING", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _shellCalendar.AddThemeColorOverride("font_color", ink);
        _shellCalendar.AddThemeFontSizeOverride("font_size", 14);
        topRow.AddChild(_shellCalendar);
        _shellRecord = new Label { Text = "0-0" };
        _shellRecord.AddThemeColorOverride("font_color", new Color("f4eddf"));
        _shellRecord.AddThemeFontSizeOverride("font_size", 14);
        topRow.AddChild(_shellRecord);
        _shellInboxBadge = CreateShellButton("INBOX 0", ink, edge);
        _shellInboxBadge.TooltipText = "Unread and action-needed messages";
        _shellInboxBadge.Pressed += ShowInboxDesk;
        topRow.AddChild(_shellInboxBadge);
        _shellAdvance = CreateShellButton("ADVANCE", new Color("f4eddf"), green);
        _shellAdvance.AddThemeStyleboxOverride("normal", CreateSurfaceStyle(new Color("193d37"), green, 0, 1));
        _shellAdvance.Pressed += () =>
        {
            if (_inboxMessages != null && _inboxMessages.Count > 0) ShowInboxDesk();
            else _btnContinue?.EmitSignal(Button.SignalName.Pressed);
        };
        topRow.AddChild(_shellAdvance);

        var rail = new PanelContainer { Name = "WorkstationNavigation" };
        rail.SetAnchorsPreset(LayoutPreset.LeftWide);
        rail.OffsetRight = 232;
        rail.OffsetBottom = -8;
        rail.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(navy, edge, 0, 1));
        AddChild(rail);
        _shellNavigation = new VBoxContainer();
        _shellNavigation.AddThemeConstantOverride("separation", 4);
        rail.AddChild(_shellNavigation);
        _railTeamLogo = CreateTeamLogoTexture(new Vector2(72, 72));
        _railTeamLogo.TooltipText = "Controlled franchise logo";
        _shellNavigation.AddChild(_railTeamLogo);
        var brand = new Label { Text = "GRIDIRON GM\nFRANCHISE DESK" };
        brand.AddThemeColorOverride("font_color", ink);
        brand.AddThemeFontSizeOverride("font_size", 20);
        brand.AddThemeConstantOverride("outline_size", 1);
        brand.AddThemeColorOverride("font_outline_color", new Color("1d3447"));
        _shellNavigation.AddChild(brand);
        _shellNavigation.AddChild(CreateNavigationRule(edge));
        _shellPrimaryNavigation[0] = AddNavigationButton("HOME", "Franchise Home", async () => await SelectMainTab(0), true, green, muted, edge);
        AddNavigationButton("INBOX", "Messages and action-needed notices", ShowInboxDesk, false, green, muted, edge);
        var teamButton = AddNavigationButton("TEAM  ▾", "Open team workspace", ToggleTeamNavigation, false, green, muted, edge);
        _teamNavigationGroup = new VBoxContainer { Visible = false };
        _teamNavigationGroup.AddThemeConstantOverride("separation", 1);
        _shellNavigation.AddChild(_teamNavigationGroup);
        AddTeamNavigationButton("Roster", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await SetRosterViewMode(false); }, muted, edge);
        AddTeamNavigationButton("Depth Chart", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await SetRosterViewMode(true); }, muted, edge);
        AddTeamNavigationButton("Practice Squad", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await ShowPracticeSquadWorkspaceAsync(); }, muted, edge);
        AddTeamNavigationButton("Team Standings", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await ShowTeamStandingsWorkspaceAsync(); }, muted, edge);
        AddTeamNavigationButton("Team History", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await ShowTeamHistoryWorkspaceAsync(); }, muted, edge);
        AddTeamNavigationButton("Staff", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await ShowStaffWorkspaceAsync(); }, muted, edge);
        AddTeamNavigationButton("Team Stats", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await ShowTeamStatsWorkspaceAsync(); }, muted, edge);
        AddTeamNavigationButton("Injuries", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await ShowInjuriesWorkspaceAsync(); }, muted, edge);
        AddTeamNavigationButton("Development", async () => await ShowDevelopmentWorkspaceAsync(), muted, edge);
        var financesButton = AddNavigationButton("FINANCES  ▾", "Team finances, contracts, and accounting", ToggleFinancesNavigation, false, green, muted, edge);
        _financesNavigationGroup = new VBoxContainer { Visible = false }; _financesNavigationGroup.AddThemeConstantOverride("separation", 1); _shellNavigation.AddChild(_financesNavigationGroup);
        AddFinancesNavigationButton("Team Finances", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await ShowTeamFinancesWorkspaceAsync(); }, muted, edge);
        AddFinancesNavigationButton("Contracts", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await ShowContractsWorkspaceAsync(); }, muted, edge);
        AddFinancesNavigationButton("Accounting", async () => { await SelectMainTab(ROSTER_TAB_INDEX); await ShowAccountingWorkspaceAsync(); }, muted, edge);
        _shellPrimaryNavigation[LEAGUE_TAB_INDEX] = AddNavigationButton("LEAGUE  ▾", "League reference and competition", ToggleLeagueNavigation, false, green, muted, edge);
        _leagueNavigationGroup = new VBoxContainer { Visible = false }; _leagueNavigationGroup.AddThemeConstantOverride("separation", 1); _shellNavigation.AddChild(_leagueNavigationGroup);
        AddLeagueNavigationButton("Standings", async () => await OpenFullLeagueStandingsAsync(), muted, edge);
        AddLeagueNavigationButton("Stats", async () => await ShowLeagueStatsWorkspaceAsync(), muted, edge);
        AddLeagueNavigationButton("Schedule & Results", async () => await ShowLeagueScheduleWorkspaceAsync(), muted, edge);
        AddLeagueNavigationButton("News", async () => await ShowLeagueNewsWorkspaceAsync(), muted, edge);
        AddLeagueNavigationButton("Player Search", async () => await ShowLeaguePlayerSearchAsync(), muted, edge);
        AddLeagueNavigationButton("History", async () => await ShowLeagueHistoryArchiveAsync(), muted, edge);
        AddLeagueNavigationButton("Awards", async () => await ShowLeagueAwardsAsync(), muted, edge);
        AddNavigationButton("SCOUTING  ▾", "Scouting, college football, and draft", ToggleScoutingNavigation, false, green, muted, edge);
        _scoutingNavigationGroup = new VBoxContainer { Visible = false }; _shellNavigation.AddChild(_scoutingNavigationGroup);
        AddScoutingNavigationButton("Scouting Board", ShowDraftBoard, muted, edge);
        AddScoutingNavigationButton("Draft Board", ShowDraftBoard, muted, edge);
        AddScoutingNavigationButton("College Football", ShowCollegeFullRankings, muted, edge);
        AddScoutingNavigationButton("Draft", ShowDraftBoard, muted, edge);
        AddScoutingNavigationButton("UDFA Market", ShowUdfaMarket, muted, edge);
        AddNavigationButton("TRADE CENTER  ▾", "Trades, market planning, free agency, waivers and transactions", ToggleTradeNavigation, false, green, muted, edge);
        _tradeNavigationGroup = new VBoxContainer { Visible = false }; _tradeNavigationGroup.AddThemeConstantOverride("separation", 1); _shellNavigation.AddChild(_tradeNavigationGroup);
        AddTradeNavigationButton("Trades", ShowTradeDialog, muted, edge);
        AddTradeNavigationButton("Trade Block / Finder", ShowTradeFinderDialog, muted, edge);
        AddTradeNavigationButton("Free Agency", ShowFreeAgency, muted, edge);
        AddTradeNavigationButton("Waivers", ShowWaiversDialog, muted, edge);
        AddTradeNavigationButton("League Transactions", ShowLeagueTransactionsDialog, muted, edge);
        var spacer = new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _shellNavigation.AddChild(spacer);
        RefreshWorkstationContext();
    }

    private void ToggleTeamNavigation() => ToggleNavigationGroup(_teamNavigationGroup);

    private void ToggleFinancesNavigation() => ToggleNavigationGroup(_financesNavigationGroup);

    private void ToggleLeagueNavigation() => ToggleNavigationGroup(_leagueNavigationGroup);
    private void ToggleScoutingNavigation() => ToggleNavigationGroup(_scoutingNavigationGroup);
    private void ToggleTradeNavigation() => ToggleNavigationGroup(_tradeNavigationGroup);

    private void ToggleNavigationGroup(Control selectedGroup)
    {
        if (selectedGroup == null) return;
        var shouldOpen = !selectedGroup.Visible;
        foreach (var group in new[] { _teamNavigationGroup, _financesNavigationGroup, _leagueNavigationGroup, _scoutingNavigationGroup, _tradeNavigationGroup })
            if (group != null) group.Visible = false;
        selectedGroup.Visible = shouldOpen;
    }

    private void AddTeamNavigationButton(string title, Action action, Color muted, Color edge)
    {
        if (_teamNavigationGroup == null) return;
        var button = CreateShellButton("   " + title, muted, edge);
        button.CustomMinimumSize = new Vector2(210, 26);
        button.AddThemeFontSizeOverride("font_size", 12);
        button.Pressed += action;
        _teamNavigationGroup.AddChild(button);
    }

    private void AddFinancesNavigationButton(string title, Action action, Color muted, Color edge)
    {
        if (_financesNavigationGroup == null) return;
        var button = CreateShellButton("   " + title, muted, edge); button.CustomMinimumSize = new Vector2(210, 26); button.AddThemeFontSizeOverride("font_size", 12); button.Pressed += action; _financesNavigationGroup.AddChild(button);
    }

    private void AddLeagueNavigationButton(string title, Action action, Color muted, Color edge)
    {
        if (_leagueNavigationGroup == null) return;
        var button = CreateShellButton("   " + title, muted, edge); button.CustomMinimumSize = new Vector2(210, 26); button.AddThemeFontSizeOverride("font_size", 12); button.Pressed += action; _leagueNavigationGroup.AddChild(button);
    }
    private void AddScoutingNavigationButton(string title, Action action, Color muted, Color edge) { if (_scoutingNavigationGroup == null) return; var button = CreateShellButton("   " + title, muted, edge); button.CustomMinimumSize = new Vector2(210, 26); button.AddThemeFontSizeOverride("font_size", 12); button.Pressed += action; _scoutingNavigationGroup.AddChild(button); }
    private void AddTradeNavigationButton(string title, Action action, Color muted, Color edge) { if (_tradeNavigationGroup == null) return; var button = CreateShellButton("   " + title, muted, edge); button.CustomMinimumSize = new Vector2(210, 26); button.AddThemeFontSizeOverride("font_size", 12); button.Pressed += action; _tradeNavigationGroup.AddChild(button); }

    private Button AddNavigationButton(string title, string tooltip, Action action, bool active, Color green, Color muted, Color edge)
    {
        var button = CreateShellButton(title, active ? new Color("f4eddf") : muted, edge);
        button.TooltipText = tooltip;
        button.CustomMinimumSize = new Vector2(210, 34);
        if (active)
            button.AddThemeStyleboxOverride("normal", CreateSurfaceStyle(new Color("193d37"), green, 0, 1));
        button.Pressed += action;
        _shellNavigation.AddChild(button);
        return button;
    }

    private static Control CreateNavigationRule(Color edge)
        => new ColorRect { Color = edge, CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore };

    private static TextureRect CreateTeamLogoTexture(Vector2 minimumSize)
    {
        return new TextureRect
        {
            CustomMinimumSize = minimumSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
    }

    private static string TeamLogoPath(string abbreviation)
    {
        var safe = new string((abbreviation ?? "")
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "" : $"res://Assets/team_logos/{safe}.png";
    }

    private static void SetTeamLogo(TextureRect target, string abbreviation)
    {
        if (target == null)
            return;

        var path = TeamLogoPath(abbreviation);
        var texture = !string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path)
            ? GD.Load<Texture2D>(path)
            : null;
        target.Texture = texture;
        target.Visible = texture != null;
    }

    private static Button CreateShellButton(string text, Color textColor, Color edge)
    {
        var button = new Button { Text = text, Alignment = HorizontalAlignment.Left };
        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", new Color("f4eddf"));
        button.AddThemeStyleboxOverride("normal", CreateSurfaceStyle(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 0, 0));
        button.AddThemeStyleboxOverride("hover", CreateSurfaceStyle(new Color("162c3c"), edge, 0, 1));
        return button;
    }

    private static StyleBoxFlat CreateSurfaceStyle(Color background, Color border, int cornerRadius, int borderWidth)
    {
        var style = new StyleBoxFlat { BgColor = background, BorderColor = border };
        style.BorderWidthLeft = borderWidth;
        style.BorderWidthTop = borderWidth;
        style.BorderWidthRight = borderWidth;
        style.BorderWidthBottom = borderWidth;
        style.CornerRadiusTopLeft = cornerRadius;
        style.CornerRadiusTopRight = cornerRadius;
        style.CornerRadiusBottomLeft = cornerRadius;
        style.CornerRadiusBottomRight = cornerRadius;
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 6;
        style.ContentMarginBottom = 6;
        return style;
    }

    // The project uses a bespoke workstation shell.  Godot's data-entry controls do
    // not inherit Button/Panel overrides, so they need their own treatment rather
    // than falling back to the editor-gray theme in dialogs and tables.
    private static void ApplyDataControlTheme(Control control, Color slate, Color edge, Color ink, Color muted, Color green)
    {
        var field = CreateSurfaceStyle(new Color("091927"), edge, 3, 1);
        var fieldFocus = CreateSurfaceStyle(new Color("102a38"), green, 3, 1);
        var selected = CreateSurfaceStyle(new Color("193d37"), green, 3, 1);

        switch (control)
        {
            case ItemList list:
                list.AddThemeStyleboxOverride("panel", field);
                list.AddThemeStyleboxOverride("selected", selected);
                list.AddThemeStyleboxOverride("selected_focus", selected);
                list.AddThemeColorOverride("font_color", ink);
                list.AddThemeColorOverride("font_selected_color", new Color("f4eddf"));
                break;
            case Tree tree:
                tree.AddThemeStyleboxOverride("panel", field);
                tree.AddThemeStyleboxOverride("selected", selected);
                tree.AddThemeStyleboxOverride("selected_focus", selected);
                tree.AddThemeStyleboxOverride("cursor", fieldFocus);
                tree.AddThemeStyleboxOverride("cursor_unfocused", field);
                tree.AddThemeColorOverride("font_color", ink);
                tree.AddThemeColorOverride("font_selected_color", new Color("f4eddf"));
                tree.AddThemeColorOverride("title_button_color", muted);
                break;
            case LineEdit lineEdit:
                lineEdit.AddThemeStyleboxOverride("normal", field);
                lineEdit.AddThemeStyleboxOverride("focus", fieldFocus);
                lineEdit.AddThemeColorOverride("font_color", ink);
                lineEdit.AddThemeColorOverride("font_placeholder_color", muted);
                lineEdit.AddThemeColorOverride("caret_color", green);
                break;
            case OptionButton optionButton:
                optionButton.AddThemeStyleboxOverride("normal", field);
                optionButton.AddThemeStyleboxOverride("hover", fieldFocus);
                optionButton.AddThemeStyleboxOverride("pressed", fieldFocus);
                optionButton.AddThemeStyleboxOverride("focus", fieldFocus);
                optionButton.AddThemeColorOverride("font_color", ink);
                ApplyPopupMenuTheme(optionButton.GetPopup(), slate, edge, ink, muted, green);
                break;
            case SpinBox spinBox:
                ApplyDataControlTheme(spinBox.GetLineEdit(), slate, edge, ink, muted, green);
                break;
            case TabContainer tabs:
                ApplyTabBarTheme(tabs.GetTabBar(), slate, edge, ink, muted, green);
                break;
        }
    }

    private static void ApplyPopupMenuTheme(PopupMenu menu, Color slate, Color edge, Color ink, Color muted, Color green)
    {
        if (menu == null)
            return;
        menu.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("0d2031"), edge, 3, 1));
        menu.AddThemeStyleboxOverride("hover", CreateSurfaceStyle(new Color("193d37"), green, 3, 1));
        menu.AddThemeColorOverride("font_color", ink);
        menu.AddThemeColorOverride("font_hover_color", new Color("f4eddf"));
        menu.AddThemeColorOverride("font_accelerator_color", muted);
    }

    private static void ApplyTabBarTheme(TabBar tabBar, Color slate, Color edge, Color ink, Color muted, Color green)
    {
        if (tabBar == null)
            return;
        tabBar.AddThemeStyleboxOverride("tab_unselected", CreateSurfaceStyle(new Color("0d2031"), edge, 3, 1));
        tabBar.AddThemeStyleboxOverride("tab_hovered", CreateSurfaceStyle(new Color("162c3c"), edge, 3, 1));
        tabBar.AddThemeStyleboxOverride("tab_selected", CreateSurfaceStyle(new Color("193d37"), green, 3, 1));
        tabBar.AddThemeColorOverride("font_unselected_color", muted);
        tabBar.AddThemeColorOverride("font_hovered_color", ink);
        tabBar.AddThemeColorOverride("font_selected_color", new Color("f4eddf"));
    }

    private static void ApplyWorkstationTheme(Node node, Color slate, Color edge, Color ink, Color muted, Color green)
    {
        if (node is PopupMenu menu)
            ApplyPopupMenuTheme(menu, slate, edge, ink, muted, green);
        if (node is Control control)
            ApplyDataControlTheme(control, slate, edge, ink, muted, green);
        if (node is PanelContainer panel)
            panel.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(slate, edge, 0, 1));
        else if (node is Button button)
        {
            button.AddThemeColorOverride("font_color", ink);
            button.AddThemeStyleboxOverride("normal", CreateSurfaceStyle(new Color("142a39"), edge, 0, 1));
            button.AddThemeStyleboxOverride("hover", CreateSurfaceStyle(new Color("1b3a44"), green, 0, 1));
        }
        else if (node is Label label)
            label.AddThemeColorOverride("font_color", muted);
        else if (node is RichTextLabel richText)
            richText.AddThemeColorOverride("default_color", ink);

        foreach (var child in node.GetChildren())
            ApplyWorkstationTheme(child, slate, edge, ink, muted, green);
    }

    private void MakeOverviewScrollable()
    {
        var overview = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab");
        if (overview == null || _franchiseHome != null) return;
        foreach (var child in overview.GetChildren())
            if (child is CanvasItem item) item.Visible = false;

        var scroll = new ScrollContainer { Name = "FranchiseHomeScroll", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overview.AddChild(scroll);
        _franchiseHome = new VBoxContainer { Name = "FranchiseHome", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _franchiseHome.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_franchiseHome);
        var header = new HBoxContainer();
        var title = HomeLabel("FRANCHISE HOME", 20, new Color("f4eddf")); title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(title);
        var edit = CreateShellButton("EDIT DASHBOARD", new Color("9cadb8"), new Color("254258"));
        edit.Pressed += ToggleDashboardEdit;
        header.AddChild(edit); _franchiseHome.AddChild(header);
        _dashboardEditorBar = new HBoxContainer { Visible = false };
        _dashboardEditorBar.AddThemeConstantOverride("separation", 6);
        _dashboardTilePicker = new OptionButton { TooltipText = "Choose the dashboard module to edit." };
        _dashboardTilePicker.ItemSelected += _ => UpdateDashboardEditor();
        _dashboardEditorBar.AddChild(_dashboardTilePicker);
        AddEditorButton("MOVE ◀", () => MoveSelectedDashboardTile(-1));
        AddEditorButton("MOVE ▶", () => MoveSelectedDashboardTile(1));
        AddEditorButton("RESIZE", ToggleSelectedDashboardTileSize);
        AddEditorButton("REMOVE", RemoveSelectedDashboardTile);
        AddEditorButton("ADD MODULE", RestoreDashboardTile);
        AddEditorButton("RESET", ResetDashboardLayout);
        AddEditorButton("SAVE", SaveDashboardLayout);
        _franchiseHome.AddChild(_dashboardEditorBar);
        _dashboardEditHint = HomeLabel("Edit mode: select a module, then move, resize, remove, or restore it. Changes snap to the 3 × 2 dashboard grid.", 11, new Color("9cadb8"));
        _dashboardEditHint.Visible = false;
        _franchiseHome.AddChild(_dashboardEditHint);
        var savedLayout = new ConfigFile();
        if (savedLayout.Load("user://dashboard_layout.cfg") == Error.Ok)
        {
            _homeConferenceIndex = (int)savedLayout.GetValue("home", "conference", 0);
            _homeProspectPanelIndex = (int)savedLayout.GetValue("home", "prospect_panel", 0);
            var savedOrder = (string)savedLayout.GetValue("home", "tile_order", string.Join(",", _dashboardTileOrder));
            var known = new[] { "standings", "profile", "news", "prospects", "leaders" };
            var order = savedOrder.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(known.Contains).Distinct().ToList();
            if (order.Count == known.Length)
            {
                _dashboardTileOrder.Clear();
                _dashboardTileOrder.AddRange(order);
            }
            var hidden = (string)savedLayout.GetValue("home", "hidden_tiles", "");
            foreach (var tile in hidden.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(known.Contains))
                _dashboardHiddenTiles.Add(tile);
            _dashboardFeaturedTileWide = (bool)savedLayout.GetValue("home", "featured_wide", true);
        }
        BuildFranchiseDashboardGrid();
    }

    private Label HomeLabel(string text, int size = 13, Color? color = null)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? new Color("c5d1d8"));
        return label;
    }

    private PanelContainer CreateHomeTile(string title, Action action, string actionText = "OPEN")
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("0d2031"), new Color("254258"), 0, 1));
        var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 5); panel.AddChild(box);
        var header = new HBoxContainer();
        var label = HomeLabel(title, 13, new Color("f4eddf")); label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; header.AddChild(label);
        var button = CreateShellButton(actionText, new Color("7fbf88"), new Color("254258")); button.AddThemeFontSizeOverride("font_size", 11); button.Pressed += action; header.AddChild(button);
        box.AddChild(header); box.AddChild(CreateNavigationRule(new Color("254258")));
        return panel;
    }

    private static VBoxContainer AddTileBody(PanelContainer panel)
    {
        var box = panel.GetChild<VBoxContainer>(0);
        var body = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 2); box.AddChild(body); return body;
    }

    private void AddEditorButton(string text, Action action)
    {
        var button = CreateShellButton(text, new Color("c5d1d8"), new Color("254258")); button.AddThemeFontSizeOverride("font_size", 11); button.Pressed += action; _dashboardEditorBar.AddChild(button);
    }

    private void BuildFranchiseDashboardGrid()
    {
        if (_franchiseHome == null) return;
        _dashboardTileGrid?.QueueFree();
        _dashboardTileGrid = new VBoxContainer { Name = "DashboardTileGrid", SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _dashboardTileGrid.AddThemeConstantOverride("separation", 8);
        _franchiseHome.AddChild(_dashboardTileGrid);
        _homeStandingsBody = _homeProfileBody = _homeNewsBody = _homeProspectsBody = _homeLeadersBody = null;

        var visible = _dashboardTileOrder.Where(tile => !_dashboardHiddenTiles.Contains(tile)).ToList();
        var top = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        top.AddThemeConstantOverride("separation", 8);
        var bottom = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        bottom.AddThemeConstantOverride("separation", 8);
        foreach (var tile in visible.Take(2))
        {
            var panel = CreateDashboardTile(tile);
            panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            panel.SizeFlagsStretchRatio = _dashboardFeaturedTileWide && tile == visible.FirstOrDefault() ? 2f : 1f;
            top.AddChild(panel);
        }
        foreach (var tile in visible.Skip(2).Take(3))
        {
            var panel = CreateDashboardTile(tile);
            panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            bottom.AddChild(panel);
        }
        if (top.GetChildCount() > 0) _dashboardTileGrid.AddChild(top);
        if (bottom.GetChildCount() > 0) _dashboardTileGrid.AddChild(bottom);
        UpdateDashboardEditor();
        RenderFranchiseHome();
    }

    private PanelContainer CreateDashboardTile(string tile)
    {
        PanelContainer panel;
        VBoxContainer body;
        switch (tile)
        {
            case "standings": panel = CreateHomeTile("CONFERENCE STANDINGS", async () => await OpenFullLeagueStandingsAsync()); body = AddTileBody(panel); _homeStandingsBody = body; break;
            case "profile": panel = CreateHomeTile("GENERAL MANAGER", ShowFranchiseSettings, "VIEW PROFILE"); body = AddTileBody(panel); _homeProfileBody = body; break;
            case "news": panel = CreateHomeTile("TOP LEAGUE NEWS", async () => await SelectMainTab(LEAGUE_TAB_INDEX), "VIEW ALL NEWS"); body = AddTileBody(panel); _homeNewsBody = body; break;
            case "prospects": panel = CreateHomeTile("TOP PROSPECTS / COLLEGE", ShowDraftBoard); body = AddTileBody(panel); _homeProspectsBody = body; break;
            default: panel = CreateHomeTile("TEAM STAT LEADERS", async () => await SelectMainTab(ROSTER_TAB_INDEX)); body = AddTileBody(panel); _homeLeadersBody = body; break;
        }
        return panel;
    }

    private void ToggleDashboardEdit()
    {
        _dashboardEditMode = !_dashboardEditMode;
        if (_dashboardEditorBar != null) _dashboardEditorBar.Visible = _dashboardEditMode;
        if (_dashboardEditHint != null) _dashboardEditHint.Visible = _dashboardEditMode;
        UpdateDashboardEditor();
    }

    private void UpdateDashboardEditor()
    {
        if (_dashboardTilePicker == null) return;
        var selected = _dashboardTilePicker.Selected >= 0 ? _dashboardTilePicker.GetItemText(_dashboardTilePicker.Selected) : "";
        _dashboardTilePicker.Clear();
        foreach (var tile in _dashboardTileOrder)
            _dashboardTilePicker.AddItem($"{TileDisplayName(tile)}{(_dashboardHiddenTiles.Contains(tile) ? " (hidden)" : "")}");
        var index = _dashboardTileOrder.FindIndex(tile => TileDisplayName(tile) == selected || $"{TileDisplayName(tile)} (hidden)" == selected);
        _dashboardTilePicker.Select(index >= 0 ? index : 0);
    }

    private string SelectedDashboardTile() => _dashboardTilePicker == null || _dashboardTilePicker.Selected < 0 ? "standings" : _dashboardTileOrder[_dashboardTilePicker.Selected];
    private static string TileDisplayName(string tile) => tile switch { "standings" => "Conference Standings", "profile" => "GM Profile", "news" => "League News", "prospects" => "Prospects / College", _ => "Team Leaders" };
    private void MoveSelectedDashboardTile(int direction)
    {
        var tile = SelectedDashboardTile(); var index = _dashboardTileOrder.IndexOf(tile); var target = Math.Clamp(index + direction, 0, _dashboardTileOrder.Count - 1);
        if (index == target) return;
        _dashboardTileOrder.RemoveAt(index); _dashboardTileOrder.Insert(target, tile); BuildFranchiseDashboardGrid();
    }
    private void ToggleSelectedDashboardTileSize() { _dashboardFeaturedTileWide = !_dashboardFeaturedTileWide; BuildFranchiseDashboardGrid(); }
    private void RemoveSelectedDashboardTile() { _dashboardHiddenTiles.Add(SelectedDashboardTile()); BuildFranchiseDashboardGrid(); }
    private void RestoreDashboardTile()
    {
        var hidden = _dashboardTileOrder.FirstOrDefault(tile => _dashboardHiddenTiles.Contains(tile));
        if (!string.IsNullOrWhiteSpace(hidden)) _dashboardHiddenTiles.Remove(hidden);
        BuildFranchiseDashboardGrid();
    }
    private void ResetDashboardLayout()
    {
        _dashboardTileOrder.Clear(); _dashboardTileOrder.AddRange(new[] { "standings", "news", "profile", "prospects", "leaders" });
        _dashboardHiddenTiles.Clear(); _dashboardFeaturedTileWide = true; _homeConferenceIndex = 0; _homeProspectPanelIndex = 0; _homeLeaderCategory = 0; BuildFranchiseDashboardGrid();
    }
    private void SaveDashboardLayout()
    {
        var config = new ConfigFile(); config.SetValue("home", "conference", _homeConferenceIndex); config.SetValue("home", "prospect_panel", _homeProspectPanelIndex);
        config.SetValue("home", "tile_order", string.Join(",", _dashboardTileOrder)); config.SetValue("home", "hidden_tiles", string.Join(",", _dashboardHiddenTiles)); config.SetValue("home", "featured_wide", _dashboardFeaturedTileWide); config.Save("user://dashboard_layout.cfg");
    }

    private static void ClearTile(VBoxContainer body) { if (body == null) return; foreach (var child in body.GetChildren()) child.QueueFree(); }

    private void RenderFranchiseHome()
    {
        if (_franchiseHome == null) return;
        var league = _nativeGameCoreContext?.ActiveLeague;
        RenderHomeStandings(league); RenderHomeProfile(league); RenderHomeNews(league); RenderHomeProspects(league); RenderHomeLeaders(league);
    }

    private void RenderHomeStandings(LeagueState league)
    {
        ClearTile(_homeStandingsBody); if (_homeStandingsBody == null) return;
        if (league == null) { _homeStandingsBody.AddChild(HomeLabel("Standings will appear when a franchise is loaded.")); return; }
        var conferences = league.Teams.Select(t => t.Conference).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
        if (conferences.Count == 0) { _homeStandingsBody.AddChild(HomeLabel("No conference data available.")); return; }
        var user = league.Teams.FirstOrDefault(t => t.TeamId == league.UserTeamId); var initial = conferences.FindIndex(c => c == user?.Conference);
        if (_homeConferenceIndex < 0 || _homeConferenceIndex >= conferences.Count) _homeConferenceIndex = initial >= 0 ? initial : 0;
        var conference = conferences[_homeConferenceIndex];
        var standings = _nativeGameCoreContext == null
            ? new List<TeamStanding>()
            : new StandingsService(_nativeGameCoreContext).BuildStandings(league);
        var controls = new HBoxContainer();
        var left = CreateShellButton("‹", new Color("f4eddf"), new Color("254258")); left.Pressed += () => { _homeConferenceIndex = (_homeConferenceIndex + conferences.Count - 1) % conferences.Count; RenderFranchiseHome(); }; controls.AddChild(left);
        var indicator = HomeLabel($"{conference.ToUpperInvariant()}  •  {_homeConferenceIndex + 1} of {conferences.Count}", 12, new Color("7fbf88")); indicator.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; indicator.HorizontalAlignment = HorizontalAlignment.Center; controls.AddChild(indicator);
        var right = CreateShellButton("›", new Color("f4eddf"), new Color("254258")); right.Pressed += () => { _homeConferenceIndex = (_homeConferenceIndex + 1) % conferences.Count; RenderFranchiseHome(); }; controls.AddChild(right); _homeStandingsBody.AddChild(controls);
        var divisionsRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        divisionsRow.AddThemeConstantOverride("separation", 8);
        foreach (var division in standings.Where(t => t.Conference == conference).GroupBy(t => t.Division).OrderBy(g => g.Key))
        {
            var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            column.AddThemeConstantOverride("separation", 0);
            column.AddChild(HomeLabel($"{division.Key.ToUpperInvariant(),-16}  W  L  T   PCT   PF  PA", 10, new Color("9cadb8")));
            foreach (var team in division.OrderByDescending(t => t.WinPct).ThenByDescending(t => t.PointDifferential))
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 4);
                var mark = CreateTeamLogoTexture(new Vector2(18, 18));
                mark.TooltipText = team.TeamName;
                SetTeamLogo(mark, team.Abbreviation);
                row.AddChild(mark);
                var record = $"{team.Wins,2} {team.Losses,2} {team.Ties,2}  {team.WinPct,4:.000}  {team.PointsFor,3} {team.PointsAgainst,3}";
                row.AddChild(HomeLabel($"{team.Abbreviation,-4} {record}", 11, team.TeamId == league.UserTeamId ? new Color("f0c96a") : new Color("d7e0e4")));
                column.AddChild(row);
            }
            divisionsRow.AddChild(column);
        }
        _homeStandingsBody.AddChild(divisionsRow);
    }

    private void RenderHomeProfile(LeagueState league)
    {
        ClearTile(_homeProfileBody); if (_homeProfileBody == null) return;
        var profile = league?.FranchiseMetadata?.GmProfileSnapshot;
        var gm = profile?.Name ?? "User GM"; var team = league?.Teams?.FirstOrDefault(t => t.TeamId == league.UserTeamId);
        var identity = new HBoxContainer();
        identity.AddThemeConstantOverride("separation", 8);
        var mark = CreateTeamLogoTexture(new Vector2(52, 52));
        mark.TooltipText = team?.Name ?? "Franchise logo";
        SetTeamLogo(mark, team?.Abbreviation);
        identity.AddChild(mark);
        var identityText = new VBoxContainer();
        identityText.AddChild(HomeLabel(gm, 17, new Color("f4eddf")));
        identityText.AddChild(HomeLabel(team == null ? "Franchise not selected" : team.Name));
        identity.AddChild(identityText);
        _homeProfileBody.AddChild(identity);
        _homeProfileBody.AddChild(HomeLabel($"TEAM RECORD   {_dashboardTeamRecord}", 12, new Color("7fbf88")));
        if (profile?.Attributes != null)
        {
            _homeProfileBody.AddChild(HomeLabel($"NEG {profile.Attributes.Negotiation}   PLAYER MGMT {profile.Attributes.PlayerManagement}", 11));
            _homeProfileBody.AddChild(HomeLabel($"SCOUT {profile.Attributes.ScoutingJudgment}   LEADERSHIP {profile.Attributes.Leadership}", 11));
        }
        else
            _homeProfileBody.AddChild(HomeLabel("GM attributes unavailable in this save.", 11, new Color("9cadb8")));
    }

    private void RenderHomeNews(LeagueState league)
    {
        ClearTile(_homeNewsBody); if (_homeNewsBody == null) return;
        if (league == null) { _homeNewsBody.AddChild(HomeLabel("League news will appear when a franchise is loaded.")); return; }
        BuildLeagueNewsStories();
        if (_leagueNewsStories.Count == 0) { _homeNewsBody.AddChild(HomeLabel("League desk is quiet. Advance the calendar for new stories.", 14, new Color("f4eddf"))); return; }
        foreach (var story in _leagueNewsStories.Take(5).Select((story, index) => (story, index)))
        {
            _homeNewsBody.AddChild(HomeLabel(story.index == 0 ? story.story.Headline.ToUpperInvariant() : story.story.Headline, story.index == 0 ? 14 : 11, story.index == 0 ? new Color("f4eddf") : new Color("d7e0e4")));
            if (story.index == 0) _homeNewsBody.AddChild(HomeLabel(story.story.Summary, 11, new Color("9cadb8")));
        }
    }

    private void RenderHomeProspects(LeagueState league)
    {
        ClearTile(_homeProspectsBody); if (_homeProspectsBody == null) return;
        var headers = new[] { "NOTABLE PROSPECTS", "ANALYST DRAFT BOARD", "COLLEGE RANKINGS" };
        var nav = new HBoxContainer(); var prev = CreateShellButton("‹", new Color("f4eddf"), new Color("254258")); prev.Pressed += () => { _homeProspectPanelIndex = (_homeProspectPanelIndex + 2) % 3; RenderFranchiseHome(); }; nav.AddChild(prev);
        var caption = HomeLabel($"{headers[_homeProspectPanelIndex]}  •  {_homeProspectPanelIndex + 1} of 3", 11, new Color("7fbf88")); caption.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; caption.HorizontalAlignment = HorizontalAlignment.Center; nav.AddChild(caption);
        var next = CreateShellButton("›", new Color("f4eddf"), new Color("254258")); next.Pressed += () => { _homeProspectPanelIndex = (_homeProspectPanelIndex + 1) % 3; RenderFranchiseHome(); }; nav.AddChild(next); _homeProspectsBody.AddChild(nav);
        if (league == null) { _homeProspectsBody.AddChild(HomeLabel("Scouting data unavailable.")); return; }
        if (_homeProspectPanelIndex == 2)
            foreach (var team in (league.CollegeUniverse?.Teams ?? new List<CollegeTeamState>()).Where(t => t.Ranking > 0).OrderBy(t => t.Ranking).Take(8)) _homeProspectsBody.AddChild(HomeLabel($"#{team.Ranking,-2} {team.Name}  {team.Wins}-{team.Losses}", 12));
        else
        {
            _homeProspectsBody.AddChild(HomeLabel("#   PLAYER                  POS   COLLEGE", 10, new Color("9cadb8")));
            var rank = 0;
            foreach (var prospect in (league.CollegeProspects ?? new List<CollegeProspectState>()).Where(p => string.IsNullOrWhiteSpace(p.DraftedByTeamId)).OrderByDescending(p => p.Potential).ThenByDescending(p => p.Overall).Take(7))
            {
                rank++;
                _homeProspectsBody.AddChild(HomeLabel($"{rank,-3} {prospect.Name,-22} {prospect.Position,-5} {prospect.College}", 11));
            }
        }
    }

    private void RenderHomeLeaders(LeagueState league)
    {
        ClearTile(_homeLeadersBody); if (_homeLeadersBody == null) return;
        var team = league?.Teams?.FirstOrDefault(t => t.TeamId == league.UserTeamId);
        if (team == null) { _homeLeadersBody.AddChild(HomeLabel("Team leaders unavailable.")); return; }
        var players = team.Roster ?? new List<PlayerState>();
        var tabs = new HBoxContainer(); tabs.AddThemeConstantOverride("separation", 4); _homeLeadersBody.AddChild(tabs);
        var categories = new[] { "OFFENSE", "DEFENSE", "SPECIAL TEAMS" };
        for (var index = 0; index < categories.Length; index++)
        {
            var category = index;
            var tab = CreateShellButton(categories[index], index == _homeLeaderCategory ? new Color("f4eddf") : new Color("748792"), new Color("254258"));
            tab.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            if (index == _homeLeaderCategory) tab.AddThemeStyleboxOverride("normal", CreateSurfaceStyle(new Color("193d37"), new Color("4f9b55"), 0, 1));
            tab.Pressed += () => { _homeLeaderCategory = category; RenderHomeLeaders(_nativeGameCoreContext?.ActiveLeague); };
            tabs.AddChild(tab);
        }
        if (_homeLeaderCategory == 0)
        {
            AddLeader("PASS", players.OrderByDescending(p => p.SeasonStats?.PassingYards ?? 0).FirstOrDefault(), p => p.SeasonStats?.PassingYards ?? 0, "YDS");
            AddLeader("RUSH", players.OrderByDescending(p => p.SeasonStats?.RushingYards ?? 0).FirstOrDefault(), p => p.SeasonStats?.RushingYards ?? 0, "YDS");
            AddLeader("REC", players.OrderByDescending(p => p.SeasonStats?.ReceivingYards ?? 0).FirstOrDefault(), p => p.SeasonStats?.ReceivingYards ?? 0, "YDS");
            AddLeader("PASS TD", players.OrderByDescending(p => p.SeasonStats?.PassingTouchdowns ?? 0).FirstOrDefault(), p => p.SeasonStats?.PassingTouchdowns ?? 0, "TD");
        }
        else if (_homeLeaderCategory == 1)
        {
            AddLeader("TACKLES", players.OrderByDescending(p => p.SeasonStats?.Tackles ?? 0).FirstOrDefault(), p => p.SeasonStats?.Tackles ?? 0, "TKL");
            AddLeader("SACKS", players.OrderByDescending(p => p.SeasonStats?.Sacks ?? 0).FirstOrDefault(), p => p.SeasonStats?.Sacks ?? 0, "SK");
            AddLeader("INTERCEPTIONS", players.OrderByDescending(p => p.SeasonStats?.Interceptions ?? 0).FirstOrDefault(), p => p.SeasonStats?.Interceptions ?? 0, "INT");
        }
        else
            _homeLeadersBody.AddChild(HomeLabel("Special-teams statistics are not tracked by the current simulation model.", 11, new Color("9cadb8")));
    }

    private void AddLeader(string category, PlayerState player, Func<PlayerState, int> value, string unit)
    { if (_homeLeadersBody == null) return; _homeLeadersBody.AddChild(HomeLabel(player == null ? $"{category,-13} —" : $"{category,-13} {player.Name} ({player.Position})   {value(player):N0} {unit}", 11)); }

    private void RefreshWorkstationContext()
    {
        var activeTeam = _nativeGameCoreContext?.ActiveLeague?.Teams?
            .FirstOrDefault(team => string.Equals(team.TeamId, _nativeGameCoreContext.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var abbreviation = !string.IsNullOrWhiteSpace(_dashboardTeamAbbreviation)
            ? _dashboardTeamAbbreviation
            : activeTeam?.Abbreviation ?? "";
        SetTeamLogo(_shellTeamLogo, abbreviation);
        SetTeamLogo(_railTeamLogo, abbreviation);
        if (_shellCalendar != null)
            _shellCalendar.Text = string.IsNullOrWhiteSpace(_calendarText?.Text) ? "LEAGUE CONTEXT LOADING" : _calendarText.Text.ToUpperInvariant();
        if (_shellTeam != null)
            _shellTeam.Text = !string.IsNullOrWhiteSpace(_dashboardTeamName) ? _dashboardTeamName.ToUpperInvariant() : string.IsNullOrWhiteSpace(_lblUserTeam?.Text) ? "FRANCHISE" : _lblUserTeam.Text.ToUpperInvariant();
        if (_shellTeamContext != null)
        {
            var conference = activeTeam?.Conference ?? "";
            var division = activeTeam?.Division ?? "";
            var context = string.Join("  •  ", new[] { conference, division }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
            _shellTeamContext.Text = !string.IsNullOrWhiteSpace(context)
                ? context.ToUpperInvariant()
                : "CONTROLLED TEAM";
        }
        if (_shellRecord != null)
            _shellRecord.Text = _dashboardTeamRecord ?? "0-0";
        if (_shellInboxBadge != null)
        {
            var count = _inboxMessages?.Count ?? 0;
            _shellInboxBadge.Text = count > 0 ? $"INBOX  {count}!" : "INBOX  0";
            _shellInboxBadge.AddThemeColorOverride("font_color", count > 0 ? new Color("f0c96a") : new Color("d7e0e4"));
        }
        if (_shellAdvance != null && _calendarText != null)
            _shellAdvance.Text = _inboxMessages != null && _inboxMessages.Count > 0 ? "REVIEW INBOX" : "ADVANCE";
    }

    private void ApplyLifecyclePresentation()
    {
        var slate = new Color("101f2d");
        var edge = new Color("294559");
        var ink = new Color("f4eddf");
        var muted = new Color("aeb9bd");
        var green = new Color("4f9b55");
        var gold = new Color("c99a45");

        foreach (var path in new[]
        {
            "StartupPanel/CenterWrap/Panel",
            "GameDayPopup/CenterWrap/Panel",
            "PostGameRecapPopup/CenterWrap/Panel",
            "BoxScorePopup/CenterWrap/Panel",
        })
        {
            var panel = GetNodeOrNull<PanelContainer>(path);
            if (panel != null)
                ApplyWorkstationTheme(panel, slate, edge, ink, muted, green);
        }

        foreach (var path in new[]
        {
            "StartupPanel/CenterWrap/Panel/Margin/Content/Title",
            "GameDayPopup/CenterWrap/Panel/Margin/Content/LblGameDayTitle",
            "PostGameRecapPopup/CenterWrap/Panel/Margin/Content/LblPostGameTitle",
        })
        {
            var title = GetNodeOrNull<Label>(path);
            if (title != null)
                title.AddThemeColorOverride("font_color", ink);
        }

        foreach (var path in new[]
        {
            "StartupPanel/CenterWrap/Panel/Margin/Content/StartupButtonRow/BtnStartupContinue",
            "StartupPanel/CenterWrap/Panel/Margin/Content/StartupButtonRow/BtnStartupNewGame",
            "GameDayPopup/CenterWrap/Panel/Margin/Content/ButtonRow/BtnGameDaySim",
            "PostGameRecapPopup/CenterWrap/Panel/Margin/Content/ButtonRow/BtnPostGameBoxScore",
        })
        {
            var button = GetNodeOrNull<Button>(path);
            if (button == null)
                continue;
            button.AddThemeStyleboxOverride("normal", CreateSurfaceStyle(new Color("193d37"), green, 0, 1));
            button.AddThemeStyleboxOverride("hover", CreateSurfaceStyle(new Color("245142"), green, 0, 1));
        }

        var warning = GetNodeOrNull<Label>("StartupPanel/CenterWrap/Panel/Margin/Content/LblStartupWarning");
        if (warning != null)
            warning.AddThemeColorOverride("font_color", gold);
    }

    public override async void _Ready()
    {
        if (await TryRunDeveloperCommand())
            return;

        var window = GetWindow();
        if (window != null)
            window.MinSize = new Vector2I(1152, 648);

        // Existing nodes
        _serverStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/HeaderPanel/HeaderRow/ContinueBlock/ServerStatus");
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
        _continueStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/HeaderPanel/HeaderRow/ContinueBlock/ContinueStatus");
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
        _btnToggleDebug = GetNodeOrWarn<CheckButton>("AppMargin/MainPadding/MainLayout/ActionButtonRow/BtnToggleDebug");
        _btnRefresh = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnRefresh");
        _btnAdvanceDay = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnAdvanceDay");
        _btnNewGame = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnNewGame");
        _btnResetSave = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnResetSave");
        _btnSaveNativeGame = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnSaveNativeGame");
        _btnLoadNativeGame = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnLoadNativeGame");
        _btnRunGameCoreSmokeTest = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/DebugPanel/DebugToolsRow/BtnRunGameCoreSmokeTest");
        _btnColumns = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/ActionButtonRow/BtnColumns", "BtnColumns not found; skipping columns menu binding.");
        _popupColumns = GetNodeOrWarn<PopupMenu>("AppMargin/MainPadding/MainLayout/MainTabs/OverviewTab/PopupColumns");
        _rosterPane = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/RosterPane");
        _playerReportPanel = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/PlayerReportPanel");
        _squadWorkspaceHeader = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/SquadWorkspaceHeader");
        _lblPlayerHeader = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/PlayerReportPanel/LblPlayerHeader");
        _lblRosterEvaluation = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/PlayerReportPanel/LblRosterEvaluation");
        _rtlPlayerStats = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/PlayerReportPanel/RtlPlayerStats");
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
        if (_newGameTeamPicker != null)
            _newGameTeamPicker.MinSize = new Vector2I(640, 480);
        if (_teamPickList != null)
            _teamPickList.CustomMinimumSize = new Vector2(560, 300);
        CreateFranchiseSetupDialog();
        CreateFreeAgencyDialog();
        CreateFreeAgencyButton();
        CreateTradeControls();
        CreateTradeFinderControls();
        CreateWaiversControls();
        CreateLeagueTransactionsControls();
        CreateRosterContractControls();
        RehomeRosterActions();
        CreateRosterManagementControls();
        CreateMarketDeskDialog();
        CreateInboxDeskDialog();
        CreateFranchiseSettingsDialog();
        CreateTrainingCampControls();
        CreateDraftBoard();
        CreateUdfaMarket();

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
        ConfigureRosterWorkspacePresentation();
        _depthChartPanel = GetNodeOrWarn<Control>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel");
        _depthChartSummary = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartSummary");
        _btnAutoFillDepthChart = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartActionRow/BtnAutoFillDepthChart");
        _btnDepthChartSetStarter = GetNodeOrWarn<Button>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartActionRow/BtnDepthChartSetStarter");
        _depthChartActionStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartActionRow/DepthChartActionStatus");
        _depthChartSelectionStatus = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartSelectionStatus");
        _depthChartTree = GetNodeOrWarn<Tree>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartTree");
        ConfigureDepthChartWorkspacePresentation();
        CreateDevelopmentWorkspace();
        CreateInjuriesWorkspace();
        CreateStaffWorkspace();
        CreateStaffDetailProfileDialog();
        CreateTeamHistoryWorkspace();
        CreateTeamSeasonRecapDialog();
        CreateTeamStandingsWorkspace();
        CreateTeamStatsWorkspace();
        CreateTeamFinancesWorkspace();
        CreateContractsWorkspace();
        CreateAccountingWorkspace();
        CreatePracticeSquadWorkspace();
        CreateLeagueStatsWorkspace();
        CreateLeagueScheduleWorkspace();
        CreateLeagueNewsWorkspace();
        CreateLeaguePlayerSearchWorkspace();
        CreateLeagueHistoryArchiveWorkspace();
        CreateLeagueAwardsWorkspace();
        CreateCollegeRankingsWorkspace();
        CreateCollegeLeadersWorkspace();
        CreateCollegePostseasonProjectionsWorkspace();
        CreateCollegeBigBoardsWorkspace();
        CreateCollegeAwardsWorkspace();
        CreateCollegeNewsWorkspace();
        _standingsTree = GetNodeOrWarn<Tree>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/StandingsTab/StandingsTree");
        _standingsTree.ItemActivated += () => _ = OpenSelectedLeagueStandingsTeam();
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
        _leagueContextSummary = GetNodeOrWarn<Label>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueContextSummary");
        _scheduleInspector = GetNodeOrWarn<RichTextLabel>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel/LeagueHubTabs/ScheduleTab/ScheduleInspector");
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
            _btnInbox.Pressed += ShowInboxDesk;
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
        if (_btnDepthChartSetStarter != null)
            _btnDepthChartSetStarter.Pressed += async () => await UpdateDepthChart("set_starter");
        if (_depthChartTree is DepthChartTree draggableDepthChart)
            draggableDepthChart.PlayerDropped += (position, playerId, targetPlayerId, insertAfter) => _ = ReorderDepthChartByDrop(position, playerId, targetPlayerId, insertAfter);
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
        if (_franchiseSetupDialog != null)
            _franchiseSetupDialog.Confirmed += async () => await CreateConfiguredNativeFranchise();

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
            _rosterTree.ItemActivated += () => OnRosterItemSelected(_rosterTree.GetSelected());
        }
        if (_depthChartTree != null)
            _depthChartTree.ItemSelected += () => OnDepthChartItemSelected(_depthChartTree.GetSelected());
        if (_rosterSplit != null)
            _rosterSplit.Dragged += OnRosterSplitDragged;
        if (_rosterSearch != null)
            _rosterSearch.TextChanged += OnRosterSearchTextChanged;
        if (_posFilter != null)
            _posFilter.ItemSelected += OnPosFilterItemSelected;
        if (_rosterStatusFilter != null)
            _rosterStatusFilter.ItemSelected += OnRosterStatusFilterItemSelected;
        if (_btnClearFilters != null)
            _btnClearFilters.Pressed += OnClearFiltersPressed;
        if (_overviewActionButton != null)
            _overviewActionButton.Pressed += async () => await OnInboxPrimaryActionPressed();
        if (_btnGameDayCancel != null)
            _btnGameDayCancel.Pressed += CloseGameDayPopup;
        if (_btnGameDayWatch != null)
            _btnGameDayWatch.Pressed += async () => await OnWatchGamePressed();
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

        MakeOverviewScrollable();
        CreateWorkstationShell();
        ApplyLifecyclePresentation();
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
        ConfigureLiveGameObserver();
        ConfigurePostGameHub();
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

    private static bool ValidateTeamLogoAssets(out string error)
    {
        error = "";
        var expected = new[]
        {
            "ATL", "BAL", "BOS", "BUF", "CHA", "CHI", "CIN", "CLE", "DAL", "DEN", "DET", "GB", "HOU", "IND", "KC", "LA",
            "LV", "MIA", "MIN", "NOR", "NY", "ORL", "PHI", "PHX", "PIT", "POR", "SD", "SEA", "SF", "TB", "TEN", "WAS",
        };
        foreach (var abbreviation in expected)
        {
            var path = TeamLogoPath(abbreviation);
            if (!ResourceLoader.Exists(path))
            {
                error = $"Missing installed team logo: {path}";
                return false;
            }
            var texture = ResourceLoader.Load<Texture2D>(path);
            if (texture == null || texture.GetWidth() <= 0 || texture.GetHeight() <= 0)
            {
                error = $"Unable to load team logo texture: {path}";
                return false;
            }
        }
        return true;
    }

    private async Task EnsureNativeGameCoreAndRefresh()
    {
        var loadedNativeState = await EnsureNativeStartupState();
        if (!loadedNativeState)
            return;

        await RefreshAll();
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



    private void UpdateNativeSourceStatus()
    {
        var diagnostics = SimulationDiagnosticsService.AnalyzeRegularSeason(_nativeGameCoreContext?.ActiveLeague);
        var summary = diagnostics.CompletedRegularSeasonGames == 0
            ? "regular-season diagnostics pending"
            : $"{diagnostics.CompletedRegularSeasonGames} games · {diagnostics.PointsPerTeamGame:0.0} pts/team/game · {diagnostics.HomeWinRate:P0} home wins · max margin {diagnostics.LargestScoreMargin}";
        SetDebugOutputStatus($"Runtime: C# GameCore | Balance: {summary}");
        SetStateDumpText($"DEVELOPER-ONLY BALANCE DIAGNOSTICS\n{summary}\nDerived from persisted completed regular-season results; read-only and not a tuning recommendation.");
        UpdateNativeSaveLoadButtons();
    }

    private void UpdateNativeSaveLoadButtons()
    {
        var hasActiveNativeLeague = _nativeGameCoreContext?.ActiveLeague != null;
        if (_btnSaveNativeGame != null)
            _btnSaveNativeGame.Disabled = false;
        if (_btnLoadNativeGame != null)
            _btnLoadNativeGame.Disabled = false;
        if (_btnSaveGame != null)
            _btnSaveGame.Disabled = !hasActiveNativeLeague;
    }

    private async Task<bool> EnsureNativeStartupState()
    {
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
            _btnStartupContinue.Text = corruptAutosave ? "RETRY AUTOSAVE" : "CONTINUE FRANCHISE";
        }

        if (_btnStartupLoadGame != null)
        {
            _btnStartupLoadGame.Disabled = !hasAnySave;
            _btnStartupLoadGame.Text = hasAnySave ? "LOAD SAVE" : "LOAD SAVE (NONE FOUND)";
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







    private static string InlineMessage(string message, int maxLength = 240)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "";
        var normalized = message.Replace("\r", " ").Replace("\n", " ").Trim();
        if (normalized.Length <= maxLength)
            return normalized;
        return normalized.Substring(0, maxLength) + "...";
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

        await Task.CompletedTask;
        return RefreshNativeDashboardState();
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

    // Refresh the header and workspaces from the native dashboard projection.
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
        var summary = BuildNativeStateSummaryDictionary();
        ApplyStateSummary(summary);
        RenderFrontOfficeLabel();
        await RefreshDashboardState();
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
        _dashboardTeamAbbreviation = teamLabel;
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
        RenderFranchiseHome();
        RenderPlayoffPicture(ComposeOverviewPlayoffSummary(playoffSummary, seasonCompletionSummary), _dashboardPlayoffBracket);
        _inboxMessages = ConvertDashboardActionItems(actionItems);
        UpdateInboxList();
        UpdateContinueButtonAvailability();
        RefreshWorkstationContext();
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
        _dashboardTeamAbbreviation = "";
        _dashboardTeamRecord = "0-0";
        _dashboardRosterSize = null;
        _dashboardInjuryCount = null;
        _dashboardCapRoom = "N/A";
        _dashboardTeam = new Godot.Collections.Dictionary();
        RefreshWorkstationContext();
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
        RenderFranchiseHome();
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
        var franchise = _nativeGameCoreContext?.ActiveLeague?.FranchiseMetadata;
        var gmName = string.IsNullOrWhiteSpace(franchise?.GmProfileSnapshot?.Name) ? "User GM" : franchise.GmProfileSnapshot.Name;
        var world = franchise?.World;
        var worldText = world == null
            ? "Standard roster"
            : $"{world.Source} roster (seed {world.Seed})";

        _rtlTeamSummary.Text =
            $"{displayName}\n" +
            $"Record: {record}\n" +
            $"Cap Room: {_dashboardCapRoom}\n" +
            $"Roster: {rosterText}   Injuries: {injuryText}\n" +
            $"GM: {gmName}\n" +
            $"World: {worldText}";
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
        UpdateShellNavigation(activeTab);
    }

    private void UpdateShellNavigation(int activeTab)
    {
        foreach (var item in _shellPrimaryNavigation)
        {
            var isActive = item.Key == activeTab;
            item.Value.AddThemeColorOverride("font_color", isActive ? new Color("f4eddf") : new Color("9cadb8"));
            item.Value.AddThemeStyleboxOverride("normal", isActive
                ? CreateSurfaceStyle(new Color("193d37"), new Color("4f9b55"), 0, 1)
                : CreateSurfaceStyle(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 0, 0));
        }
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
        RenderFrontOfficeLabel();
        await Task.CompletedTask;
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
        _dashboardTeamAbbreviation = "";
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
        if (index < 0 || index >= _teams.Count) return;
        var team = (Godot.Collections.Dictionary)_teams[index];
        if (!team.ContainsKey("id")) return;
        var version = ++_teamSelectionVersion;
        var teamId = team["id"].ToString();
        _currentTeamId = teamId;
        var schedule = RefreshScheduleAsync(teamId, version);
        var injuries = RefreshInjuryReportAsync(teamId, version);
        try { await RefreshRosterTab(); }
        finally { await Task.WhenAll(schedule, injuries); }
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
        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeContinueService.Continue(1);
            if (response?.Ok != true)
            {
                SetPrimaryStatus(response?.Error ?? "Advance day failed.");
                return;
            }
            ApplyNativeContinueStatus(response.Result);
            if (response.Result?.Advanced == true)
                await SaveNativeAutosave("Native autosave updated.");
            await RefreshStateSummary();
            await RefreshLeagueHub();
        }
        finally
        {
            _btnAdvanceDay.Disabled = false;
        }
    }

    private void CreateDraftBoard()
    {
        var actionRow = GetNodeOrNull<Container>("AppMargin/MainPadding/MainLayout/ActionButtonRow");
        if (actionRow == null)
            return;
        _btnDraftBoard = new Button { Text = "Draft Board" };
        actionRow.AddChild(_btnDraftBoard);
        _btnDraftBoard.Pressed += ShowDraftBoard;
        _draftBoardDialog = new AcceptDialog { Title = "Scouting & Draft Board", MinSize = new Vector2I(1040, 650), Exclusive = false };
        AddChild(_draftBoardDialog);
        _draftBoardDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _draftBoardDialog.AddChild(content);
        var title = new Label { Text = "SCOUTING & DRAFT BOARD" };
        title.AddThemeFontSizeOverride("font_size", 20);
        content.AddChild(title);
        content.AddChild(new Label { Text = "Public combine and pro-day data are facts. Scouting ranges, traits, interviews, and reports are estimates, never hidden ratings." });
        _draftStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_draftStatus);
        _draftPickContext = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_draftPickContext);
        _draftOwnedPicks = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_draftOwnedPicks);
        _btnStartDraft = new Button { Text = "START DRAFT", TooltipText = "Open the live Draft Stage and advance CPU selections until your team is on the clock.", CustomMinimumSize = new Vector2(0, 38) };
        _btnStartDraft.Pressed += async () => await StartDraftFromWarRoom();
        content.AddChild(_btnStartDraft);
        var filters = new HBoxContainer();
        filters.AddChild(new Label { Text = "FILTER" });
        _draftSearch = new LineEdit { PlaceholderText = "Search prospect or college", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        filters.AddChild(_draftSearch);
        _draftPositionFilter = new OptionButton { CustomMinimumSize = new Vector2(110, 0) };
        filters.AddChild(_draftPositionFilter);
        content.AddChild(filters);
        _draftSearch.TextChanged += _ => RefreshDraftProspectList();
        _draftPositionFilter.ItemSelected += _ => RefreshDraftProspectList();
        var body = new HSplitContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SplitOffset = 520 };
        content.AddChild(body);
        var board = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        board.AddChild(new Label { Text = "PROSPECT BOARD" });
        _draftProspectList = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill, AllowReselect = true };
        board.AddChild(_draftProspectList);
        _draftProspectList.ItemSelected += index => { _selectedDraftProspectId = _draftProspectList.GetItemMetadata((int)index).ToString(); UpdateDraftProspectDetail(); UpdateDraftStatus(); };
        board.AddChild(new Label { Text = "DRAFT ORDER" });
        _draftOrderList = new ItemList { CustomMinimumSize = new Vector2(0, 130) };
        board.AddChild(_draftOrderList);
        board.AddChild(new Label { Text = "RECENT PICKS / LEAGUE WIRE" });
        _draftRecentPicks = new ItemList { CustomMinimumSize = new Vector2(0, 90) };
        board.AddChild(_draftRecentPicks);
        body.AddChild(board);
        var inspector = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        inspector.AddChild(new Label { Text = "TEAM DRAFT BOARD · MAX 100" });
        var boardFilters = new HBoxContainer(); inspector.AddChild(boardFilters);
        _teamDraftBoardPositionFilter = new OptionButton { CustomMinimumSize = new Vector2(130, 0) }; _teamDraftBoardPositionFilter.AddItem("All positions"); _teamDraftBoardPositionFilter.ItemSelected += _ => RefreshTeamDraftBoard(_nativeGameCoreContext?.ActiveLeague); boardFilters.AddChild(_teamDraftBoardPositionFilter);
        _teamDraftBoardConfidenceFilter = new OptionButton { CustomMinimumSize = new Vector2(140, 0) }; foreach (var label in new[] { "All confidence", "High", "Medium", "Low" }) _teamDraftBoardConfidenceFilter.AddItem(label); _teamDraftBoardConfidenceFilter.ItemSelected += _ => RefreshTeamDraftBoard(_nativeGameCoreContext?.ActiveLeague); boardFilters.AddChild(_teamDraftBoardConfidenceFilter);
        _teamDraftBoardFilterSummary = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; boardFilters.AddChild(_teamDraftBoardFilterSummary);
        _teamDraftBoardList = new TeamDraftBoardList { CustomMinimumSize = new Vector2(0, 130), AllowReselect = true };
        _teamDraftBoardList.ItemSelected += SelectTeamDraftBoardProspect;
        ((TeamDraftBoardList)_teamDraftBoardList).ProspectDropped += MoveTeamDraftBoardProspect;
        inspector.AddChild(_teamDraftBoardList);
        var boardActions = new HBoxContainer(); inspector.AddChild(boardActions);
        var addBoard = new Button { Text = "ADD / REMOVE" }; addBoard.Pressed += async () => await ToggleSelectedTeamDraftBoardProspect(); boardActions.AddChild(addBoard);
        boardActions.AddChild(new Label { Text = "Drag prospects to rank them" });
        var boardContext = new HBoxContainer(); inspector.AddChild(boardContext);
        _teamDraftBoardTag = new OptionButton { CustomMinimumSize = new Vector2(105, 0) }; _teamDraftBoardTag.AddItem("No tag"); _teamDraftBoardTag.AddItem("Target"); _teamDraftBoardTag.AddItem("Avoid"); boardContext.AddChild(_teamDraftBoardTag);
        _teamDraftBoardTier = new LineEdit { PlaceholderText = "Tier (e.g. Day One)", MaxLength = 32, CustomMinimumSize = new Vector2(150, 0) }; boardContext.AddChild(_teamDraftBoardTier);
        _teamDraftBoardNote = new LineEdit { PlaceholderText = "Private note (240 characters)", MaxLength = 240, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; boardContext.AddChild(_teamDraftBoardNote);
        var saveContext = new Button { Text = "SAVE CONTEXT" }; saveContext.Pressed += async () => await SaveSelectedTeamDraftBoardContext(); boardContext.AddChild(saveContext);
        inspector.AddChild(new Label { Text = "PROSPECT INSPECTOR" });
        _draftProspectDetail = new RichTextLabel { BbcodeEnabled = false, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        inspector.AddChild(_draftProspectDetail);
        var draftActions = new HBoxContainer(); inspector.AddChild(draftActions);
        _btnMakeDraftPick = new Button { Text = "MAKE DRAFT PICK", Disabled = true, CustomMinimumSize = new Vector2(0, 34), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        draftActions.AddChild(_btnMakeDraftPick);
        _btnMakeDraftPick.Pressed += MakeDraftPick;
        _btnTradeCurrentDraftPick = new Button { Text = "SHOP CURRENT PICK", Disabled = true, Visible = false, CustomMinimumSize = new Vector2(0, 34), TooltipText = "Open the existing Trade Block / Finder with the current owned pick selected. No offer is generated until you submit it." };
        _btnTradeCurrentDraftPick.Pressed += OpenCurrentDraftPickTradeMarket;
        draftActions.AddChild(_btnTradeCurrentDraftPick);
        body.AddChild(inspector);
        CreateDraftAnnouncementDialog();
    }

    private void CreateDraftAnnouncementDialog()
    {
        _draftAnnouncementDialog = new AcceptDialog { Title = "Draft Selection", MinSize = new Vector2I(620, 420), Exclusive = true };
        AddChild(_draftAnnouncementDialog);
        _draftAnnouncementDialog.GetOkButton().Text = "NEXT PICK";
        _draftAnnouncementDialog.Confirmed += ShowNextDraftAnnouncement;
        _draftAnnouncementDialog.Canceled += () => _pendingDraftAnnouncements.Clear();
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _draftAnnouncementDialog.AddChild(content);
        _draftAnnouncementKicker = HomeLabel("DRAFT SELECTION", 13, new Color("f0c96a")); content.AddChild(_draftAnnouncementKicker);
        var identity = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; content.AddChild(identity);
        _draftAnnouncementLogo = CreateTeamLogoTexture(new Vector2(112, 112)); identity.AddChild(_draftAnnouncementLogo);
        _draftAnnouncementHeadline = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _draftAnnouncementHeadline.AddThemeFontSizeOverride("font_size", 24); identity.AddChild(_draftAnnouncementHeadline);
        _draftAnnouncementBody = new RichTextLabel { BbcodeEnabled = false, CustomMinimumSize = new Vector2(0, 165), SizeFlagsVertical = Control.SizeFlags.ExpandFill }; content.AddChild(_draftAnnouncementBody);
        var controls = new HBoxContainer(); content.AddChild(controls);
        _draftShortAnnouncements = new CheckBox { Text = "Use short announcements for subsequent picks", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _draftShortAnnouncements.Toggled += value => { if (!_syncingDraftAnnouncementPreference) _ = SaveDraftAnnouncementPreference(value); }; controls.AddChild(_draftShortAnnouncements);
        var skipRemaining = new Button { Text = "SKIP REMAINING" }; skipRemaining.Pressed += () => { _pendingDraftAnnouncements.Clear(); _draftAnnouncementDialog.Hide(); RefreshDraftBoard(); }; controls.AddChild(skipRemaining);
        ApplyWorkstationTheme(_draftAnnouncementDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private async Task SaveDraftAnnouncementPreference(bool useShortAnnouncements)
    {
        if (_nativeGameCoreContext?.ActiveLeague?.Draft == null)
            return;
        _nativeGameCoreContext.ActiveLeague.Draft.UseShortDraftAnnouncements = useShortAnnouncements;
        await SaveNativeAutosave("Draft announcement preference saved.");
    }

    private void QueueDraftAnnouncements(IEnumerable<DraftClassRecapEntry> entries)
    {
        foreach (var entry in entries ?? Enumerable.Empty<DraftClassRecapEntry>())
            if (entry != null) _pendingDraftAnnouncements.Enqueue(entry);
        if (_pendingDraftAnnouncements.Count > 0 && _draftAnnouncementDialog?.Visible != true)
            ShowNextDraftAnnouncement();
    }

    private void ShowNextDraftAnnouncement()
    {
        if (_pendingDraftAnnouncements.Count == 0)
        {
            _draftAnnouncementDialog.Hide();
            RefreshDraftBoard();
            return;
        }
        var entry = _pendingDraftAnnouncements.Dequeue();
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, entry.TeamId, StringComparison.OrdinalIgnoreCase));
        var shortMode = league?.Draft?.UseShortDraftAnnouncements == true || entry.OverallPick > 10;
        _syncingDraftAnnouncementPreference = true;
        _draftShortAnnouncements.ButtonPressed = league?.Draft?.UseShortDraftAnnouncements == true;
        _syncingDraftAnnouncementPreference = false;
        _draftAnnouncementKicker.Text = shortMode ? "QUICK PICK ANNOUNCEMENT" : "FEATURED TOP-TEN SELECTION";
        _draftAnnouncementHeadline.Text = $"WITH PICK #{entry.OverallPick}\n{team?.Name?.ToUpperInvariant() ?? entry.TeamName.ToUpperInvariant()} SELECT\n{entry.Name.ToUpperInvariant()} · {entry.Position}";
        var publicRank = entry.PublicBoardRank > 0 ? $"Public Analyst Board rank: #{entry.PublicBoardRank}" : "Public Analyst Board rank: outside the published top 50";
        _draftAnnouncementBody.Text = shortMode
            ? $"{entry.College}\nRound {entry.Round}, pick {entry.PickInRound}."
            : $"{entry.College} · Age {entry.Age}\nRound {entry.Round}, pick {entry.PickInRound}\n{publicRank}\nPublic combine {entry.CombineScore}/100 · pro day {entry.ProDayScore}/100\n\n{(string.IsNullOrWhiteSpace(entry.PublicReaction) ? "No notable public-board reaction accompanied this selection." : entry.PublicReaction)}";
        _draftAnnouncementLogo.Texture = LoadTeamLogo(team?.Abbreviation ?? "");
        _draftAnnouncementDialog.GetOkButton().Text = _pendingDraftAnnouncements.Count > 0 ? "NEXT PICK" : "RETURN TO DRAFT";
        _draftAnnouncementDialog.PopupCentered(new Vector2I(620, 420));
    }

    private void ShowDraftBoard() { RefreshDraftBoard(); _draftBoardDialog.PopupCentered(new Vector2I(1040, 650)); }
    private void RefreshDraftBoard()
    {
        EnsureNativeGameCoreServices();
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league == null) { _draftStatus.Text = "Start or load a franchise to view the draft board."; return; }
        var draft = new DraftService(_nativeGameCoreContext);
        draft.PrepareDraftBoard();
        var liveDraft = string.Equals(league.Calendar?.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase);
        var preDraft = string.Equals(league.Calendar?.Phase, ScheduleService.DraftPrepPendingPhase, StringComparison.OrdinalIgnoreCase);
        _btnStartDraft.Visible = preDraft;
        _btnStartDraft.Disabled = !preDraft;
        if (liveDraft)
            draft.AdvanceCpuPicksUntilUserTurn();
        RefreshDraftPositionFilter(league);
        _selectedDraftProspectId = "";
        RefreshDraftProspectList();
        RefreshTeamDraftBoard(league);
        RefreshDraftOrder(league, draft.GetCurrentPick());
        RefreshOwnedDraftPicks(league);
        RefreshDraftRecentPicks(league);
        _draftProspectDetail.Text = "Select a prospect to review public facts and scouting estimates. Hidden player ratings are never shown here.";
        UpdateDraftStatus();
    }

    private void RefreshDraftPositionFilter(LeagueState league)
    {
        var selected = _draftPositionFilter.Selected >= 0 ? _draftPositionFilter.GetItemText(_draftPositionFilter.Selected) : "All positions";
        _draftPositionFilter.Clear();
        _draftPositionFilter.AddItem("All positions");
        foreach (var position in league.CollegeProspects.Where(prospect => prospect != null).Select(prospect => prospect.Position).Where(position => !string.IsNullOrWhiteSpace(position)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(position => position, StringComparer.OrdinalIgnoreCase))
            _draftPositionFilter.AddItem(position);
        var index = Enumerable.Range(0, _draftPositionFilter.ItemCount).FirstOrDefault(item => string.Equals(_draftPositionFilter.GetItemText(item), selected, StringComparison.OrdinalIgnoreCase));
        _draftPositionFilter.Select(index);
    }

    private void RefreshDraftProspectList()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league == null || _draftProspectList == null || _draftPositionFilter == null)
            return;
        var search = _draftSearch?.Text?.Trim() ?? "";
        var position = _draftPositionFilter.GetItemText(Math.Max(0, _draftPositionFilter.Selected));
        _draftProspectList.Clear();
        var evaluations = new ProspectEvaluationService(_nativeGameCoreContext);
        foreach (var prospect in league.CollegeProspects.Where(prospect => prospect != null && string.IsNullOrWhiteSpace(prospect.DraftedByTeamId))
                     .Where(prospect => string.Equals(position, "All positions", StringComparison.OrdinalIgnoreCase) || string.Equals(prospect.Position, position, StringComparison.OrdinalIgnoreCase))
                     .Where(prospect => string.IsNullOrWhiteSpace(search) || prospect.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || prospect.College.Contains(search, StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(prospect => prospect.ScoutedOverall).ThenBy(prospect => prospect.Name, StringComparer.OrdinalIgnoreCase))
        {
            var evaluation = evaluations.GetEvaluation(prospect.ProspectId);
            _draftProspectList.AddItem($"{prospect.Position,-4} {prospect.Name,-24} EST OVR {evaluation.EstimatedOverall,-5} POT {evaluation.EstimatedPotential,-5} {evaluation.Confidence}");
            _draftProspectList.SetItemMetadata(_draftProspectList.ItemCount - 1, prospect.ProspectId);
        }
    }

    private void RefreshDraftOrder(LeagueState league, DraftPickState currentPick)
    {
        _draftOrderList.Clear();
        foreach (var pick in league.Draft.Picks.Where(pick => pick != null).OrderBy(pick => pick.OverallPick).Take(32))
        {
            var team = league.Teams.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, pick.TeamId, StringComparison.OrdinalIgnoreCase));
            var marker = currentPick != null && pick.OverallPick == currentPick.OverallPick ? "ON CLOCK " : string.IsNullOrWhiteSpace(pick.ProspectId) ? "" : "DONE ";
            _draftOrderList.AddItem($"{marker}#{pick.OverallPick,3} R{pick.Round} {team?.Abbreviation ?? pick.TeamId}");
        }
    }

    private void RefreshDraftRecentPicks(LeagueState league)
    {
        if (_draftRecentPicks == null)
            return;
        _draftRecentPicks.Clear();
        var recent = (league?.Draft?.RecapEntries ?? new List<DraftClassRecapEntry>()).OrderByDescending(entry => entry.OverallPick).Take(8).OrderBy(entry => entry.OverallPick).ToList();
        foreach (var entry in recent)
        {
            var team = league.Teams.FirstOrDefault(candidate => string.Equals(candidate.TeamId, entry.TeamId, StringComparison.OrdinalIgnoreCase));
            var reaction = string.IsNullOrWhiteSpace(entry.PublicReaction) ? "" : $"\n      {entry.PublicReaction}";
            _draftRecentPicks.AddItem($"#{entry.OverallPick,3}  {team?.Abbreviation ?? entry.TeamId,-4}  {entry.Position,-4} {entry.Name}{reaction}");
        }
        if (recent.Count == 0)
            _draftRecentPicks.AddItem("The live draft wire will populate after the first selection.");
    }

    private void RefreshTeamDraftBoard(LeagueState league)
    {
        if (_teamDraftBoardList == null)
            return;
        RefreshTeamDraftBoardPositionFilter(league);
        _teamDraftBoardList.Clear();
        var board = league?.Draft?.UserBoardProspectIds ?? new List<string>();
        var visible = 0;
        var positionFilter = _teamDraftBoardPositionFilter?.Selected > 0 ? _teamDraftBoardPositionFilter.GetItemText(_teamDraftBoardPositionFilter.Selected) : "";
        var confidenceFilter = _teamDraftBoardConfidenceFilter?.Selected > 0 ? _teamDraftBoardConfidenceFilter.GetItemText(_teamDraftBoardConfidenceFilter.Selected) : "";
        var evaluations = new ProspectEvaluationService(_nativeGameCoreContext);
        var draftService = new DraftService(_nativeGameCoreContext);
        for (var boardIndex = 0; boardIndex < board.Count; boardIndex++)
        {
            var prospectId = board[boardIndex];
            var prospect = league.CollegeProspects.FirstOrDefault(candidate => string.Equals(candidate?.ProspectId, prospectId, StringComparison.OrdinalIgnoreCase));
            if (prospect == null || !string.IsNullOrWhiteSpace(prospect.DraftedByTeamId))
                continue;
            var evaluation = evaluations.GetEvaluation(prospect.ProspectId);
            var confidence = evaluation?.Confidence?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Low";
            if (!string.IsNullOrWhiteSpace(positionFilter) && !string.Equals(prospect.Position, positionFilter, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(confidenceFilter) && !string.Equals(confidence, confidenceFilter, StringComparison.OrdinalIgnoreCase))
                continue;
            visible++;
            league.Draft.UserBoardTags.TryGetValue(prospect.ProspectId, out var tag);
            league.Draft.UserBoardTiers.TryGetValue(prospect.ProspectId, out var tier);
            var tagText = string.IsNullOrWhiteSpace(tag) ? "" : $" [{tag.ToUpperInvariant()}]";
            var tierText = string.IsNullOrWhiteSpace(tier) ? "" : $" · {tier}";
            var context = draftService.GetUserBoardNeedContext(prospect.ProspectId);
            _teamDraftBoardList.AddItem($"{boardIndex + 1,2}. {prospect.Position,-4} {prospect.Name}{tagText}{tierText} · NEED {context?.NeedLevel?.ToUpperInvariant() ?? "N/A"} · CONF {confidence.ToUpperInvariant()} · {prospect.College}");
            _teamDraftBoardList.SetItemMetadata(_teamDraftBoardList.ItemCount - 1, prospect.ProspectId);
        }
        if (_teamDraftBoardFilterSummary != null)
            _teamDraftBoardFilterSummary.Text = $"Showing {visible} of {board.Count}";
        if (visible == 0)
            _teamDraftBoardList.AddItem(board.Count == 0 ? "Add prospects from the main board to create your private ranking." : "No team-board prospects match these filters.");
    }

    private void RefreshTeamDraftBoardPositionFilter(LeagueState league)
    {
        if (_teamDraftBoardPositionFilter == null)
            return;
        var selected = _teamDraftBoardPositionFilter.Selected > 0 ? _teamDraftBoardPositionFilter.GetItemText(_teamDraftBoardPositionFilter.Selected) : "All positions";
        var positions = (league?.Draft?.UserBoardProspectIds ?? new List<string>())
            .Select(id => league?.CollegeProspects?.FirstOrDefault(prospect => string.Equals(prospect?.ProspectId, id, StringComparison.OrdinalIgnoreCase))?.Position)
            .Where(position => !string.IsNullOrWhiteSpace(position)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(position => position, StringComparer.OrdinalIgnoreCase).ToList();
        _teamDraftBoardPositionFilter.Clear();
        _teamDraftBoardPositionFilter.AddItem("All positions");
        foreach (var position in positions) _teamDraftBoardPositionFilter.AddItem(position);
        for (var index = 0; index < _teamDraftBoardPositionFilter.ItemCount; index++)
            if (string.Equals(_teamDraftBoardPositionFilter.GetItemText(index), selected, StringComparison.OrdinalIgnoreCase)) { _teamDraftBoardPositionFilter.Select(index); return; }
        _teamDraftBoardPositionFilter.Select(0);
    }

    private async Task ToggleSelectedTeamDraftBoardProspect()
    {
        if (string.IsNullOrWhiteSpace(_selectedDraftProspectId)) { _draftStatus.Text = "Select a prospect first."; return; }
        var league = _nativeGameCoreContext?.ActiveLeague;
        var draft = new DraftService(_nativeGameCoreContext);
        var onBoard = league?.Draft?.UserBoardProspectIds?.Contains(_selectedDraftProspectId, StringComparer.OrdinalIgnoreCase) == true;
        var changed = onBoard ? draft.RemoveFromUserBoard(_selectedDraftProspectId) : draft.AddToUserBoard(_selectedDraftProspectId);
        _draftStatus.Text = draft.LastMessage;
        if (!changed) return;
        await SaveNativeAutosave("Team draft board saved.");
        RefreshTeamDraftBoard(league);
    }

    private async Task MoveSelectedTeamDraftBoardProspect(int direction)
    {
        if (string.IsNullOrWhiteSpace(_selectedDraftProspectId)) { _draftStatus.Text = "Select a team-board prospect first."; return; }
        var draft = new DraftService(_nativeGameCoreContext);
        if (!draft.MoveOnUserBoard(_selectedDraftProspectId, direction)) { _draftStatus.Text = draft.LastMessage; return; }
        await SaveNativeAutosave("Team draft board order saved.");
        RefreshTeamDraftBoard(_nativeGameCoreContext.ActiveLeague);
        _draftStatus.Text = draft.LastMessage;
    }

    private async void MoveTeamDraftBoardProspect(string prospectId, string targetProspectId, bool insertAfter)
    {
        var draft = new DraftService(_nativeGameCoreContext);
        if (!draft.MoveOnUserBoard(prospectId, targetProspectId, insertAfter)) { _draftStatus.Text = draft.LastMessage; return; }
        _selectedDraftProspectId = prospectId;
        await SaveNativeAutosave("Team draft board order saved.");
        RefreshTeamDraftBoard(_nativeGameCoreContext.ActiveLeague);
        SelectTeamDraftBoardProspectById(prospectId);
        _draftStatus.Text = draft.LastMessage;
    }

    private void SelectTeamDraftBoardProspectById(string prospectId)
    {
        for (var index = 0; index < _teamDraftBoardList.ItemCount; index++)
        {
            var metadata = _teamDraftBoardList.GetItemMetadata(index);
            if (metadata.VariantType == Variant.Type.String && metadata.AsString().Equals(prospectId, StringComparison.OrdinalIgnoreCase))
            {
                _teamDraftBoardList.Select(index);
                return;
            }
        }
    }

    private void SelectTeamDraftBoardProspect(long index)
    {
        var metadata = _teamDraftBoardList.GetItemMetadata((int)index);
        if (IsNil(metadata)) return;
        _selectedDraftProspectId = metadata.AsString();
        var draft = _nativeGameCoreContext?.ActiveLeague?.Draft;
        var tag = draft?.UserBoardTags != null && draft.UserBoardTags.TryGetValue(_selectedDraftProspectId, out var savedTag) ? savedTag : "";
        _teamDraftBoardTag.Select(string.Equals(tag, "target", StringComparison.OrdinalIgnoreCase) ? 1 : string.Equals(tag, "avoid", StringComparison.OrdinalIgnoreCase) ? 2 : 0);
        _teamDraftBoardTier.Text = draft?.UserBoardTiers != null && draft.UserBoardTiers.TryGetValue(_selectedDraftProspectId, out var tier) ? tier : "";
        _teamDraftBoardNote.Text = draft?.UserBoardNotes != null && draft.UserBoardNotes.TryGetValue(_selectedDraftProspectId, out var note) ? note : "";
        UpdateDraftProspectDetail();
        var need = new DraftService(_nativeGameCoreContext).GetUserBoardNeedContext(_selectedDraftProspectId);
        if (need != null)
            _draftProspectDetail.Text += $"\n\nTEAM CONTEXT\nNeed: {need.NeedLevel} · Role path: {need.RolePath}\n{need.Explanation}";
        UpdateDraftStatus();
    }

    private async Task SaveSelectedTeamDraftBoardContext()
    {
        if (string.IsNullOrWhiteSpace(_selectedDraftProspectId)) { _draftStatus.Text = "Select a team-board prospect first."; return; }
        var tag = _teamDraftBoardTag.Selected switch { 1 => "target", 2 => "avoid", _ => "" };
        var draft = new DraftService(_nativeGameCoreContext);
        if (!draft.SetUserBoardContext(_selectedDraftProspectId, tag, _teamDraftBoardNote.Text, _teamDraftBoardTier.Text)) { _draftStatus.Text = draft.LastMessage; return; }
        await SaveNativeAutosave("Private team draft board context saved.");
        RefreshTeamDraftBoard(_nativeGameCoreContext.ActiveLeague);
        _draftStatus.Text = draft.LastMessage;
    }

    private void RefreshOwnedDraftPicks(LeagueState league)
    {
        if (_draftOwnedPicks == null)
            return;
        var owned = (league?.Draft?.Picks ?? new List<DraftPickState>())
            .Where(pick => pick != null && string.Equals(pick.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(pick.ProspectId))
            .OrderBy(pick => pick.OverallPick)
            .Select(pick => $"R{pick.Round}.{pick.PickInRound} (#{pick.OverallPick})")
            .ToList();
        _draftOwnedPicks.Text = owned.Count == 0
            ? "YOUR REMAINING PICKS: none"
            : $"YOUR REMAINING PICKS: {string.Join("  ·  ", owned)}";
    }

    private async Task StartDraftFromWarRoom()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league == null || !string.Equals(league.Calendar?.Phase, ScheduleService.DraftPrepPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            _draftStatus.Text = "The draft can only be started from Draft Prep.";
            return;
        }
        _btnStartDraft.Disabled = true;
        _draftStatus.Text = "Opening the live Draft Stage…";
        var response = new ContinueService(_nativeGameCoreContext).Continue();
        if (response?.Ok != true || !string.Equals(league.Calendar?.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            _draftStatus.Text = string.IsNullOrWhiteSpace(response?.Error) ? "The draft could not be opened." : response.Error;
            _btnStartDraft.Disabled = false;
            return;
        }
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Draft Stage opened.", autosaveToo: true);
        RefreshDraftBoard();
    }

    private void UpdateDraftProspectDetail()
    {
        if (_draftProspectDetail == null)
            return;

        var evaluation = new ProspectEvaluationService(_nativeGameCoreContext).GetEvaluation(_selectedDraftProspectId);
        _draftProspectDetail.Text = evaluation == null
            ? "Prospect evaluation unavailable."
            : $"PUBLIC FACTS\n{evaluation.KnownFacts}\n\n{new CollegeUniverseService(_nativeGameCoreContext).GetCompactProspectContext(_selectedDraftProspectId)}\n\nTEAM SCOUTING ESTIMATE - NOT HIDDEN RATINGS\nEstimated OVR range: {evaluation.EstimatedOverall}\nEstimated potential range: {evaluation.EstimatedPotential}\nConfidence: {evaluation.Confidence}\n\nTRAIT\n{evaluation.Trait}\n\nINTERVIEW\n{evaluation.Interview}\n\nSCOUT REPORT\n{evaluation.Report}";
    }
    private void UpdateDraftStatus()
    {
        var league = _nativeGameCoreContext?.ActiveLeague; var draft = new DraftService(_nativeGameCoreContext); var pick = draft.GetCurrentPick();
        var liveDraft = string.Equals(league?.Calendar?.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase);
        var userOwnsCurrentPick = pick != null && string.Equals(pick.TeamId, league?.UserTeamId, StringComparison.OrdinalIgnoreCase);
        var canPick = userOwnsCurrentPick && liveDraft && !string.IsNullOrWhiteSpace(_selectedDraftProspectId);
        _btnMakeDraftPick.Disabled = !canPick; _draftStatus.Text = pick == null ? "Draft complete." : string.Equals(pick.TeamId, league?.UserTeamId, StringComparison.OrdinalIgnoreCase) ? $"Your pick: Round {pick.Round}, Pick {pick.PickInRound}. {(canPick ? "Select this prospect." : "Choose a prospect.")}" : $"CPU team is on the clock for Round {pick.Round}, Pick {pick.PickInRound}.";
        if (_btnTradeCurrentDraftPick != null) { _btnTradeCurrentDraftPick.Visible = liveDraft; _btnTradeCurrentDraftPick.Disabled = !userOwnsCurrentPick; }
        if (_draftPickContext != null)
            _draftPickContext.Text = pick == null ? "Draft order complete." : $"PICK CONTEXT: Overall #{pick.OverallPick} | Round {pick.Round}, pick {pick.PickInRound} | Owner {league?.Teams.FirstOrDefault(team => string.Equals(team?.TeamId, pick.TeamId, StringComparison.OrdinalIgnoreCase))?.Name ?? pick.TeamId}";
    }

    private void OpenCurrentDraftPickTradeMarket()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        var pick = new DraftService(_nativeGameCoreContext).GetCurrentPick();
        if (pick == null || !string.Equals(league?.Calendar?.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase) || !string.Equals(pick.TeamId, league?.UserTeamId, StringComparison.OrdinalIgnoreCase))
        {
            _draftStatus.Text = "Only the current unused pick owned by your team can be opened from the live Draft Stage.";
            return;
        }
        _tradeFinderSelectedAssets.Clear();
        _tradeFinderSelectedAssets.Add($"K:{pick.OverallPick}");
        ShowTradeFinderDialog();
        SetPrimaryStatus($"Current pick #{pick.OverallPick} is selected. Submit the asset only if you want clubs to generate concrete offers.");
    }
    private async void MakeDraftPick() { var league = _nativeGameCoreContext.ActiveLeague; var draft = new DraftService(_nativeGameCoreContext); var recapCount = league.Draft?.RecapEntries?.Count ?? 0; if (draft.MakePick(league.UserTeamId, _selectedDraftProspectId)) { var announcements = (league.Draft?.RecapEntries ?? new List<DraftClassRecapEntry>()).Skip(recapCount).OrderBy(entry => entry.OverallPick).ToList(); await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Draft pick saved.", true); RefreshDraftBoard(); QueueDraftAnnouncements(announcements); } else { _draftStatus.Text = draft.LastMessage; } }

    private void CreateRosterContractControls()
    {
        var actionRow = GetNodeOrNull<Container>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterModeRow");
        if (actionRow == null)
            return;

        _btnOfferExtension = new Button { Text = "Offer Extension" };
        _btnApplyFranchiseTag = new Button { Text = "Apply Franchise Tag", TooltipText = "Available only during the Franchise Tag phase." };
        _btnReleaseSelectedPlayer = new Button { Text = "Release Selected" };
        _btnWaiveSelectedPlayer = new Button { Text = "Waive Selected" };
        _btnMoveSelectedPlayerToIr = new Button { Text = "Move to IR" };
        actionRow.AddChild(_btnOfferExtension);
        actionRow.AddChild(_btnApplyFranchiseTag);
        actionRow.AddChild(_btnReleaseSelectedPlayer);
        actionRow.AddChild(_btnWaiveSelectedPlayer);
        actionRow.AddChild(_btnMoveSelectedPlayerToIr);
        _btnOfferExtension.Pressed += ShowExtensionOffer;
        _btnApplyFranchiseTag.Pressed += async () => await ApplyFranchiseTagToSelectedPlayer();
        _btnReleaseSelectedPlayer.Pressed += ShowReleaseConfirmation;
        _btnWaiveSelectedPlayer.Pressed += async () => await WaiveSelectedPlayer();
        _btnMoveSelectedPlayerToIr.Pressed += async () => await MoveSelectedPlayerToIr();

        _extensionDialog = new AcceptDialog { Title = "Offer Contract Extension", MinSize = new Vector2I(480, 300) };
        AddChild(_extensionDialog);
        _extensionDialog.GetOkButton().Text = "Submit Offer";
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _extensionDialog.AddChild(content);
        _extensionPlayerLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_extensionPlayerLabel);
        _extensionAnnualOffer = CreateMoneyOffer(1m, 40m);
        _extensionGuaranteeOffer = CreateMoneyOffer(0m, 80m);
        _extensionYearsOffer = new SpinBox { MinValue = 1, MaxValue = 5, Step = 1, Value = 2 };
        content.AddChild(SetupRow("Annual ($M)", _extensionAnnualOffer));
        content.AddChild(SetupRow("Guaranteed ($M)", _extensionGuaranteeOffer));
        content.AddChild(SetupRow("Years", _extensionYearsOffer));
        _extensionStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_extensionStatus);
        _extensionDialog.Confirmed += async () => await SubmitExtensionOffer();

        _releaseDialog = new ConfirmationDialog { Title = "Confirm Player Release", MinSize = new Vector2I(560, 390) };
        _releaseDialog.GetOkButton().Text = "RELEASE PLAYER";
        AddChild(_releaseDialog);
        _releaseDetails = new RichTextLabel { BbcodeEnabled = false, FitContent = false, CustomMinimumSize = new Vector2(520, 285), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _releaseDialog.AddChild(_releaseDetails);
        _releaseDialog.Confirmed += async () => await ConfirmSelectedPlayerRelease();

        _rosterContextMenu = new PopupMenu { Name = "RosterContextMenu" };
        _rosterContractMenu = new PopupMenu { Name = "RosterContractActions" };
        AddChild(_rosterContextMenu);
        _rosterContextMenu.AddSubmenuNodeItem("Contract", _rosterContractMenu);
        _rosterContractMenu.AddItem("Offer Extension", 1);
        _rosterContractMenu.AddItem("Release", 2);
        _rosterContractMenu.IdPressed += id =>
        {
            if (id == 1) ShowExtensionOffer();
            else if (id == 2) ShowReleaseConfirmation();
        };
        if (_rosterTree != null) _rosterTree.GuiInput += OnRosterTreeContextInput;
    }

    private void RehomeRosterActions()
    {
        if (_playerReportPanel == null || _squadPlayerActions != null)
            return;

        _squadPlayerActions = new HBoxContainer { Name = "SquadPlayerActions" };
        _squadPlayerActions.AddThemeConstantOverride("separation", 6);
        _playerReportPanel.AddChild(_squadPlayerActions);
        _playerReportPanel.MoveChild(_squadPlayerActions, 0);
        var backToRoster = new Button { Text = "‹  BACK TO ROSTER", CustomMinimumSize = new Vector2(150, 28) };
        backToRoster.Pressed += () => SetReportPlaceholder("Select a player to open the player profile.");
        _squadPlayerActions.AddChild(backToRoster);
        foreach (var action in new[] { _btnOfferExtension, _btnApplyFranchiseTag, _btnReleaseSelectedPlayer, _btnWaiveSelectedPlayer, _btnMoveSelectedPlayerToIr })
        {
            if (action == null)
                continue;
            action.Reparent(_squadPlayerActions);
            action.CustomMinimumSize = new Vector2(0, 28);
            action.AddThemeFontSizeOverride("font_size", 12);
        }
    }

    private void ShowExtensionOffer()
    {
        if (!ContractPhaseRules.CanOfferExtensions(_nativeGameCoreContext?.ActiveLeague, out var phaseError))
        {
            SetPrimaryStatus(phaseError);
            return;
        }
        var player = GetSelectedNativeRosterPlayer();
        if (player == null)
        {
            SetPrimaryStatus("Select a player from your roster first.");
            return;
        }

        var league = _nativeGameCoreContext.ActiveLeague;
        var team = league.Teams.First(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var required = new ContractService(_nativeGameCoreContext).GetRequiredAnnualSalary(player, team);
        _extensionPlayerId = player.PlayerId;
        _extensionAnnualOffer.Value = (double)(required / 1_000_000m);
        _extensionGuaranteeOffer.Value = (double)(required * 0.30m / 1_000_000m);
        _extensionYearsOffer.Value = 2;
        _extensionPlayerLabel.Text = $"{player.Name} | {player.Position} | OVR {player.Overall}\nEstimated requirement: {GameCoreStateHelper.FormatCapRoom(required)} per year.";
        _extensionStatus.Text = "";
        _extensionDialog.PopupCentered(new Vector2I(480, 300));
    }

    private async Task SubmitExtensionOffer()
    {
        if (string.IsNullOrWhiteSpace(_extensionPlayerId))
            return;

        var result = new ContractService(_nativeGameCoreContext).ReSignPlayer(_extensionPlayerId, null, new ContractOffer
        {
            AnnualSalary = (decimal)_extensionAnnualOffer.Value * 1_000_000m,
            GuaranteedSalary = (decimal)_extensionGuaranteeOffer.Value * 1_000_000m,
            Years = (int)_extensionYearsOffer.Value,
        });
        _extensionStatus.Text = result.Accepted ? result.Message : $"{result.Message} Requirement: {GameCoreStateHelper.FormatCapRoom(result.RequiredAnnualSalary)}";
        if (!result.Accepted)
            return;

        _extensionDialog.Hide();
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Contract extension saved.", autosaveToo: true);
        await RefreshAll();
    }

    private async Task ApplyFranchiseTagToSelectedPlayer()
    {
        if (!ContractPhaseRules.CanApplyFranchiseTag(_nativeGameCoreContext?.ActiveLeague, out var phaseError))
        {
            SetPrimaryStatus(phaseError);
            return;
        }

        var player = GetSelectedNativeRosterPlayer();
        if (player == null)
        {
            SetPrimaryStatus("Select an eligible player from your roster first.");
            return;
        }

        var result = new ContractService(_nativeGameCoreContext).ApplyFranchiseTag(player.PlayerId);
        SetPrimaryStatus(result.Message);
        if (!result.Accepted)
            return;

        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Franchise tag saved.", autosaveToo: true);
        await RefreshAll();
    }

    private void OnRosterTreeContextInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouse || mouse.ButtonIndex != MouseButton.Right || !mouse.Pressed || _rosterTree == null)
            return;
        var item = _rosterTree.GetItemAtPosition(mouse.Position);
        if (item == null || IsNil(item.GetMetadata(0))) return;
        item.Select(0);
        OnRosterItemSelected(item);
        var popupPosition = _rosterTree.GetScreenPosition() + mouse.Position;
        _rosterContextMenu.Position = new Vector2I(Mathf.RoundToInt(popupPosition.X), Mathf.RoundToInt(popupPosition.Y));
        _rosterContextMenu.Popup();
        GetViewport().SetInputAsHandled();
    }

    private void ShowReleaseConfirmation()
    {
        var player = GetSelectedNativeRosterPlayer();
        if (player == null)
        {
            SetPrimaryStatus("Select a player from your roster first.");
            return;
        }

        ShowReleaseConfirmationForPlayer(player.PlayerId);
    }

    private void ShowReleaseConfirmationForPlayer(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return;

        var preview = new ContractService(_nativeGameCoreContext).PreviewRelease(playerId);
        if (!preview.Ok)
        {
            SetPrimaryStatus(preview.Error);
            return;
        }
        _releasePlayerId = preview.PlayerId;
        _releaseDetails.Text =
            $"{preview.PlayerName} · {preview.Position}\n\n" +
            $"CURRENT CONTRACT\nType: {preview.ContractType}\nYears remaining: {preview.YearsRemaining}\nAnnual salary: {GameCoreStateHelper.FormatCapRoom(preview.AnnualSalary)}\nRecorded guarantee: {GameCoreStateHelper.FormatCapRoom(preview.GuaranteedSalary)}\n\n" +
            $"FINANCIAL EFFECT IN THIS SAVE\nCommitted payroll: {GameCoreStateHelper.FormatCapRoom(preview.PayrollBefore)} → {GameCoreStateHelper.FormatCapRoom(preview.PayrollAfter)}\nCap room: {GameCoreStateHelper.FormatCapRoom(preview.CapRoomBefore)} → {GameCoreStateHelper.FormatCapRoom(preview.CapRoomAfter)}\n\n" +
            $"ROSTER EFFECT\nActive roster: {preview.RosterCountBefore} → {preview.RosterCountAfter}\nThe player will enter free agency and be removed from every saved depth-chart assignment. This action is recorded in league transactions.";
        _releaseDialog.PopupCentered(new Vector2I(560, 390));
    }

    private async Task ConfirmSelectedPlayerRelease()
    {
        if (string.IsNullOrWhiteSpace(_releasePlayerId)) return;

        var result = new ContractService(_nativeGameCoreContext).ReleasePlayer(_releasePlayerId);
        SetPrimaryStatus(result.Message);
        if (!result.Accepted)
            return;

        _releasePlayerId = "";
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Player release saved.", autosaveToo: true);
        await RefreshAll();
        if (_finalCutdownDialog?.Visible == true) RenderFinalCutdown();
    }

    private async Task WaiveSelectedPlayer()
    {
        var player = GetSelectedNativeRosterPlayer();
        if (player == null)
        {
            SetPrimaryStatus("Select a player from your roster first.");
            return;
        }

        var result = new TransactionService(_nativeGameCoreContext).PlaceOnWaivers(player.PlayerId, null, new ContractService(_nativeGameCoreContext));
        SetPrimaryStatus(result.Message);
        if (!result.Accepted)
            return;

        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Waiver transaction saved.", autosaveToo: true);
        await RefreshAll();
    }

    private async Task MoveSelectedPlayerToIr()
    {
        var player = GetSelectedNativeRosterPlayer();
        if (player == null)
        {
            SetPrimaryStatus("Select an injured player from your roster first.");
            return;
        }

        var result = new TransactionService(_nativeGameCoreContext).MoveToInjuredReserve(player.PlayerId, null, new ContractService(_nativeGameCoreContext));
        SetPrimaryStatus(result.Message);
        if (!result.Accepted)
            return;

        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Injured reserve move saved.", autosaveToo: true);
        await RefreshAll();
    }

    private PlayerState GetSelectedNativeRosterPlayer()
    {
        var playerId = GetSelectedPlayerId();
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        return team?.Roster?.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
    }

    private void CreateFreeAgencyButton()
    {
        var actionRow = GetNodeOrNull<Container>("AppMargin/MainPadding/MainLayout/ActionButtonRow");
        if (actionRow == null)
            return;

        _btnFreeAgency = new Button { Text = "Free Agency", TooltipText = "Browse free agents and make contract offers." };
        actionRow.AddChild(_btnFreeAgency);
        actionRow.MoveChild(_btnFreeAgency, Math.Min(4, actionRow.GetChildCount() - 1));
        _btnFreeAgency.Pressed += ShowFreeAgency;
    }

    private void CreateMarketDeskDialog()
    {
        _marketDeskDialog = new AcceptDialog
        {
            Name = "MarketDeskDialog",
            Title = "Transactions & Market",
            MinSize = new Vector2I(1040, 650),
            Exclusive = false,
        };
        AddChild(_marketDeskDialog);
        _marketDeskDialog.GetOkButton().Visible = false;

        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _marketDeskDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("MARKET DESK", "Review roster capacity, cap room, phase availability, and the latest franchise ledger before making an explicit move."));
        _marketDeskSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_marketDeskSummary);

        var body = new HSplitContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SplitOffset = 590 };
        content.AddChild(body);
        var actions = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddChild(actions);
        actions.AddChild(CreateMarketHeading("ACTION WORKSPACES", "Each workspace uses GameCore validation. Nothing changes until you submit an action."));
        actions.AddChild(CreateMarketAction("FREE AGENCY", "Search available players, inspect asking prices, and submit a contract offer.", ShowFreeAgency));
        actions.AddChild(CreateMarketAction("TRADE DESK", "Build a player/pick proposal and review the deterministic counterparty rationale.", ShowTradeDialog));
        actions.AddChild(CreateMarketAction("WAIVERS & PRACTICE SQUAD", "Claim waived players, sign eligible developmental players, or promote a squad member on a permanent active-roster contract.", ShowRosterManagement));
        actions.AddChild(CreateMarketAction("CONTRACTS & TAGS", "Open the squad inspector to negotiate, tag, release, waive, or move a selected player to IR.", async () => await SelectMainTab(ROSTER_TAB_INDEX)));

        var ledger = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddChild(ledger);
        ledger.AddChild(CreateMarketHeading("RECENT TRANSACTION LEDGER", "Persisted franchise activity, newest first."));
        _marketDeskHistory = new RichTextLabel { BbcodeEnabled = false, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        ledger.AddChild(_marketDeskHistory);
        _marketDeskStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_marketDeskStatus);
        ApplyWorkstationTheme(_marketDeskDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void CreateFranchiseSettingsDialog()
    {
        _franchiseSettingsDialog = new AcceptDialog
        {
            Name = "FranchiseSettingsDialog",
            Title = "Franchise Settings & Profile",
            MinSize = new Vector2I(980, 620),
            Exclusive = false,
        };
        AddChild(_franchiseSettingsDialog);
        _franchiseSettingsDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _franchiseSettingsDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("FRANCHISE UTILITIES", "Review the active GM identity and save state. Profile changes remain part of new-franchise setup; display choices retain their existing owners."));
        var tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        content.AddChild(tabs);

        var profileTab = new VBoxContainer { Name = "Profile" };
        tabs.AddChild(profileTab);
        profileTab.AddChild(new Label { Text = "ACTIVE FRANCHISE PROFILE" });
        _franchiseProfileSummary = new RichTextLabel { BbcodeEnabled = false, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        profileTab.AddChild(_franchiseProfileSummary);
        var setupButton = new Button { Text = "NEW FRANCHISE PROFILE SETUP", TooltipText = "Profile selection and editing occur when starting a new franchise." };
        setupButton.Pressed += () => SetPrimaryStatus("GM profiles are selected during New Franchise setup to preserve the active franchise snapshot.");
        profileTab.AddChild(setupButton);

        var saveTab = new VBoxContainer { Name = "Save & Display" };
        tabs.AddChild(saveTab);
        saveTab.AddChild(CreateMarketHeading("SAVE MANAGEMENT", "Named saves and autosaves continue to use the existing native GameCore save service."));
        var saveActions = new HBoxContainer();
        saveTab.AddChild(saveActions);
        var saveButton = new Button { Text = "SAVE FRANCHISE", CustomMinimumSize = new Vector2(180, 38) };
        saveButton.Pressed += async () => { await SaveNativeGame(); RefreshFranchiseSettingsUi(); };
        saveActions.AddChild(saveButton);
        var loadButton = new Button { Text = "LOAD FRANCHISE", CustomMinimumSize = new Vector2(180, 38) };
        loadButton.Pressed += async () => { await LoadNativeGame(); RefreshFranchiseSettingsUi(); };
        saveActions.AddChild(loadButton);
        _franchiseSaveStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        saveTab.AddChild(_franchiseSaveStatus);
        saveTab.AddChild(CreateMarketHeading("DISPLAY PREFERENCES", "Roster columns, filters, and panel sizing are already persisted in ui.cfg and remain controlled in the Squad workspace."));
        var displayButton = new Button { Text = "OPEN SQUAD DISPLAY CONTROLS" };
        displayButton.Pressed += async () =>
        {
            _franchiseSettingsDialog.Hide();
            await SelectMainTab(ROSTER_TAB_INDEX);
            SetPrimaryStatus("Roster display controls are available in Squad.");
        };
        saveTab.AddChild(displayButton);

        var developerTab = new VBoxContainer { Name = "Developer" };
        tabs.AddChild(developerTab);
        developerTab.AddChild(CreateMarketHeading("DEVELOPER DIAGNOSTICS", "Developer-only tools are separate from normal franchise actions. Visibility is session-only and uses the existing diagnostics panel."));
        _franchiseDeveloperToggle = new CheckButton { Text = "SHOW DEVELOPER TOOLS" };
        _franchiseDeveloperToggle.Toggled += ApplyDebugPanelVisibility;
        developerTab.AddChild(_franchiseDeveloperToggle);
        developerTab.AddChild(new Label { Text = "The developer panel exposes refresh, simulation, save, load, reset, and smoke-test controls already present in this build.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        ApplyWorkstationTheme(_franchiseSettingsDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void ShowFranchiseSettings()
    {
        RefreshFranchiseSettingsUi();
        _franchiseSettingsDialog.PopupCentered(new Vector2I(980, 620));
    }

    private void RefreshFranchiseSettingsUi()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        var profile = league?.FranchiseMetadata?.GmProfileSnapshot;
        var world = league?.FranchiseMetadata?.World;
        if (_franchiseProfileSummary != null)
        {
            _franchiseProfileSummary.Text = league == null
                ? "No active franchise is loaded. Start or load a franchise to view its immutable profile snapshot."
                : $"GM: {profile?.Name ?? "User GM"}\nFranchise: {league.Teams.FirstOrDefault(team => string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase))?.Name ?? "Unassigned"}\n\nNegotiation {profile?.Attributes?.Negotiation ?? 0} | Player Management {profile?.Attributes?.PlayerManagement ?? 0}\nScouting {profile?.Attributes?.ScoutingJudgment ?? 0} | Leadership {profile?.Attributes?.Leadership ?? 0}\n\nAppearance: {profile?.Appearance?.Outfit ?? "Unspecified"}\nWorld: {world?.Source.ToString() ?? "Unknown"} roster | Seed {world?.Seed.ToString() ?? "Unknown"}\n\nThe active franchise stores a profile snapshot; reusable profile editing is intentionally kept in New Franchise setup.";
        }
        if (_franchiseSaveStatus != null)
        {
            var saves = GetNativeGameCoreSaveService();
            var named = saves?.SaveExists(GameCoreSaveService.NamedSaveFileName) == true ? "available" : "not found";
            var autosave = saves?.SaveExists() == true ? "available" : "not found";
            _franchiseSaveStatus.Text = $"Named franchise save: {named}. Autosave: {autosave}. Successful explicit saves also update the existing autosave.";
        }
        if (_franchiseDeveloperToggle != null)
            _franchiseDeveloperToggle.SetPressedNoSignal(_debugPanel?.Visible ?? false);
    }

    private void CreateInboxDeskDialog()
    {
        _inboxDeskDialog = new AcceptDialog
        {
            Name = "InboxDeskDialog",
            Title = "Inbox & Decision Queue",
            MinSize = new Vector2I(1040, 630),
            Exclusive = false,
        };
        AddChild(_inboxDeskDialog);
        _inboxDeskDialog.GetOkButton().Visible = false;

        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _inboxDeskDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("INBOX & DECISION QUEUE", "Only active actions from the current franchise state are shown. Select an item to review its context and open the existing workflow."));
        var toolbar = new HBoxContainer();
        content.AddChild(toolbar);
        _inboxDeskCount = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        toolbar.AddChild(_inboxDeskCount);
        _inboxCategoryFilter = new OptionButton { TooltipText = "Filter active decisions by presentation category." };
        _inboxCategoryFilter.AddItem("All active");
        _inboxCategoryFilter.AddItem("High priority");
        _inboxCategoryFilter.AddItem("Game day");
        _inboxCategoryFilter.AddItem("Roster");
        _inboxCategoryFilter.AddItem("League & offseason");
        _inboxCategoryFilter.ItemSelected += _ => RefreshInboxDesk();
        toolbar.AddChild(_inboxCategoryFilter);

        var split = new HSplitContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SplitOffset = 430 };
        content.AddChild(split);
        _inboxQueueList = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill, AllowReselect = true };
        _inboxQueueList.ItemSelected += OnInboxDeskItemSelected;
        split.AddChild(_inboxQueueList);
        var detail = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        split.AddChild(detail);
        _inboxDeskSubject = new Label();
        _inboxDeskSubject.AddThemeFontSizeOverride("font_size", 18);
        detail.AddChild(_inboxDeskSubject);
        _inboxDeskContext = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        detail.AddChild(_inboxDeskContext);
        _inboxDeskBody = new RichTextLabel { BbcodeEnabled = false, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        detail.AddChild(_inboxDeskBody);
        _inboxDeskAction = new Button { Text = "OPEN WORKFLOW", CustomMinimumSize = new Vector2(0, 36) };
        _inboxDeskAction.Pressed += async () =>
        {
            _inboxDeskDialog.Hide();
            await OnInboxPrimaryActionPressed();
        };
        detail.AddChild(_inboxDeskAction);
        ApplyWorkstationTheme(_inboxDeskDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void ShowInboxDesk()
    {
        RefreshInboxDesk();
        _inboxDeskDialog.PopupCentered(new Vector2I(1040, 630));
    }

    private void OnInboxDeskItemSelected(long index)
    {
        if (_inboxQueueList == null || index < 0 || index >= _inboxQueueList.ItemCount)
            return;
        var messageId = _inboxQueueList.GetItemMetadata((int)index).ToString();
        if (TrySelectInboxMessage(messageId))
            RefreshInboxDesk();
    }

    private void RefreshInboxDesk()
    {
        if (_inboxDeskDialog == null || _inboxQueueList == null)
            return;

        var filterIndex = _inboxCategoryFilter?.Selected ?? -1;
        var category = _inboxCategoryFilter != null && filterIndex >= 0 ? _inboxCategoryFilter.GetItemText(filterIndex) : "All active";
        var entries = new List<Godot.Collections.Dictionary>();
        for (var i = 0; i < (_inboxMessages?.Count ?? 0); i++)
        {
            if (TryGetDictionary((Variant)_inboxMessages[i], out var message) && ShouldShowInboxMessage(message, category))
                entries.Add(message);
        }

        _inboxQueueList.Clear();
        foreach (var message in entries)
        {
            var priority = FmtString(GetFirstNonNil(message, "severity"), "info").ToUpperInvariant();
            var state = GetBoolValue(GetFirstNonNil(message, "read"), false) ? "READ" : "ACTIVE";
            _inboxQueueList.AddItem($"{priority,-7} {ResolveInboxCategory(message),-18} {GetMessageSubject(message)}\n{state} | {GetMessageBody(message)}");
            _inboxQueueList.SetItemMetadata(_inboxQueueList.ItemCount - 1, GetMessageId(message));
        }

        _inboxDeskCount.Text = entries.Count == 0 ? "No active decisions." : $"{entries.Count} active decision(s) | authoritative franchise queue";
        var selectedIndex = entries.FindIndex(message => string.Equals(GetMessageId(message), _selectedInboxMessageId, StringComparison.OrdinalIgnoreCase));
        if (selectedIndex >= 0)
            _inboxQueueList.Select(selectedIndex);
        UpdateInboxDeskDetail(_selectedInboxActionItem);
    }

    private static bool ShouldShowInboxMessage(Godot.Collections.Dictionary message, string category)
    {
        if (string.Equals(category, "High priority", StringComparison.OrdinalIgnoreCase))
        {
            var severity = FmtString(GetFirstNonNil(message, "severity"), "info");
            return string.Equals(severity, "danger", StringComparison.OrdinalIgnoreCase) || string.Equals(severity, "warning", StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(category, "Game day", StringComparison.OrdinalIgnoreCase))
            return IsGameDayMessage(message);
        if (string.Equals(category, "Roster", StringComparison.OrdinalIgnoreCase))
            return IsRosterInvalidMessage(message) || IsDepthChartInvalidMessage(message) || IsInjuryDepthAdvisoryMessage(message) || IsOpeningWeekReadinessMessage(message) || IsWaiverClaimConfirmationMessage(message);
        if (string.Equals(category, "League & offseason", StringComparison.OrdinalIgnoreCase))
            return IsPostseasonPendingMessage(message) || IsSeasonCompleteMessage(message) || IsOffseasonPendingMessage(message);
        return true;
    }

    private static string ResolveInboxCategory(Godot.Collections.Dictionary message)
    {
        if (IsGameDayMessage(message)) return "GAME DAY";
        if (IsRosterInvalidMessage(message) || IsDepthChartInvalidMessage(message) || IsInjuryDepthAdvisoryMessage(message) || IsOpeningWeekReadinessMessage(message) || IsWaiverClaimConfirmationMessage(message)) return "ROSTER";
        if (IsSeasonCompleteMessage(message) || IsPostseasonPendingMessage(message)) return "LEAGUE";
        if (IsOffseasonPendingMessage(message)) return "OFFSEASON";
        return "FRANCHISE";
    }

    private void UpdateInboxDeskDetail(Godot.Collections.Dictionary message)
    {
        if (_inboxDeskSubject == null || _inboxDeskContext == null || _inboxDeskBody == null || _inboxDeskAction == null)
            return;
        if (message == null)
        {
            _inboxDeskSubject.Text = "No active decision selected";
            _inboxDeskContext.Text = "The queue shows only current authoritative action items.";
            _inboxDeskBody.Text = "";
            _inboxDeskAction.Disabled = true;
            return;
        }

        var severity = FmtString(GetFirstNonNil(message, "severity"), "info").ToUpperInvariant();
        var action = ResolveInboxPrimaryActionLabel(message);
        _inboxDeskSubject.Text = GetMessageSubject(message);
        _inboxDeskContext.Text = $"{severity} PRIORITY | {ResolveInboxCategory(message)} | {FmtString(GetFirstNonNil(message, "primary_action", "primaryAction"), "Review action")}";
        _inboxDeskBody.Text = GetMessageBody(message);
        var canRoute = IsGameDayMessage(message) || IsRosterInvalidMessage(message) || IsDepthChartInvalidMessage(message) || IsInjuryDepthAdvisoryMessage(message) || IsOpeningWeekReadinessMessage(message) || IsWaiverClaimConfirmationMessage(message) || IsPostseasonPendingMessage(message) || IsSeasonCompleteMessage(message) || IsOffseasonPendingMessage(message);
        _inboxDeskAction.Text = canRoute ? action.ToUpperInvariant() : "NO WORKFLOW AVAILABLE";
        _inboxDeskAction.Disabled = !canRoute;
    }

    private static Control CreateMarketHeading(string title, string description)
    {
        var heading = new VBoxContainer();
        var titleLabel = new Label { Text = title };
        titleLabel.AddThemeFontSizeOverride("font_size", 15);
        titleLabel.AddThemeColorOverride("font_color", new Color("f4eddf"));
        heading.AddChild(titleLabel);
        heading.AddChild(new Label { Text = description, AutowrapMode = TextServer.AutowrapMode.WordSmart });
        return heading;
    }

    private static Button CreateMarketAction(string title, string description, Action action)
    {
        var button = new Button { Text = $"{title}\n{description}", TooltipText = description, CustomMinimumSize = new Vector2(0, 68), Alignment = HorizontalAlignment.Left };
        button.Pressed += action;
        return button;
    }

    private void ShowMarketDesk()
    {
        RefreshMarketDeskUi();
        _marketDeskDialog.PopupCentered(new Vector2I(1040, 650));
    }

    private void RefreshMarketDeskUi()
    {
        EnsureNativeGameCoreServices();
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || team == null)
        {
            _marketDeskSummary.Text = "Start or load a franchise to access market actions.";
            _marketDeskHistory.Text = "No franchise ledger is available.";
            _marketDeskStatus.Text = "GameCore franchise required.";
            return;
        }

        var contracts = new ContractService(_nativeGameCoreContext);
        var phaseStatus = ContractPhaseRules.GetStatus(league);
        var rosterStatus = new RosterService(_nativeGameCoreContext).GetTeamRoster(team.TeamId).RosterStatus;
        var rosterNeeds = rosterStatus == null
            ? "Roster status unavailable"
            : rosterStatus.Issues?.Count > 0
                ? string.Join("; ", rosterStatus.Issues)
                : rosterStatus.OpenSlots > 0 ? $"{rosterStatus.OpenSlots} active-roster slot(s) open" : "Active roster at capacity";
        _marketDeskSummary.Text = $"{team.Name} | Phase: {league.Calendar?.Phase ?? "Unavailable"} | Cap room: {GameCoreStateHelper.FormatCapRoom(contracts.GetCapRoom(team))} | Active: {team.Roster.Count}/53 | Practice squad: {team.PracticeSquad.Count}/16 | Free agents: {league.FreeAgents.Count} | Waivers: {league.Waivers.Count}\nRoster needs: {rosterNeeds}";
        var history = _nativeDashboardService.GetTransactionHistory();
        _marketDeskHistory.Text = history.Ok && history.Transactions.Count > 0
            ? string.Join("\n", history.Transactions.Take(12).Select(transaction => $"{transaction.DateLabel} | {transaction.Type} | {transaction.PlayerName}\n{transaction.Details}"))
            : "No transactions recorded.";
        _marketDeskStatus.Text = phaseStatus.Explanation;
    }

    private void CreateFreeAgencyDialog()
    {
        _freeAgencyDialog = new AcceptDialog
        {
            Name = "FreeAgencyDialog",
            Title = "Trade Center > Free Agency",
            MinSize = new Vector2I(1040, 650),
            Exclusive = false,
        };
        AddChild(_freeAgencyDialog);
        _freeAgencyDialog.GetOkButton().Visible = false;

        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, AnchorsPreset = (int)LayoutPreset.FullRect, OffsetLeft = 12, OffsetTop = 10, OffsetRight = -12, OffsetBottom = -48 };
        _freeAgencyDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("FREE AGENCY", "Search and compare actual free agents. Player selection opens profile context; contract terms remain in the existing negotiation workflow."));
        _freeAgencyCapSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_freeAgencyCapSummary);

        var controls = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        controls.AddThemeConstantOverride("separation", 6);
        content.AddChild(controls);
        controls.AddChild(HomeLabel("SEARCH", 11, new Color("9cadb8")));
        _freeAgencySearch = new LineEdit { PlaceholderText = "Player name", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClearButtonEnabled = true };
        _freeAgencySearch.TextChanged += _ => RenderFreeAgentTable();
        controls.AddChild(_freeAgencySearch);
        controls.AddChild(HomeLabel("POSITION", 11, new Color("9cadb8")));
        _freeAgencyPositionFilter = new OptionButton();
        _freeAgencyPositionFilter.AddItem("All positions");
        foreach (var position in new[] { "QB", "RB", "WR", "TE", "OT", "OG", "C", "DL", "LB", "CB", "S", "K", "P" }) _freeAgencyPositionFilter.AddItem(position);
        _freeAgencyPositionFilter.ItemSelected += _ => RenderFreeAgentTable();
        controls.AddChild(_freeAgencyPositionFilter);
        _freeAgencyColumnsMenu = new MenuButton { Text = "COLUMNS / VIEW", TooltipText = "Initial comparison columns use only tracked free-agent data. The anchored Player column remains first." };
        _freeAgencyColumnsMenu.GetPopup().AddItem("Player, Pos, Age, Overall, Potential, Ask, Status");
        _freeAgencyColumnsMenu.GetPopup().SetItemDisabled(0, true);
        controls.AddChild(_freeAgencyColumnsMenu);

        var tableScroll = new ScrollContainer { CustomMinimumSize = new Vector2(860, 390), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto };
        content.AddChild(tableScroll);
        _freeAgentList = new Tree { Columns = 7, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(960, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _freeAgentList.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1));
        _freeAgentList.SetColumnTitle(0, "PLAYER"); _freeAgentList.SetColumnTitle(1, "POS"); _freeAgentList.SetColumnTitle(2, "AGE"); _freeAgentList.SetColumnTitle(3, "OVR"); _freeAgentList.SetColumnTitle(4, "POT"); _freeAgentList.SetColumnTitle(5, "ASK / YR"); _freeAgentList.SetColumnTitle(6, "STATUS");
        for (var column = 0; column < 7; column++) _freeAgentList.SetColumnExpand(column, column == 0);
        _freeAgentList.SetColumnCustomMinimumWidth(0, 165); _freeAgentList.SetColumnCustomMinimumWidth(1, 48); _freeAgentList.SetColumnCustomMinimumWidth(2, 44); _freeAgentList.SetColumnCustomMinimumWidth(3, 45); _freeAgentList.SetColumnCustomMinimumWidth(4, 45); _freeAgentList.SetColumnCustomMinimumWidth(5, 78); _freeAgentList.SetColumnCustomMinimumWidth(6, 86);
        _freeAgentList.ItemSelected += OnFreeAgentSelected;
        _freeAgentList.ItemActivated += ShowFreeAgentNegotiation;
        _freeAgentList.ColumnTitleClicked += OnFreeAgentColumnTitleClicked;
        _freeAgentList.GuiInput += OnFreeAgentTableInput;
        tableScroll.AddChild(_freeAgentList);

        _freeAgencyStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_freeAgencyStatus);
        ApplyWorkstationTheme(_freeAgencyDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));

        _freeAgentContextMenu = new PopupMenu { Name = "FreeAgentContextMenu" };
        AddChild(_freeAgentContextMenu);
        _freeAgentContextMenu.AddItem("View Player Details", 1);
        _freeAgentContextMenu.AddItem("Make Offer", 2);
        _freeAgentContextMenu.IdPressed += id => { if (id == 1) ShowSelectedFreeAgentDetails(); else if (id == 2) ShowFreeAgentNegotiation(); };

        _freeAgentNegotiationDialog = new AcceptDialog { Name = "FreeAgentNegotiationDialog", Title = "Free Agency > Contract Negotiation", MinSize = new Vector2I(600, 470), Exclusive = false };
        AddChild(_freeAgentNegotiationDialog);
        _freeAgentNegotiationDialog.GetOkButton().Visible = false;
        var negotiation = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, AnchorsPreset = (int)LayoutPreset.FullRect, OffsetLeft = 12, OffsetTop = 10, OffsetRight = -12, OffsetBottom = -48 };
        _freeAgentNegotiationDialog.AddChild(negotiation);
        negotiation.AddChild(CreateMarketHeading("CONTRACT NEGOTIATION", "Set the complete offer here. The free-agency table remains a comparison list rather than a permanent offer panel."));
        _freeAgentNegotiationContext = new RichTextLabel { BbcodeEnabled = false, CustomMinimumSize = new Vector2(0, 145), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        negotiation.AddChild(_freeAgentNegotiationContext);
        _freeAgentAnnualOffer = CreateMoneyOffer(.25m, 40m);
        _freeAgentGuaranteeOffer = CreateMoneyOffer(0m, 80m);
        _freeAgentYearsOffer = new SpinBox { MinValue = 1, MaxValue = 5, Step = 1, Value = 2 };
        _freeAgentContractType = new OptionButton();
        _freeAgentContractType.ItemSelected += _ => RefreshFreeAgentNegotiationTerms();
        negotiation.AddChild(SetupRow("Contract type", _freeAgentContractType));
        negotiation.AddChild(SetupRow("Annual salary ($M)", _freeAgentAnnualOffer));
        negotiation.AddChild(SetupRow("Guaranteed ($M)", _freeAgentGuaranteeOffer));
        negotiation.AddChild(SetupRow("Contract years", _freeAgentYearsOffer));
        var negotiationActions = new HBoxContainer(); negotiation.AddChild(negotiationActions);
        _btnSubmitFreeAgentOffer = new Button { Text = "SUBMIT OFFER", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _btnSubmitFreeAgentOffer.Pressed += async () => await SubmitFreeAgentOffer(); negotiationActions.AddChild(_btnSubmitFreeAgentOffer);
        var cancelOffer = new Button { Text = "CANCEL" }; cancelOffer.Pressed += _freeAgentNegotiationDialog.Hide; negotiationActions.AddChild(cancelOffer);
        ApplyWorkstationTheme(_freeAgentNegotiationDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private static SpinBox CreateMoneyOffer(decimal minimum, decimal maximum)
        => new() { MinValue = (double)minimum, MaxValue = (double)maximum, Step = 0.25d, Rounded = false };

    private void CreateUdfaMarket()
    {
        _udfaMarketDialog = new AcceptDialog { Name = "UdfaMarketDialog", Title = "Scouting > UDFA Market", MinSize = new Vector2I(980, 620), Exclusive = false };
        AddChild(_udfaMarketDialog); _udfaMarketDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, AnchorsPreset = (int)LayoutPreset.FullRect, OffsetLeft = 12, OffsetTop = 10, OffsetRight = -12, OffsetBottom = -48 };
        _udfaMarketDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("UNDRAFTED FREE AGENTS", "Review the real post-draft rookie market. Contract offers and optional rookie-minicamp invitations are separate explicit actions."));
        _udfaMarketSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_udfaMarketSummary);
        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto }; content.AddChild(scroll);
        _udfaMarketTree = new Tree { Columns = 8, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(920, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var headers = new[] { "PLAYER", "POS", "AGE", "OVR", "POT", "PLAYSTYLE / TRAIT", "ASK / YR", "MINICAMP" }; var widths = new[] { 190, 50, 50, 50, 50, 190, 100, 115 };
        for (var column = 0; column < headers.Length; column++) { _udfaMarketTree.SetColumnTitle(column, headers[column]); _udfaMarketTree.SetColumnCustomMinimumWidth(column, widths[column]); _udfaMarketTree.SetColumnExpand(column, column is 0 or 5); }
        _udfaMarketTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); _udfaMarketTree.ItemSelected += OnUdfaSelected; _udfaMarketTree.ItemActivated += OpenSelectedUdfaOffer; scroll.AddChild(_udfaMarketTree);
        var actions = new HBoxContainer(); actions.AddThemeConstantOverride("separation", 8); content.AddChild(actions);
        _udfaOfferContract = new Button { Text = "OFFER CONTRACT", Disabled = true, TooltipText = "Open one negotiation with the required undrafted-rookie contract type included." }; _udfaOfferContract.Pressed += OpenSelectedUdfaOffer; actions.AddChild(_udfaOfferContract);
        _udfaInvite = new Button { Text = "INVITE TO ROOKIE MINICAMP", Disabled = true }; _udfaInvite.Pressed += async () => await ToggleSelectedUdfaInvitation(); actions.AddChild(_udfaInvite);
        var close = new Button { Text = "CLOSE", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; close.Pressed += _udfaMarketDialog.Hide; actions.AddChild(close);
        _udfaMarketStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_udfaMarketStatus);
        ApplyWorkstationTheme(_udfaMarketDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void ShowUdfaMarket()
    {
        RefreshUdfaMarket(); var viewport = GetViewportRect().Size; _udfaMarketDialog.PopupCentered(new Vector2I(Mathf.Clamp((int)(viewport.X * .92f), 760, 1240), Mathf.Clamp((int)(viewport.Y * .88f), 540, 760)));
    }

    private void RefreshUdfaMarket()
    {
        _selectedUdfaPlayerId = ""; _udfaMarketTree.Clear(); _udfaOfferContract.Disabled = true; _udfaInvite.Disabled = true;
        var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(candidate => candidate.TeamId == league.UserTeamId);
        if (league == null || team == null) { _udfaMarketSummary.Text = "No active franchise is loaded."; _udfaMarketStatus.Text = "Load a franchise to review undrafted rookies."; return; }
        var state = new RookieMinicampService(_nativeGameCoreContext).GetState(); var invited = state.InvitedPlayerIds.ToHashSet(StringComparer.OrdinalIgnoreCase); var players = league.FreeAgents.Where(UndraftedFreeAgentService.IsUndraftedRookie).OrderByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase).ToList(); var contracts = new ContractService(_nativeGameCoreContext);
        _udfaMarketSummary.Text = $"{team.Name} · Rookie Signing · {players.Count} unsigned UDFA(s) · Minicamp invitations {invited.Count}/{RookieMinicampService.InvitationLimit} · Active roster {team.Roster.Count}/{RosterService.RosterLimit} · Cap room {GameCoreStateHelper.FormatCapRoom(contracts.GetCapRoom(team))}";
        var root = _udfaMarketTree.CreateItem(); var index = 0;
        foreach (var player in players) { var row = _udfaMarketTree.CreateItem(root); row.SetMetadata(0, player.PlayerId); row.SetText(0, player.Name); row.SetText(1, player.Position); row.SetText(2, player.Age.ToString()); row.SetText(3, player.Overall.ToString()); row.SetText(4, player.Potential.ToString()); row.SetText(5, string.IsNullOrWhiteSpace(player.Trait) ? "No known trait" : player.Trait); row.SetText(6, GameCoreStateHelper.FormatCapRoom(contracts.GetRequiredAnnualSalary(player, team))); row.SetText(7, invited.Contains(player.PlayerId) ? "INVITED" : "—"); for (var column = 0; column < 8; column++) row.SetCustomBgColor(column, index % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); index++; }
        if (players.Count == 0) AddHistoryEmptyRow(_udfaMarketTree, root, string.Equals(league.Calendar?.Phase, ScheduleService.RookieSigningPendingPhase, StringComparison.OrdinalIgnoreCase) ? "No unsigned undrafted rookies remain." : "The UDFA market opens when the seven-round draft is complete.");
        _udfaMarketStatus.Text = string.Equals(league.Calendar?.Phase, ScheduleService.RookieSigningPendingPhase, StringComparison.OrdinalIgnoreCase) ? "Select a player to offer the supported three-year UDFA contract or manage one of ten optional minicamp invitations." : "UDFA actions are closed outside Rookie Signing.";
    }

    private void OnUdfaSelected()
    {
        var selected = _udfaMarketTree?.GetSelected(); _selectedUdfaPlayerId = selected == null || IsNil(selected.GetMetadata(0)) ? "" : selected.GetMetadata(0).AsString(); var league = _nativeGameCoreContext?.ActiveLeague; var open = !string.IsNullOrWhiteSpace(_selectedUdfaPlayerId) && string.Equals(league?.Calendar?.Phase, ScheduleService.RookieSigningPendingPhase, StringComparison.OrdinalIgnoreCase); _udfaOfferContract.Disabled = !open; _udfaInvite.Disabled = !open; if (!open) return; var invited = new RookieMinicampService(_nativeGameCoreContext).GetState().InvitedPlayerIds.Contains(_selectedUdfaPlayerId, StringComparer.OrdinalIgnoreCase); _udfaInvite.Text = invited ? "WITHDRAW MINICAMP INVITE" : "INVITE TO ROOKIE MINICAMP";
    }

    private void OpenSelectedUdfaOffer()
    {
        if (string.IsNullOrWhiteSpace(_selectedUdfaPlayerId)) return; _selectedFreeAgentId = _selectedUdfaPlayerId; ShowFreeAgentNegotiation();
    }

    private async Task ToggleSelectedUdfaInvitation()
    {
        if (string.IsNullOrWhiteSpace(_selectedUdfaPlayerId)) return; var service = new RookieMinicampService(_nativeGameCoreContext); var invited = service.GetState().InvitedPlayerIds.Contains(_selectedUdfaPlayerId, StringComparer.OrdinalIgnoreCase); var result = invited ? service.Withdraw(_selectedUdfaPlayerId) : service.Invite(_selectedUdfaPlayerId); _udfaMarketStatus.Text = result.Message; if (!result.Accepted) return; await SaveNativeAutosave("Native autosave updated."); RefreshUdfaMarket();
    }

    private void CreateTradeControls()
    {
        var actionRow = GetNodeOrNull<Container>("AppMargin/MainPadding/MainLayout/ActionButtonRow");
        if (actionRow == null)
            return;

        _btnTrades = new Button { Text = "Trades", TooltipText = "Submit a player and draft-pick proposal to one CPU team." };
        actionRow.AddChild(_btnTrades);
        _btnTrades.Pressed += ShowTradeDialog;
        _tradeDialog = new AcceptDialog { Name = "TradeDialog", Title = "Trade Center > Build a Trade", MinSize = new Vector2I(1080, 640), Exclusive = false };
        AddChild(_tradeDialog);
        _tradeDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = 12,
            OffsetTop = 10,
            OffsetRight = -12,
            OffsetBottom = -48,
        };
        _tradeDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("BUILD A TRADE", "Choose a partner, select real player/pick assets on each side, then review the opposing GM reaction before sending the offer."));
        _tradePartnerSelect = new OptionButton();
        content.AddChild(SetupRow("Counterparty", _tradePartnerSelect));
        _tradePartnerSelect.ItemSelected += _ => RefreshTradeAssets();
        content.AddChild(new Label { Text = "Select one or more assets on each side. Trades are available only in Free Agency and Draft Prep." });
        _tradePartnerEvaluation = new RichTextLabel { BbcodeEnabled = false, CustomMinimumSize = new Vector2(0, 54) };
        content.AddChild(_tradePartnerEvaluation);
        var assets = new HSplitContainer { CustomMinimumSize = new Vector2(0, 300), SizeFlagsVertical = Control.SizeFlags.ExpandFill, SplitOffset = 510 };
        content.AddChild(assets);
        var offered = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        assets.AddChild(offered);
        offered.AddChild(new Label { Text = "YOUR TEAM · OFFERED ASSETS" });
        _tradeOfferAssets = new ItemList { SelectMode = ItemList.SelectModeEnum.Multi, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _tradeOfferAssets.MultiSelected += (_, _) => UpdateTradePackagePreview();
        offered.AddChild(_tradeOfferAssets);
        var requested = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        assets.AddChild(requested);
        requested.AddChild(new Label { Text = "PARTNER TEAM · REQUESTED ASSETS" });
        _tradeRequestAssets = new ItemList { SelectMode = ItemList.SelectModeEnum.Multi, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _tradeRequestAssets.MultiSelected += (_, _) => UpdateTradePackagePreview();
        requested.AddChild(_tradeRequestAssets);
        var submit = new Button { Text = "SEND TRADE OFFER", CustomMinimumSize = new Vector2(0, 38) };
        content.AddChild(submit);
        submit.Pressed += async () => await SubmitTradeProposal();
        _tradeRationale = new RichTextLabel { BbcodeEnabled = false, CustomMinimumSize = new Vector2(0, 48) };
        content.AddChild(_tradeRationale);
        _tradeStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_tradeStatus);
        ApplyWorkstationTheme(_tradeDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void CreateWaiversControls()
    {
        _waiversDialog = new AcceptDialog { Name = "WaiversDialog", Title = "Trade Center > Waivers", MinSize = new Vector2I(900, 570), Exclusive = false };
        AddChild(_waiversDialog);
        _waiversDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = 12,
            OffsetTop = 10,
            OffsetRight = -12,
            OffsetBottom = -48,
        };
        _waiversDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("WAIVERS", "A short-lived claim market. Submit a claim here; a winning user opportunity is finalized or cancelled later from its Action Required inbox item."));
        _waiversSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_waiversSummary);
        var tableScroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto };
        content.AddChild(tableScroll);
        _waiversTree = new Tree { Columns = 6, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(760, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _waiversTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1));
        _waiversTree.SetColumnTitle(0, "PLAYER"); _waiversTree.SetColumnTitle(1, "POS"); _waiversTree.SetColumnTitle(2, "OVR"); _waiversTree.SetColumnTitle(3, "CONTRACT / CAP"); _waiversTree.SetColumnTitle(4, "WAIVER STATUS"); _waiversTree.SetColumnTitle(5, "EXPIRES");
        for (var column = 0; column < 6; column++) _waiversTree.SetColumnExpand(column, column == 0 || column == 4);
        _waiversTree.SetColumnCustomMinimumWidth(0, 160); _waiversTree.SetColumnCustomMinimumWidth(1, 45); _waiversTree.SetColumnCustomMinimumWidth(2, 45); _waiversTree.SetColumnCustomMinimumWidth(3, 105); _waiversTree.SetColumnCustomMinimumWidth(4, 115); _waiversTree.SetColumnCustomMinimumWidth(5, 90);
        _waiversTree.ItemSelected += OnWaiverMarketSelected;
        _waiversTree.ItemActivated += () => OnWaiverMarketSelected();
        tableScroll.AddChild(_waiversTree);
        var actions = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; actions.AddThemeConstantOverride("separation", 8); content.AddChild(actions);
        actions.AddChild(new Label { Text = "Conditional release:" });
        _waiverConditionalReleasePicker = new OptionButton { CustomMinimumSize = new Vector2(260, 0), TooltipText = "Released only if this claim is won and finalized." };
        _waiverConditionalReleasePicker.ItemSelected += _ => OnWaiverMarketSelected();
        actions.AddChild(_waiverConditionalReleasePicker);
        _btnClaimWaiverMarket = new Button { Text = "SUBMIT CLAIM", TooltipText = "Submit a claim without immediately transferring the player. A winning opportunity requires later confirmation." }; _btnClaimWaiverMarket.Pressed += async () => await ClaimSelectedWaiverMarket(); actions.AddChild(_btnClaimWaiverMarket);
        var refresh = new Button { Text = "REFRESH WAIVERS" }; refresh.Pressed += RefreshWaiversUi; actions.AddChild(refresh);
        var close = new Button { Text = "CLOSE", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; close.Pressed += _waiversDialog.Hide; actions.AddChild(close);
        _waiversStatus = new RichTextLabel { BbcodeEnabled = false, CustomMinimumSize = new Vector2(0, 54) }; content.AddChild(_waiversStatus);
        ApplyWorkstationTheme(_waiversDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void ShowWaiversDialog()
    {
        RefreshWaiversUi();
        var viewport = GetViewportRect().Size;
        _waiversDialog.PopupCentered(new Vector2I(Mathf.Clamp((int)(viewport.X * .92f), 720, 1240), Mathf.Clamp((int)(viewport.Y * .86f), 500, 740)));
    }

    private void RefreshWaiversUi()
    {
        EnsureNativeGameCoreServices();
        _waiversTree.Clear(); _selectedWaiverMarketPlayerId = ""; _btnClaimWaiverMarket.Disabled = true;
        var league = _nativeGameCoreContext?.ActiveLeague;
        var user = league?.Teams?.FirstOrDefault(team => string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || user == null)
        {
            _waiversSummary.Text = "Waiver market unavailable — no active franchise is loaded."; _waiversStatus.Text = "Load or start a franchise to review current waiver claims."; return;
        }
        _waiverConditionalReleasePicker.Clear();
        _waiverConditionalReleasePicker.AddItem("No conditional release");
        _waiverConditionalReleasePicker.SetItemMetadata(0, "");
        foreach (var rosterPlayer in user.Roster.OrderBy(player => player.Position, StringComparer.OrdinalIgnoreCase).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase))
        {
            _waiverConditionalReleasePicker.AddItem($"{rosterPlayer.Position} {rosterPlayer.Name} — {GameCoreStateHelper.FormatCapRoom(rosterPlayer.Contract?.AnnualSalary ?? 0m)}");
            _waiverConditionalReleasePicker.SetItemMetadata(_waiverConditionalReleasePicker.ItemCount - 1, rosterPlayer.PlayerId);
        }
        var activeWeek = league.Calendar?.AbsoluteWeek ?? 0;
        var activeWaivers = (league.Waivers ?? new List<WaiverClaimState>()).Where(waiver => waiver?.Player != null && (waiver.ExpiresAbsoluteWeek > activeWeek || waiver.PendingConfirmation)).ToList();
        _waiversSummary.Text = $"{user.Name} | Active roster {user.Roster.Count}/53 | Cap room {GameCoreStateHelper.FormatCapRoom(user.CapRoom)} | Active waiver claims {activeWaivers.Count}";
        if (activeWaivers.Count == 0)
        {
            _waiversStatus.Text = "NO PLAYERS ON WAIVERS\nThere are no active waiver claims in the current league week. Expired claims move to free agency when the game advances."; return;
        }
        var root = _waiversTree.CreateItem();
        var rowIndex = 0;
        foreach (var group in activeWaivers.GroupBy(waiver => waiver.WaivedByTeamId).OrderBy(group => league.Teams.FirstOrDefault(team => string.Equals(team.TeamId, group.Key, StringComparison.OrdinalIgnoreCase))?.Name ?? group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var waivedBy = league.Teams.FirstOrDefault(team => string.Equals(team.TeamId, group.Key, StringComparison.OrdinalIgnoreCase))?.Name ?? "Waiving team unavailable";
            var heading = _waiversTree.CreateItem(root); heading.SetText(0, waivedBy); heading.SetText(4, "WAIVED PLAYERS"); heading.SetSelectable(0, false);
            heading.SetCustomColor(0, new Color("8fcf98")); heading.SetCustomColor(4, new Color("8fcf98"));
            foreach (var waiver in group.OrderByDescending(item => item.Player.Overall).ThenBy(item => item.Player.Name, StringComparer.OrdinalIgnoreCase))
            {
                var player = waiver.Player; var expiresIn = waiver.ExpiresAbsoluteWeek - activeWeek;
                var row = _waiversTree.CreateItem(heading);
                var status = waiver.PendingConfirmation
                    ? string.Equals(waiver.PendingClaimTeamId, user.TeamId, StringComparison.OrdinalIgnoreCase) ? "AWAITING YOUR CONFIRMATION" : "PENDING LEAGUE DECISION"
                    : waiver.DeclinedTeamIds?.Any(teamId => string.Equals(teamId, user.TeamId, StringComparison.OrdinalIgnoreCase)) == true ? "DECLINED BY YOUR TEAM"
                    : waiver.Claims?.Any(claim => string.Equals(claim?.TeamId, user.TeamId, StringComparison.OrdinalIgnoreCase)) == true ? "YOUR CLAIM SUBMITTED" : "CLAIMS OPEN";
                row.SetText(0, player.Name); row.SetText(1, player.Position); row.SetText(2, player.Overall.ToString()); row.SetText(3, GameCoreStateHelper.FormatCapRoom(player.Contract?.AnnualSalary ?? 0m)); row.SetText(4, status); row.SetText(5, waiver.PendingConfirmation ? "PAUSED" : $"Week {waiver.ExpiresAbsoluteWeek} ({expiresIn} left)");
                row.SetMetadata(0, player.PlayerId);
                for (var column = 0; column < 6; column++) row.SetCustomBgColor(column, rowIndex % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
                row.SetTextAlignment(2, HorizontalAlignment.Right); row.SetTextAlignment(3, HorizontalAlignment.Right); rowIndex++;
            }
        }
        _waiversStatus.Text = "Select a player to review claim eligibility. Claims are blocked when the active roster is full, the inherited contract exceeds cap room, or the current phase disallows roster moves.";
    }

    private void OnWaiverMarketSelected()
    {
        var selected = _waiversTree?.GetSelected();
        if (selected == null || IsNil(selected.GetMetadata(0))) return;
        _selectedWaiverMarketPlayerId = selected.GetMetadata(0).AsString();
        var league = _nativeGameCoreContext?.ActiveLeague; var user = league?.Teams?.FirstOrDefault(team => string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var waiver = league?.Waivers?.FirstOrDefault(item => string.Equals(item?.Player?.PlayerId, _selectedWaiverMarketPlayerId, StringComparison.OrdinalIgnoreCase));
        if (user == null || waiver?.Player == null) return;
        var contracts = new ContractService(_nativeGameCoreContext);
        var conditionalReleaseId = GetSelectedWaiverConditionalReleaseId();
        var conditionalRelease = user.Roster.FirstOrDefault(player => string.Equals(player.PlayerId, conditionalReleaseId, StringComparison.OrdinalIgnoreCase));
        var projectedRosterCount = user.Roster.Count - (conditionalRelease == null ? 0 : 1) + 1;
        var projectedCapRoom = contracts.GetCapRoom(user) + Math.Max(0m, conditionalRelease?.Contract?.AnnualSalary ?? 0m);
        var reason = waiver.PendingConfirmation
            ? string.Equals(waiver.PendingClaimTeamId, user.TeamId, StringComparison.OrdinalIgnoreCase) ? "Your winning opportunity is awaiting finalization or cancellation from Action Required." : "This player already has a pending winning opportunity."
            : waiver.DeclinedTeamIds?.Any(teamId => string.Equals(teamId, user.TeamId, StringComparison.OrdinalIgnoreCase)) == true ? "Your team already declined this opportunity."
            : waiver.Claims?.Any(claim => string.Equals(claim?.TeamId, user.TeamId, StringComparison.OrdinalIgnoreCase)) == true ? "Your claim is submitted and will resolve in the original league waiver order when the period closes."
            : !ContractPhaseRules.CanManageRoster(league, out var phaseError) ? phaseError
            : projectedRosterCount > RosterService.RosterLimit ? $"Projected roster remains over {RosterService.RosterLimit}; select a conditional release."
            : (waiver.Player.Contract?.AnnualSalary ?? 0m) > projectedCapRoom ? "Inherited contract exceeds projected cap room, including the selected conditional release."
            : $"Eligible to submit. Projected final roster: {projectedRosterCount}/{RosterService.RosterLimit}; projected cap room before the claim: {GameCoreStateHelper.FormatCapRoom(projectedCapRoom)}. No transfer or release occurs until finalization.";
        _btnClaimWaiverMarket.Disabled = !reason.StartsWith("Eligible", StringComparison.Ordinal);
        _waiversStatus.Text = $"SELECTED | {waiver.Player.Name} ({waiver.Player.Position})\n{reason}\nShared profile routing for non-rostered waiver players is unavailable until the player is claimed; player facts are shown in this claim list.";
    }

    private async Task ClaimSelectedWaiverMarket()
    {
        if (string.IsNullOrWhiteSpace(_selectedWaiverMarketPlayerId)) { _waiversStatus.Text = "Select a waived player before claiming."; return; }
        var result = new TransactionService(_nativeGameCoreContext).SubmitWaiverClaim(_selectedWaiverMarketPlayerId, null, new ContractService(_nativeGameCoreContext), GetSelectedWaiverConditionalReleaseId());
        _waiversStatus.Text = result?.Message ?? "Waiver claim failed.";
        if (result?.Accepted != true) return;
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Pending waiver claim saved.", autosaveToo: true);
        await RefreshAll();
        RefreshWaiversUi();
        _waiversStatus.Text = $"CLAIM SUBMITTED\n{result.Message} Current cap room: {GameCoreStateHelper.FormatCapRoom(result.CapRoomAfterSigning)}";
    }

    private string GetSelectedWaiverConditionalReleaseId()
    {
        if (_waiverConditionalReleasePicker == null || _waiverConditionalReleasePicker.Selected < 0)
            return "";
        var metadata = _waiverConditionalReleasePicker.GetItemMetadata(_waiverConditionalReleasePicker.Selected);
        return IsNil(metadata) ? "" : metadata.AsString();
    }

    private void CreateLeagueTransactionsControls()
    {
        _leagueTransactionsDialog = new AcceptDialog { Name = "LeagueTransactionsDialog", Title = "Trade Center > League Transactions", MinSize = new Vector2I(1040, 620), Exclusive = false };
        AddChild(_leagueTransactionsDialog); _leagueTransactionsDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, AnchorsPreset = (int)LayoutPreset.FullRect, OffsetLeft = 12, OffsetTop = 10, OffsetRight = -12, OffsetBottom = -48 };
        _leagueTransactionsDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("LEAGUE TRANSACTIONS", "Authoritative chronological log of persisted player-movement and contract events. Live trade-market responses remain in Trade Block / Finder."));
        var filters = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; filters.AddThemeConstantOverride("separation", 6); content.AddChild(filters);
        filters.AddChild(HomeLabel("TYPE", 11, new Color("9cadb8"))); _leagueTransactionsTypeFilter = new OptionButton(); _leagueTransactionsTypeFilter.ItemSelected += _ => RenderLeagueTransactions(); filters.AddChild(_leagueTransactionsTypeFilter);
        filters.AddChild(HomeLabel("TEAM", 11, new Color("9cadb8"))); _leagueTransactionsTeamFilter = new OptionButton(); _leagueTransactionsTeamFilter.ItemSelected += _ => RenderLeagueTransactions(); filters.AddChild(_leagueTransactionsTeamFilter);
        filters.AddChild(HomeLabel("PLAYER", 11, new Color("9cadb8"))); _leagueTransactionsPlayerSearch = new LineEdit { PlaceholderText = "Filter player or summary", ClearButtonEnabled = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _leagueTransactionsPlayerSearch.TextChanged += _ => RenderLeagueTransactions(); filters.AddChild(_leagueTransactionsPlayerSearch);
        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto }; content.AddChild(scroll);
        _leagueTransactionsTree = new Tree { Columns = 6, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(980, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _leagueTransactionsTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1));
        _leagueTransactionsTree.SetColumnTitle(0, "DATE"); _leagueTransactionsTree.SetColumnTitle(1, "TYPE"); _leagueTransactionsTree.SetColumnTitle(2, "PLAYER"); _leagueTransactionsTree.SetColumnTitle(3, "TEAM"); _leagueTransactionsTree.SetColumnTitle(4, "SUMMARY / CONTEXT"); _leagueTransactionsTree.SetColumnTitle(5, "PHASE");
        for (var column = 0; column < 6; column++) _leagueTransactionsTree.SetColumnExpand(column, column == 4);
        _leagueTransactionsTree.SetColumnCustomMinimumWidth(0, 110); _leagueTransactionsTree.SetColumnCustomMinimumWidth(1, 130); _leagueTransactionsTree.SetColumnCustomMinimumWidth(2, 150); _leagueTransactionsTree.SetColumnCustomMinimumWidth(3, 145); _leagueTransactionsTree.SetColumnCustomMinimumWidth(4, 300); _leagueTransactionsTree.SetColumnCustomMinimumWidth(5, 115);
        _leagueTransactionsTree.ItemActivated += () => _ = OpenSelectedLeagueTransactionContext(); scroll.AddChild(_leagueTransactionsTree);
        var actions = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; content.AddChild(actions); var refresh = new Button { Text = "REFRESH LOG" }; refresh.Pressed += RefreshLeagueTransactionsUi; actions.AddChild(refresh); var close = new Button { Text = "CLOSE", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; close.Pressed += _leagueTransactionsDialog.Hide; actions.AddChild(close);
        _leagueTransactionsStatus = new RichTextLabel { BbcodeEnabled = false, CustomMinimumSize = new Vector2(0, 42) }; content.AddChild(_leagueTransactionsStatus);
        ApplyWorkstationTheme(_leagueTransactionsDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void ShowLeagueTransactionsDialog()
    {
        RefreshLeagueTransactionsUi(); var viewport = GetViewportRect().Size; _leagueTransactionsDialog.PopupCentered(new Vector2I(Mathf.Clamp((int)(viewport.X * .94f), 760, 1320), Mathf.Clamp((int)(viewport.Y * .88f), 540, 780)));
    }

    private void RefreshLeagueTransactionsUi()
    {
        EnsureNativeGameCoreServices(); var league = _nativeGameCoreContext?.ActiveLeague;
        var priorType = _leagueTransactionsTypeFilter?.Selected > 0 ? _leagueTransactionsTypeFilter.GetItemText(_leagueTransactionsTypeFilter.Selected) : "";
        var priorTeamId = _leagueTransactionsTeamFilter?.Selected > 0 ? _leagueTransactionsTeamFilter.GetItemMetadata(_leagueTransactionsTeamFilter.Selected).ToString() : "";
        _leagueTransactionsTypeFilter.Clear(); _leagueTransactionsTeamFilter.Clear(); _leagueTransactionsTypeFilter.AddItem("All types"); _leagueTransactionsTeamFilter.AddItem("All teams");
        if (league == null) { _leagueTransactionsTree.Clear(); _leagueTransactionsStatus.Text = "No active franchise is loaded. Load or start a franchise to view its persistent league transaction log."; return; }
        foreach (var type in (league.Transactions ?? new List<TransactionRecord>()).Where(item => !string.IsNullOrWhiteSpace(item?.Type)).Select(item => item.Type).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(type => type, StringComparer.OrdinalIgnoreCase)) _leagueTransactionsTypeFilter.AddItem(type);
        foreach (var team in league.Teams.Where(team => team != null).OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase)) { _leagueTransactionsTeamFilter.AddItem(team.Name); _leagueTransactionsTeamFilter.SetItemMetadata(_leagueTransactionsTeamFilter.ItemCount - 1, team.TeamId); }
        for (var index = 1; index < _leagueTransactionsTypeFilter.ItemCount; index++) if (string.Equals(_leagueTransactionsTypeFilter.GetItemText(index), priorType, StringComparison.OrdinalIgnoreCase)) { _leagueTransactionsTypeFilter.Select(index); break; }
        for (var index = 1; index < _leagueTransactionsTeamFilter.ItemCount; index++) if (string.Equals(_leagueTransactionsTeamFilter.GetItemMetadata(index).ToString(), priorTeamId, StringComparison.OrdinalIgnoreCase)) { _leagueTransactionsTeamFilter.Select(index); break; }
        RenderLeagueTransactions();
    }

    private void RenderLeagueTransactions()
    {
        if (_leagueTransactionsTree == null) return; _leagueTransactionsTree.Clear(); var league = _nativeGameCoreContext?.ActiveLeague; if (league == null) return;
        var selectedType = _leagueTransactionsTypeFilter.Selected <= 0 ? "" : _leagueTransactionsTypeFilter.GetItemText(_leagueTransactionsTypeFilter.Selected);
        var selectedTeamId = _leagueTransactionsTeamFilter.Selected <= 0 ? "" : _leagueTransactionsTeamFilter.GetItemMetadata(_leagueTransactionsTeamFilter.Selected).ToString();
        var search = _leagueTransactionsPlayerSearch?.Text?.Trim() ?? "";
        var records = (league.Transactions ?? new List<TransactionRecord>()).Where(record => record != null && (string.IsNullOrWhiteSpace(selectedType) || string.Equals(record.Type, selectedType, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(selectedTeamId) || string.Equals(record.TeamId, selectedTeamId, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(search) || (record.PlayerName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) || (record.Details ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))).OrderByDescending(record => record.SeasonYear).ThenByDescending(record => record.TransactionId, StringComparer.OrdinalIgnoreCase).ToList();
        var root = _leagueTransactionsTree.CreateItem(); var index = 0;
        foreach (var record in records)
        {
            var row = _leagueTransactionsTree.CreateItem(root); row.SetText(0, string.IsNullOrWhiteSpace(record.DateLabel) ? "Date unavailable" : record.DateLabel); row.SetText(1, string.IsNullOrWhiteSpace(record.Type) ? "Type unavailable" : record.Type); row.SetText(2, string.IsNullOrWhiteSpace(record.PlayerName) ? "Player unavailable" : record.PlayerName); row.SetText(3, string.IsNullOrWhiteSpace(record.TeamName) ? "Team unavailable" : record.TeamName); row.SetText(4, string.IsNullOrWhiteSpace(record.Details) ? "Recorded detail unavailable" : record.Details); row.SetText(5, string.IsNullOrWhiteSpace(record.Phase) ? "Phase unavailable" : record.Phase); row.SetMetadata(0, record.PlayerId); row.SetMetadata(1, record.TeamId); row.SetMetadata(2, record.TransactionId);
            for (var column = 0; column < 6; column++) row.SetCustomBgColor(column, index % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); index++;
        }
        _leagueTransactionsStatus.Text = index == 0 ? "NO MATCHING TRANSACTIONS\nNo persisted transaction records match the active filters." : $"{index} persisted transaction record(s) shown · newest first. Select an entry to open supported player/team context.";
    }

    private async Task OpenSelectedLeagueTransactionContext()
    {
        var selected = _leagueTransactionsTree?.GetSelected(); if (selected == null) return; var playerId = IsNil(selected.GetMetadata(0)) ? "" : selected.GetMetadata(0).AsString(); var teamId = IsNil(selected.GetMetadata(1)) ? "" : selected.GetMetadata(1).AsString();
        if (!string.IsNullOrWhiteSpace(playerId)) { _leagueTransactionsDialog.Hide(); await SelectMainTab(ROSTER_TAB_INDEX); await TrySelectTeamInRoster(teamId); TrySelectRosterPlayer(playerId); return; }
        _leagueTransactionsStatus.Text = "The selected record has no persisted player route. Its recorded team, date, type, phase, and detail remain visible in the log.";
    }

    private void CreateTradeFinderControls()
    {
        _tradeFinderDialog = new AcceptDialog { Name = "TradeFinderDialog", Title = "Trade Center > Trade Block / Finder", MinSize = new Vector2I(940, 620), Exclusive = false };
        AddChild(_tradeFinderDialog);
        _tradeFinderDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = 12,
            OffsetTop = 10,
            OffsetRight = -12,
            OffsetBottom = -48,
        };
        _tradeFinderDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("TRADE BLOCK / FINDER", "Select up to eight owned players or unused picks, optionally name a position to target, then submit the package. Offers appear only after submission."));
        _tradeFinderSelectionStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_tradeFinderSelectionStatus);
        var requestRow = new HBoxContainer(); requestRow.AddChild(new Label { Text = "OPTIONAL RETURN POSITION" });
        _tradeFinderRequestedPosition = new OptionButton();
        foreach (var position in new[] { "Any", "QB", "RB", "WR", "TE", "OL", "DL", "LB", "CB", "S", "K", "P" }) _tradeFinderRequestedPosition.AddItem(position);
        requestRow.AddChild(_tradeFinderRequestedPosition); content.AddChild(requestRow);

        var split = new HSplitContainer { CustomMinimumSize = new Vector2(0, 350), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, SplitOffset = 470 };
        content.AddChild(split);
        var assets = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        assets.AddThemeConstantOverride("separation", 5);
        split.AddChild(assets);
        assets.AddChild(HomeLabel("YOUR TRADEABLE ASSETS", 13, new Color("f4eddf")));
        assets.AddChild(new Label { Text = "Select one or more assets you are willing to move. Session selection is carried into Build a Trade; no trade occurs here.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        _tradeFinderAssets = new ItemList { SelectMode = ItemList.SelectModeEnum.Multi, SizeFlagsVertical = Control.SizeFlags.ExpandFill, AllowReselect = true };
        _tradeFinderAssets.MultiSelected += (_, _) => UpdateTradeFinderSelection();
        assets.AddChild(_tradeFinderAssets);

        var offers = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        offers.AddThemeConstantOverride("separation", 5);
        split.AddChild(offers);
        offers.AddChild(HomeLabel("INTERESTED GM OFFERS", 13, new Color("f4eddf")));
        _tradeFinderOffers = new Tree { Columns = 3, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _tradeFinderOffers.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1));
        _tradeFinderOffers.SetColumnTitle(0, "PARTNER"); _tradeFinderOffers.SetColumnTitle(1, "CORE OFFER"); _tradeFinderOffers.SetColumnTitle(2, "STATUS");
        _tradeFinderOffers.SetColumnExpand(0, true); _tradeFinderOffers.SetColumnExpand(1, true); _tradeFinderOffers.SetColumnExpand(2, true);
        _tradeFinderOffers.SetColumnCustomMinimumWidth(0, 110); _tradeFinderOffers.SetColumnCustomMinimumWidth(1, 180); _tradeFinderOffers.SetColumnCustomMinimumWidth(2, 115);
        _tradeFinderOffers.ItemSelected += UpdateTradeMarketOfferActions;
        _tradeFinderOffers.ItemActivated += () => _ = AcceptSelectedTradeMarketOffer();
        offers.AddChild(_tradeFinderOffers);
        _tradeFinderStatus = new RichTextLabel { BbcodeEnabled = false, CustomMinimumSize = new Vector2(0, 58) };
        offers.AddChild(_tradeFinderStatus);

        var actions = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        actions.AddThemeConstantOverride("separation", 8);
        content.AddChild(actions);
        var clear = new Button { Text = "CLEAR SELECTION" }; clear.Pressed += ClearTradeFinderSelection; actions.AddChild(clear);
        var refresh = new Button { Text = "SUBMIT ASSETS TO MARKET" }; refresh.Pressed += async () => await SubmitTradeFinderMarket(); actions.AddChild(refresh);
        _tradeFinderAcceptOffer = new Button { Text = "ACCEPT OFFER", Disabled = true, TooltipText = "Accept the selected concrete offer and complete the validated trade." };
        _tradeFinderAcceptOffer.Pressed += async () => await AcceptSelectedTradeMarketOffer(); actions.AddChild(_tradeFinderAcceptOffer);
        _tradeFinderRejectOffer = new Button { Text = "REJECT OFFER", Disabled = true };
        _tradeFinderRejectOffer.Pressed += async () => await RejectSelectedTradeMarketOffer(); actions.AddChild(_tradeFinderRejectOffer);
        _tradeFinderWithdraw = new Button { Text = "WITHDRAW PACKAGE", Disabled = true };
        _tradeFinderWithdraw.Pressed += async () => await WithdrawTradeMarketPackage(); actions.AddChild(_tradeFinderWithdraw);
        var build = new Button { Text = "OPEN BUILD A TRADE", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, TooltipText = "Carry the selected user assets into the proposal builder." };
        build.Pressed += OpenTradeFinderSelectionInBuilder;
        actions.AddChild(build);
        var close = new Button { Text = "CLOSE" }; close.Pressed += _tradeFinderDialog.Hide; actions.AddChild(close);
        ApplyWorkstationTheme(_tradeFinderDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void ShowTradeFinderDialog()
    {
        RefreshTradeFinderUi();
        var viewport = GetViewportRect().Size;
        _tradeFinderDialog.PopupCentered(new Vector2I(
            Mathf.Clamp((int)(viewport.X * 0.92f), 760, 1240),
            Mathf.Clamp((int)(viewport.Y * 0.88f), 540, 760)));
    }

    private void RefreshTradeFinderUi()
    {
        EnsureNativeGameCoreServices();
        var league = _nativeGameCoreContext?.ActiveLeague;
        _tradeFinderAssets.Clear();
        _tradeFinderOffers.Clear();
        if (league == null)
        {
            _tradeFinderSelectionStatus.Text = "No active franchise is loaded.";
            _tradeFinderStatus.Text = "Load or start a franchise to search the trade market.";
            return;
        }
        new DraftService(_nativeGameCoreContext).PrepareDraftBoard();
        var user = league.Teams.FirstOrDefault(team => string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        PopulateTradeAssets(_tradeFinderAssets, league, user);
        for (var index = 0; index < _tradeFinderAssets.ItemCount; index++)
        {
            var key = _tradeFinderAssets.GetItemMetadata(index).ToString();
            _tradeFinderAssets.Select(index, _tradeFinderSelectedAssets.Contains(key, StringComparer.Ordinal));
        }
        UpdateTradeFinderSelection();
        RefreshTradeFinderInterest();
    }

    private void UpdateTradeFinderSelection()
    {
        _tradeFinderSelectedAssets.Clear();
        if (_tradeFinderAssets != null)
            foreach (var index in _tradeFinderAssets.GetSelectedItems())
                _tradeFinderSelectedAssets.Add(_tradeFinderAssets.GetItemMetadata(index).ToString());
        var count = _tradeFinderSelectedAssets.Count;
        _tradeFinderSelectionStatus.Text = count == 0
            ? "SELECTED ASSETS: none · Select real players or unused picks to begin a market check."
            : count > TradeMarketService.MaxSubmittedAssets
                ? $"SELECTED ASSETS: {count} · Reduce the package to {TradeMarketService.MaxSubmittedAssets} assets before submission."
                : $"SELECTED ASSETS: {count}/{TradeMarketService.MaxSubmittedAssets} · No offers are generated until you submit this package.";
    }

    private void ClearTradeFinderSelection()
    {
        _tradeFinderAssets?.DeselectAll();
        UpdateTradeFinderSelection();
        RefreshTradeFinderInterest();
    }

    private void RefreshTradeFinderInterest()
    {
        _tradeFinderOffers?.Clear();
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league == null)
        {
            _tradeFinderStatus.Text = "MARKET STATUS\nNo active franchise is loaded.";
            return;
        }
        RenderTradeMarketState(new TradeMarketService(_nativeGameCoreContext).GetState());
    }

    private async Task SubmitTradeFinderMarket()
    {
        UpdateTradeFinderSelection();
        var players = new List<string>(); var picks = new List<int>();
        foreach (var asset in _tradeFinderSelectedAssets)
        {
            if (asset.StartsWith("P:", StringComparison.Ordinal)) players.Add(asset[2..]);
            else if (asset.StartsWith("K:", StringComparison.Ordinal) && int.TryParse(asset[2..], out var pick)) picks.Add(pick);
        }
        var position = _tradeFinderRequestedPosition != null && _tradeFinderRequestedPosition.Selected >= 0
            ? _tradeFinderRequestedPosition.GetItemText(_tradeFinderRequestedPosition.Selected)
            : "Any";
        var response = new TradeMarketService(_nativeGameCoreContext).Submit(players, picks, position);
        RenderTradeMarketState(response);
        if (!response.Ok) { SetPrimaryStatus(response.Error); return; }
        await SaveNativeAutosave("Native autosave updated.");
        SetPrimaryStatus(response.Offers.Count > 0 ? $"Trade package submitted; {response.Offers.Count} offer(s) received." : "Trade package submitted; no teams made an offer.");
    }

    private void RenderTradeMarketState(TradeMarketResponse response)
    {
        _tradeFinderOffers.Clear();
        _selectedTradeMarketOfferId = "";
        UpdateTradeMarketOfferActions();
        if (response?.Ok != true)
        {
            _tradeFinderStatus.Text = $"MARKET STATUS\n{response?.Error ?? "Trade market unavailable."}";
            return;
        }
        if (!response.Submitted)
        {
            if (_tradeFinderWithdraw != null) _tradeFinderWithdraw.Disabled = true;
            _tradeFinderStatus.Text = "MARKET STATUS\nNo package has been submitted. Select assets and use Submit Assets to Market; selection alone never generates offers.";
            return;
        }
        if (_tradeFinderWithdraw != null) _tradeFinderWithdraw.Disabled = false;
        SelectSetupOption(_tradeFinderRequestedPosition, string.IsNullOrWhiteSpace(response.RequestedPosition) ? "Any" : response.RequestedPosition);
        var root = _tradeFinderOffers.CreateItem();
        foreach (var offer in response.Offers)
        {
            var row = _tradeFinderOffers.CreateItem(root); row.SetText(0, offer.PartnerTeamName); row.SetText(1, string.Join(" + ", offer.PartnerAssets)); row.SetText(2, offer.Status.ToUpperInvariant()); row.SetMetadata(0, offer.OfferId); row.SetMetadata(1, offer.PartnerTeamId);
        }
        var submitted = string.Join(" + ", response.OfferedAssets);
        var target = string.IsNullOrWhiteSpace(response.RequestedPosition) ? "best available return" : response.RequestedPosition;
        _tradeFinderStatus.Text = response.Offers.Count == 0
            ? $"SUBMITTED {response.SubmittedDate}\n{submitted}\nTarget: {target}. No clubs submitted a valid offer."
            : $"SUBMITTED {response.SubmittedDate} · {response.Offers.Count} RECEIVED OFFER(S)\nShopping: {submitted}\nTarget: {target}. Select an open response to accept or reject it, or withdraw the full package.";
    }

    private void UpdateTradeMarketOfferActions()
    {
        var selected = _tradeFinderOffers?.GetSelected();
        _selectedTradeMarketOfferId = selected == null || IsNil(selected.GetMetadata(0)) ? "" : selected.GetMetadata(0).AsString();
        var isOpen = selected != null && string.Equals(selected.GetText(2), "OPEN", StringComparison.OrdinalIgnoreCase);
        if (_tradeFinderAcceptOffer != null) _tradeFinderAcceptOffer.Disabled = !isOpen;
        if (_tradeFinderRejectOffer != null) _tradeFinderRejectOffer.Disabled = !isOpen;
    }

    private async Task AcceptSelectedTradeMarketOffer()
    {
        UpdateTradeMarketOfferActions();
        if (string.IsNullOrWhiteSpace(_selectedTradeMarketOfferId)) return;
        var result = new TradeMarketService(_nativeGameCoreContext).AcceptOffer(_selectedTradeMarketOfferId);
        if (!result.Ok || !result.Accepted)
        {
            _tradeFinderStatus.Text = $"OFFER NOT COMPLETED\n{result.Message}\n{result.Rationale}";
            SetPrimaryStatus(result.Message);
            return;
        }
        RenderTradeMarketState(new TradeMarketService(_nativeGameCoreContext).GetState());
        if (string.Equals(_nativeGameCoreContext?.ActiveLeague?.Calendar?.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase) && _draftBoardDialog?.Visible == true)
            RefreshDraftBoard();
        await SaveNativeAutosave("Native autosave updated.");
        SetPrimaryStatus("Trade-market offer accepted and completed.");
    }

    private async Task RejectSelectedTradeMarketOffer()
    {
        UpdateTradeMarketOfferActions();
        if (string.IsNullOrWhiteSpace(_selectedTradeMarketOfferId)) return;
        var response = new TradeMarketService(_nativeGameCoreContext).RejectOffer(_selectedTradeMarketOfferId);
        RenderTradeMarketState(response);
        if (!response.Ok) { SetPrimaryStatus(response.Error); return; }
        await SaveNativeAutosave("Native autosave updated.");
        SetPrimaryStatus("Trade-market offer rejected.");
    }

    private async Task WithdrawTradeMarketPackage()
    {
        var response = new TradeMarketService(_nativeGameCoreContext).Withdraw();
        RenderTradeMarketState(response);
        if (!response.Ok) { SetPrimaryStatus(response.Error); return; }
        await SaveNativeAutosave("Native autosave updated.");
        SetPrimaryStatus("Trade-market package withdrawn.");
    }

    private void OpenTradeFinderSelectionInBuilder()
    {
        UpdateTradeFinderSelection();
        _pendingTradeOfferAssets.Clear();
        _pendingTradeOfferAssets.AddRange(_tradeFinderSelectedAssets);
        _tradeFinderDialog.Hide();
        ShowTradeDialog();
    }

    private void ShowTradeDialog()
    {
        RefreshTradeUi();
        var viewport = GetViewportRect().Size;
        var dialogSize = new Vector2I(
            Mathf.Clamp((int)(viewport.X * 0.92f), 900, 1240),
            Mathf.Clamp((int)(viewport.Y * 0.88f), 560, 760));
        _tradeDialog.PopupCentered(dialogSize);
    }

    private void RefreshTradeUi()
    {
        EnsureNativeGameCoreServices();
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league == null)
        {
            _tradeStatus.Text = "Start or load a franchise to propose a trade.";
            return;
        }

        new DraftService(_nativeGameCoreContext).PrepareDraftBoard();
        _tradePartnerSelect.Clear();
        foreach (var team in league.Teams.Where(team => team != null && !string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase)).OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase))
        {
            _tradePartnerSelect.AddItem(team.Name);
            _tradePartnerSelect.SetItemMetadata(_tradePartnerSelect.ItemCount - 1, team.TeamId);
        }
        RefreshTradeAssets();
        _tradeRationale.Text = "Select assets to preview package value plus projected cap and roster effects. No league state changes until an accepted submission.";
        _tradeStatus.Text = ContractPhaseRules.CanProposeTrades(league, out var error) ? "Build an offer, then submit it explicitly. No roster changes occur until a proposal is accepted." : error;
    }

    private void RefreshTradeAssets()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league == null || _tradePartnerSelect.ItemCount == 0)
            return;
        var user = league.Teams.FirstOrDefault(team => string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var partnerId = _tradePartnerSelect.GetItemMetadata(_tradePartnerSelect.Selected).ToString();
        var partner = league.Teams.FirstOrDefault(team => string.Equals(team.TeamId, partnerId, StringComparison.OrdinalIgnoreCase));
        PopulateTradeAssets(_tradeOfferAssets, league, user);
        PopulateTradeAssets(_tradeRequestAssets, league, partner);
        ApplyPendingTradeFinderAssets();
        var evaluation = new FrontOfficeEvaluationService(_nativeGameCoreContext).EvaluateTeam(partnerId);
        _tradePartnerEvaluation.Text = evaluation.Ok
            ? $"COUNTERPARTY FRONT OFFICE REPORT | {evaluation.TeamName}\nCap room: {GameCoreStateHelper.FormatCapRoom(evaluation.CapRoom)} | Roster: {evaluation.RosterSize}/53 | Avg age: {evaluation.AverageAge} | Avg potential: {evaluation.AveragePotential} | Expiring: {evaluation.ExpiringContracts} | Picks: {evaluation.DraftPicksAvailable}\n{evaluation.Rationale}"
            : evaluation.Error;
        UpdateTradePackagePreview();
    }

    private void ApplyPendingTradeFinderAssets()
    {
        if (_tradeOfferAssets == null || _pendingTradeOfferAssets.Count == 0)
            return;
        for (var index = 0; index < _tradeOfferAssets.ItemCount; index++)
        {
            var assetKey = _tradeOfferAssets.GetItemMetadata(index).ToString();
            if (_pendingTradeOfferAssets.Contains(assetKey, StringComparer.Ordinal))
                _tradeOfferAssets.Select(index, false);
        }
        _pendingTradeOfferAssets.Clear();
    }

    private void UpdateTradePackagePreview()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league == null || _tradeOfferAssets == null || _tradeRequestAssets == null || _tradeRationale == null) return;
        if (_tradePartnerSelect.ItemCount == 0) return;
        var proposal = new TradeProposal { ProposingTeamId = league.UserTeamId, ReceivingTeamId = _tradePartnerSelect.GetItemMetadata(_tradePartnerSelect.Selected).ToString() };
        AddSelectedTradeAssets(_tradeOfferAssets, proposal.ProposingPlayerIds, proposal.ProposingPickOverallNumbers);
        AddSelectedTradeAssets(_tradeRequestAssets, proposal.ReceivingPlayerIds, proposal.ReceivingPickOverallNumbers);
        var preview = new TransactionService(_nativeGameCoreContext).PreviewUserTradeProposal(proposal, new ContractService(_nativeGameCoreContext));
        _tradeRationale.Text = preview.Ok ? $"PACKAGE PREVIEW\n{preview.Rationale}\n{preview.Message}\nFinal transaction validation and the counterparty decision occur only after explicit submission." : $"PACKAGE PREVIEW\n{preview.Message}";
    }

    private static void PopulateTradeAssets(ItemList list, LeagueState league, TeamState team)
    {
        list.Clear();
        if (team == null)
            return;
        foreach (var player in team.Roster.OrderByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase))
        {
            list.AddItem($"Player | {player.Position,-4} {player.Name,-24} OVR {player.Overall,2} Age {player.Age,2}");
            list.SetItemMetadata(list.ItemCount - 1, $"P:{player.PlayerId}");
        }
        foreach (var pick in league.Draft.Picks.Where(pick => pick != null && string.Equals(pick.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(pick.ProspectId)).OrderBy(pick => pick.OverallPick))
        {
            list.AddItem($"Pick   | Round {pick.Round}, pick {pick.PickInRound} (overall {pick.OverallPick}) | originally {pick.OriginalTeamId}");
            list.SetItemMetadata(list.ItemCount - 1, $"K:{pick.OverallPick}");
        }
    }

    private async Task SubmitTradeProposal()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league == null || _tradePartnerSelect.ItemCount == 0)
            return;
        var proposal = new TradeProposal { ProposingTeamId = league.UserTeamId, ReceivingTeamId = _tradePartnerSelect.GetItemMetadata(_tradePartnerSelect.Selected).ToString() };
        AddSelectedTradeAssets(_tradeOfferAssets, proposal.ProposingPlayerIds, proposal.ProposingPickOverallNumbers);
        AddSelectedTradeAssets(_tradeRequestAssets, proposal.ReceivingPlayerIds, proposal.ReceivingPickOverallNumbers);
        var result = new TransactionService(_nativeGameCoreContext).SubmitUserTradeProposal(proposal, new ContractService(_nativeGameCoreContext));
        _tradeStatus.Text = result.Message;
        _tradeRationale.Text = result.Rationale;
        if (!result.Accepted)
            return;

        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Trade proposal saved.", autosaveToo: true);
        await RefreshAll();
        RefreshTradeUi();
        _tradeStatus.Text = result.Message;
        _tradeRationale.Text = result.Rationale;
    }

    private static void AddSelectedTradeAssets(ItemList list, List<string> playerIds, List<int> pickNumbers)
    {
        foreach (var index in list.GetSelectedItems())
        {
            var value = list.GetItemMetadata(index).ToString();
            if (value.StartsWith("P:", StringComparison.Ordinal))
                playerIds.Add(value[2..]);
            else if (value.StartsWith("K:", StringComparison.Ordinal) && int.TryParse(value[2..], out var overallPick))
                pickNumbers.Add(overallPick);
        }
    }

    private void ShowFreeAgency()
    {
        RefreshFreeAgencyUi();
        var viewport = GetViewportRect().Size;
        _freeAgencyDialog.PopupCentered(new Vector2I(Mathf.Clamp((int)(viewport.X * .92f), 760, 1240), Mathf.Clamp((int)(viewport.Y * .88f), 540, 760)));
    }

    private void RefreshFreeAgencyUi()
    {
        EnsureNativeGameCoreServices();
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || team == null)
        {
            _freeAgencyCapSummary.Text = "Start or load a franchise to access free agency.";
            _freeAgentList.Clear();
            return;
        }

        var contracts = new ContractService(_nativeGameCoreContext);
        var phaseStatus = ContractPhaseRules.GetStatus(league);
        _freeAgencyCapSummary.Text = $"{team.Name} | Cap room: {GameCoreStateHelper.FormatCapRoom(contracts.GetCapRoom(team))} | Active roster: {team.Roster.Count}/53 | Free agents: {league.FreeAgents.Count}\n{phaseStatus.Explanation}";
        _selectedFreeAgentId = "";
        _freeAgencyStatus.Text = phaseStatus.CanSignFreeAgents ? "Right-click a player and choose Make Offer. Selecting a row alone never submits or reveals an offer panel." : phaseStatus.Explanation;
        RenderFreeAgentTable();
    }

    private void RenderFreeAgentTable()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || team == null || _freeAgentList == null)
            return;
        var search = _freeAgencySearch?.Text?.Trim() ?? "";
        var position = _freeAgencyPositionFilter == null || _freeAgencyPositionFilter.Selected <= 0 ? "" : _freeAgencyPositionFilter.GetItemText(_freeAgencyPositionFilter.Selected);
        var contracts = new ContractService(_nativeGameCoreContext);
        IEnumerable<PlayerState> players = league.FreeAgents.Where(player => player != null && (string.IsNullOrWhiteSpace(search) || player.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(position) || string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase)));
        players = _freeAgentSortColumn switch
        {
            1 => _freeAgentSortDescending ? players.OrderByDescending(player => player.Position) : players.OrderBy(player => player.Position),
            2 => _freeAgentSortDescending ? players.OrderByDescending(player => player.Age) : players.OrderBy(player => player.Age),
            3 => _freeAgentSortDescending ? players.OrderByDescending(player => player.Overall) : players.OrderBy(player => player.Overall),
            4 => _freeAgentSortDescending ? players.OrderByDescending(player => player.Potential) : players.OrderBy(player => player.Potential),
            5 => _freeAgentSortDescending ? players.OrderByDescending(player => contracts.GetRequiredAnnualSalary(player, team)) : players.OrderBy(player => contracts.GetRequiredAnnualSalary(player, team)),
            _ => _freeAgentSortDescending ? players.OrderByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase) : players.OrderBy(player => player.Name, StringComparer.OrdinalIgnoreCase),
        };
        _freeAgentList.Clear();
        var root = _freeAgentList.CreateItem();
        var rowIndex = 0;
        foreach (var player in players)
        {
            var asking = contracts.GetRequiredAnnualSalary(player, team) / 1_000_000m;
            var item = _freeAgentList.CreateItem(root);
            item.SetText(0, player.Name); item.SetText(1, player.Position); item.SetText(2, player.Age.ToString()); item.SetText(3, player.Overall.ToString()); item.SetText(4, player.Potential.ToString()); item.SetText(5, $"${asking:0.00}M"); item.SetText(6, string.IsNullOrWhiteSpace(player.Status) ? "Unavailable" : player.Status);
            item.SetMetadata(0, player.PlayerId);
            for (var column = 0; column < 7; column++) item.SetCustomBgColor(column, rowIndex % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
            for (var column = 2; column <= 5; column++) item.SetTextAlignment(column, HorizontalAlignment.Right);
            rowIndex++;
        }
        if (rowIndex == 0) _freeAgencyStatus.Text = "No free agents match the current search and position filters.";
    }

    private void OnFreeAgentSelected()
    {
        var selected = _freeAgentList?.GetSelected();
        if (selected == null || IsNil(selected.GetMetadata(0))) return;
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || team == null) return;
        _selectedFreeAgentId = selected.GetMetadata(0).AsString();
        var player = league.FreeAgents.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, _selectedFreeAgentId, StringComparison.OrdinalIgnoreCase));
        if (player == null) return;
        var required = new ContractService(_nativeGameCoreContext).GetRequiredAnnualSalary(player, team);
        _freeAgentAnnualOffer.Value = (double)(required / 1_000_000m); _freeAgentGuaranteeOffer.Value = (double)(required * 0.30m / 1_000_000m); _freeAgentYearsOffer.Value = 2; _freeAgentYearsOffer.Editable = true;
        _freeAgencyStatus.Text = $"Selected: {player.Name} · {player.Position} · OVR {player.Overall} · estimated ask ${required / 1_000_000m:0.00}M/year. Right-click for player details or Make Offer.";
    }

    private void OnFreeAgentColumnTitleClicked(long column, long _) { if (column < 0 || column > 6) return; _freeAgentSortDescending = _freeAgentSortColumn == column ? !_freeAgentSortDescending : column != 0; _freeAgentSortColumn = (int)column; RenderFreeAgentTable(); }
    private void OnFreeAgentTableInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouse || mouse.ButtonIndex != MouseButton.Right || !mouse.Pressed || _freeAgentList == null) return;
        var item = _freeAgentList.GetItemAtPosition(mouse.Position);
        if (item == null || IsNil(item.GetMetadata(0))) return;
        item.Select(0); OnFreeAgentSelected();
        var popupPosition = _freeAgentList.GetScreenPosition() + mouse.Position;
        _freeAgentContextMenu.Position = new Vector2I(Mathf.RoundToInt(popupPosition.X), Mathf.RoundToInt(popupPosition.Y));
        _freeAgentContextMenu.Popup();
        GetViewport().SetInputAsHandled();
    }

    private void ShowSelectedFreeAgentDetails()
    {
        var player = _nativeGameCoreContext?.ActiveLeague?.FreeAgents?.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, _selectedFreeAgentId, StringComparison.OrdinalIgnoreCase));
        if (player == null) return;
        var team = _nativeGameCoreContext.ActiveLeague.Teams.First(candidate => string.Equals(candidate.TeamId, _nativeGameCoreContext.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var required = new ContractService(_nativeGameCoreContext).GetRequiredAnnualSalary(player, team);
        _freeAgencyStatus.Text = $"{player.Name} · {player.Position} · Age {player.Age} · OVR {player.Overall} · Potential {player.Potential} · {player.Status} · Morale {player.Morale} ({player.MoraleTrend}) · estimated ask {GameCoreStateHelper.FormatCapRoom(required)}/year.";
    }

    private void ShowFreeAgentNegotiation()
    {
        if (string.IsNullOrWhiteSpace(_selectedFreeAgentId)) return;
        var league = _nativeGameCoreContext?.ActiveLeague;
        var player = league?.FreeAgents?.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, _selectedFreeAgentId, StringComparison.OrdinalIgnoreCase));
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (player == null || team == null) return;
        var canSignActive = ContractPhaseRules.CanSignFreeAgent(league, player, out var activeError);
        var canOfferPracticeSquad = player.Age <= 25 && ContractPhaseRules.CanManageRoster(league, out _);
        if (!canSignActive && !canOfferPracticeSquad) { SetPrimaryStatus(activeError); return; }
        var required = new ContractService(_nativeGameCoreContext).GetRequiredAnnualSalary(player, team);
        var isUdfa = UndraftedFreeAgentService.IsUndraftedRookie(player);
        _freeAgentContractType.Clear();
        if (canSignActive)
        {
            _freeAgentContractType.AddItem(isUdfa ? "Undrafted Rookie Contract" : "Active Roster");
            _freeAgentContractType.SetItemMetadata(_freeAgentContractType.ItemCount - 1, "active");
        }
        if (canOfferPracticeSquad && !isUdfa)
        {
            _freeAgentContractType.AddItem("Practice Squad");
            _freeAgentContractType.SetItemMetadata(_freeAgentContractType.ItemCount - 1, "practice_squad");
        }
        _freeAgentContractType.Select(0);
        _freeAgentAnnualOffer.Value = (double)(required / 1_000_000m);
        _freeAgentGuaranteeOffer.Value = (double)(required * 0.30m / 1_000_000m);
        _freeAgentYearsOffer.Value = isUdfa ? 3 : 2;
        _freeAgentYearsOffer.Editable = !isUdfa;
        RefreshFreeAgentNegotiationTerms();
        _btnSubmitFreeAgentOffer.Disabled = false;
        _freeAgentNegotiationDialog.PopupCentered(new Vector2I(600, 470));
    }

    private async Task SubmitFreeAgentOffer()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        if (league == null || string.IsNullOrWhiteSpace(_selectedFreeAgentId))
            return;

        var service = new ContractService(_nativeGameCoreContext);
        var contractType = GetSelectedFreeAgentContractType();
        var result = contractType == "practice_squad"
            ? new TransactionService(_nativeGameCoreContext).SignToPracticeSquad(_selectedFreeAgentId, league.UserTeamId, service, (decimal)_freeAgentAnnualOffer.Value * 1_000_000m)
            : service.SignFreeAgent(_selectedFreeAgentId, league.UserTeamId, new ContractOffer
            {
                AnnualSalary = (decimal)_freeAgentAnnualOffer.Value * 1_000_000m,
                GuaranteedSalary = (decimal)_freeAgentGuaranteeOffer.Value * 1_000_000m,
                Years = (int)_freeAgentYearsOffer.Value,
            });
        var transactionMessage = result.Accepted
            ? $"Accepted: {result.Message} Cap room after signing: {GameCoreStateHelper.FormatCapRoom(result.CapRoomAfterSigning)}"
            : $"{result.Message} Estimated requirement: {GameCoreStateHelper.FormatCapRoom(result.RequiredAnnualSalary)}";
        _freeAgencyStatus.Text = transactionMessage;
        _freeAgentNegotiationContext.Text += $"\n\nLATEST RESPONSE\n{transactionMessage}";
        if (!result.Accepted)
            return;

        _freeAgentNegotiationDialog.Hide();
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Free-agent signing saved.", autosaveToo: true);
        await RefreshAll();
        RefreshFreeAgencyUi();
        if (_udfaMarketDialog?.Visible == true) RefreshUdfaMarket();
        _freeAgencyStatus.Text = transactionMessage;
    }

    private string GetSelectedFreeAgentContractType()
    {
        if (_freeAgentContractType == null || _freeAgentContractType.Selected < 0)
            return "active";
        var metadata = _freeAgentContractType.GetItemMetadata(_freeAgentContractType.Selected);
        return IsNil(metadata) ? "active" : metadata.AsString();
    }

    private void RefreshFreeAgentNegotiationTerms()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var player = league?.FreeAgents?.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, _selectedFreeAgentId, StringComparison.OrdinalIgnoreCase));
        if (player == null || team == null || _freeAgentNegotiationContext == null)
            return;
        var practiceSquad = GetSelectedFreeAgentContractType() == "practice_squad";
        var requirement = practiceSquad
            ? new TransactionService(_nativeGameCoreContext).GetPracticeSquadRequiredSalary(player.PlayerId, team.TeamId)
            : new ContractService(_nativeGameCoreContext).GetRequiredAnnualSalary(player, team);
        _freeAgentAnnualOffer.Value = (double)(requirement / 1_000_000m);
        _freeAgentGuaranteeOffer.Value = practiceSquad ? 0 : (double)(requirement * .30m / 1_000_000m);
        _freeAgentGuaranteeOffer.Editable = !practiceSquad;
        _freeAgentYearsOffer.Value = practiceSquad ? 1 : UndraftedFreeAgentService.IsUndraftedRookie(player) ? 3 : 2;
        _freeAgentYearsOffer.Editable = !practiceSquad && !UndraftedFreeAgentService.IsUndraftedRookie(player);
        var typeLabel = practiceSquad ? "Practice Squad · required 1 year" : UndraftedFreeAgentService.IsUndraftedRookie(player) ? "Undrafted Rookie Contract · required 3 years" : "Active Roster";
        _freeAgentNegotiationContext.Text = $"{player.Name} · {player.Position} · Age {player.Age} · OVR {player.Overall} · Potential {player.Potential}\nStatus: {player.Status} · Morale: {player.Morale} ({player.MoraleTrend})\nContract type: {typeLabel}\nEstimated requirement: {GameCoreStateHelper.FormatCapRoom(requirement)} per year\nTeam cap room: {GameCoreStateHelper.FormatCapRoom(new ContractService(_nativeGameCoreContext).GetCapRoom(team))}\n\nSubmitting is the only action that can sign the player. Closing this window changes nothing.";
    }

    private void CreateRosterManagementControls()
    {
        var actionRow = GetNodeOrNull<Container>("AppMargin/MainPadding/MainLayout/ActionButtonRow");
        if (actionRow == null)
            return;

        _btnRosterManagement = new Button { Text = "Roster Moves", TooltipText = "Claim waivers, manage the practice squad, and browse transactions." };
        actionRow.AddChild(_btnRosterManagement);
        _btnRosterManagement.Pressed += ShowRosterManagement;

        _rosterManagementDialog = new AcceptDialog { Name = "RosterManagementDialog", Title = "Waivers, Practice Squad & Ledger", MinSize = new Vector2I(1040, 650), Exclusive = false };
        AddChild(_rosterManagementDialog);
        _rosterManagementDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _rosterManagementDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("ROSTER TRANSACTIONS", "Claims, practice-squad signings, and permanent active-roster promotions are explicit actions validated for ownership, eligibility, cap, roster limit, and phase."));
        var tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        content.AddChild(tabs);

        var waiversTab = new VBoxContainer { Name = "Waivers" };
        tabs.AddChild(waiversTab);
        waiversTab.AddChild(new Label { Text = "Claiming a player inherits the existing contract and must satisfy roster and cap rules." });
        _waiverClaimList = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill, AllowReselect = true };
        waiversTab.AddChild(_waiverClaimList);
        _waiverClaimList.ItemSelected += index => _selectedWaiverPlayerId = _waiverClaimList.GetItemMetadata((int)index).ToString();
        var claimButton = new Button { Text = "Submit Claim" };
        waiversTab.AddChild(claimButton);
        claimButton.Pressed += async () => await ClaimSelectedWaiver();
        var waiverDecisionRow = new HBoxContainer(); waiverDecisionRow.AddThemeConstantOverride("separation", 8); waiversTab.AddChild(waiverDecisionRow);
        var finalizeClaim = new Button { Text = "FINALIZE PENDING CLAIM", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; finalizeClaim.Pressed += async () => await FinalizeSelectedWaiverClaim(); waiverDecisionRow.AddChild(finalizeClaim);
        var cancelClaim = new Button { Text = "CANCEL PENDING CLAIM" }; cancelClaim.Pressed += async () => await CancelSelectedWaiverClaim(); waiverDecisionRow.AddChild(cancelClaim);

        var practiceTab = new VBoxContainer { Name = "Practice Squad" };
        tabs.AddChild(practiceTab);
        practiceTab.AddChild(new Label { Text = "Eligible free agents are age 25 or younger. Submit a one-year practice-squad offer; the player weighs salary and positional opportunity. No signing is automatic." });
        var practiceSplit = new HSplitContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        practiceTab.AddChild(practiceSplit);
        var candidates = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        practiceSplit.AddChild(candidates);
        candidates.AddChild(new Label { Text = "Eligible Free Agents" });
        _practiceSquadFreeAgentList = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill, AllowReselect = true };
        candidates.AddChild(_practiceSquadFreeAgentList);
        _practiceSquadFreeAgentList.ItemSelected += OnPracticeSquadCandidateSelected;
        var offerRow = new HBoxContainer(); candidates.AddChild(offerRow);
        offerRow.AddChild(new Label { Text = "Annual offer:" });
        _practiceSquadAnnualOffer = new SpinBox { MinValue = 250_000, MaxValue = 1_000_000, Step = 25_000, Value = 300_000, CustomMinimumSize = new Vector2(170, 0) };
        offerRow.AddChild(_practiceSquadAnnualOffer);
        var signButton = new Button { Text = "Make Practice Squad Offer" };
        candidates.AddChild(signButton);
        signButton.Pressed += async () => await SignSelectedPracticeSquadPlayer();
        var squad = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        practiceSplit.AddChild(squad);
        squad.AddChild(new Label { Text = "Current Practice Squad" });
        _practiceSquadList = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill, AllowReselect = true };
        squad.AddChild(_practiceSquadList);
        _practiceSquadList.ItemSelected += index => _selectedPracticeSquadPlayerId = _practiceSquadList.GetItemMetadata((int)index).ToString();
        var elevateButton = new Button { Text = "Sign Permanently to Active Roster" };
        squad.AddChild(elevateButton);
        elevateButton.Pressed += async () => await ElevateSelectedPracticeSquadPlayer();

        var transactionsTab = new VBoxContainer { Name = "Transactions" };
        tabs.AddChild(transactionsTab);
        _transactionHistoryText = new RichTextLabel { BbcodeEnabled = false, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        transactionsTab.AddChild(_transactionHistoryText);
        _rosterManagementStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_rosterManagementStatus);
        ApplyWorkstationTheme(_rosterManagementDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));

        _practiceSquadActiveSigningDialog = new ConfirmationDialog { Title = "Confirm Permanent Active-Roster Signing", MinSize = new Vector2I(600, 430) };
        _practiceSquadActiveSigningDialog.GetOkButton().Text = "SIGN TO ACTIVE ROSTER";
        AddChild(_practiceSquadActiveSigningDialog);
        _practiceSquadActiveSigningDetails = new RichTextLabel { BbcodeEnabled = false, FitContent = false, CustomMinimumSize = new Vector2(560, 320), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _practiceSquadActiveSigningDialog.AddChild(_practiceSquadActiveSigningDetails);
        _practiceSquadActiveSigningDialog.Confirmed += async () => await ConfirmPracticeSquadActiveSigning();
    }

    private void ShowRosterManagement()
    {
        RefreshRosterManagementUi();
        _rosterManagementDialog.PopupCentered(new Vector2I(1040, 650));
    }

    private void RefreshRosterManagementUi()
    {
        EnsureNativeGameCoreServices();
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || team == null)
        {
            _rosterManagementStatus.Text = "Start or load a franchise to manage roster moves.";
            return;
        }

        _selectedWaiverPlayerId = "";
        _selectedPracticeSquadFreeAgentId = "";
        _selectedPracticeSquadPlayerId = "";
        _waiverClaimList.Clear();
        foreach (var waiver in league.Waivers.Where(waiver => waiver?.Player != null).OrderByDescending(waiver => waiver.Player.Overall))
        {
            var player = waiver.Player;
            var conditionalRelease = team.Roster.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, waiver.ConditionalReleasePlayerId, StringComparison.OrdinalIgnoreCase));
            var submitted = waiver.Claims?.Any(claim => string.Equals(claim?.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase)) == true;
            var pending = waiver.PendingConfirmation ? $"  PENDING CONFIRMATION: {waiver.PendingClaimTeamId}{(conditionalRelease == null ? "" : $" | Release if finalized: {conditionalRelease.Name}")}" : submitted ? "  YOUR CLAIM SUBMITTED" : "";
            _waiverClaimList.AddItem($"{player.Position,-4} {player.Name,-24} OVR {player.Overall,2}  Contract {GameCoreStateHelper.FormatCapRoom(player.Contract?.AnnualSalary ?? 0m)}  Expires week {waiver.ExpiresAbsoluteWeek}{pending}");
            _waiverClaimList.SetItemMetadata(_waiverClaimList.ItemCount - 1, player.PlayerId);
            if (waiver.PendingConfirmation && string.Equals(waiver.PendingClaimTeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase))
            {
                _selectedWaiverPlayerId = player.PlayerId;
                _waiverClaimList.Select(_waiverClaimList.ItemCount - 1);
            }
        }
        _practiceSquadFreeAgentList.Clear();
        foreach (var player in league.FreeAgents.Where(player => player.Age <= 25).OrderByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase))
        {
            var requiredSalary = new TransactionService(_nativeGameCoreContext).GetPracticeSquadRequiredSalary(player.PlayerId, team.TeamId);
            _practiceSquadFreeAgentList.AddItem($"{player.Position,-4} {player.Name,-24} OVR {player.Overall,2}  Age {player.Age}  Ask {GameCoreStateHelper.FormatCapRoom(requiredSalary)}");
            _practiceSquadFreeAgentList.SetItemMetadata(_practiceSquadFreeAgentList.ItemCount - 1, player.PlayerId);
        }
        _practiceSquadList.Clear();
        foreach (var player in (team.PracticeSquad ?? new List<PlayerState>()).OrderByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase))
        {
            _practiceSquadList.AddItem($"{player.Position,-4} {player.Name,-24} OVR {player.Overall,2}  Age {player.Age}");
            _practiceSquadList.SetItemMetadata(_practiceSquadList.ItemCount - 1, player.PlayerId);
        }

        var history = _nativeDashboardService.GetTransactionHistory();
        _transactionHistoryText.Text = history.Ok && history.Transactions.Count > 0
            ? string.Join("\n", history.Transactions.Select(transaction => $"{transaction.DateLabel} | {transaction.TeamName} | {transaction.PlayerName} | {transaction.Type}: {transaction.Details}"))
            : "No transactions recorded.";
        var topCallUp = (team.PracticeSquad ?? new List<PlayerState>()).OrderByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        var callUpContext = team.Roster.Count >= RosterService.RosterLimit
            ? "Call-up guidance: no active-roster opening; release or move a player before a permanent signing."
            : topCallUp == null
                ? "Call-up guidance: no practice-squad player is available for a permanent active-roster signing."
                : $"Call-up guidance: active opening available; highest-rated permanent-signing option is {topCallUp.Name} ({topCallUp.Position}, OVR {topCallUp.Overall}).";
        _rosterManagementStatus.Text = $"{team.Name}: active {team.Roster.Count}/53, practice squad {team.PracticeSquad.Count}/16, waivers {league.Waivers.Count}.\n{callUpContext}";
    }

    private async Task ClaimSelectedWaiver()
    {
        if (string.IsNullOrWhiteSpace(_selectedWaiverPlayerId))
        {
            _rosterManagementStatus.Text = "Select a waived player first.";
            return;
        }
        var result = new TransactionService(_nativeGameCoreContext).SubmitWaiverClaim(_selectedWaiverPlayerId, null, new ContractService(_nativeGameCoreContext));
        await CompleteRosterManagementAction(result, "Pending waiver claim saved.");
    }

    private async Task FinalizeSelectedWaiverClaim()
    {
        if (string.IsNullOrWhiteSpace(_selectedWaiverPlayerId)) { _rosterManagementStatus.Text = "Select the player with the pending waiver confirmation."; return; }
        var result = new TransactionService(_nativeGameCoreContext).FinalizeWaiverClaim(_selectedWaiverPlayerId, null, new ContractService(_nativeGameCoreContext));
        await CompleteRosterManagementAction(result, "Waiver claim finalized.");
    }

    private async Task CancelSelectedWaiverClaim()
    {
        if (string.IsNullOrWhiteSpace(_selectedWaiverPlayerId)) { _rosterManagementStatus.Text = "Select the player with the pending waiver confirmation."; return; }
        var result = new TransactionService(_nativeGameCoreContext).CancelWaiverClaim(_selectedWaiverPlayerId, null, new ContractService(_nativeGameCoreContext));
        await CompleteRosterManagementAction(result, "Waiver opportunity cancelled.");
    }

    private async Task SignSelectedPracticeSquadPlayer()
    {
        if (string.IsNullOrWhiteSpace(_selectedPracticeSquadFreeAgentId))
        {
            _rosterManagementStatus.Text = "Select an eligible free agent first.";
            return;
        }
        var result = new TransactionService(_nativeGameCoreContext).SignToPracticeSquad(_selectedPracticeSquadFreeAgentId, null, new ContractService(_nativeGameCoreContext), (decimal)(_practiceSquadAnnualOffer?.Value ?? 0));
        await CompleteRosterManagementAction(result, "Practice-squad signing saved.");
    }

    private void OnPracticeSquadCandidateSelected(long index)
    {
        _selectedPracticeSquadFreeAgentId = _practiceSquadFreeAgentList.GetItemMetadata((int)index).ToString();
        var league = _nativeGameCoreContext?.ActiveLeague;
        var requiredSalary = new TransactionService(_nativeGameCoreContext).GetPracticeSquadRequiredSalary(_selectedPracticeSquadFreeAgentId, league?.UserTeamId);
        if (_practiceSquadAnnualOffer != null && requiredSalary > 0m)
            _practiceSquadAnnualOffer.Value = (double)requiredSalary;
        if (_rosterManagementStatus != null && requiredSalary > 0m)
            _rosterManagementStatus.Text = $"Practice-squad ask: {GameCoreStateHelper.FormatCapRoom(requiredSalary)} for one year. The requirement reflects the player's ability, morale, and opportunity at their position.";
    }

    private async Task ElevateSelectedPracticeSquadPlayer()
    {
        if (string.IsNullOrWhiteSpace(_selectedPracticeSquadPlayerId))
        {
            _rosterManagementStatus.Text = "Select a practice-squad player first.";
            return;
        }
        ShowPracticeSquadActiveSigningConfirmation(_selectedPracticeSquadPlayerId);
        await Task.CompletedTask;
    }

    private void ShowPracticeSquadActiveSigningConfirmation(string playerId)
    {
        var preview = new ContractService(_nativeGameCoreContext).PreviewPracticeSquadActiveSigning(playerId);
        if (!preview.Ok)
        {
            if (_rosterManagementStatus != null) _rosterManagementStatus.Text = preview.Error;
            if (_practiceSquadWorkspaceStatus != null) _practiceSquadWorkspaceStatus.Text = preview.Error;
            SetPrimaryStatus(preview.Error);
            return;
        }

        _practiceSquadActiveSigningPlayerId = preview.PlayerId;
        _practiceSquadActiveSigningDetails.Text = $"{preview.PlayerName} · {preview.Position}\n\nTHIS IS A PERMANENT ACTIVE-ROSTER SIGNING\nThe player leaves the practice squad immediately. This is not a temporary game-day elevation and has no automatic reversion.\n\nCONTRACT\n{preview.CurrentContractType}: {GameCoreStateHelper.FormatCapRoom(preview.CurrentAnnualSalary)} annually\nActive Roster: {GameCoreStateHelper.FormatCapRoom(preview.NewAnnualSalary)} annually\n\nROSTER AND CAP\nActive roster: {preview.RosterCountBefore} → {preview.RosterCountAfter}\nCap room: {GameCoreStateHelper.FormatCapRoom(preview.CapRoomBefore)} → {GameCoreStateHelper.FormatCapRoom(preview.CapRoomAfter)}\n\nConfirmation revalidates phase, ownership, cap room, and the active-roster limit, then records the transaction.";
        _practiceSquadActiveSigningDialog.PopupCentered(new Vector2I(600, 430));
    }

    private async Task ConfirmPracticeSquadActiveSigning()
    {
        if (string.IsNullOrWhiteSpace(_practiceSquadActiveSigningPlayerId)) return;
        var result = new ContractService(_nativeGameCoreContext).SignPracticeSquadPlayerToActiveRoster(_practiceSquadActiveSigningPlayerId);
        if (_rosterManagementStatus != null) _rosterManagementStatus.Text = result.Message;
        if (_practiceSquadWorkspaceStatus != null) _practiceSquadWorkspaceStatus.Text = result.Message;
        SetPrimaryStatus(result.Message);
        if (!result.Accepted) return;

        _practiceSquadActiveSigningPlayerId = "";
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Permanent practice-squad promotion saved.", autosaveToo: true);
        await RefreshAll();
        if (_rosterManagementDialog?.Visible == true) RefreshRosterManagementUi();
        if (_practiceSquadViewActive) RenderPracticeSquadWorkspace();
    }

    private async Task CompleteRosterManagementAction(ContractTransactionResult result, string saveMessage)
    {
        _rosterManagementStatus.Text = result?.Message ?? "Roster action failed.";
        if (result?.Accepted != true)
            return;

        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, saveMessage, autosaveToo: true);
        await RefreshAll();
        RefreshRosterManagementUi();
    }

    private void CreateTrainingCampControls()
    {
        var actionRow = GetNodeOrNull<Container>("AppMargin/MainPadding/MainLayout/ActionButtonRow");
        if (actionRow == null)
            return;

        _btnTrainingCamp = new Button { Text = "Training Camp", TooltipText = "Choose a position focus and finalize the legal roster." };
        actionRow.AddChild(_btnTrainingCamp);
        _btnTrainingCamp.Pressed += ShowTrainingCamp;
        _trainingCampDialog = new AcceptDialog { Name = "TrainingCampDialog", Title = "Training Camp", MinSize = new Vector2I(660, 560), Exclusive = false };
        AddChild(_trainingCampDialog);
        _trainingCampDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _trainingCampDialog.AddChild(content);
        content.AddChild(new Label { Text = "Choose one position group for focused camp reps. The focus improves readiness and eligible player development once.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var positionFocus = new Button { Text = "Open Position Groups" };
        content.AddChild(positionFocus); positionFocus.Pressed += ShowTrainingCampPositionFocus;
        var playerFocus = new Button { Text = "Open Player Focus" };
        content.AddChild(playerFocus); playerFocus.Pressed += ShowTrainingCampPlayerFocus;
        var refreshReport = new Button { Text = "Open Weekly Camp Report" };
        content.AddChild(refreshReport);
        refreshReport.Pressed += ShowTrainingCampWeeklyReport;
        _trainingCampReport = new RichTextLabel { BbcodeEnabled = false, CustomMinimumSize = new Vector2(600, 190), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        content.AddChild(_trainingCampReport);
        _trainingCampRoles = new RichTextLabel { BbcodeEnabled = false, CustomMinimumSize = new Vector2(600, 150), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        content.AddChild(_trainingCampRoles);
        var cutdown = new Button { Text = "Open Final Roster Cut-Down" };
        content.AddChild(cutdown);
        cutdown.Pressed += ShowFinalCutdown;
        var finalize = new Button { Text = "Finalize Legal Roster" };
        content.AddChild(finalize);
        finalize.Pressed += async () => await FinalizeTrainingCampRoster();
        _trainingCampStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        content.AddChild(_trainingCampStatus);
        CreateTrainingCampPositionFocusDialog();
        CreateTrainingCampPlayerFocusDialog();
        CreateTrainingCampWeeklyReportDialog();
        CreateFinalCutdownDialog();
    }

    private void CreateTrainingCampPositionFocusDialog()
    {
        _trainingCampPositionFocusDialog = new AcceptDialog { Name = "TrainingCampPositionFocusDialog", Title = "Training Camp > Position Groups", MinSize = new Vector2I(1020, 620), Exclusive = false };
        AddChild(_trainingCampPositionFocusDialog); _trainingCampPositionFocusDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, AnchorsPreset = (int)LayoutPreset.FullRect, OffsetLeft = 12, OffsetTop = 10, OffsetRight = -12, OffsetBottom = -48 };
        _trainingCampPositionFocusDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("POSITION GROUPS", "Compare depth, availability, upside, and fatigue before committing the one bounded position-group focus. Staff recommendations inform the choice but never select it for the GM."));
        _trainingCampPositionFocusSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_trainingCampPositionFocusSummary);
        _trainingCampPositionFocusTree = new Tree { Columns = 9, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(980, 430), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        ConfigureTrainingCampReportTree(_trainingCampPositionFocusTree, new[] { "POS", "PLAYERS", "NEED", "AVAILABLE", "OUT", "AVG OVR", "AVG POT", "AVG FAT", "STAFF ASSESSMENT / STATUS" }, new[] { 58, 70, 58, 78, 48, 70, 70, 70, 360 }); _trainingCampPositionFocusTree.ItemSelected += OnTrainingCampPositionFocusSelected; content.AddChild(_trainingCampPositionFocusTree);
        var actions = new HBoxContainer(); actions.AddThemeConstantOverride("separation", 8); content.AddChild(actions);
        _trainingCampApplyPositionFocus = new Button { Text = "APPLY POSITION FOCUS", Disabled = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _trainingCampApplyPositionFocus.Pressed += async () => await ApplyTrainingCampFocus(); actions.AddChild(_trainingCampApplyPositionFocus);
        var close = new Button { Text = "CLOSE" }; close.Pressed += _trainingCampPositionFocusDialog.Hide; actions.AddChild(close);
        _trainingCampPositionFocusStatus = new Label { Text = "Select a position group to review the current recommendation. Selection alone changes nothing.", AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_trainingCampPositionFocusStatus);
        ApplyWorkstationTheme(_trainingCampPositionFocusDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void ShowTrainingCampPositionFocus()
    {
        RenderTrainingCampPositionFocus(); var viewport = GetViewportRect().Size; _trainingCampPositionFocusDialog.PopupCentered(new Vector2I(Mathf.Clamp((int)(viewport.X * .92f), 820, 1320), Mathf.Clamp((int)(viewport.Y * .86f), 540, 780)));
    }

    private void RenderTrainingCampPositionFocus()
    {
        _trainingCampSelectedFocusPosition = ""; _trainingCampPositionFocusTree.Clear(); _trainingCampApplyPositionFocus.Disabled = true;
        var league = _nativeGameCoreContext?.ActiveLeague; var team = GameCoreStateHelper.GetUserTeam(league); if (league == null || team == null) { _trainingCampPositionFocusSummary.Text = "No active franchise roster is available."; return; }
        var camp = new TrainingCampService(_nativeGameCoreContext).GetStatus(team.TeamId).Status; var report = TrainingCampReportService.Build(team); _trainingCampPositionFocusSummary.Text = $"{team.Name} · Active roster {team.Roster.Count}/{RosterService.RosterLimit} · Phase {league.Calendar?.Phase ?? "Unavailable"} · Position focus {(camp.FocusApplied ? camp.FocusPosition : "not assigned")}\n{report.Summary}";
        var counts = team.Roster.GroupBy(player => player.Position, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase); var root = _trainingCampPositionFocusTree.CreateItem(); var index = 0;
        foreach (var position in report.Positions)
        {
            var focused = string.Equals(camp.FocusPosition, position.Position, StringComparison.OrdinalIgnoreCase); var row = _trainingCampPositionFocusTree.CreateItem(root); row.SetMetadata(0, position.Position); row.SetText(0, position.Position); row.SetText(1, counts.GetValueOrDefault(position.Position).ToString()); row.SetText(2, position.RequiredStarters.ToString()); row.SetText(3, position.AvailablePlayers.ToString()); row.SetText(4, position.UnavailablePlayers.ToString()); row.SetText(5, position.AverageOverall.ToString()); row.SetText(6, position.AveragePotential.ToString()); row.SetText(7, position.AverageFatigue.ToString()); row.SetText(8, focused ? $"FOCUSED · {position.Recommendation}" : position.Recommendation); for (var column = 0; column < 9; column++) row.SetCustomBgColor(column, focused ? new Color("193d37") : index % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); if (focused) row.SetCustomColor(8, new Color("f0c96a")); index++;
        }
        if (index == 0) { var empty = _trainingCampPositionFocusTree.CreateItem(root); empty.SetText(0, "No active-roster position groups are available."); }
        _trainingCampPositionFocusStatus.Text = camp.FocusApplied ? $"Position focus is already committed to {camp.FocusPosition} for this camp." : "Select one position group. Applying focus is the only action that changes state.";
    }

    private void OnTrainingCampPositionFocusSelected()
    {
        var selected = _trainingCampPositionFocusTree?.GetSelected(); _trainingCampSelectedFocusPosition = selected == null || IsNil(selected.GetMetadata(0)) ? "" : selected.GetMetadata(0).AsString(); var team = GameCoreStateHelper.GetUserTeam(_nativeGameCoreContext?.ActiveLeague); var camp = team == null ? null : new TrainingCampService(_nativeGameCoreContext).GetStatus(team.TeamId).Status; var report = team == null ? null : TrainingCampReportService.Build(team).Positions.FirstOrDefault(position => string.Equals(position.Position, _trainingCampSelectedFocusPosition, StringComparison.OrdinalIgnoreCase)); var eligible = report != null && report.AvailablePlayers > 0 && camp?.IsAvailable == true && camp.FocusApplied == false; _trainingCampApplyPositionFocus.Disabled = !eligible; _trainingCampPositionFocusStatus.Text = report == null ? "Select a position group to review its recommendation." : camp?.FocusApplied == true ? $"Position focus is already committed to {camp.FocusPosition}." : report.AvailablePlayers <= 0 ? $"{report.Position} has no available active-roster players and cannot receive the focus." : !eligible ? "Position focus is available only during Training Camp Pending." : $"{report.Position}: {report.Recommendation} Applying focus improves readiness for up to three available players and can add at most one overall point per affected player without exceeding potential.";
    }

    private void CreateTrainingCampPlayerFocusDialog()
    {
        _trainingCampPlayerFocusDialog = new AcceptDialog { Name = "TrainingCampPlayerFocusDialog", Title = "Training Camp > Player Focus", MinSize = new Vector2I(1080, 680), Exclusive = false };
        AddChild(_trainingCampPlayerFocusDialog); _trainingCampPlayerFocusDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, AnchorsPreset = (int)LayoutPreset.FullRect, OffsetLeft = 12, OffsetTop = 10, OffsetRight = -12, OffsetBottom = -48 };
        _trainingCampPlayerFocusDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("PLAYER FOCUS", "Choose one available active-roster player for additional camp attention. The current bounded system does not expose unapproved drill catalogs or first-team-rep percentages."));
        _trainingCampPlayerFocusSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_trainingCampPlayerFocusSummary);
        var filters = new HBoxContainer(); filters.AddThemeConstantOverride("separation", 8); content.AddChild(filters);
        _trainingCampPlayerFocusSearch = new LineEdit { PlaceholderText = "Search players…", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _trainingCampPlayerFocusSearch.TextChanged += _ => RenderTrainingCampPlayerFocus(); filters.AddChild(_trainingCampPlayerFocusSearch);
        _trainingCampPlayerFocusPosition = new OptionButton { CustomMinimumSize = new Vector2(150, 0) }; _trainingCampPlayerFocusPosition.ItemSelected += _ => RenderTrainingCampPlayerFocus(); filters.AddChild(_trainingCampPlayerFocusPosition);
        _trainingCampPlayerFocusTree = new Tree { Columns = 9, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(1040, 460), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        ConfigureTrainingCampReportTree(_trainingCampPlayerFocusTree, new[] { "PLAYER", "POS", "AGE", "OVR", "POT", "HEALTH", "FATIGUE", "ROLE / READINESS", "FOCUS STATUS" }, new[] { 190, 52, 50, 50, 50, 120, 70, 250, 150 });
        _trainingCampPlayerFocusTree.ItemSelected += OnTrainingCampPlayerFocusSelected; content.AddChild(_trainingCampPlayerFocusTree);
        var actions = new HBoxContainer(); actions.AddThemeConstantOverride("separation", 8); content.AddChild(actions);
        _trainingCampApplyPlayerFocus = new Button { Text = "APPLY PLAYER FOCUS", Disabled = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _trainingCampApplyPlayerFocus.Pressed += async () => await ApplyTrainingCampPlayerFocus(); actions.AddChild(_trainingCampApplyPlayerFocus);
        var close = new Button { Text = "CLOSE" }; close.Pressed += _trainingCampPlayerFocusDialog.Hide; actions.AddChild(close);
        _trainingCampPlayerFocusStatus = new Label { Text = "Select a player to review eligibility. Selection alone changes nothing.", AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_trainingCampPlayerFocusStatus);
        ApplyWorkstationTheme(_trainingCampPlayerFocusDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private void ShowTrainingCampPlayerFocus()
    {
        PopulateTrainingCampPlayerFocusPositions(); RenderTrainingCampPlayerFocus(); var viewport = GetViewportRect().Size; _trainingCampPlayerFocusDialog.PopupCentered(new Vector2I(Mathf.Clamp((int)(viewport.X * .94f), 860, 1360), Mathf.Clamp((int)(viewport.Y * .9f), 580, 820)));
    }

    private void PopulateTrainingCampPlayerFocusPositions()
    {
        if (_trainingCampPlayerFocusPosition == null) return; var selected = _trainingCampPlayerFocusPosition.Selected >= 0 ? _trainingCampPlayerFocusPosition.GetItemText(_trainingCampPlayerFocusPosition.Selected) : "All positions"; _trainingCampPlayerFocusPosition.Clear(); _trainingCampPlayerFocusPosition.AddItem("All positions"); var team = GameCoreStateHelper.GetUserTeam(_nativeGameCoreContext?.ActiveLeague); foreach (var position in (team?.Roster ?? new List<PlayerState>()).Select(player => player.Position).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(position => position, StringComparer.OrdinalIgnoreCase)) _trainingCampPlayerFocusPosition.AddItem(position); for (var index = 0; index < _trainingCampPlayerFocusPosition.ItemCount; index++) if (string.Equals(_trainingCampPlayerFocusPosition.GetItemText(index), selected, StringComparison.OrdinalIgnoreCase)) { _trainingCampPlayerFocusPosition.Select(index); return; }
    }

    private void RenderTrainingCampPlayerFocus()
    {
        if (_trainingCampPlayerFocusTree == null) return; _trainingCampSelectedFocusPlayerId = ""; _trainingCampPlayerFocusTree.Clear(); if (_trainingCampApplyPlayerFocus != null) _trainingCampApplyPlayerFocus.Disabled = true;
        var league = _nativeGameCoreContext?.ActiveLeague; var team = GameCoreStateHelper.GetUserTeam(league); if (league == null || team == null) { _trainingCampPlayerFocusSummary.Text = "No active franchise roster is available."; return; }
        var camp = new TrainingCampService(_nativeGameCoreContext).GetStatus(team.TeamId).Status; var roles = new RosterEvaluationService(_nativeGameCoreContext).GetPlayerRoles(team.TeamId).Players.ToDictionary(player => player.PlayerId, StringComparer.OrdinalIgnoreCase); var search = _trainingCampPlayerFocusSearch?.Text?.Trim() ?? ""; var position = _trainingCampPlayerFocusPosition?.Selected > 0 ? _trainingCampPlayerFocusPosition.GetItemText(_trainingCampPlayerFocusPosition.Selected) : "";
        _trainingCampPlayerFocusSummary.Text = $"{team.Name} · Active roster {team.Roster.Count}/{RosterService.RosterLimit} · Phase {league.Calendar?.Phase ?? "Unavailable"} · Featured focus {(camp.PlayerFocusApplied ? camp.FocusPlayerName : "not assigned")}";
        var root = _trainingCampPlayerFocusTree.CreateItem(); var index = 0;
        foreach (var player in team.Roster.Where(player => string.IsNullOrWhiteSpace(search) || player.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).Where(player => string.IsNullOrWhiteSpace(position) || string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase)).OrderBy(player => player.Position, StringComparer.OrdinalIgnoreCase).ThenByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase))
        {
            roles.TryGetValue(player.PlayerId, out var role); var available = PlayerInjuryService.IsAvailableForGame(player); var focused = string.Equals(camp.FocusPlayerId, player.PlayerId, StringComparison.OrdinalIgnoreCase); var row = _trainingCampPlayerFocusTree.CreateItem(root); row.SetMetadata(0, player.PlayerId); row.SetText(0, player.Name); row.SetText(1, player.Position); row.SetText(2, player.Age.ToString()); row.SetText(3, player.Overall.ToString()); row.SetText(4, player.Potential.ToString()); row.SetText(5, available ? "Available" : $"Out · {player.CurrentInjury?.DaysRemaining ?? 0}d"); row.SetText(6, player.Fatigue.ToString()); row.SetText(7, role == null ? "Unassigned" : $"{role.Role} · {role.Readiness}"); row.SetText(8, focused ? "FOCUSED" : camp.PlayerFocusApplied ? "Not selected" : available ? "Eligible" : "Unavailable"); for (var column = 0; column < 9; column++) row.SetCustomBgColor(column, focused ? new Color("193d37") : index % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); row.SetCustomColor(5, available ? new Color("8fcf98") : new Color("e58b7a")); row.SetCustomColor(8, focused ? new Color("f0c96a") : new Color("9cadb8")); index++;
        }
        if (index == 0) { var empty = _trainingCampPlayerFocusTree.CreateItem(root); empty.SetText(0, "No active-roster players match these filters."); }
        _trainingCampPlayerFocusStatus.Text = camp.PlayerFocusApplied ? $"Player focus is already committed to {camp.FocusPlayerName} for this camp." : "Select one available player. Applying focus is the only action that changes state.";
    }

    private void OnTrainingCampPlayerFocusSelected()
    {
        var selected = _trainingCampPlayerFocusTree?.GetSelected(); _trainingCampSelectedFocusPlayerId = selected == null || IsNil(selected.GetMetadata(0)) ? "" : selected.GetMetadata(0).AsString(); var team = GameCoreStateHelper.GetUserTeam(_nativeGameCoreContext?.ActiveLeague); var player = team?.Roster.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, _trainingCampSelectedFocusPlayerId, StringComparison.OrdinalIgnoreCase)); var camp = team == null ? null : new TrainingCampService(_nativeGameCoreContext).GetStatus(team.TeamId).Status; var eligible = player != null && PlayerInjuryService.IsAvailableForGame(player) && camp?.IsAvailable == true && camp.PlayerFocusApplied == false; _trainingCampApplyPlayerFocus.Disabled = !eligible; _trainingCampPlayerFocusStatus.Text = player == null ? "Select a player to review eligibility." : !PlayerInjuryService.IsAvailableForGame(player) ? $"{player.Name} is unavailable and cannot receive focused camp reps." : camp?.PlayerFocusApplied == true ? $"Player focus is already committed to {camp.FocusPlayerName}." : !eligible ? "Player focus is available only during Training Camp Pending." : $"{player.Name}: focus will reduce fatigue by up to 15 and can add at most one overall point without exceeding potential. No depth assignment changes automatically.";
    }

    private void CreateTrainingCampWeeklyReportDialog()
    {
        _trainingCampWeeklyReportDialog = new AcceptDialog { Name = "TrainingCampWeeklyReportDialog", Title = "Training Camp > Weekly Report", MinSize = new Vector2I(1080, 720), Exclusive = false };
        AddChild(_trainingCampWeeklyReportDialog); _trainingCampWeeklyReportDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, AnchorsPreset = (int)LayoutPreset.FullRect, OffsetLeft = 12, OffsetTop = 10, OffsetRight = -12, OffsetBottom = -48 };
        _trainingCampWeeklyReportDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("WEEKLY CAMP REPORT", "Current authoritative roster, position-competition, health, and staff-assessment context. Staff identify leaders, but the GM retains every depth-chart decision."));
        _trainingCampWeeklyReportSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_trainingCampWeeklyReportSummary);

        content.AddChild(HomeLabel("POSITION GROUPS", 13, new Color("f4eddf")));
        _trainingCampWeeklyPositionTree = new Tree { Columns = 8, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(1040, 190), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        ConfigureTrainingCampReportTree(_trainingCampWeeklyPositionTree, new[] { "POS", "NEED", "AVAILABLE", "OUT", "AVG OVR", "AVG POT", "AVG FAT", "STAFF ASSESSMENT" }, new[] { 48, 55, 78, 48, 70, 70, 70, 360 }); content.AddChild(_trainingCampWeeklyPositionTree);

        content.AddChild(HomeLabel("POSITION COMPETITIONS", 13, new Color("f4eddf")));
        _trainingCampWeeklyBattleTree = new Tree { Columns = 4, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(1040, 145), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        ConfigureTrainingCampReportTree(_trainingCampWeeklyBattleTree, new[] { "POS", "LEADER", "CHALLENGER", "STAFF ASSESSMENT" }, new[] { 55, 190, 190, 500 }); content.AddChild(_trainingCampWeeklyBattleTree);

        content.AddChild(HomeLabel("HEALTH & AVAILABILITY", 13, new Color("f4eddf")));
        _trainingCampWeeklyHealthTree = new Tree { Columns = 5, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(1040, 135), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        ConfigureTrainingCampReportTree(_trainingCampWeeklyHealthTree, new[] { "PLAYER", "POS", "STATUS", "FATIGUE", "RECOVERY / WORKLOAD CONTEXT" }, new[] { 210, 55, 120, 75, 480 }); content.AddChild(_trainingCampWeeklyHealthTree);

        var actions = new HBoxContainer(); actions.AddThemeConstantOverride("separation", 8); content.AddChild(actions);
        var assess = new Button { Text = "UPDATE STAFF ASSESSMENTS" }; assess.Pressed += async () => await ResolvePositionBattles(); actions.AddChild(assess);
        var refresh = new Button { Text = "REFRESH REPORT" }; refresh.Pressed += async () => await RefreshTrainingCampReport(); actions.AddChild(refresh);
        var depth = new Button { Text = "VIEW DEPTH CHART", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; depth.Pressed += async () => { _trainingCampWeeklyReportDialog.Hide(); _trainingCampDialog.Hide(); await SelectMainTab(ROSTER_TAB_INDEX); await SetRosterViewMode(true); }; actions.AddChild(depth);
        var close = new Button { Text = "CLOSE" }; close.Pressed += _trainingCampWeeklyReportDialog.Hide; actions.AddChild(close);
        _trainingCampWeeklyReportStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_trainingCampWeeklyReportStatus);
        ApplyWorkstationTheme(_trainingCampWeeklyReportDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private static void ConfigureTrainingCampReportTree(Tree tree, string[] headers, int[] widths)
    {
        tree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1));
        for (var column = 0; column < headers.Length; column++) { tree.SetColumnTitle(column, headers[column]); tree.SetColumnCustomMinimumWidth(column, widths[column]); tree.SetColumnExpand(column, column == headers.Length - 1); }
    }

    private void ShowTrainingCampWeeklyReport()
    {
        RenderTrainingCampWeeklyReport(); var viewport = GetViewportRect().Size; _trainingCampWeeklyReportDialog.PopupCentered(new Vector2I(Mathf.Clamp((int)(viewport.X * .95f), 880, 1380), Mathf.Clamp((int)(viewport.Y * .92f), 620, 860)));
    }

    private void RenderTrainingCampWeeklyReport()
    {
        var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        _trainingCampWeeklyPositionTree.Clear(); _trainingCampWeeklyBattleTree.Clear(); _trainingCampWeeklyHealthTree.Clear();
        if (league == null || team == null) { _trainingCampWeeklyReportSummary.Text = "No active franchise camp report is available."; _trainingCampWeeklyReportStatus.Text = "Load a franchise to review training camp."; return; }
        var status = new TrainingCampService(_nativeGameCoreContext).GetStatus(team.TeamId).Status; var report = status.Report;
        _trainingCampWeeklyReportSummary.Text = $"{team.Name} · {league.Calendar?.WeekLabel ?? league.Calendar?.Phase ?? "Training Camp"} · Active roster {team.Roster.Count}/{RosterService.RosterLimit} · Position focus {(status.FocusApplied ? status.FocusPosition : "not assigned")} · Player focus {(status.PlayerFocusApplied ? status.FocusPlayerName : "not assigned")}";

        var positionRoot = _trainingCampWeeklyPositionTree.CreateItem(); var positionIndex = 0;
        foreach (var position in report?.Positions ?? new List<TrainingCampPositionReportDto>())
        {
            var item = _trainingCampWeeklyPositionTree.CreateItem(positionRoot); item.SetText(0, position.Position); item.SetText(1, position.RequiredStarters.ToString()); item.SetText(2, position.AvailablePlayers.ToString()); item.SetText(3, position.UnavailablePlayers.ToString()); item.SetText(4, position.AverageOverall.ToString()); item.SetText(5, position.AveragePotential.ToString()); item.SetText(6, position.AverageFatigue.ToString()); item.SetText(7, position.Recommendation); for (var column = 0; column < 8; column++) item.SetCustomBgColor(column, positionIndex % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); positionIndex++;
        }
        if (positionIndex == 0) { var empty = _trainingCampWeeklyPositionTree.CreateItem(positionRoot); empty.SetText(0, "Refresh the report to evaluate position groups."); }

        var battleRoot = _trainingCampWeeklyBattleTree.CreateItem(); var battleIndex = 0;
        foreach (var battle in team.TrainingCamp?.PositionBattles ?? new List<PositionBattleOutcome>())
        {
            var item = _trainingCampWeeklyBattleTree.CreateItem(battleRoot); item.SetText(0, battle.Position); item.SetText(1, battle.WinnerName); item.SetText(2, battle.RunnerUpName); item.SetText(3, battle.Explanation); for (var column = 0; column < 4; column++) item.SetCustomBgColor(column, battleIndex % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); battleIndex++;
        }
        if (battleIndex == 0) { var empty = _trainingCampWeeklyBattleTree.CreateItem(battleRoot); empty.SetText(0, "No close position competitions are recorded yet. Update staff assessments to evaluate the roster."); }

        var healthRoot = _trainingCampWeeklyHealthTree.CreateItem(); var healthIndex = 0;
        foreach (var player in team.Roster.Where(player => player.CurrentInjury?.IsActive == true || !string.IsNullOrWhiteSpace(player.Injury) || player.Fatigue >= 20).OrderByDescending(player => player.CurrentInjury?.IsActive == true).ThenByDescending(player => player.Fatigue).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase))
        {
            var injured = player.CurrentInjury?.IsActive == true || !string.IsNullOrWhiteSpace(player.Injury); var statusText = injured ? "Out" : player.Fatigue >= 40 ? "Limited" : "Managing workload"; var context = injured ? $"{player.CurrentInjury?.Name ?? player.Injury} · {player.CurrentInjury?.DaysRemaining ?? 0} day(s) remaining" : $"Daily recovery applies; current fatigue {player.Fatigue}."; var item = _trainingCampWeeklyHealthTree.CreateItem(healthRoot); item.SetText(0, player.Name); item.SetText(1, player.Position); item.SetText(2, statusText); item.SetText(3, player.Fatigue.ToString()); item.SetText(4, context); for (var column = 0; column < 5; column++) item.SetCustomBgColor(column, healthIndex % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); item.SetCustomColor(2, injured ? new Color("e58b7a") : new Color("f0c96a")); healthIndex++;
        }
        if (healthIndex == 0) { var empty = _trainingCampWeeklyHealthTree.CreateItem(healthRoot); empty.SetText(0, "No active injury or workload concern is recorded."); }
        _trainingCampWeeklyReportStatus.Text = report?.Positions?.Count > 0 ? report.Summary : "No persisted report yet. Refresh Report derives one from the current roster without changing assignments.";
    }

    private void CreateFinalCutdownDialog()
    {
        _finalCutdownDialog = new AcceptDialog { Name = "FinalCutdownDialog", Title = "Training Camp > Final Roster Cut-Down", MinSize = new Vector2I(1040, 650), Exclusive = false };
        AddChild(_finalCutdownDialog); _finalCutdownDialog.GetOkButton().Visible = false;
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, AnchorsPreset = (int)LayoutPreset.FullRect, OffsetLeft = 12, OffsetTop = 10, OffsetRight = -12, OffsetBottom = -48 };
        _finalCutdownDialog.AddChild(content);
        content.AddChild(CreateMarketHeading("FINAL ROSTER CUT-DOWN", "Build a proposed cut list, review its combined roster and financial effect, then confirm the entire validated batch. Checking a player changes nothing by itself."));
        _finalCutdownSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_finalCutdownSummary);
        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto }; content.AddChild(scroll);
        _finalCutdownTree = new Tree { Columns = 10, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(1000, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var headers = new[] { "CUT", "PLAYER", "POS", "AGE", "OVR", "POT", "CONTRACT", "HEALTH", "ROLE", "READINESS" }; var widths = new[] { 42, 175, 48, 48, 48, 48, 130, 120, 100, 150 };
        for (var column = 0; column < headers.Length; column++) { _finalCutdownTree.SetColumnTitle(column, headers[column]); _finalCutdownTree.SetColumnCustomMinimumWidth(column, widths[column]); _finalCutdownTree.SetColumnExpand(column, column is 1 or 9); }
        _finalCutdownTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); _finalCutdownTree.ItemEdited += OnFinalCutdownItemEdited; scroll.AddChild(_finalCutdownTree);
        var actions = new HBoxContainer(); actions.AddThemeConstantOverride("separation", 8); content.AddChild(actions);
        _finalCutdownConfirmCuts = new Button { Text = "REVIEW & CONFIRM CUTS", Disabled = true }; _finalCutdownConfirmCuts.Pressed += ShowFinalCutdownBatchConfirmation; actions.AddChild(_finalCutdownConfirmCuts);
        var finalize = new Button { Text = "FINALIZE LEGAL ROSTER", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; finalize.Pressed += async () => await FinalizeTrainingCampRoster(); actions.AddChild(finalize);
        var close = new Button { Text = "CLOSE" }; close.Pressed += _finalCutdownDialog.Hide; actions.AddChild(close);
        _finalCutdownStatus = new Label { Text = "Check players to build a proposed cut list. No cut is executed until the reviewed batch is confirmed.", AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_finalCutdownStatus);
        ApplyWorkstationTheme(_finalCutdownDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));

        _finalCutdownBatchDialog = new ConfirmationDialog { Title = "Confirm Final Roster Cuts", MinSize = new Vector2I(640, 470) };
        _finalCutdownBatchDialog.GetOkButton().Text = "CONFIRM ALL CUTS";
        AddChild(_finalCutdownBatchDialog);
        _finalCutdownBatchDetails = new RichTextLabel { BbcodeEnabled = false, FitContent = false, CustomMinimumSize = new Vector2(600, 360), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _finalCutdownBatchDialog.AddChild(_finalCutdownBatchDetails);
        _finalCutdownBatchDialog.Confirmed += async () => await ConfirmFinalCutdownBatch();
    }

    private void ShowFinalCutdown()
    {
        RenderFinalCutdown(); var viewport = GetViewportRect().Size; _finalCutdownDialog.PopupCentered(new Vector2I(Mathf.Clamp((int)(viewport.X * .94f), 820, 1320), Mathf.Clamp((int)(viewport.Y * .9f), 560, 800)));
    }

    private void RenderFinalCutdown()
    {
        _finalCutdownSelectedPlayerIds.Clear(); _finalCutdownTree.Clear(); _finalCutdownConfirmCuts.Disabled = true;
        var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(candidate => candidate.TeamId == league.UserTeamId);
        if (league == null || team == null) { _finalCutdownSummary.Text = "No active franchise roster is available."; _finalCutdownStatus.Text = "Load a franchise to review final cuts."; return; }
        var requiredCuts = Math.Max(0, team.Roster.Count - RosterService.RosterLimit); var roles = new RosterEvaluationService(_nativeGameCoreContext).GetPlayerRoles(team.TeamId); var roleByPlayer = roles.Players.ToDictionary(player => player.PlayerId, StringComparer.OrdinalIgnoreCase);
        _finalCutdownSummary.Text = $"{team.Name} · Active roster {team.Roster.Count}/{RosterService.RosterLimit} · Required cuts {requiredCuts} · Practice squad {team.PracticeSquad.Count}/16 · Phase {league.Calendar?.Phase ?? "Unavailable"}";
        var root = _finalCutdownTree.CreateItem(); var index = 0;
        foreach (var player in team.Roster.OrderBy(player => player.Position, StringComparer.OrdinalIgnoreCase).ThenByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase))
        {
            roleByPlayer.TryGetValue(player.PlayerId, out var feedback); var row = _finalCutdownTree.CreateItem(root); row.SetMetadata(0, player.PlayerId); row.SetCellMode(0, TreeItem.TreeCellMode.Check); row.SetEditable(0, true); row.SetChecked(0, false); row.SetText(1, player.Name); row.SetText(2, player.Position); row.SetText(3, player.Age.ToString()); row.SetText(4, player.Overall.ToString()); row.SetText(5, player.Potential.ToString()); row.SetText(6, $"{GameCoreStateHelper.FormatCapRoom(player.Contract?.AnnualSalary ?? 0m)} · {player.Contract?.YearsRemaining ?? 0} yr"); row.SetText(7, player.CurrentInjury?.IsActive == true ? $"{player.CurrentInjury.Name} · {player.CurrentInjury.DaysRemaining}d" : "Available"); row.SetText(8, feedback?.Role ?? "Unassigned"); row.SetText(9, feedback?.Readiness ?? "Unavailable"); for (var column = 0; column < 10; column++) row.SetCustomBgColor(column, index % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); index++;
        }
        _finalCutdownStatus.Text = requiredCuts > 0 ? $"Resolve {requiredCuts} explicit roster cut(s) before finalization. Select a player to preview the real financial effect." : team.TrainingCamp?.RosterFinalized == true ? "The legal roster is finalized." : "Roster count is legal. Apply the camp focus and confirm the depth chart before finalization.";
    }

    private void OnFinalCutdownItemEdited()
    {
        var item = _finalCutdownTree?.GetEdited();
        if (item == null || IsNil(item.GetMetadata(0))) return;
        var playerId = item.GetMetadata(0).AsString();
        if (item.IsChecked(0)) _finalCutdownSelectedPlayerIds.Add(playerId); else _finalCutdownSelectedPlayerIds.Remove(playerId);
        RenderFinalCutdownProjection();
    }

    private void RenderFinalCutdownProjection()
    {
        _finalCutdownConfirmCuts.Disabled = _finalCutdownSelectedPlayerIds.Count == 0;
        if (_finalCutdownSelectedPlayerIds.Count == 0) { _finalCutdownStatus.Text = "Check players to build a proposed cut list. No cut is executed until the reviewed batch is confirmed."; return; }
        var preview = new TrainingCampService(_nativeGameCoreContext).PreviewRosterCuts(_finalCutdownSelectedPlayerIds);
        if (!preview.Ok) { _finalCutdownStatus.Text = preview.Error; return; }
        var warning = preview.PositionWarnings.Count == 0 ? "Projected depth remains starter-valid." : $"Depth warnings: {string.Join("; ", preview.PositionWarnings)}.";
        _finalCutdownStatus.Text = $"Proposed cuts: {preview.PlayerIds.Count} · Active roster {preview.RosterCountBefore} → {preview.RosterCountAfter} · Required cuts after proposal {preview.RequiredCutsAfter} · Cap room {GameCoreStateHelper.FormatCapRoom(preview.CapRoomBefore)} → {GameCoreStateHelper.FormatCapRoom(preview.CapRoomAfter)}. {warning}";
    }

    private void ShowFinalCutdownBatchConfirmation()
    {
        var preview = new TrainingCampService(_nativeGameCoreContext).PreviewRosterCuts(_finalCutdownSelectedPlayerIds);
        if (!preview.Ok) { _finalCutdownStatus.Text = preview.Error; return; }
        var warnings = preview.PositionWarnings.Count == 0 ? "No projected starter-depth shortage." : string.Join("\n", preview.PositionWarnings.Select(warning => $"• {warning}"));
        _finalCutdownBatchDetails.Text = $"PROPOSED CUT LIST ({preview.PlayerIds.Count})\n{string.Join("\n", preview.PlayerNames.Select(name => $"• {name}"))}\n\nROSTER EFFECT\nActive roster: {preview.RosterCountBefore} → {preview.RosterCountAfter}\nRequired cuts remaining: {preview.RequiredCutsBefore} → {preview.RequiredCutsAfter}\n\nFINANCIAL EFFECT\nCommitted payroll: {GameCoreStateHelper.FormatCapRoom(preview.PayrollBefore)} → {GameCoreStateHelper.FormatCapRoom(preview.PayrollAfter)}\nCap room: {GameCoreStateHelper.FormatCapRoom(preview.CapRoomBefore)} → {GameCoreStateHelper.FormatCapRoom(preview.CapRoomAfter)}\n\nDEPTH REVIEW\n{warnings}\n\nConfirming validates the entire current list again, then releases every listed player and records each transaction. Cancel returns to the unchanged proposal.";
        _finalCutdownBatchDialog.PopupCentered(new Vector2I(640, 470));
    }

    private async Task ConfirmFinalCutdownBatch()
    {
        var response = new TrainingCampService(_nativeGameCoreContext).ConfirmRosterCuts(_finalCutdownSelectedPlayerIds);
        _finalCutdownStatus.Text = response.Message;
        SetPrimaryStatus(response.Message);
        if (!response.Ok) return;
        _finalCutdownSelectedPlayerIds.Clear();
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Final roster cuts saved.", autosaveToo: true);
        await RefreshAll();
        RenderFinalCutdown();
    }

    private void ShowTrainingCamp()
    {
        RefreshTrainingCampUi();
        _trainingCampDialog.PopupCentered(new Vector2I(660, 560));
    }

    private void RefreshTrainingCampUi()
    {
        EnsureNativeGameCoreServices();
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || team == null)
        {
            _trainingCampStatus.Text = "Start or load a franchise to manage training camp.";
            return;
        }

        var camp = new TrainingCampService(_nativeGameCoreContext).GetStatus(team.TeamId);
        _trainingCampStatus.Text = camp.Status.IsAvailable
            ? camp.Status.RosterFinalized
                ? camp.Status.Summary
                : $"Active roster: {team.Roster.Count}/53. {camp.Status.Summary} Choose focus, then finalize."
            : "Training-camp decisions are only available during Training Camp Pending.";
        _trainingCampReport.Text = FormatTrainingCampReport(camp.Status.Report);
        if (team.TrainingCamp.PositionBattles.Count > 0)
            _trainingCampReport.Text += "\n\nBattles:\n" + string.Join("\n", team.TrainingCamp.PositionBattles.Select(battle => $"{battle.Position}: {battle.Explanation}"));
        var roles = new RosterEvaluationService(_nativeGameCoreContext).GetPlayerRoles(team.TeamId);
        _trainingCampRoles.Text = roles.Ok ? string.Join("\n", roles.Players.Select(player => $"{player.Name} ({player.Position}) - {player.Role}, {player.Readiness}. {player.Explanation}")) : roles.Error;
        if (camp.Status.PlayerFocusApplied)
            _trainingCampRoles.Text = $"PLAYER FOCUS: {camp.Status.FocusPlayerName}\n\n{_trainingCampRoles.Text}";
    }

    private async Task ApplyTrainingCampFocus()
    {
        if (string.IsNullOrWhiteSpace(_trainingCampSelectedFocusPosition))
        {
            _trainingCampPositionFocusStatus.Text = "Select an eligible active-roster position group first.";
            return;
        }

        var response = new TrainingCampService(_nativeGameCoreContext).ApplyPositionFocus(_trainingCampSelectedFocusPosition);
        _trainingCampStatus.Text = response.Message;
        _trainingCampPositionFocusStatus.Text = response.Message;
        if (!response.Ok)
            return;
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Training-camp focus saved.", autosaveToo: true);
        await RefreshAll();
        RefreshTrainingCampUi();
        if (_trainingCampPositionFocusDialog?.Visible == true) RenderTrainingCampPositionFocus();
    }

    private async Task ApplyTrainingCampPlayerFocus()
    {
        if (string.IsNullOrWhiteSpace(_trainingCampSelectedFocusPlayerId))
        {
            _trainingCampPlayerFocusStatus.Text = "Select an eligible active-roster player first.";
            return;
        }

        var response = new TrainingCampService(_nativeGameCoreContext).ApplyPlayerFocus(_trainingCampSelectedFocusPlayerId);
        _trainingCampStatus.Text = response.Message;
        _trainingCampPlayerFocusStatus.Text = response.Message;
        if (!response.Ok)
            return;
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Training-camp player focus saved.", autosaveToo: true);
        await RefreshAll();
        RefreshTrainingCampUi();
        if (_trainingCampPlayerFocusDialog?.Visible == true) RenderTrainingCampPlayerFocus();
    }

    private async Task RefreshTrainingCampReport()
    {
        var response = new TrainingCampService(_nativeGameCoreContext).GenerateReport();
        _trainingCampStatus.Text = response.Message;
        if (!response.Ok)
            return;
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Training-camp report saved.", autosaveToo: true);
        RefreshTrainingCampUi();
        if (_trainingCampWeeklyReportDialog?.Visible == true) RenderTrainingCampWeeklyReport();
    }

    private async Task ResolvePositionBattles()
    {
        var outcomes = new PositionBattleService(_nativeGameCoreContext).Resolve();
        _trainingCampStatus.Text = outcomes.Count == 0 ? "Staff found no close position competitions to assess." : $"Staff updated {outcomes.Count} position competition assessment(s). No depth assignments changed.";
        if (_trainingCampWeeklyReportStatus != null) _trainingCampWeeklyReportStatus.Text = _trainingCampStatus.Text;
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Position-battle outcomes saved.", autosaveToo: true);
        await RefreshAll();
        RefreshTrainingCampUi();
        if (_trainingCampWeeklyReportDialog?.Visible == true) RenderTrainingCampWeeklyReport();
    }

    private async Task FinalizeTrainingCampRoster()
    {
        var response = new TrainingCampService(_nativeGameCoreContext).FinalizeRoster();
        _trainingCampStatus.Text = response.Message;
        if (_finalCutdownStatus != null) _finalCutdownStatus.Text = response.Message;
        if (!response.Ok)
            return;
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, "Training-camp roster decision saved.", autosaveToo: true);
        await RefreshAll();
        RefreshTrainingCampUi();
        if (_finalCutdownDialog?.Visible == true) RenderFinalCutdown();
    }

    private static string FormatTrainingCampReport(TrainingCampReportDto report)
    {
        if (report?.Positions == null || report.Positions.Count == 0)
            return "Refresh the camp report to evaluate roster readiness.";

        var lines = new List<string> { report.Summary };
        foreach (var position in report.Positions)
            lines.Add($"{position.Position}: {position.AvailablePlayers}/{position.RequiredStarters} available starters | OVR {position.AverageOverall} | POT {position.AveragePotential} | FAT {position.AverageFatigue} | OUT {position.UnavailablePlayers}. {position.Recommendation}");
        return string.Join("\n", lines);
    }

    private void CreateFranchiseSetupDialog()
    {
        _franchiseSetupDialog = new AcceptDialog { Name = "FranchiseSetupDialog", Title = "Start a Franchise", MinSize = new Vector2I(660, 580), Exclusive = true };
        AddChild(_franchiseSetupDialog);
        _franchiseSetupDialog.GetOkButton().Text = "Choose Team";
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(600, 450) };
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _franchiseSetupDialog.AddChild(scroll); scroll.AddChild(content);
        content.AddChild(new Label { Text = "Choose a reusable GM profile and starting world before selecting your franchise.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        _setupProfileSelect = new OptionButton();
        _setupGmName = new LineEdit { Text = "User GM", PlaceholderText = "GM name" };
        content.AddChild(SetupRow("Saved Profile", _setupProfileSelect));
        content.AddChild(SetupRow("GM Name", _setupGmName));
        content.AddChild(new Label { Text = "Management Attributes (maximum 220 points)" });
        _setupNegotiation = SetupAttribute(); _setupPlayerManagement = SetupAttribute(); _setupScouting = SetupAttribute(); _setupLeadership = SetupAttribute();
        content.AddChild(SetupRow("Negotiation", _setupNegotiation));
        content.AddChild(SetupRow("Player Management", _setupPlayerManagement));
        content.AddChild(SetupRow("Scouting Judgment", _setupScouting));
        content.AddChild(SetupRow("Leadership", _setupLeadership));
        _setupBudget = new Label(); content.AddChild(_setupBudget);
        _setupOutfit = SetupOptions("Team Polo", "Suit", "Hoodie");
        content.AddChild(SetupRow("Game Day Outfit", _setupOutfit));
        _setupRosterSource = SetupOptions("Standard Roster", "Generated Roster");
        content.AddChild(SetupRow("Starting World", _setupRosterSource));
        content.AddChild(new Label { Text = "Standard uses the fixed fictional world seed. Generated creates a new, saved seed for this franchise.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        _setupStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_setupStatus);
        _setupProfileSelect.ItemSelected += OnSetupProfileSelected;
        _setupNegotiation.ValueChanged += _ => UpdateSetupBudget(); _setupPlayerManagement.ValueChanged += _ => UpdateSetupBudget();
        _setupScouting.ValueChanged += _ => UpdateSetupBudget(); _setupLeadership.ValueChanged += _ => UpdateSetupBudget();
        UpdateSetupBudget();
    }

    private static HBoxContainer SetupRow(string label, Control input)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(190, 0), VerticalAlignment = VerticalAlignment.Center });
        input.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(input); return row;
    }
    private static SpinBox SetupAttribute() => new() { MinValue = 20, MaxValue = 80, Step = 1, Value = 50 };
    private static OptionButton SetupOptions(params string[] options) { var select = new OptionButton(); foreach (var option in options) select.AddItem(option); return select; }
    private void UpdateSetupBudget()
    {
        var total = (int)_setupNegotiation.Value + (int)_setupPlayerManagement.Value + (int)_setupScouting.Value + (int)_setupLeadership.Value;
        _setupBudget.Text = total <= GmAttributes.MaximumTotal ? $"Attribute total: {total}/{GmAttributes.MaximumTotal}" : $"Attribute total: {total}/{GmAttributes.MaximumTotal} - reduce attributes.";
    }
    private async Task ShowFranchiseSetupDialog()
    {
        _setupProfiles = new GmProfileStore().LoadAll().ToList();
        _setupProfileSelect.Clear(); _setupProfileSelect.AddItem("Create New Profile");
        foreach (var profile in _setupProfiles) _setupProfileSelect.AddItem(profile.Name);
        _setupProfileSelect.Select(0); _setupStatus.Text = ""; UpdateSetupBudget();
        _franchiseSetupDialog.PopupCentered(new Vector2I(660, 580)); _franchiseSetupDialog.GrabFocus(); _setupGmName.GrabFocus();
        await Task.CompletedTask;
    }
    private void OnSetupProfileSelected(long index)
    {
        if (index <= 0 || index > _setupProfiles.Count) return;
        var profile = _setupProfiles[(int)index - 1]; _setupGmName.Text = profile.Name;
        _setupNegotiation.Value = profile.Attributes.Negotiation; _setupPlayerManagement.Value = profile.Attributes.PlayerManagement;
        _setupScouting.Value = profile.Attributes.ScoutingJudgment; _setupLeadership.Value = profile.Attributes.Leadership;
        SelectSetupOption(_setupOutfit, profile.Appearance.Outfit); UpdateSetupBudget();
    }
    private static void SelectSetupOption(OptionButton select, string value) { for (var i = 0; i < select.ItemCount; i++) if (string.Equals(select.GetItemText(i), value, StringComparison.OrdinalIgnoreCase)) { select.Select(i); return; } }
    private async Task CreateConfiguredNativeFranchise()
    {
        var attributes = new GmAttributes { Negotiation = (int)_setupNegotiation.Value, PlayerManagement = (int)_setupPlayerManagement.Value, ScoutingJudgment = (int)_setupScouting.Value, Leadership = (int)_setupLeadership.Value };
        if (string.IsNullOrWhiteSpace(_setupGmName.Text)) { _setupStatus.Text = "Enter a GM name."; return; }
        try { attributes.Validate(); } catch (ArgumentException error) { _setupStatus.Text = error.Message; return; }
        var profile = _setupProfileSelect.Selected > 0 && _setupProfileSelect.Selected <= _setupProfiles.Count ? _setupProfiles[_setupProfileSelect.Selected - 1] : new GmProfile();
        profile.Name = _setupGmName.Text.Trim(); profile.Attributes = attributes; profile.Appearance.Outfit = _setupOutfit.GetItemText(_setupOutfit.Selected); new GmProfileStore().Save(profile);
        var world = _setupRosterSource.Selected == 0 ? WorldDefinition.Standard() : WorldDefinition.Generated(unchecked((ulong)DateTime.UtcNow.Ticks ^ (ulong)Guid.NewGuid().GetHashCode()));
        _franchiseSetupDialog.Hide(); SetNewGameButtonsDisabled(true);
        try { await StartFreshNativeLeague(world, profile); } finally { SetNewGameButtonsDisabled(false); }
    }

    private async Task NewGame()
    {
        if (HasExistingNativeSave())
        {
            if (_newGameConfirmDialog != null)
                _newGameConfirmDialog.PopupCentered();
            else
                await ConfirmNativeNewGame();
        }
        else
            await ShowFranchiseSetupDialog();
    }

    private bool HasExistingNativeSave()
    {
        var saveService = GetNativeGameCoreSaveService();
        return saveService.SaveExists() || saveService.SaveExists(GameCoreSaveService.NamedSaveFileName);
    }

    private async Task ConfirmNativeNewGame()
    {
        await ShowFranchiseSetupDialog();
    }

    private async Task StartFreshNativeLeague(WorldDefinition world = null, GmProfile profile = null)
    {
        EnsureNativeGameCoreServices();
        _nativeGameCoreContext.ActiveLeague = null;
        new LeagueBootstrapService(_nativeGameCoreContext).CreateTestLeague(GetTeamSeedPath(), world, profile);
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
        _newGameTeamPicker.PopupCentered(new Vector2I(640, 480));
        _newGameTeamPicker.GrabFocus();
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

        EnsureNativeGameCoreServices();
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams.FirstOrDefault(candidate => string.Equals(candidate.TeamId, _currentTeamId, StringComparison.OrdinalIgnoreCase));
        if (team == null)
        {
            _gmTeamLabel = "(error)";
            RenderFrontOfficeLabel();
            if (_btnSetUserTeam != null)
                _btnSetUserTeam.Disabled = false;
            return;
        }
        league.UserTeamId = team.TeamId;
        await SaveCurrentNativeGame(GameCoreSaveService.NamedSaveFileName, $"User franchise changed to {team.Name}.", autosaveToo: true);
        if (_btnSetUserTeam != null)
            _btnSetUserTeam.Disabled = false;
        await RefreshAll();
    }

    private bool IsRosterTabActive()
    {
        return _currentMainTab == ROSTER_TAB_INDEX;
    }

    private async Task SetRosterViewMode(bool showDepthChart)
    {
        _developmentViewActive = false;
        _injuriesViewActive = false;
        _staffViewActive = false;
        _teamHistoryViewActive = false;
        _teamStandingsViewActive = false;
        _teamStatsViewActive = false;
        _teamFinancesViewActive = false;
        _contractsViewActive = false;
        _accountingViewActive = false;
        _practiceSquadViewActive = false;
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" })
        {
            var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = true;
        }
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
            _rosterSplit.Visible = !_depthChartViewActive && !_developmentViewActive && !_injuriesViewActive && !_staffViewActive && !_teamHistoryViewActive && !_teamStandingsViewActive && !_teamStatsViewActive && !_teamFinancesViewActive && !_contractsViewActive && !_accountingViewActive && !_practiceSquadViewActive;
        if (_depthChartPanel != null)
            _depthChartPanel.Visible = _depthChartViewActive && !_developmentViewActive && !_injuriesViewActive && !_staffViewActive && !_teamHistoryViewActive && !_teamStandingsViewActive && !_teamStatsViewActive && !_teamFinancesViewActive && !_contractsViewActive && !_accountingViewActive && !_practiceSquadViewActive;
        if (_developmentWorkspace != null)
            _developmentWorkspace.Visible = _developmentViewActive;
        if (_injuriesWorkspace != null)
            _injuriesWorkspace.Visible = _injuriesViewActive;
        if (_staffWorkspace != null)
            _staffWorkspace.Visible = _staffViewActive;
        if (_teamHistoryWorkspace != null)
            _teamHistoryWorkspace.Visible = _teamHistoryViewActive;
        if (_teamStandingsWorkspace != null)
            _teamStandingsWorkspace.Visible = _teamStandingsViewActive;
        if (_teamStatsWorkspace != null)
            _teamStatsWorkspace.Visible = _teamStatsViewActive;
        if (_teamFinancesWorkspace != null)
            _teamFinancesWorkspace.Visible = _teamFinancesViewActive;
        if (_contractsWorkspace != null)
            _contractsWorkspace.Visible = _contractsViewActive;
        if (_accountingWorkspace != null)
            _accountingWorkspace.Visible = _accountingViewActive;
        if (_practiceSquadWorkspace != null)
            _practiceSquadWorkspace.Visible = _practiceSquadViewActive;
    }

    private void ConfigureDepthChartWorkspacePresentation()
    {
        if (_depthChartPanel == null || _depthChartTree == null) return;
        var header = GetNodeOrNull<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartWorkspaceHeader");
        if (header != null) { header.Text = "TEAM > DEPTH CHART"; header.AddThemeFontSizeOverride("font_size", 18); }
        var hint = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartWorkspaceHint");
        if (hint != null) hint.Visible = false;
        if (_depthChartSummary != null) { _depthChartSummary.AddThemeFontSizeOverride("font_size", 12); _depthChartSummary.AutowrapMode = TextServer.AutowrapMode.Off; }
        _depthChartTree.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _depthChartTree.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _depthChartTree.TooltipText = "Drag a player onto another player in the same position group to reorder the depth chart. Seasonal production is informational.";
        if (_depthChartActionStatus != null) _depthChartActionStatus.Text = "Drag and drop within a position group to reorder players.";
        var actionRow = GetNodeOrNull<Container>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/DepthChartPanel/DepthChartActionRow");
        if (actionRow != null && _btnDepthChartToggleLock == null)
        {
            _btnDepthChartToggleLock = new Button { Text = "LOCK POSITION", Disabled = true, CustomMinimumSize = new Vector2(125, 28), TooltipText = "Protect this position group's saved order from Auto-Fill." };
            _btnDepthChartToggleLock.Pressed += async () => await ToggleSelectedDepthChartLock();
            actionRow.AddChild(_btnDepthChartToggleLock);
            actionRow.MoveChild(_btnDepthChartToggleLock, Math.Min(2, actionRow.GetChildCount() - 1));
        }
        if (actionRow != null && _btnReturnToLiveGame == null)
        {
            _btnReturnToLiveGame = new Button { Text = "RETURN TO LIVE GAME", Visible = false, CustomMinimumSize = new Vector2(170, 28), TooltipText = "Return to the paused observer. Resume when the depth chart is ready." };
            _btnReturnToLiveGame.Pressed += ReturnToLiveGameObserver;
            actionRow.AddChild(_btnReturnToLiveGame);
            actionRow.MoveChild(_btnReturnToLiveGame, 0);
        }
        var filterRow = new HBoxContainer { Name = "DepthChartFilterRow" };
        filterRow.AddThemeConstantOverride("separation", 5);
        _depthChartPanel.AddChild(filterRow);
        _depthChartPanel.MoveChild(filterRow, 3);
        foreach (var entry in new[] { ("OFFENSE", 0), ("DEFENSE", 1), ("SPECIAL TEAMS", 2) })
        {
            var button = CreateShellButton(entry.Item1, entry.Item2 == _depthChartUnitFilter ? new Color("f4eddf") : new Color("9cadb8"), new Color("254258"));
            button.CustomMinimumSize = new Vector2(entry.Item2 == 2 ? 125 : 95, 28);
            if (entry.Item2 == _depthChartUnitFilter) button.AddThemeStyleboxOverride("normal", CreateSurfaceStyle(new Color("193d37"), new Color("4f9b55"), 0, 1));
            var unit = entry.Item2;
            button.Pressed += () => { _depthChartUnitFilter = unit; ConfigureDepthChartFilterButtons(filterRow); RenderFilteredDepthChart(); };
            filterRow.AddChild(button);
        }
        _depthChartSearch = new LineEdit { PlaceholderText = "Search players…", ClearButtonEnabled = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(190, 28) };
        _depthChartSearch.TextChanged += _ => RenderFilteredDepthChart();
        filterRow.AddChild(_depthChartSearch);
    }

    private void ConfigureDepthChartFilterButtons(HBoxContainer row)
    {
        if (row == null) return;
        for (var index = 0; index < Math.Min(3, row.GetChildCount()); index++)
        {
            if (row.GetChild(index) is not Button button) continue;
            var active = index == _depthChartUnitFilter;
            button.AddThemeColorOverride("font_color", active ? new Color("f4eddf") : new Color("9cadb8"));
            button.AddThemeStyleboxOverride("normal", active ? CreateSurfaceStyle(new Color("193d37"), new Color("4f9b55"), 0, 1) : CreateSurfaceStyle(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 0, 0));
        }
    }

    private void RenderFilteredDepthChart()
    {
        if (_depthChartPayload != null)
            RenderDepthChartSnapshot(_depthChartPayload);
    }

    private void CreateDevelopmentWorkspace()
    {
        if (_rosterTabPanel == null || _developmentWorkspace != null) return;
        _developmentWorkspace = new VBoxContainer { Name = "DevelopmentWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _developmentWorkspace.AddThemeConstantOverride("separation", 6);
        _rosterTabPanel.AddChild(_developmentWorkspace);
        var header = new HBoxContainer();
        var title = new Label { Text = "TEAM > DEVELOPMENT", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); header.AddChild(title);
        _developmentWindowLabel = new Label { Text = "Trailing 12 months: loading…" }; _developmentWindowLabel.AddThemeColorOverride("font_color", new Color("9cadb8")); header.AddChild(_developmentWindowLabel); _developmentWorkspace.AddChild(header);
        var filters = new HBoxContainer(); filters.AddThemeConstantOverride("separation", 6);
        _developmentSearch = new LineEdit { PlaceholderText = "Search player", CustomMinimumSize = new Vector2(200, 28), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _developmentPositionFilter = new OptionButton { CustomMinimumSize = new Vector2(92, 28) };
        foreach (var option in PosFilterOptions) _developmentPositionFilter.AddItem(option);
        _developmentTrendFilter = new OptionButton { CustomMinimumSize = new Vector2(128, 28) };
        foreach (var option in new[] { "All trends", "Breakouts", "Regressions", "No rating history" }) _developmentTrendFilter.AddItem(option);
        _developmentChangeFilter = new OptionButton { CustomMinimumSize = new Vector2(142, 28) };
        foreach (var option in new[] { "All changes", "Material change", "No movement history" }) _developmentChangeFilter.AddItem(option);
        filters.AddChild(_developmentSearch); filters.AddChild(_developmentPositionFilter); filters.AddChild(_developmentTrendFilter); filters.AddChild(_developmentChangeFilter); _developmentWorkspace.AddChild(filters);
        _developmentTree = new Tree { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row };
        _developmentTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1));
        _developmentWorkspace.AddChild(_developmentTree);
        _developmentSearch.TextChanged += _ => RenderDevelopmentRows();
        _developmentPositionFilter.ItemSelected += _ => RenderDevelopmentRows();
        _developmentTrendFilter.ItemSelected += _ => RenderDevelopmentRows();
        _developmentChangeFilter.ItemSelected += _ => RenderDevelopmentRows();
        _developmentTree.ColumnTitleClicked += OnDevelopmentColumnTitleClicked;
        _developmentTree.ItemSelected += () => _ = OpenSelectedDevelopmentPlayerProfile();
        _developmentTree.ItemActivated += () => _ = OpenSelectedDevelopmentPlayerProfile();
    }

    private async Task ShowDevelopmentWorkspaceAsync()
    {
        _practiceSquadViewActive = false; _developmentViewActive = true; _injuriesViewActive = false; _staffViewActive = false; _teamHistoryViewActive = false; _teamStandingsViewActive = false; _teamStatsViewActive = false; _teamFinancesViewActive = false; _depthChartViewActive = false; UpdateRosterViewModeUi();
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" })
        {
            var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = false;
        }
        BuildDevelopmentRows(); RenderDevelopmentRows();
        await Task.CompletedTask;
    }

    private void CreateInjuriesWorkspace()
    {
        if (_rosterTabPanel == null || _injuriesWorkspace != null) return;
        _injuriesWorkspace = new ScrollContainer { Name = "InjuriesWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto, VerticalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _injuryPanelRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _injuryPanelRow.AddThemeConstantOverride("separation", 8); _injuriesWorkspace.AddChild(_injuryPanelRow); _rosterTabPanel.AddChild(_injuriesWorkspace);
        AddInjuryPanel("Healthy / Available", "healthy", "Healthy rostered players and current availability.", new Color("8fcf98"));
        AddInjuryPanel("Injured", "injured", "Active injuries and expected recovery.", new Color("f0c96a"));
        AddInjuryPanel("Injured Reserve", "ir", "Injured reserve and recovery context.", new Color("e58b7a"));
    }

    private void AddInjuryPanel(string title, string key, string subtitle, Color accent)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(300, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("0d2031"), new Color("254258"), 0, 1));
        var content = new VBoxContainer(); content.AddThemeConstantOverride("separation", 4); panel.AddChild(content);
        var label = new Label { Text = title.ToUpperInvariant() }; label.AddThemeFontSizeOverride("font_size", 14); label.AddThemeColorOverride("font_color", accent); content.AddChild(label);
        var hint = new Label { Text = subtitle, AutowrapMode = TextServer.AutowrapMode.WordSmart }; hint.AddThemeFontSizeOverride("font_size", 11); hint.AddThemeColorOverride("font_color", new Color("9cadb8")); content.AddChild(hint);
        var tree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        tree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); tree.ItemSelected += () => _ = OpenSelectedInjuryPlayerProfile(tree); tree.ItemActivated += () => _ = OpenSelectedInjuryPlayerProfile(tree);
        content.AddChild(tree); _injuryPanelTrees[key] = tree; _injuryPanelRow?.AddChild(panel);
    }

    private async Task ShowInjuriesWorkspaceAsync()
    {
        _practiceSquadViewActive = false; _injuriesViewActive = true; _developmentViewActive = false; _staffViewActive = false; _teamHistoryViewActive = false; _teamStandingsViewActive = false; _teamStatsViewActive = false; _teamFinancesViewActive = false; _depthChartViewActive = false; UpdateRosterViewModeUi();
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" }) { var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = false; }
        RenderInjuryPanels(); await Task.CompletedTask;
    }

    private void RenderInjuryPanels()
    {
        var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var healthy = new List<InjuryRow>(); var injured = new List<InjuryRow>(); var ir = new List<InjuryRow>();
        foreach (var player in team?.Roster ?? Enumerable.Empty<PlayerState>())
        {
            var activeInjury = player.CurrentInjury?.IsActive == true || !string.IsNullOrWhiteSpace(player.Injury);
            var row = InjuryRow.FromPlayer(player, false);
            if (activeInjury) injured.Add(row); else healthy.Add(row);
        }
        foreach (var player in team?.InjuredReserve ?? Enumerable.Empty<PlayerState>()) ir.Add(InjuryRow.FromPlayer(player, true));
        PopulateInjuryPanel("healthy", healthy, false); PopulateInjuryPanel("injured", injured, true); PopulateInjuryPanel("ir", ir, true);
    }

    private void PopulateInjuryPanel(string key, List<InjuryRow> rows, bool recoveryView)
    {
        if (!_injuryPanelTrees.TryGetValue(key, out var tree)) return;
        tree.Clear(); tree.Columns = recoveryView ? 4 : 3; tree.SetColumnTitle(0, "Player"); tree.SetColumnTitle(1, "Pos"); tree.SetColumnTitle(2, recoveryView ? "Injury / Status" : "Availability"); if (recoveryView) tree.SetColumnTitle(3, "Recovery");
        tree.SetColumnCustomMinimumWidth(0, 115); tree.SetColumnExpand(0, true); tree.SetColumnCustomMinimumWidth(1, 42); tree.SetColumnCustomMinimumWidth(2, 116); if (recoveryView) tree.SetColumnCustomMinimumWidth(3, 72);
        var root = tree.CreateItem();
        if (rows.Count == 0) { var empty = tree.CreateItem(root); empty.SetText(0, recoveryView ? "No players in this list." : "No healthy players available."); return; }
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index]; var item = tree.CreateItem(root); item.SetMetadata(0, row.PlayerId); item.SetText(0, row.Name); item.SetText(1, row.Position); item.SetText(2, recoveryView ? row.InjuryStatus : row.Availability); if (recoveryView) item.SetText(3, row.Recovery);
            for (var column = 0; column < tree.Columns; column++) item.SetCustomBgColor(column, index % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
            item.SetCustomColor(2, recoveryView ? new Color("f0c96a") : new Color("8fcf98"));
        }
    }

    private async Task OpenSelectedInjuryPlayerProfile(Tree tree)
    {
        var selected = tree?.GetSelected(); if (selected == null || IsNil(selected.GetMetadata(0))) return;
        var playerId = selected.GetMetadata(0).AsString(); await SetRosterViewMode(false); await RefreshRosterTab(); TrySelectRosterPlayer(playerId);
    }

    private void CreateStaffWorkspace()
    {
        if (_rosterTabPanel == null || _staffWorkspace != null) return;
        _staffWorkspace = new VBoxContainer { Name = "StaffWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _staffWorkspace.AddThemeConstantOverride("separation", 7);
        var header = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _staffWorkspace.AddChild(header);
        var headerText = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        header.AddChild(headerText);
        var title = new Label { Text = "TEAM > STAFF" }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); headerText.AddChild(title);
        _staffOrganizationLabel = new Label { Text = "Franchise staffing directory" }; _staffOrganizationLabel.AddThemeFontSizeOverride("font_size", 12); _staffOrganizationLabel.AddThemeColorOverride("font_color", new Color("9cadb8")); headerText.AddChild(_staffOrganizationLabel);
        var settings = new Button { Text = "FRANCHISE SETTINGS", TooltipText = "Open the existing franchise management utilities.", CustomMinimumSize = new Vector2(160, 30) };
        settings.Pressed += ShowFranchiseSettings; header.AddChild(settings);
        var note = new Label { Text = "Staff changes are available only in the Staff Carousel offseason phase. Implemented scouting, development, recovery, conditioning, and coordinator effects are bounded and explained in each profile.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        note.AddThemeFontSizeOverride("font_size", 11); note.AddThemeColorOverride("font_color", new Color("9cadb8")); _staffWorkspace.AddChild(note);
        var management = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _staffWorkspace.AddChild(management);
        _staffMarketPicker = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, TooltipText = "Available staff-market candidates." };
        management.AddChild(_staffMarketPicker);
        _staffChangeButton = new Button { Text = "SELECT A STAFF ROLE", Disabled = true, CustomMinimumSize = new Vector2(205, 30) };
        _staffChangeButton.Pressed += async () => await ChangeSelectedStaffAsync(); management.AddChild(_staffChangeButton);
        _staffChangeStatus = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _staffChangeStatus.AddThemeFontSizeOverride("font_size", 11); _staffChangeStatus.AddThemeColorOverride("font_color", new Color("9cadb8")); _staffWorkspace.AddChild(_staffChangeStatus);
        _staffTree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _staffTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1));
        _staffTree.ItemSelected += () => OpenSelectedStaffProfile(); _staffTree.ItemActivated += () => OpenSelectedStaffProfile();
        _staffWorkspace.AddChild(_staffTree); _rosterTabPanel.AddChild(_staffWorkspace);
    }

    private void CreateStaffDetailProfileDialog()
    {
        if (_staffDetailDialog != null) return;
        _staffDetailDialog = new AcceptDialog { Name = "StaffDetailProfile", Title = "Staff Profile", MinSize = new Vector2I(620, 430), Exclusive = false };
        _staffDetailDialog.GetOkButton().Text = "CLOSE"; AddChild(_staffDetailDialog);
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _staffDetailDialog.AddChild(content);
        _staffDetailHeader = new Label(); _staffDetailHeader.AddThemeFontSizeOverride("font_size", 18); _staffDetailHeader.AddThemeColorOverride("font_color", new Color("f4eddf")); content.AddChild(_staffDetailHeader);
        _staffDetailBody = new RichTextLabel { BbcodeEnabled = false, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        content.AddChild(_staffDetailBody); ApplyWorkstationTheme(_staffDetailDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private async Task ShowStaffWorkspaceAsync()
    {
        _practiceSquadViewActive = false; _staffViewActive = true; _injuriesViewActive = false; _developmentViewActive = false; _teamHistoryViewActive = false; _teamStandingsViewActive = false; _teamStatsViewActive = false; _teamFinancesViewActive = false; _depthChartViewActive = false; UpdateRosterViewModeUi();
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" })
        {
            var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = false;
        }
        BuildStaffRows(); RenderStaffRows(); UpdateStaffManagementControls(); await Task.CompletedTask;
    }

    private void BuildStaffRows()
    {
        _staffRows.Clear();
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (_staffOrganizationLabel != null) _staffOrganizationLabel.Text = $"{team?.Name ?? "Unassigned franchise"} staff organization · select a role for its profile";
        var roles = new[]
        {
            ("Leadership", "Head Coach"),
            ("Coaching", "Offensive Coordinator"),
            ("Coaching", "Defensive Coordinator"),
            ("Coaching", "Special Teams Coordinator"),
            ("Personnel", "Director of Player Personnel"),
            ("Medical", "Medical Director"),
            ("Performance", "Strength & Conditioning Coach"),
        };
        foreach (var (department, role) in roles)
        {
            var coach = team?.Coaches?.FirstOrDefault(item => string.Equals(item.Role, role, StringComparison.OrdinalIgnoreCase));
            _staffRows.Add(StaffRow.FromCoach(department, role, coach));
        }
        foreach (var coach in team?.Coaches ?? Enumerable.Empty<CoachState>())
        {
            if (_staffRows.Any(row => string.Equals(row.CoachId, coach.CoachId, StringComparison.OrdinalIgnoreCase))) continue;
            _staffRows.Add(StaffRow.FromCoach("Other Staff", string.IsNullOrWhiteSpace(coach.Role) ? "Unspecified role" : coach.Role, coach));
        }
    }

    private void RenderStaffRows()
    {
        if (_staffTree == null) return;
        _staffTree.Clear(); _staffTree.Columns = 4;
        var headers = new[] { "Staff Position", "Assigned Staff Member", "Tendency", "Aptitude in Current Role" };
        for (var column = 0; column < headers.Length; column++) _staffTree.SetColumnTitle(column, headers[column]);
        _staffTree.SetColumnCustomMinimumWidth(0, 190); _staffTree.SetColumnExpand(0, true);
        _staffTree.SetColumnCustomMinimumWidth(1, 210); _staffTree.SetColumnExpand(1, true);
        _staffTree.SetColumnCustomMinimumWidth(2, 170); _staffTree.SetColumnExpand(2, true);
        _staffTree.SetColumnCustomMinimumWidth(3, 190); _staffTree.SetColumnExpand(3, true);
        var root = _staffTree.CreateItem(); string department = null; var index = 0;
        foreach (var row in _staffRows)
        {
            if (!string.Equals(department, row.Department, StringComparison.OrdinalIgnoreCase))
            {
                department = row.Department; var divider = _staffTree.CreateItem(root); divider.SetText(0, department.ToUpperInvariant());
                for (var column = 0; column < headers.Length; column++) { divider.SetCustomBgColor(column, new Color("163242")); divider.SetCustomColor(column, new Color("8fcf98")); }
            }
            var item = _staffTree.CreateItem(root); item.SetMetadata(0, row.CoachId); item.SetMetadata(1, row.Role); item.SetText(0, row.Role); item.SetText(1, row.Member); item.SetText(2, row.Tendency); item.SetText(3, row.Aptitude);
            for (var column = 0; column < headers.Length; column++) item.SetCustomBgColor(column, index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
            item.SetCustomColor(1, row.IsVacant ? new Color("f0c96a") : new Color("f4eddf")); item.SetCustomColor(3, row.IsVacant ? new Color("f0c96a") : new Color("9cadb8"));
        }
    }

    private void OpenSelectedStaffProfile()
    {
        var selected = _staffTree?.GetSelected(); if (selected == null || IsNil(selected.GetMetadata(1))) return;
        var role = selected.GetMetadata(1).AsString(); var coachId = IsNil(selected.GetMetadata(0)) ? string.Empty : selected.GetMetadata(0).AsString();
        var row = _staffRows.FirstOrDefault(item => string.Equals(item.Role, role, StringComparison.OrdinalIgnoreCase) && string.Equals(item.CoachId, coachId, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(row.Role)) return;
        _selectedStaffRole = row.Role; _selectedStaffCoachId = row.CoachId;
        UpdateStaffManagementControls();
        _staffDetailHeader.Text = $"{row.Role} · {row.Member}";
        var authorityNote = ResolveHeadCoachAuthoritySummary(row);
        var staffEffect = row.Role switch
        {
            "Director of Player Personnel" => ResolvePersonnelStaffEffect(row),
            "Head Coach" => ResolveHeadCoachEffect(row),
            "Medical Director" => ResolveMedicalStaffEffect(row),
            "Strength & Conditioning Coach" => ResolveConditioningStaffEffect(row),
            "Offensive Coordinator" or "Defensive Coordinator" => "Paired coordinator strategy can shift simulated team strength by at most 1 point; ratings, fatigue, injuries, and randomness remain primary.",
            _ => "No gameplay effect is modeled for this role yet.",
        };
        _staffDetailBody.Text = $"Role: {row.Role}\nAssigned staff member: {row.Member}\nAge: {row.Age}\nCurrent-role aptitude: {row.Aptitude}\nTendency: {row.Tendency}\n\nCurrent effect: {staffEffect}{authorityNote}\n\nStaff changes are available only during the Staff Carousel phase. Head Coach authority terms are changed only through a new-hire or extension negotiation; contract negotiation is not yet playable.";
        _staffDetailDialog?.PopupCentered(new Vector2I(620, 430));
    }

    private string ResolveHeadCoachAuthoritySummary(StaffRow row)
    {
        if (!string.Equals(row.Role, "Head Coach", StringComparison.OrdinalIgnoreCase) || row.IsVacant)
            return string.Empty;
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var coach = team?.Coaches?.FirstOrDefault(item => string.Equals(item.CoachId, row.CoachId, StringComparison.OrdinalIgnoreCase));
        var controlled = HeadCoachAuthorityService.Domains
            .Where(domain => coach?.Authority?.ControlledDomains?.Contains(domain, StringComparer.OrdinalIgnoreCase) == true)
            .Select(HeadCoachAuthorityService.GetDisplayName)
            .ToList();
        var retained = HeadCoachAuthorityService.Domains
            .Where(domain => coach?.Authority?.ControlledDomains?.Contains(domain, StringComparer.OrdinalIgnoreCase) != true)
            .Select(HeadCoachAuthorityService.GetDisplayName)
            .ToList();
        var coachText = controlled.Count == 0 ? "None" : string.Join(", ", controlled);
        var gmText = retained.Count == 0 ? "None" : string.Join(", ", retained);
        return $"\n\nAuthority agreement\nHead Coach controls: {coachText}\nGeneral Manager retains: {gmText}";
    }

    private static string ResolvePersonnelStaffEffect(StaffRow row)
    {
        if (row.IsVacant) return "No personnel scouting support while this role is vacant.";
        var overall = GetStaffOverall(row.Aptitude);
        var modifier = Math.Clamp((overall - 65) / 4, -3, 6);
        return $"Private scouting confidence {modifier:+#;-#;0} (capped; estimates and hidden ratings do not change).";
    }

    private static string ResolveHeadCoachEffect(StaffRow row)
    {
        if (row.IsVacant) return "No Head Coach development support while this role is vacant.";
        var overall = GetStaffOverall(row.Aptitude);
        return overall >= 82 ? "Eligible rostered players may receive +1 annual development, capped by potential." : "No additional development bonus below 82 overall.";
    }

    private static string ResolveMedicalStaffEffect(StaffRow row)
    {
        if (row.IsVacant) return "No medical recovery support while this role is vacant.";
        return GetStaffOverall(row.Aptitude) >= 85 ? "Active injuries recover one additional day at daily recovery; injury occurrence and eligibility do not change." : "No additional recovery day below 85 overall.";
    }

    private static string ResolveConditioningStaffEffect(StaffRow row)
    {
        if (row.IsVacant) return "No conditioning recovery support while this role is vacant.";
        return GetStaffOverall(row.Aptitude) >= 85 ? "Rostered players recover one additional fatigue point during daily recovery; injury recovery and game workload do not change." : "No additional fatigue recovery below 85 overall.";
    }

    private static int GetStaffOverall(string aptitude)
        => (aptitude ?? string.Empty).Split(' ').Select(part => int.TryParse(part, out var value) ? value : 0).FirstOrDefault(value => value > 0);

    private void UpdateStaffManagementControls()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (_staffMarketPicker == null || _staffChangeButton == null || _staffChangeStatus == null) return;
        _staffMarketPicker.Clear();
        foreach (var coach in (league?.AvailableCoaches ?? new List<CoachState>()).OrderByDescending(item => item.Overall).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            _staffMarketPicker.AddItem($"{coach.Name} · OVR {coach.Overall} · Age {coach.Age}");
            _staffMarketPicker.SetItemMetadata(_staffMarketPicker.ItemCount - 1, coach.CoachId);
        }
        var selected = _staffRows.FirstOrDefault(item => string.Equals(item.Role, _selectedStaffRole, StringComparison.OrdinalIgnoreCase) && string.Equals(item.CoachId, _selectedStaffCoachId, StringComparison.OrdinalIgnoreCase));
        var phaseOpen = StaffService.CanChangeStaff(league, out var phaseError);
        _staffChangeButton.Disabled = !phaseOpen || string.IsNullOrWhiteSpace(selected.Role) || (!selected.IsVacant && string.IsNullOrWhiteSpace(selected.CoachId)) || (selected.IsVacant && _staffMarketPicker.ItemCount == 0);
        _staffChangeButton.Text = string.IsNullOrWhiteSpace(selected.Role) ? "SELECT A STAFF ROLE" : selected.IsVacant ? $"HIRE AS {selected.Role.ToUpperInvariant()}" : $"RELEASE {selected.Member.ToUpperInvariant()}";
        _staffChangeStatus.Text = phaseOpen
            ? string.IsNullOrWhiteSpace(selected.Role) ? "Select a staff role, then release its occupant or hire into a vacancy." : selected.IsVacant ? $"{selected.Role} is vacant. Select a candidate from the staff market." : $"{selected.Role} is filled. Releasing this staff member creates a market vacancy."
            : phaseError;
    }

    private async Task ChangeSelectedStaffAsync()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var selected = _staffRows.FirstOrDefault(item => string.Equals(item.Role, _selectedStaffRole, StringComparison.OrdinalIgnoreCase) && string.Equals(item.CoachId, _selectedStaffCoachId, StringComparison.OrdinalIgnoreCase));
        if (team == null || string.IsNullOrWhiteSpace(selected.Role)) { SetPrimaryStatus("Select a staff role first."); return; }
        var service = new StaffService(_nativeGameCoreContext);
        StaffChangeResult result;
        if (selected.IsVacant)
        {
            if (_staffMarketPicker == null || _staffMarketPicker.Selected < 0 || IsNil(_staffMarketPicker.GetSelectedMetadata())) { SetPrimaryStatus("Select a staff-market candidate first."); return; }
            result = service.HireCoach(team.TeamId, selected.Role, _staffMarketPicker.GetSelectedMetadata().AsString());
        }
        else result = service.ReleaseCoach(team.TeamId, selected.CoachId);
        SetPrimaryStatus(result.Message);
        if (result.Ok) { await SaveNativeAutosave("Staff change saved."); BuildStaffRows(); _selectedStaffCoachId = result.Ok && !selected.IsVacant ? string.Empty : _selectedStaffCoachId; RenderStaffRows(); UpdateStaffManagementControls(); }
    }

    private void CreateTeamHistoryWorkspace()
    {
        if (_rosterTabPanel == null || _teamHistoryWorkspace != null) return;
        _teamHistoryWorkspace = new VBoxContainer { Name = "TeamHistoryWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _teamHistoryWorkspace.AddThemeConstantOverride("separation", 7);
        var header = new VBoxContainer(); _teamHistoryWorkspace.AddChild(header);
        var title = new Label { Text = "TEAM > HISTORY" }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); header.AddChild(title);
        _teamHistoryOrganizationLabel = new Label { Text = "Franchise archive · completed-season records only" }; _teamHistoryOrganizationLabel.AddThemeFontSizeOverride("font_size", 12); _teamHistoryOrganizationLabel.AddThemeColorOverride("font_color", new Color("9cadb8")); header.AddChild(_teamHistoryOrganizationLabel);
        _teamHistoryTabs = new TabContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _teamHistoryWorkspace.AddChild(_teamHistoryTabs);
        _teamSeasonHistoryTree = AddTeamHistoryTab("Season History", "Completed franchise seasons. Select a season for its saved recap.");
        _teamFinancialHistoryTree = AddTeamHistoryTab("Financial History", "Only saved annual financial records appear here.");
        _teamDraftHistoryTree = AddTeamHistoryTab("Draft History", "Completed draft selections archived with the franchise.");
        _teamTransactionHistoryTree = AddTeamHistoryTab("Transaction History", "Chronological persisted franchise transactions.");
        _teamStaffHistoryTree = AddTeamHistoryTab("Coach / Staff History", "Saved staff tenure and staff-change records.");
        _teamSeasonHistoryTree.ColumnTitleClicked += OnTeamHistorySeasonColumnClicked;
        _teamSeasonHistoryTree.ItemSelected += OpenSelectedTeamSeasonRecap;
        _teamSeasonHistoryTree.ItemActivated += OpenSelectedTeamSeasonRecap;
        _teamDraftHistoryTree.ItemActivated += () => _ = OpenSelectedHistoryPlayerProfile(_teamDraftHistoryTree);
        _teamTransactionHistoryTree.ItemActivated += () => _ = OpenSelectedHistoryPlayerProfile(_teamTransactionHistoryTree);
        _rosterTabPanel.AddChild(_teamHistoryWorkspace);
    }

    private Tree AddTeamHistoryTab(string name, string hint)
    {
        var panel = new VBoxContainer { Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var description = new Label { Text = hint, AutowrapMode = TextServer.AutowrapMode.WordSmart }; description.AddThemeFontSizeOverride("font_size", 11); description.AddThemeColorOverride("font_color", new Color("9cadb8")); panel.AddChild(description);
        var tree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        tree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); panel.AddChild(tree); _teamHistoryTabs.AddChild(panel); return tree;
    }

    private void CreateTeamSeasonRecapDialog()
    {
        if (_teamSeasonRecapDialog != null) return;
        _teamSeasonRecapDialog = new AcceptDialog { Name = "TeamSeasonRecap", Title = "Season Recap", MinSize = new Vector2I(720, 530), Exclusive = false };
        _teamSeasonRecapDialog.GetOkButton().Text = "CLOSE"; AddChild(_teamSeasonRecapDialog);
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _teamSeasonRecapDialog.AddChild(content);
        _teamSeasonRecapHeader = new Label(); _teamSeasonRecapHeader.AddThemeFontSizeOverride("font_size", 18); _teamSeasonRecapHeader.AddThemeColorOverride("font_color", new Color("f4eddf")); content.AddChild(_teamSeasonRecapHeader);
        _teamSeasonRecapBody = new RichTextLabel { BbcodeEnabled = false, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; content.AddChild(_teamSeasonRecapBody);
        ApplyWorkstationTheme(_teamSeasonRecapDialog, new Color("101f2d"), new Color("294559"), new Color("f4eddf"), new Color("aeb9bd"), new Color("4f9b55"));
    }

    private async Task ShowTeamHistoryWorkspaceAsync()
    {
        _practiceSquadViewActive = false; _teamHistoryViewActive = true; _staffViewActive = false; _injuriesViewActive = false; _developmentViewActive = false; _teamStandingsViewActive = false; _teamStatsViewActive = false; _teamFinancesViewActive = false; _depthChartViewActive = false; UpdateRosterViewModeUi();
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" })
        {
            var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = false;
        }
        BuildTeamHistoryViews(); await Task.CompletedTask;
    }

    private void BuildTeamHistoryViews()
    {
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (_teamHistoryOrganizationLabel != null) _teamHistoryOrganizationLabel.Text = $"{team?.Name ?? "Unassigned franchise"} archive · saved franchise records only";
        BuildTeamSeasonRows(league, team); RenderTeamSeasonHistory(); RenderUnavailableHistory(_teamFinancialHistoryTree, new[] { "Season", "Revenue", "Payroll / Cap", "Attendance", "Stadium Costs" }, "No annual financial history has been saved for this franchise.");
        RenderTeamDraftHistory(league, team); RenderTeamTransactionHistory(league, team); RenderTeamStaffHistory(league, team);
    }

    private void BuildTeamSeasonRows(LeagueState league, TeamState team)
    {
        _teamHistorySeasons.Clear(); if (league == null || team == null) return;
        foreach (var season in league.HistoricalSeasons ?? Enumerable.Empty<SeasonHistoryRecord>())
        {
            var record = season?.TeamRecords?.FirstOrDefault(item => string.Equals(item.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase)); if (record == null) continue;
            var divisionRows = season.TeamRecords.Where(item => string.Equals(item.Division, record.Division, StringComparison.OrdinalIgnoreCase)).OrderByDescending(item => item.WinPercentage).ThenByDescending(item => item.PointsFor - item.PointsAgainst).ThenBy(item => item.TeamName).ToList();
            var divisionFinish = divisionRows.FindIndex(item => string.Equals(item.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase)) + 1;
            var seed = season.PlayoffSeeds?.FirstOrDefault(item => string.Equals(item.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase));
            var teamPlayoffs = season.PlayoffResults?.Where(item => string.Equals(item.HomeTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase) || string.Equals(item.AwayTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase)).ToList() ?? new List<SeasonPlayoffResultRecord>();
            var championship = string.Equals(season.ChampionTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase);
            var conferenceWinner = teamPlayoffs.Any(item => string.Equals(NormalizeHistoryRound(item.Round), "Conference Championship", StringComparison.OrdinalIgnoreCase) && string.Equals(item.WinnerTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase));
            var playoffResult = championship ? "League Champions" : conferenceWinner ? "Conference Champions" : teamPlayoffs.Count > 0 ? $"Eliminated: {NormalizeHistoryRound(teamPlayoffs.Last().Round)}" : "Missed playoffs";
            _teamHistorySeasons.Add(new TeamHistorySeasonRow(season.SeasonYear, $"{record.Wins}-{record.Losses}" + (record.Ties > 0 ? $"-{record.Ties}" : ""), divisionFinish > 0 ? $"{divisionFinish}{OrdinalSuffix(divisionFinish)} · {record.Division}" : "Division finish unavailable", seed == null ? playoffResult : $"Seed {seed.Seed} · {playoffResult}", championship ? "LEAGUE TITLE" : conferenceWinner ? "Conference title" : "No championship", season));
        }
    }

    private void RenderTeamSeasonHistory()
    {
        if (_teamSeasonHistoryTree == null) return;
        var headers = new[] { "Season", "Record", "Division Finish", "Playoff Result", "Championship Result" }; ConfigureHistoryTree(_teamSeasonHistoryTree, headers, new[] { 78, 88, 190, 220, 170 });
        IEnumerable<TeamHistorySeasonRow> rows = _teamHistorySeasons;
        rows = _teamHistorySortColumn switch { "record" => _teamHistorySortAscending ? rows.OrderBy(row => row.Record) : rows.OrderByDescending(row => row.Record), "division" => _teamHistorySortAscending ? rows.OrderBy(row => row.DivisionFinish) : rows.OrderByDescending(row => row.DivisionFinish), _ => _teamHistorySortAscending ? rows.OrderBy(row => row.Season) : rows.OrderByDescending(row => row.Season) };
        var root = _teamSeasonHistoryTree.CreateItem(); var index = 0;
        foreach (var row in rows)
        {
            var item = _teamSeasonHistoryTree.CreateItem(root); item.SetMetadata(0, row.Season); item.SetText(0, row.Season.ToString()); item.SetText(1, row.Record); item.SetText(2, row.DivisionFinish); item.SetText(3, row.PlayoffResult); item.SetText(4, row.ChampionshipResult);
            for (var column = 0; column < headers.Length; column++) item.SetCustomBgColor(column, index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
            var milestone = row.ChampionshipResult == "LEAGUE TITLE" ? new Color("f0c96a") : row.PlayoffResult.Contains("Conference", StringComparison.OrdinalIgnoreCase) ? new Color("8fcf98") : new Color("9cadb8"); item.SetCustomColor(3, milestone); item.SetCustomColor(4, milestone);
        }
        if (!_teamHistorySeasons.Any()) AddHistoryEmptyRow(_teamSeasonHistoryTree, root, "No completed seasons have been saved for this franchise yet.");
    }

    private void RenderUnavailableHistory(Tree tree, string[] headers, string message)
    {
        ConfigureHistoryTree(tree, headers, Enumerable.Repeat(150, headers.Length).ToArray()); var root = tree.CreateItem(); AddHistoryEmptyRow(tree, root, message);
    }

    private void RenderTeamDraftHistory(LeagueState league, TeamState team)
    {
        var headers = new[] { "Year", "Pick", "Player", "Position", "Current / Career Outcome" }; ConfigureHistoryTree(_teamDraftHistoryTree, headers, new[] { 70, 90, 210, 90, 260 }); var root = _teamDraftHistoryTree.CreateItem(); var index = 0;
        var drafts = (league?.HistoricalDrafts ?? new List<DraftState>()).Concat(league?.Draft?.IsCompleted == true ? new[] { league.Draft } : Enumerable.Empty<DraftState>());
        foreach (var draft in drafts.Where(item => item != null).OrderByDescending(item => item.DraftYear))
            foreach (var entry in (draft.RecapEntries ?? new List<DraftClassRecapEntry>()).Where(item => string.Equals(item.TeamId, team?.TeamId, StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.OverallPick))
            {
                var item = _teamDraftHistoryTree.CreateItem(root); item.SetMetadata(0, entry.PlayerId); item.SetText(0, draft.DraftYear.ToString()); item.SetText(1, $"R{entry.Round} · #{entry.OverallPick}"); item.SetText(2, string.IsNullOrWhiteSpace(entry.Name) ? "Player unavailable" : entry.Name); item.SetText(3, string.IsNullOrWhiteSpace(entry.Position) ? "Unavailable" : entry.Position); item.SetText(4, string.IsNullOrWhiteSpace(entry.RookiePlacement) ? "Career outcome unavailable" : entry.RookiePlacement);
                for (var column = 0; column < headers.Length; column++) item.SetCustomBgColor(column, index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
            }
        if (index == 0) AddHistoryEmptyRow(_teamDraftHistoryTree, root, "No completed draft selections have been saved for this franchise.");
    }

    private void RenderTeamTransactionHistory(LeagueState league, TeamState team)
    {
        var headers = new[] { "Season", "Date", "Type", "Player", "Details" }; ConfigureHistoryTree(_teamTransactionHistoryTree, headers, new[] { 72, 130, 150, 190, 300 }); var root = _teamTransactionHistoryTree.CreateItem(); var index = 0;
        foreach (var transaction in (league?.Transactions ?? new List<TransactionRecord>()).Where(item => string.Equals(item.TeamId, team?.TeamId, StringComparison.OrdinalIgnoreCase)).OrderByDescending(item => item.TransactionId, StringComparer.OrdinalIgnoreCase))
        {
            var item = _teamTransactionHistoryTree.CreateItem(root); item.SetMetadata(0, transaction.PlayerId); item.SetText(0, transaction.SeasonYear > 0 ? transaction.SeasonYear.ToString() : "Unavailable"); item.SetText(1, string.IsNullOrWhiteSpace(transaction.DateLabel) ? "Date unavailable" : transaction.DateLabel); item.SetText(2, string.IsNullOrWhiteSpace(transaction.Type) ? "Type unavailable" : transaction.Type); item.SetText(3, string.IsNullOrWhiteSpace(transaction.PlayerName) ? "Player unavailable" : transaction.PlayerName); item.SetText(4, string.IsNullOrWhiteSpace(transaction.Details) ? "Details unavailable" : transaction.Details);
            for (var column = 0; column < headers.Length; column++) item.SetCustomBgColor(column, index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
        }
        if (index == 0) AddHistoryEmptyRow(_teamTransactionHistoryTree, root, "No persisted franchise transactions are available.");
    }

    private void RenderTeamStaffHistory(LeagueState league, TeamState team)
    {
        var headers = new[] { "Role / Change", "Staff Member", "Date", "Details" }; ConfigureHistoryTree(_teamStaffHistoryTree, headers, new[] { 170, 210, 130, 330 }); var root = _teamStaffHistoryTree.CreateItem(); var index = 0;
        foreach (var transaction in (league?.Transactions ?? new List<TransactionRecord>()).Where(item => string.Equals(item.TeamId, team?.TeamId, StringComparison.OrdinalIgnoreCase) && item.Type.StartsWith("staff_", StringComparison.OrdinalIgnoreCase)).OrderByDescending(item => item.TransactionId, StringComparer.OrdinalIgnoreCase))
        {
            var item = _teamStaffHistoryTree.CreateItem(root); item.SetText(0, transaction.Type.Replace('_', ' ')); item.SetText(1, string.IsNullOrWhiteSpace(transaction.StaffName) ? "Staff member unavailable" : transaction.StaffName); item.SetText(2, string.IsNullOrWhiteSpace(transaction.DateLabel) ? "Date unavailable" : transaction.DateLabel); item.SetText(3, string.IsNullOrWhiteSpace(transaction.Details) ? "Details unavailable" : transaction.Details);
            for (var column = 0; column < headers.Length; column++) item.SetCustomBgColor(column, index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
        }
        if (index == 0) AddHistoryEmptyRow(_teamStaffHistoryTree, root, "No staff changes have been saved for this franchise.");
    }

    private static void ConfigureHistoryTree(Tree tree, string[] headers, int[] widths)
    {
        if (tree == null) return; tree.Clear(); tree.Columns = headers.Length;
        for (var column = 0; column < headers.Length; column++) { tree.SetColumnTitle(column, headers[column]); tree.SetColumnCustomMinimumWidth(column, widths[Math.Min(column, widths.Length - 1)]); tree.SetColumnExpand(column, column >= 2); }
    }

    private static void AddHistoryEmptyRow(Tree tree, TreeItem root, string message)
    {
        if (tree == null || root == null) return; var empty = tree.CreateItem(root); empty.SetText(0, message); empty.SetCustomColor(0, new Color("9cadb8"));
    }

    private static string OrdinalSuffix(int value)
    {
        var remainder = value % 100;
        if (remainder is 11 or 12 or 13) return "th";
        return (value % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
    }

    private void OnTeamHistorySeasonColumnClicked(long column, long mouseButton)
    {
        var id = column switch { 1 => "record", 2 => "division", _ => "season" }; _teamHistorySortAscending = _teamHistorySortColumn == id ? !_teamHistorySortAscending : id != "season"; _teamHistorySortColumn = id; RenderTeamSeasonHistory();
    }

    private void OpenSelectedTeamSeasonRecap()
    {
        var selected = _teamSeasonHistoryTree?.GetSelected(); if (selected == null || IsNil(selected.GetMetadata(0))) return; var year = (int)selected.GetMetadata(0).AsInt64(); var row = _teamHistorySeasons.FirstOrDefault(item => item.Season == year); if (row.Source == null) return;
        var source = row.Source; _teamSeasonRecapHeader.Text = $"{row.Season} · {row.Record} · {row.ChampionshipResult}";
        var teamId = _nativeGameCoreContext?.ActiveLeague?.UserTeamId ?? ""; var playoffGames = (source.PlayoffResults ?? new List<SeasonPlayoffResultRecord>()).Where(game => string.Equals(game.HomeTeamId, teamId, StringComparison.OrdinalIgnoreCase) || string.Equals(game.AwayTeamId, teamId, StringComparison.OrdinalIgnoreCase)).ToList();
        var awards = (source.Awards ?? new List<SeasonAwardRecord>()).Where(award => string.Equals(award.TeamId, teamId, StringComparison.OrdinalIgnoreCase)).ToList();
        var lines = new List<string> { $"Division finish: {row.DivisionFinish}", $"Playoff result: {row.PlayoffResult}", $"League result: {row.ChampionshipResult}", "", "Postseason path:" };
        if (playoffGames.Count == 0) lines.Add("No saved postseason games for this franchise."); else foreach (var game in playoffGames) lines.Add($"{NormalizeHistoryRound(game.Round)}: {game.HomeTeamName} {game.HomeScore}, {game.AwayTeamName} {game.AwayScore} · Winner: {game.WinnerTeamName}");
        lines.Add("\nAwards:"); if (awards.Count == 0) lines.Add("No saved franchise awards for this season."); else foreach (var award in awards) lines.Add($"{award.AwardName}: {award.PlayerName} ({award.Position}) — {award.Summary}");
        lines.Add("\nSeason leaders: unavailable; team leader snapshots are not saved in the season archive."); lines.Add($"Notable event: {source.ChampionshipGameLabel} — {source.ChampionTeamName} {source.ChampionshipWinnerScore}, {source.RunnerUpTeamName} {source.ChampionshipRunnerUpScore}.");
        _teamSeasonRecapBody.Text = string.Join("\n", lines); _teamSeasonRecapDialog?.PopupCentered(new Vector2I(720, 530));
    }

    private async Task OpenSelectedHistoryPlayerProfile(Tree source)
    {
        var selected = source?.GetSelected(); if (selected == null || IsNil(selected.GetMetadata(0))) return; var playerId = selected.GetMetadata(0).AsString();
        if (string.IsNullOrWhiteSpace(playerId)) { SetPrimaryStatus("A player profile is not available for this archived record."); return; }
        await SetRosterViewMode(false); await RefreshRosterTab(); TrySelectRosterPlayer(playerId);
    }

    private void CreateTeamStandingsWorkspace()
    {
        if (_rosterTabPanel == null || _teamStandingsWorkspace != null) return;
        _teamStandingsWorkspace = new ScrollContainer { Name = "TeamStandingsWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto, VerticalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var content = new VBoxContainer { CustomMinimumSize = new Vector2(900, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _teamStandingsWorkspace.AddChild(content);
        var header = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; content.AddChild(header);
        var titleColumn = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; header.AddChild(titleColumn);
        var title = new Label { Text = "TEAM > STANDINGS" }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); titleColumn.AddChild(title);
        _teamStandingsContext = new Label { Text = "Division and conference playoff context" }; _teamStandingsContext.AddThemeFontSizeOverride("font_size", 12); _teamStandingsContext.AddThemeColorOverride("font_color", new Color("9cadb8")); titleColumn.AddChild(_teamStandingsContext);
        var fullStandings = new Button { Text = "OPEN LEAGUE STANDINGS", TooltipText = "Open the authoritative full-league standings view.", CustomMinimumSize = new Vector2(190, 30) }; fullStandings.Pressed += async () => await OpenFullLeagueStandingsAsync(); header.AddChild(fullStandings);
        _teamStandingsPanels = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _teamStandingsPanels.AddThemeConstantOverride("separation", 8); content.AddChild(_teamStandingsPanels);
        _teamDivisionStandingsTree = AddTeamStandingsPanel("DIVISION RACE", "Your division · record and games behind", new Color("8fcf98"));
        _teamConferencePlayoffTree = AddTeamStandingsPanel("CONFERENCE PLAYOFF PICTURE", "Saved playoff seeds when the season state provides them", new Color("f0c96a"));
        _rosterTabPanel.AddChild(_teamStandingsWorkspace);
    }

    private Tree AddTeamStandingsPanel(string heading, string hint, Color accent)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(440, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; panel.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("0d2031"), new Color("254258"), 0, 1)); _teamStandingsPanels.AddChild(panel);
        var content = new VBoxContainer(); content.AddThemeConstantOverride("separation", 4); panel.AddChild(content);
        var label = new Label { Text = heading }; label.AddThemeFontSizeOverride("font_size", 14); label.AddThemeColorOverride("font_color", accent); content.AddChild(label);
        var description = new Label { Text = hint, AutowrapMode = TextServer.AutowrapMode.WordSmart }; description.AddThemeFontSizeOverride("font_size", 11); description.AddThemeColorOverride("font_color", new Color("9cadb8")); content.AddChild(description);
        var tree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; tree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); content.AddChild(tree); return tree;
    }

    private async Task ShowTeamStandingsWorkspaceAsync()
    {
        _practiceSquadViewActive = false; _teamStandingsViewActive = true; _teamHistoryViewActive = false; _staffViewActive = false; _injuriesViewActive = false; _developmentViewActive = false; _teamStatsViewActive = false; _teamFinancesViewActive = false; _depthChartViewActive = false; UpdateRosterViewModeUi();
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" }) { var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = false; }
        RenderTeamStandingsContext(); await Task.CompletedTask;
    }

    private void RenderTeamStandingsContext()
    {
        var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var hasResults = league?.Results?.Any(ScheduleService.CountsTowardRegularSeasonStandings) == true;
        if (league == null || team == null || !hasResults)
        {
            if (_teamStandingsContext != null) _teamStandingsContext.Text = league == null ? "Standings unavailable: no active franchise." : "Preseason / no regular-season standings have been recorded.";
            RenderTeamStandingsEmpty("No regular-season standings are available yet."); return;
        }
        var response = new StandingsService(_nativeGameCoreContext).GetStandings();
        if (response?.Ok != true) { if (_teamStandingsContext != null) _teamStandingsContext.Text = "Standings unavailable."; RenderTeamStandingsEmpty(response?.Error ?? "Standings could not be loaded."); return; }
        var user = response.Standings.FirstOrDefault(item => string.Equals(item.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase));
        if (user == null) { RenderTeamStandingsEmpty("Your franchise has no current standings row."); return; }
        var division = response.Standings.Where(item => string.Equals(item.Division, user.Division, StringComparison.OrdinalIgnoreCase)).OrderByDescending(item => item.WinPct).ThenByDescending(item => item.PointsFor - item.PointsAgainst).ToList();
        var leader = division.FirstOrDefault(); var gamesBack = ((leader?.Wins ?? 0) - user.Wins + user.Losses - (leader?.Losses ?? 0)) / 2d;
        var seeds = response.PlayoffBracket?.ConferenceBrackets?.FirstOrDefault(item => string.Equals(item.Conference, user.Conference, StringComparison.OrdinalIgnoreCase))?.Seeds ?? new List<PlayoffSeedDto>();
        var userSeed = seeds.FirstOrDefault(item => string.Equals(item.TeamId, user.TeamId, StringComparison.OrdinalIgnoreCase));
        var postseason = userSeed == null ? "Postseason seed unavailable" : $"Seed {userSeed.Seed} · {(userSeed.IsDivisionWinner ? "division winner" : "wild card")}";
        if (_teamStandingsContext != null) _teamStandingsContext.Text = $"{team.Name} · {user.Wins}-{user.Losses}{(user.Ties > 0 ? $"-{user.Ties}" : "")} · {gamesBack:0.0} GB · {postseason}";
        RenderTeamDivisionTable(division, user.TeamId, leader?.TeamId); RenderTeamConferenceTable(seeds, user.TeamId, user.Conference);
    }

    private void RenderTeamDivisionTable(List<StandingRowDto> rows, string userTeamId, string leaderTeamId)
    {
        var headers = new[] { "Team", "W-L-T", "GB", "Status" }; ConfigureHistoryTree(_teamDivisionStandingsTree, headers, new[] { 175, 80, 60, 150 }); var root = _teamDivisionStandingsTree.CreateItem(); var index = 0; var leader = rows.FirstOrDefault();
        foreach (var row in rows)
        {
            var gap = ((leader?.Wins ?? 0) - row.Wins + row.Losses - (leader?.Losses ?? 0)) / 2d; var isUser = string.Equals(row.TeamId, userTeamId, StringComparison.OrdinalIgnoreCase); var item = _teamDivisionStandingsTree.CreateItem(root); item.SetText(0, (isUser ? "◆ " : "") + row.TeamName); item.SetText(1, $"{row.Wins}-{row.Losses}" + (row.Ties > 0 ? $"-{row.Ties}" : "")); item.SetText(2, string.Equals(row.TeamId, leaderTeamId, StringComparison.OrdinalIgnoreCase) ? "—" : gap.ToString("0.0")); item.SetText(3, isUser ? "YOUR TEAM" : string.Equals(row.TeamId, leaderTeamId, StringComparison.OrdinalIgnoreCase) ? "Division leader" : "Division race");
            for (var column = 0; column < headers.Length; column++) item.SetCustomBgColor(column, isUser ? new Color("193d37") : index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); item.SetTextAlignment(1, HorizontalAlignment.Right); item.SetTextAlignment(2, HorizontalAlignment.Right); if (isUser) item.SetCustomColor(3, new Color("8fcf98"));
        }
    }

    private void RenderTeamConferenceTable(List<PlayoffSeedDto> seeds, string userTeamId, string conference)
    {
        var headers = new[] { "Seed", "Team", "Record", "Postseason Status" }; ConfigureHistoryTree(_teamConferencePlayoffTree, headers, new[] { 56, 190, 85, 170 }); var root = _teamConferencePlayoffTree.CreateItem();
        if (seeds == null || seeds.Count == 0) { AddHistoryEmptyRow(_teamConferencePlayoffTree, root, $"No saved {conference} playoff seeds are available in the current season state."); return; }
        var index = 0;
        foreach (var seed in seeds.OrderBy(item => item.Seed))
        {
            var isUser = string.Equals(seed.TeamId, userTeamId, StringComparison.OrdinalIgnoreCase); var item = _teamConferencePlayoffTree.CreateItem(root); item.SetText(0, seed.Seed.ToString()); item.SetText(1, (isUser ? "◆ " : "") + seed.TeamName); item.SetText(2, $"{seed.Wins}-{seed.Losses}" + (seed.Ties > 0 ? $"-{seed.Ties}" : "")); item.SetText(3, seed.IsDivisionWinner ? "Division winner" : "Wild card");
            for (var column = 0; column < headers.Length; column++) item.SetCustomBgColor(column, isUser ? new Color("193d37") : index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); item.SetTextAlignment(0, HorizontalAlignment.Right); item.SetTextAlignment(2, HorizontalAlignment.Right); if (isUser) item.SetCustomColor(3, new Color("f0c96a"));
        }
    }

    private void RenderTeamStandingsEmpty(string message)
    {
        ConfigureHistoryTree(_teamDivisionStandingsTree, new[] { "Team", "W-L-T", "GB", "Status" }, new[] { 175, 80, 60, 150 }); var divisionRoot = _teamDivisionStandingsTree.CreateItem(); AddHistoryEmptyRow(_teamDivisionStandingsTree, divisionRoot, message);
        ConfigureHistoryTree(_teamConferencePlayoffTree, new[] { "Seed", "Team", "Record", "Postseason Status" }, new[] { 56, 190, 85, 170 }); var conferenceRoot = _teamConferencePlayoffTree.CreateItem(); AddHistoryEmptyRow(_teamConferencePlayoffTree, conferenceRoot, message);
    }

    private async Task OpenFullLeagueStandingsAsync()
    {
        SetLeagueStatsWorkspaceVisible(false);
        SetLeagueScheduleWorkspaceVisible(false);
        SetLeagueNewsWorkspaceVisible(false);
        await SelectMainTab(LEAGUE_TAB_INDEX);
        if (_leagueHubTabs != null && _leagueHubTabs.GetTabCount() > 0) _leagueHubTabs.CurrentTab = 0;
    }

    private void CreateLeagueStatsWorkspace()
    {
        if (_leagueTabPanel == null || _leagueStatsWorkspace != null) return;
        _leagueStatsWorkspace = new VBoxContainer { Name = "LeagueStatsWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leagueStatsWorkspace.AddThemeConstantOverride("separation", 7); _leagueTabPanel.AddChild(_leagueStatsWorkspace);
        var header = new HBoxContainer(); _leagueStatsWorkspace.AddChild(header); var title = new Label { Text = "LEAGUE > STATS", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); header.AddChild(title); var standings = new Button { Text = "STANDINGS" }; standings.Pressed += async () => await OpenFullLeagueStandingsAsync(); header.AddChild(standings);
        var controls = new HBoxContainer(); controls.AddThemeConstantOverride("separation", 6); _leagueStatsWorkspace.AddChild(controls); controls.AddChild(HomeLabel("CATEGORY", 11, new Color("9cadb8"))); _leagueStatsCategory = new OptionButton(); _leagueStatsCategory.AddItem("Player leaders"); _leagueStatsCategory.AddItem("Team comparison"); _leagueStatsCategory.ItemSelected += _ => RenderLeagueStats(); controls.AddChild(_leagueStatsCategory); controls.AddChild(HomeLabel("STATISTIC", 11, new Color("9cadb8"))); _leagueStatsMeasure = new OptionButton(); foreach (var item in new[] { "Passing yards", "Rushing yards", "Receiving yards", "Tackles", "Sacks", "Interceptions", "Points per game" }) _leagueStatsMeasure.AddItem(item); _leagueStatsMeasure.ItemSelected += _ => RenderLeagueStats(); controls.AddChild(_leagueStatsMeasure); controls.AddChild(HomeLabel("VIEW", 11, new Color("9cadb8"))); _leagueStatsViewMode = new OptionButton(); _leagueStatsViewMode.AddItem("Leader table"); _leagueStatsViewMode.AddItem("Comparison tiles"); _leagueStatsViewMode.ItemSelected += _ => RenderLeagueStats(); controls.AddChild(_leagueStatsViewMode);
        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto }; _leagueStatsWorkspace.AddChild(scroll); _leagueStatsTree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(850, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leagueStatsTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); _leagueStatsTree.ColumnTitleClicked += (_, _) => RenderLeagueStats(); _leagueStatsTree.ItemActivated += () => _ = OpenSelectedLeagueStatsProfile(); scroll.AddChild(_leagueStatsTree);
    }

    private async Task ShowLeagueStatsWorkspaceAsync()
    {
        await SelectMainTab(LEAGUE_TAB_INDEX); SetLeagueScheduleWorkspaceVisible(false); SetLeagueNewsWorkspaceVisible(false); SetLeagueStatsWorkspaceVisible(true); RenderLeagueStats();
    }

    private void SetLeagueStatsWorkspaceVisible(bool visible)
    {
        _leagueStatsWorkspaceActive = visible; if (_leagueStatsWorkspace != null) _leagueStatsWorkspace.Visible = visible; var hub = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel"); if (hub != null) hub.Visible = !visible;
    }

    private void RenderLeagueStats()
    {
        if (_leagueStatsTree == null) return; _leagueStatsTree.Clear(); var league = _nativeGameCoreContext?.ActiveLeague; var category = _leagueStatsCategory?.GetItemText(_leagueStatsCategory.Selected) ?? "Player leaders"; var measure = _leagueStatsMeasure?.GetItemText(_leagueStatsMeasure.Selected) ?? "Passing yards"; var tileView = _leagueStatsViewMode?.Selected == 1;
        _leagueStatsTree.Columns = tileView ? 3 : 5; var headers = tileView ? new[] { "Scope", "Current season", "Value / Unit" } : new[] { "Rank", category == "Player leaders" ? "Player" : "Team", "Position / Division", "Value", "Scope" }; for (var column = 0; column < headers.Length; column++) { _leagueStatsTree.SetColumnTitle(column, headers[column]); _leagueStatsTree.SetColumnCustomMinimumWidth(column, column == 1 ? 230 : 110); _leagueStatsTree.SetColumnExpand(column, column is 1 or 4); }
        var root = _leagueStatsTree.CreateItem();
        if (league == null) { AddHistoryEmptyRow(_leagueStatsTree, root, "No active league statistics are available."); return; }
        if (category == "Team comparison") RenderLeagueTeamStats(root, league, measure, tileView); else RenderLeaguePlayerStats(root, league, measure, tileView);
    }

    private void RenderLeaguePlayerStats(TreeItem root, LeagueState league, string measure, bool tileView)
    {
        Func<PlayerState, int> value = measure switch { "Rushing yards" => player => player.SeasonStats?.RushingYards ?? 0, "Receiving yards" => player => player.SeasonStats?.ReceivingYards ?? 0, "Tackles" => player => player.SeasonStats?.Tackles ?? 0, "Sacks" => player => player.SeasonStats?.Sacks ?? 0, "Interceptions" => player => player.SeasonStats?.Interceptions ?? 0, _ => player => player.SeasonStats?.PassingYards ?? 0 }; var leaders = league.Teams.SelectMany(team => team.Roster.Select(player => (team, player))).OrderByDescending(item => value(item.player)).Take(tileView ? 6 : 60).ToList(); if (leaders.All(item => value(item.player) == 0)) { AddHistoryEmptyRow(_leagueStatsTree, root, $"No {measure.ToLowerInvariant()} have been recorded in the current season."); return; }
        var rank = 1; foreach (var (team, player) in leaders) { var item = _leagueStatsTree.CreateItem(root); item.SetMetadata(0, player.PlayerId); item.SetMetadata(1, team.TeamId); if (tileView) { item.SetText(0, $"#{rank++} {player.Name}"); item.SetText(1, $"{team.Name} · {player.Position}"); item.SetText(2, $"{value(player):N0} {measure}"); } else { item.SetText(0, (rank++).ToString()); item.SetText(1, player.Name); item.SetText(2, $"{player.Position} · {team.Name}"); item.SetText(3, value(player).ToString("N0")); item.SetText(4, "Current season · total"); item.SetTextAlignment(0, HorizontalAlignment.Right); item.SetTextAlignment(3, HorizontalAlignment.Right); } }
    }

    private void RenderLeagueTeamStats(TreeItem root, LeagueState league, string measure, bool tileView)
    {
        var completed = (league.Results ?? new List<GameResult>()).Where(ScheduleService.CountsTowardRegularSeasonStandings).ToList(); if (completed.Count == 0) { AddHistoryEmptyRow(_leagueStatsTree, root, "No completed regular-season games are available for team comparison."); return; }
        var values = league.Teams.Select(team => new { Team = team, Value = measure == "Points per game" ? completed.Where(game => game.HomeTeamId == team.TeamId || game.AwayTeamId == team.TeamId).DefaultIfEmpty().Sum(game => game == null ? 0 : game.HomeTeamId == team.TeamId ? game.HomeScore : game.AwayScore) : 0 }).OrderByDescending(item => item.Value).ToList(); var rank = 1; foreach (var row in values) { var item = _leagueStatsTree.CreateItem(root); item.SetMetadata(1, row.Team.TeamId); if (tileView) { item.SetText(0, $"#{rank++} {row.Team.Name}"); item.SetText(1, row.Team.Division); item.SetText(2, measure == "Points per game" ? $"{row.Value} points · total" : "Unavailable — team aggregate not recorded"); } else { item.SetText(0, (rank++).ToString()); item.SetText(1, row.Team.Name); item.SetText(2, row.Team.Division); item.SetText(3, measure == "Points per game" ? row.Value.ToString() : "Unavailable"); item.SetText(4, measure == "Points per game" ? "Current season · total points" : "Team aggregate not recorded"); } }
    }

    private async Task OpenSelectedLeagueStatsProfile()
    {
        var selected = _leagueStatsTree?.GetSelected(); if (selected == null) return; var playerId = IsNil(selected.GetMetadata(0)) ? "" : selected.GetMetadata(0).AsString(); var teamId = IsNil(selected.GetMetadata(1)) ? "" : selected.GetMetadata(1).AsString(); if (string.IsNullOrWhiteSpace(teamId)) return; await SelectMainTab(ROSTER_TAB_INDEX); await TrySelectTeamInRoster(teamId); if (!string.IsNullOrWhiteSpace(playerId)) TrySelectRosterPlayer(playerId);
    }

    private void CreateLeagueScheduleWorkspace()
    {
        if (_leagueTabPanel == null || _leagueScheduleWorkspace != null) return;
        _leagueScheduleWorkspace = new VBoxContainer { Name = "LeagueScheduleWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leagueScheduleWorkspace.AddThemeConstantOverride("separation", 7); _leagueTabPanel.AddChild(_leagueScheduleWorkspace);
        var header = new HBoxContainer(); _leagueScheduleWorkspace.AddChild(header); var title = new Label { Text = "LEAGUE > SCHEDULE & RESULTS", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); header.AddChild(title); header.AddChild(HomeLabel("WEEK", 11, new Color("9cadb8"))); _leagueScheduleWeekPicker = new OptionButton(); _leagueScheduleWeekPicker.ItemSelected += _ => RenderLeagueScheduleWorkspace(); header.AddChild(_leagueScheduleWeekPicker);
        _leagueScheduleTabs = new TabContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leagueScheduleWorkspace.AddChild(_leagueScheduleTabs); _leagueWeekScheduleTree = AddLeagueScheduleTab("Weekly Schedule", "Complete league slate for the selected week."); _leaguePlayoffTree = AddLeagueScheduleTab("Playoff Tree", "Current saved postseason bracket and progression."); _leagueWeekScheduleTree.ItemActivated += () => _ = OpenSelectedLeagueScheduleTeam(); _leaguePlayoffTree.ItemActivated += () => _ = OpenSelectedLeagueScheduleTeam();
    }

    private Tree AddLeagueScheduleTab(string name, string description)
    {
        var panel = new VBoxContainer { Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; var hint = HomeLabel(description, 11, new Color("9cadb8")); panel.AddChild(hint); var tree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; tree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); panel.AddChild(tree); _leagueScheduleTabs.AddChild(panel); return tree;
    }

    private async Task ShowLeagueScheduleWorkspaceAsync()
    {
        await SelectMainTab(LEAGUE_TAB_INDEX); SetLeagueStatsWorkspaceVisible(false); SetLeagueNewsWorkspaceVisible(false); SetLeagueScheduleWorkspaceVisible(true); PopulateLeagueScheduleWeeks(); RenderLeagueScheduleWorkspace();
    }

    private void SetLeagueScheduleWorkspaceVisible(bool visible)
    {
        _leagueScheduleWorkspaceActive = visible; if (_leagueScheduleWorkspace != null) _leagueScheduleWorkspace.Visible = visible; var hub = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel"); if (hub != null) hub.Visible = !visible;
    }

    private void PopulateLeagueScheduleWeeks()
    {
        if (_leagueScheduleWeekPicker == null) return; var league = _nativeGameCoreContext?.ActiveLeague; var current = _leagueScheduleWeekPicker.Selected >= 0 ? _leagueScheduleWeekPicker.GetItemText(_leagueScheduleWeekPicker.Selected) : ""; _leagueScheduleWeekPicker.Clear(); var weeks = (league?.Schedule ?? new List<ScheduledGame>()).Select(game => game.AbsoluteWeek).Distinct().OrderBy(week => week).ToList(); if (weeks.Count == 0) { _leagueScheduleWeekPicker.AddItem("No scheduled weeks"); return; }
        foreach (var week in weeks) _leagueScheduleWeekPicker.AddItem($"Week {week}"); var index = Math.Max(0, weeks.FindIndex(week => string.Equals($"Week {week}", current, StringComparison.OrdinalIgnoreCase))); _leagueScheduleWeekPicker.Select(index);
    }

    private void RenderLeagueScheduleWorkspace()
    {
        var league = _nativeGameCoreContext?.ActiveLeague; ConfigureHistoryTree(_leagueWeekScheduleTree, new[] { "Matchup", "State", "Score / Time", "Week" }, new[] { 320, 130, 160, 80 }); var scheduleRoot = _leagueWeekScheduleTree.CreateItem(); if (league == null || _leagueScheduleWeekPicker?.Selected < 0 || !int.TryParse((_leagueScheduleWeekPicker.GetItemText(_leagueScheduleWeekPicker.Selected) ?? "").Replace("Week ", ""), out var week)) { AddHistoryEmptyRow(_leagueWeekScheduleTree, scheduleRoot, "No league schedule is available."); RenderLeaguePlayoffTree(league); return; }
        var games = (league.Schedule ?? new List<ScheduledGame>()).Where(game => game.AbsoluteWeek == week).ToList(); if (games.Count == 0) AddHistoryEmptyRow(_leagueWeekScheduleTree, scheduleRoot, "No games are scheduled for this week."); else foreach (var game in games) { var result = (league.Results ?? new List<GameResult>()).FirstOrDefault(item => string.Equals(item.GameId, game.GameId, StringComparison.OrdinalIgnoreCase)); var homeName = league.Teams.FirstOrDefault(team => team.TeamId == game.HomeTeamId)?.Name ?? "Home"; var awayName = league.Teams.FirstOrDefault(team => team.TeamId == game.AwayTeamId)?.Name ?? "Away"; var item = _leagueWeekScheduleTree.CreateItem(scheduleRoot); item.SetMetadata(0, game.HomeTeamId); item.SetMetadata(1, game.AwayTeamId); item.SetText(0, $"{awayName} at {homeName}"); item.SetText(1, result == null ? (string.IsNullOrWhiteSpace(game.Status) ? "Scheduled" : game.Status) : "Final"); item.SetText(2, result == null ? (string.IsNullOrWhiteSpace(game.WeekLabel) ? "Time unavailable" : game.WeekLabel) : $"{result.AwayTeam} {result.AwayScore}, {result.HomeTeam} {result.HomeScore}"); item.SetText(3, $"Week {week}"); if (result != null) item.SetCustomColor(1, new Color("8fcf98")); }
        RenderLeaguePlayoffTree(league);
    }

    private void RenderLeaguePlayoffTree(LeagueState league)
    {
        ConfigureHistoryTree(_leaguePlayoffTree, new[] { "Round / Seed", "Matchup", "Status", "Result" }, new[] { 170, 290, 130, 180 }); var root = _leaguePlayoffTree.CreateItem(); var bracket = league?.PlayoffBracket; if (bracket?.ConferenceBrackets == null || bracket.ConferenceBrackets.Count == 0) { AddHistoryEmptyRow(_leaguePlayoffTree, root, "Playoff bracket is not yet available for the current season."); return; }
        foreach (var conference in bracket.ConferenceBrackets) { var conferenceHeader = _leaguePlayoffTree.CreateItem(root); conferenceHeader.SetText(0, (conference.Conference ?? "Conference").ToUpperInvariant()); conferenceHeader.SetCustomColor(0, new Color("f0c96a")); foreach (var round in conference.Rounds ?? new List<PlayoffRound>()) foreach (var game in round.Games ?? new List<PlayoffGame>()) { var homeName = league.Teams.FirstOrDefault(team => team.TeamId == game.HomeTeamId)?.Name ?? "Home"; var awayName = league.Teams.FirstOrDefault(team => team.TeamId == game.AwayTeamId)?.Name ?? "Away"; var winnerName = league.Teams.FirstOrDefault(team => team.TeamId == game.WinnerTeamId)?.Name; var item = _leaguePlayoffTree.CreateItem(root); item.SetMetadata(0, game.HomeTeamId); item.SetMetadata(1, game.AwayTeamId); item.SetText(0, round.Round ?? "Round"); item.SetText(1, $"{awayName} at {homeName}"); item.SetText(2, game.Status ?? "Unavailable"); item.SetText(3, string.IsNullOrWhiteSpace(winnerName) ? "Result unavailable" : $"{winnerName} {game.HomeScore}-{game.AwayScore}"); } }
    }

    private async Task OpenSelectedLeagueScheduleTeam()
    {
        var tree = _leagueScheduleTabs?.CurrentTab == 1 ? _leaguePlayoffTree : _leagueWeekScheduleTree; var selected = tree?.GetSelected(); if (selected == null) return; var teamId = IsNil(selected.GetMetadata(0)) ? "" : selected.GetMetadata(0).AsString(); if (string.IsNullOrWhiteSpace(teamId)) return; await SelectMainTab(ROSTER_TAB_INDEX); await TrySelectTeamInRoster(teamId);
    }

    private void CreateLeagueNewsWorkspace()
    {
        if (_leagueTabPanel == null || _leagueNewsWorkspace != null) return;
        _leagueNewsWorkspace = new ScrollContainer { Name = "LeagueNewsWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leagueTabPanel.AddChild(_leagueNewsWorkspace); _leagueNewsGrid = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leagueNewsGrid.AddThemeConstantOverride("separation", 8); _leagueNewsWorkspace.AddChild(_leagueNewsGrid);
    }

    private void CreateLeaguePlayerSearchWorkspace()
    {
        if (_leagueTabPanel == null || _leaguePlayerSearchWorkspace != null) return;
        _leaguePlayerSearchWorkspace = new VBoxContainer { Name = "LeaguePlayerSearchWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leaguePlayerSearchWorkspace.AddThemeConstantOverride("separation", 6); _leagueTabPanel.AddChild(_leaguePlayerSearchWorkspace);
        var header = new HBoxContainer(); _leaguePlayerSearchWorkspace.AddChild(header); var title = new Label { Text = "LEAGUE > PLAYER SEARCH", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", 18); header.AddChild(title); _leaguePlayerSearchCount = HomeLabel("0 results", 12); header.AddChild(_leaguePlayerSearchCount);
        var filters = new HBoxContainer(); _leaguePlayerSearchWorkspace.AddChild(filters); _leaguePlayerSearchText = new LineEdit { PlaceholderText = "Search active players…", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _leaguePlayerSearchText.TextChanged += _ => { _leaguePlayerSearchPage = 0; RenderLeaguePlayerSearch(); }; filters.AddChild(_leaguePlayerSearchText); _leaguePlayerPositionFilter = new OptionButton(); _leaguePlayerPositionFilter.AddItem("All positions"); foreach (var position in new[] { "QB", "RB", "WR", "TE", "OL", "DL", "LB", "CB", "S", "K", "P" }) _leaguePlayerPositionFilter.AddItem(position); _leaguePlayerPositionFilter.ItemSelected += _ => { _leaguePlayerSearchPage = 0; RenderLeaguePlayerSearch(); }; filters.AddChild(_leaguePlayerPositionFilter); _leaguePlayerStatusFilter = new OptionButton(); _leaguePlayerStatusFilter.AddItem("All statuses"); _leaguePlayerStatusFilter.AddItem("Active"); _leaguePlayerStatusFilter.AddItem("Injured"); _leaguePlayerStatusFilter.ItemSelected += _ => { _leaguePlayerSearchPage = 0; RenderLeaguePlayerSearch(); }; filters.AddChild(_leaguePlayerStatusFilter); var previous = new Button { Text = "◀" }; previous.Pressed += () => { _leaguePlayerSearchPage = Math.Max(0, _leaguePlayerSearchPage - 1); RenderLeaguePlayerSearch(); }; filters.AddChild(previous); var next = new Button { Text = "▶" }; next.Pressed += () => { _leaguePlayerSearchPage++; RenderLeaguePlayerSearch(); }; filters.AddChild(next);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto }; _leaguePlayerSearchWorkspace.AddChild(scroll); _leaguePlayerSearchTree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(850, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leaguePlayerSearchTree.ItemActivated += () => _ = OpenLeagueSearchPlayer(); scroll.AddChild(_leaguePlayerSearchTree);
    }

    private async Task ShowLeaguePlayerSearchAsync() { await SelectMainTab(LEAGUE_TAB_INDEX); SetLeagueStatsWorkspaceVisible(false); SetLeagueScheduleWorkspaceVisible(false); SetLeagueNewsWorkspaceVisible(false); SetLeaguePlayerSearchVisible(true); RenderLeaguePlayerSearch(); }
    private void SetLeaguePlayerSearchVisible(bool visible) { if (_leaguePlayerSearchWorkspace != null) _leaguePlayerSearchWorkspace.Visible = visible; var hub = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel"); if (hub != null) hub.Visible = !visible; }
    private void RenderLeaguePlayerSearch()
    {
        if (_leaguePlayerSearchTree == null) return; _leaguePlayerSearchTree.Clear(); _leaguePlayerSearchTree.Columns = 6; var headers = new[] { "Player", "Pos", "Team", "Age", "OVR", "Status" }; for (var i = 0; i < headers.Length; i++) { _leaguePlayerSearchTree.SetColumnTitle(i, headers[i]); _leaguePlayerSearchTree.SetColumnCustomMinimumWidth(i, i == 0 ? 210 : 90); _leaguePlayerSearchTree.SetColumnExpand(i, i is 0 or 2); }
        var root = _leaguePlayerSearchTree.CreateItem(); var league = _nativeGameCoreContext?.ActiveLeague; var text = _leaguePlayerSearchText?.Text ?? ""; var position = _leaguePlayerPositionFilter?.GetItemText(_leaguePlayerPositionFilter.Selected) ?? "All positions"; var status = _leaguePlayerStatusFilter?.GetItemText(_leaguePlayerStatusFilter.Selected) ?? "All statuses"; var players = (league?.Teams ?? new List<TeamState>()).SelectMany(team => team.Roster.Select(player => (team, player))).Where(item => item.player.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).Where(item => position == "All positions" || item.player.Position == position || (position == "OL" && new[] { "LT", "LG", "C", "RG", "RT" }.Contains(item.player.Position)) || (position == "DL" && new[] { "DE", "DT" }.Contains(item.player.Position))).Where(item => status == "All statuses" || (status == "Injured" ? item.player.CurrentInjury?.IsActive == true : item.player.CurrentInjury?.IsActive != true)).OrderByDescending(item => item.player.Overall).ToList(); _leaguePlayerSearchCount.Text = $"{players.Count} results · page {_leaguePlayerSearchPage + 1}"; foreach (var (team, player) in players.Skip(_leaguePlayerSearchPage * 50).Take(50)) { var item = _leaguePlayerSearchTree.CreateItem(root); item.SetMetadata(0, player.PlayerId); item.SetMetadata(1, team.TeamId); item.SetText(0, player.Name); item.SetText(1, player.Position); item.SetText(2, team.Name); item.SetText(3, player.Age.ToString()); item.SetText(4, player.Overall.ToString()); item.SetText(5, player.CurrentInjury?.IsActive == true ? "Injured" : player.Status); }
        if (root.GetFirstChild() == null) AddHistoryEmptyRow(_leaguePlayerSearchTree, root, "No active players match these supported filters.");
    }
    private async Task OpenLeagueSearchPlayer() { var item = _leaguePlayerSearchTree?.GetSelected(); if (item == null || IsNil(item.GetMetadata(0))) return; await SelectMainTab(ROSTER_TAB_INDEX); await TrySelectTeamInRoster(item.GetMetadata(1).AsString()); TrySelectRosterPlayer(item.GetMetadata(0).AsString()); }

    private void CreateLeagueHistoryArchiveWorkspace()
    {
        if (_leagueTabPanel == null || _leagueHistoryWorkspace != null) return; _leagueHistoryWorkspace = new VBoxContainer { Name = "LeagueHistoryArchive", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leagueTabPanel.AddChild(_leagueHistoryWorkspace); var header = new HBoxContainer(); _leagueHistoryWorkspace.AddChild(header); var title = new Label { Text = "LEAGUE > HISTORY", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", 18); header.AddChild(title); header.AddChild(HomeLabel("SEASON", 11, new Color("9cadb8"))); _leagueHistoryYearPicker = new OptionButton(); _leagueHistoryYearPicker.ItemSelected += _ => RenderLeagueHistoryArchive(); header.AddChild(_leagueHistoryYearPicker); _leagueHistoryArchiveTabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leagueHistoryWorkspace.AddChild(_leagueHistoryArchiveTabs);
    }
    private async Task ShowLeagueHistoryArchiveAsync() { await SelectMainTab(LEAGUE_TAB_INDEX); SetLeagueStatsWorkspaceVisible(false); SetLeagueScheduleWorkspaceVisible(false); SetLeagueNewsWorkspaceVisible(false); SetLeaguePlayerSearchVisible(false); _leagueHistoryWorkspace.Visible = true; var hub = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel"); if (hub != null) hub.Visible = false; _leagueHistoryYearPicker.Clear(); foreach (var year in (_nativeGameCoreContext?.ActiveLeague?.HistoricalSeasons ?? new List<SeasonHistoryRecord>()).Select(record => record.SeasonYear).OrderByDescending(year => year)) _leagueHistoryYearPicker.AddItem(year.ToString()); RenderLeagueHistoryArchive(); }
    private void RenderLeagueHistoryArchive()
    {
        foreach (var child in _leagueHistoryArchiveTabs.GetChildren()) child.QueueFree(); var records = _nativeGameCoreContext?.ActiveLeague?.HistoricalSeasons ?? new List<SeasonHistoryRecord>(); if (_leagueHistoryYearPicker.Selected < 0) { var empty = new VBoxContainer { Name = "Archive" }; empty.AddChild(HomeLabel("No completed league seasons have been saved yet.", 13)); _leagueHistoryArchiveTabs.AddChild(empty); return; }
        var year = int.Parse(_leagueHistoryYearPicker.GetItemText(_leagueHistoryYearPicker.Selected)); var season = records.FirstOrDefault(record => record.SeasonYear == year); var college = _nativeGameCoreContext?.ActiveLeague?.CollegeSeasonArchives?.FirstOrDefault(record => record.SeasonYear == year); foreach (var pair in new[] { ("Champions", season == null ? "Unavailable" : $"League Champion: {season.ChampionTeamName}\nRunner-Up: {season.RunnerUpTeamName}\n{season.ChampionshipGameLabel}: {season.ChampionshipWinnerScore}-{season.ChampionshipRunnerUpScore}"), ("Final Standings", season == null ? "Unavailable" : string.Join("\n", season.TeamRecords.OrderBy(record => record.Conference).ThenBy(record => record.Division).ThenByDescending(record => record.WinPercentage).Select(record => $"{record.Conference} {record.Division} · {record.TeamName} {record.Wins}-{record.Losses}-{record.Ties}"))), ("Records & Awards", season == null ? "Unavailable" : string.Join("\n", season.Awards.Select(award => $"{award.AwardName}: {award.PlayerName} ({award.TeamName}) — {award.Summary}"))), ("College Season", college == null ? "Unavailable" : $"College Champion: {college.ChampionTeamName}\n\n{string.Join("\n", college.PostseasonGames.Select(game => $"{game.Label}: {game.AwayScore}-{game.HomeScore}"))}\n\n{string.Join("\n", college.Awards.Select(award => $"{award.AwardName}: {award.PlayerName} ({award.TeamName})"))}") }) { var tab = new VBoxContainer { Name = pair.Item1, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; tab.AddChild(HomeLabel($"{year} · {pair.Item1}", 14, new Color("f4eddf"))); var text = new RichTextLabel { Text = string.IsNullOrWhiteSpace(pair.Item2) ? "No saved record for this subject." : pair.Item2, BbcodeEnabled = false, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; tab.AddChild(text); _leagueHistoryArchiveTabs.AddChild(tab); }
    }

    private void CreateLeagueAwardsWorkspace() { if (_leagueTabPanel == null || _leagueAwardsWorkspace != null) return; _leagueAwardsWorkspace = new VBoxContainer { Name = "LeagueAwardsWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _leagueAwardsWorkspace.AddThemeConstantOverride("separation", 8); _leagueTabPanel.AddChild(_leagueAwardsWorkspace); }
    private void CreateCollegeRankingsWorkspace() { if (_collegeRankingsDialog != null) return; _collegeRankingsDialog = new AcceptDialog { Title = "College Football > Full Rankings", MinSize = new Vector2I(980, 720), Exclusive = false }; _collegeRankingsDialog.GetOkButton().Text = "CLOSE"; AddChild(_collegeRankingsDialog); var box = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _collegeRankingsDialog.AddChild(box); var nav = new HBoxContainer(); box.AddChild(nav); foreach (var item in new[] { "Home", "League Leaders", "Bowl Projections", "Public Boards", "Full Rankings", "Awards", "News" }) { var button = new Button { Text = item.ToUpperInvariant(), Disabled = item != "Full Rankings" && item != "League Leaders" && item != "Bowl Projections" && item != "Public Boards" && item != "Awards" && item != "News" }; if (item == "League Leaders") button.Pressed += ShowCollegeLeaders; if (item == "Bowl Projections") button.Pressed += ShowCollegePostseasonProjections; if (item == "Public Boards") button.Pressed += ShowCollegeBigBoards; if (item == "Awards") button.Pressed += ShowCollegeAwards; if (item == "News") button.Pressed += ShowCollegeNews; nav.AddChild(button); } box.AddChild(HomeLabel("CURRENT COLLEGE SEASON · Select a ranked team for its season profile", 11, new Color("9cadb8"))); var filters = new HBoxContainer(); box.AddChild(filters); _collegeRankingsSearch = new LineEdit { PlaceholderText = "Search program or abbreviation", CustomMinimumSize = new Vector2(300, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _collegeRankingsSearch.TextChanged += _ => RefreshCollegeRankings(); filters.AddChild(_collegeRankingsSearch); _collegeRankingsConference = new OptionButton { CustomMinimumSize = new Vector2(190, 0) }; _collegeRankingsConference.ItemSelected += _ => RefreshCollegeRankings(); filters.AddChild(_collegeRankingsConference); _collegeRankingsCount = HomeLabel("", 11, new Color("9cadb8")); filters.AddChild(_collegeRankingsCount); var split = new VSplitContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SplitOffset = 410 }; box.AddChild(split); _collegeRankingsTree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _collegeRankingsTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); _collegeRankingsTree.ItemSelected += UpdateCollegeTeamProfile; split.AddChild(_collegeRankingsTree); _collegeTeamProfile = new RichTextLabel { BbcodeEnabled = false, FitContent = false, CustomMinimumSize = new Vector2(0, 190), SizeFlagsVertical = Control.SizeFlags.ExpandFill, Text = "Select a team to view its season profile." }; split.AddChild(_collegeTeamProfile); }
    private void CreateCollegeLeadersWorkspace() { if (_collegeLeadersDialog != null) return; _collegeLeadersDialog = new AcceptDialog { Title = "College Football > League Leaders", MinSize = new Vector2I(900, 540), Exclusive = false }; _collegeLeadersDialog.GetOkButton().Text = "CLOSE"; AddChild(_collegeLeadersDialog); }
    private void CreateCollegePostseasonProjectionsWorkspace() { if (_collegePostseasonProjectionsDialog != null) return; _collegePostseasonProjectionsDialog = new AcceptDialog { Title = "College Football > Bowl Projections", MinSize = new Vector2I(900, 540), Exclusive = false }; _collegePostseasonProjectionsDialog.GetOkButton().Text = "CLOSE"; AddChild(_collegePostseasonProjectionsDialog); }
    private void CreateCollegeBigBoardsWorkspace() { if (_collegeBigBoardsDialog != null) return; _collegeBigBoardsDialog = new AcceptDialog { Title = "College Football > Public Boards", MinSize = new Vector2I(900, 600), Exclusive = false }; _collegeBigBoardsDialog.GetOkButton().Text = "CLOSE"; AddChild(_collegeBigBoardsDialog); }
    private void CreateCollegeAwardsWorkspace() { if (_collegeAwardsDialog != null) return; _collegeAwardsDialog = new AcceptDialog { Title = "College Football > Awards", MinSize = new Vector2I(900, 540), Exclusive = false }; _collegeAwardsDialog.GetOkButton().Text = "CLOSE"; AddChild(_collegeAwardsDialog); }
    private void ShowCollegeLeaders() { foreach (var child in _collegeLeadersDialog.GetChildren()) child.QueueFree(); var box = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _collegeLeadersDialog.AddChild(box); var leaders = new CollegeLeadersService(_nativeGameCoreContext).GetLeaders(5); if (!leaders.Ok || leaders.Categories.All(category => category.Leaders.Count == 0)) box.AddChild(HomeLabel(leaders.Message.Length > 0 ? leaders.Message : "College leader statistics are unavailable.", 13)); else { box.AddChild(HomeLabel($"COLLEGE LEAGUE LEADERS · {leaders.SeasonYear}", 18, new Color("f4eddf"))); var row = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; box.AddChild(row); foreach (var category in leaders.Categories) { var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; var body = new VBoxContainer(); panel.AddChild(body); body.AddChild(HomeLabel($"{category.Name.ToUpperInvariant()} · {category.StatLabel}", 12, new Color("f0c96a"))); foreach (var entry in category.Leaders) body.AddChild(HomeLabel($"{entry.PlayerName} · {entry.Position} · {entry.Value:N0}", 11)); row.AddChild(panel); } } _collegeLeadersDialog.PopupCentered(new Vector2I(900, 540)); }
    private void ShowCollegePostseasonProjections() { foreach (var child in _collegePostseasonProjectionsDialog.GetChildren()) child.QueueFree(); var box = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _collegePostseasonProjectionsDialog.AddChild(box); var universe = _nativeGameCoreContext?.ActiveLeague?.CollegeUniverse; var teams = universe?.Teams?.Where(team => team != null).ToDictionary(team => team.TeamId, team => team.Name) ?? new Dictionary<string, string>(); if (universe?.Postseason?.Completed == true) { box.AddChild(HomeLabel($"COLLEGE POSTSEASON · {universe.SeasonYear} · FINAL RESULTS", 18, new Color("f4eddf"))); foreach (var game in universe.Postseason.Games) { var panel = new PanelContainer(); var body = new VBoxContainer(); panel.AddChild(body); body.AddChild(HomeLabel(game.Label.ToUpperInvariant(), 12, new Color("f0c96a"))); body.AddChild(HomeLabel($"{teams.GetValueOrDefault(game.AwayTeamId, "Away")} {game.AwayScore}, {teams.GetValueOrDefault(game.HomeTeamId, "Home")} {game.HomeScore}", 12)); box.AddChild(panel); } } else { var projections = new CollegePostseasonProjectionService(_nativeGameCoreContext).GetProjections(); if (!projections.Ok) box.AddChild(HomeLabel(projections.Message, 13)); else { box.AddChild(HomeLabel($"COLLEGE POSTSEASON PROJECTIONS · {projections.SeasonYear}", 18, new Color("f4eddf"))); box.AddChild(HomeLabel("Current-ranking outlook only · no postseason games have been scheduled or simulated.", 11, new Color("9cadb8"))); foreach (var matchup in projections.PlayoffMatchups.Concat(projections.BowlMatchups)) box.AddChild(HomeLabel($"{matchup.Label}: #{matchup.Home.Ranking} {matchup.Home.TeamName} vs #{matchup.Away.Ranking} {matchup.Away.TeamName}", 12)); } } _collegePostseasonProjectionsDialog.PopupCentered(new Vector2I(900, 540)); }
    private void ShowCollegeBigBoards() { foreach (var child in _collegeBigBoardsDialog.GetChildren()) child.QueueFree(); var box = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _collegeBigBoardsDialog.AddChild(box); var boards = new CollegeBigBoardService(_nativeGameCoreContext).GetBoards(12); if (!boards.Ok) box.AddChild(HomeLabel(boards.Message, 13)); else { box.AddChild(HomeLabel("PUBLIC COLLEGE BIG BOARDS", 18, new Color("f4eddf"))); box.AddChild(HomeLabel("Public workout, production, team, and declared-outlook context · distinct from private scouting.", 11, new Color("9cadb8"))); var row = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; box.AddChild(row); foreach (var board in boards.Boards) { var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; var body = new VBoxContainer(); panel.AddChild(body); body.AddChild(HomeLabel(board.Name.ToUpperInvariant(), 13, new Color("f0c96a"))); foreach (var entry in board.Entries) body.AddChild(HomeLabel($"{entry.Rank}. {entry.Name} · {entry.Position} · {entry.College}", 11)); row.AddChild(panel); } } _collegeBigBoardsDialog.PopupCentered(new Vector2I(900, 600)); }
    private void CreateCollegeNewsWorkspace() { if (_collegeNewsDialog != null) return; _collegeNewsDialog = new AcceptDialog { Title = "College Football > News", MinSize = new Vector2I(900, 580), Exclusive = false }; _collegeNewsDialog.GetOkButton().Text = "CLOSE"; AddChild(_collegeNewsDialog); }
    private void ShowCollegeNews() { foreach (var child in _collegeNewsDialog.GetChildren()) child.QueueFree(); var body = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _collegeNewsDialog.AddChild(body); var news = new CollegeNewsService(_nativeGameCoreContext).GetNews(); body.AddChild(HomeLabel($"COLLEGE FOOTBALL > NEWS{(news.Ok ? $" · {news.SeasonYear}" : "")}", 18, new Color("f4eddf"))); body.AddChild(HomeLabel("Authoritative results, rankings, performances, recruiting, transfers, and coaching context · no external feeds.", 11, new Color("9cadb8"))); var list = new ItemList { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; body.AddChild(list); if (!news.Ok) list.AddItem(news.Message); else foreach (var item in news.Items) list.AddItem($"{item.Category} · {item.Headline}\n{item.Detail}"); _collegeNewsDialog.PopupCentered(new Vector2I(900, 580)); }
    private void ShowCollegeAwards() { foreach (var child in _collegeAwardsDialog.GetChildren()) child.QueueFree(); var box = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _collegeAwardsDialog.AddChild(box); var universe = _nativeGameCoreContext?.ActiveLeague?.CollegeUniverse; CollegeAwardsService.EnsureAwards(universe); if (universe == null || universe.Awards.Count == 0) box.AddChild(HomeLabel($"College awards are published after the completed {CollegeUniverseService.RegularSeasonWeeks}-week college season.", 13)); else { box.AddChild(HomeLabel($"COLLEGE AWARDS · {universe.SeasonYear} · final results", 18, new Color("f4eddf"))); foreach (var award in universe.Awards) { var panel = new PanelContainer(); var body = new VBoxContainer(); panel.AddChild(body); body.AddChild(HomeLabel(award.AwardName.ToUpperInvariant(), 13, new Color("f0c96a"))); body.AddChild(HomeLabel($"{award.PlayerName} · {award.Position} · {award.TeamName}", 12)); body.AddChild(HomeLabel(award.Summary, 11, new Color("9cadb8"))); box.AddChild(panel); } } _collegeAwardsDialog.PopupCentered(new Vector2I(900, 540)); }
    private void ShowCollegeFullRankings() { var universe = _nativeGameCoreContext?.ActiveLeague?.CollegeUniverse; _collegeRankingsConference.Clear(); _collegeRankingsConference.AddItem("ALL CONFERENCES"); foreach (var conference in universe?.Teams?.Where(team => team != null).Select(team => team.Conference).Where(conference => !string.IsNullOrWhiteSpace(conference)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(conference => conference, StringComparer.Ordinal) ?? Enumerable.Empty<string>()) _collegeRankingsConference.AddItem(conference); _collegeRankingsConference.Select(0); RefreshCollegeRankings(); _collegeRankingsDialog.PopupCentered(new Vector2I(980, 720)); }
    private void RefreshCollegeRankings() { if (_collegeRankingsTree == null) return; var universe = _nativeGameCoreContext?.ActiveLeague?.CollegeUniverse; var selectedId = _collegeRankingsTree.GetSelected()?.GetMetadata(0).AsString() ?? ""; var query = _collegeRankingsSearch?.Text?.Trim() ?? ""; var conference = _collegeRankingsConference != null && _collegeRankingsConference.Selected > 0 ? _collegeRankingsConference.GetItemText(_collegeRankingsConference.Selected) : ""; var teams = universe?.Teams?.Where(team => team != null && team.Ranking > 0 && (query.Length == 0 || team.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || team.Abbreviation.Contains(query, StringComparison.OrdinalIgnoreCase)) && (conference.Length == 0 || string.Equals(team.Conference, conference, StringComparison.OrdinalIgnoreCase))).OrderBy(team => team.Ranking).ToList() ?? new List<CollegeTeamState>(); _collegeRankingsTree.Clear(); _collegeRankingsTree.Columns = 4; foreach (var pair in new[] { ("Rank", 60), ("Team", 290), ("Record", 90), ("Conference", 170) }.Select((x, i) => (x.Item1, x.Item2, i))) { _collegeRankingsTree.SetColumnTitle(pair.i, pair.Item1); _collegeRankingsTree.SetColumnCustomMinimumWidth(pair.i, pair.Item2); _collegeRankingsTree.SetColumnExpand(pair.i, pair.i == 1); } var root = _collegeRankingsTree.CreateItem(); _collegeRankingsCount.Text = $"{teams.Count} PROGRAM{(teams.Count == 1 ? "" : "S")}"; if (teams.Count == 0) { AddHistoryEmptyRow(_collegeRankingsTree, root, universe == null ? "College season unavailable." : "No programs match the current filters."); _collegeTeamProfile.Text = universe == null ? "College season unavailable." : "No program matches the current filters."; return; } TreeItem first = null; TreeItem retained = null; foreach (var team in teams) { var item = _collegeRankingsTree.CreateItem(root); first ??= item; if (string.Equals(team.TeamId, selectedId, StringComparison.OrdinalIgnoreCase)) retained = item; item.SetMetadata(0, team.TeamId); item.SetText(0, team.Ranking.ToString()); item.SetText(1, team.Name); item.SetText(2, $"{team.Wins}-{team.Losses}"); item.SetText(3, team.Conference); item.SetTextAlignment(0, HorizontalAlignment.Right); item.SetTextAlignment(2, HorizontalAlignment.Right); } (retained ?? first)?.Select(0); UpdateCollegeTeamProfile(); }
    private void UpdateCollegeTeamProfile() { if (_collegeTeamProfile == null || _collegeRankingsTree?.GetSelected() == null) return; var teamId = _collegeRankingsTree.GetSelected().GetMetadata(0).AsString(); var profile = new CollegeTeamProfileService(_nativeGameCoreContext).GetProfile(teamId); if (!profile.Ok) { _collegeTeamProfile.Text = profile.Message; return; } var schedule = string.Join("   ", profile.Schedule.Select(game => game.IsFinal ? $"W{game.Week} {game.Result} {game.TeamScore}-{game.OpponentScore} {(game.IsHome ? "vs" : "@")} {game.OpponentName}" : $"W{game.Week} {(game.IsHome ? "vs" : "@")} {game.OpponentName}")); var leaders = profile.StatLeaders.Count == 0 ? "No player statistics yet." : string.Join("\n", profile.StatLeaders.Select(CollegeTeamPlayerSummary)); var roster = profile.Roster.Count == 0 ? "No roster is available." : string.Join("\n", profile.Roster.Select(CollegeTeamPlayerSummary)); var recruiting = profile.RecruitingClass.Count == 0 ? "No recruiting class is recorded for this season." : string.Join("\n", profile.RecruitingClass.Select(recruit => $"{recruit.PlayerName} · {recruit.Position} · {recruit.PublicTier}{(recruit.WillRedshirt ? " · REDSHIRT" : "")} · {recruit.Summary}")); var history = profile.ProgramHistory.Count == 0 ? "No completed seasons are archived yet." : string.Join("\n", profile.ProgramHistory.Select(season => $"{season.SeasonYear} · #{season.FinalRanking} · {season.Wins}-{season.Losses} · {season.Conference}{(season.WonChampionship ? " · NATIONAL CHAMPION" : "")}")); _collegeTeamProfile.Text = $"#{profile.Ranking} {profile.TeamName} ({profile.Abbreviation}) · {profile.Conference} · {profile.Wins}-{profile.Losses}\nHead Coach: {profile.HeadCoachName} · Year {profile.HeadCoachTenure} · Program {profile.ProgramLeadership} · Recruiting {profile.RecruitingLeadership}\n\nPROGRAM HISTORY\n{history}\n\nSCHEDULE\n{schedule}\n\nRECRUITING CLASS\n{recruiting}\n\nSEASON STAT LEADERS\n{leaders}\n\nROSTER\n{roster}"; }
    private static string CollegeTeamPlayerSummary(CollegeTeamPlayerLine player)
    {
        var eligibility = player.IsRedshirted
            ? $"RS · Yr {player.CollegeYear} · {player.PlayableSeasonsRemaining} seasons left"
            : $"Class {player.ClassYear} · Yr {player.CollegeYear} · {player.PlayableSeasonsRemaining} seasons left";
        var transfer = string.IsNullOrWhiteSpace(player.TransferContext) ? "" : $" · {player.TransferContext}";
        return $"{player.Name} · {player.Position} · {eligibility} · {player.GamesPlayed} GP · {player.PassingYards} PY · {player.RushingYards} RY · {player.ReceivingYards} REC · {player.Touchdowns} TD · Career {player.CareerGames} GP/{player.CareerYards} YD/{player.CareerTouchdowns} TD · {player.Availability}{transfer}";
    }
    private async Task ShowLeagueAwardsAsync() { await SelectMainTab(LEAGUE_TAB_INDEX); SetLeagueStatsWorkspaceVisible(false); SetLeagueScheduleWorkspaceVisible(false); SetLeagueNewsWorkspaceVisible(false); SetLeaguePlayerSearchVisible(false); _leagueHistoryWorkspace.Visible = false; _leagueAwardsWorkspace.Visible = true; var hub = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel"); if (hub != null) hub.Visible = false; RenderLeagueAwards(); }
    private void RenderLeagueAwards() { foreach (var child in _leagueAwardsWorkspace.GetChildren()) child.QueueFree(); var league = _nativeGameCoreContext?.ActiveLeague; _leagueAwardsWorkspace.AddChild(HomeLabel("LEAGUE > AWARDS", 18, new Color("f4eddf"))); var week = league?.Calendar?.AbsoluteWeek ?? 0; if (league == null || week < 9) { _leagueAwardsWorkspace.AddChild(HomeLabel($"Award-race projections open around Week 9. Current context: Week {week}.", 13)); return; } var grid = new HBoxContainer(); _leagueAwardsWorkspace.AddChild(grid); var awards = new (string, Func<PlayerState, int>)[] { ("MOST VALUABLE PLAYER", p => (p.SeasonStats?.PassingYards ?? 0) + (p.SeasonStats?.RushingYards ?? 0) + (p.SeasonStats?.ReceivingYards ?? 0)), ("OFFENSIVE PLAYER", p => (p.SeasonStats?.PassingYards ?? 0) + (p.SeasonStats?.RushingYards ?? 0)), ("DEFENSIVE PLAYER", p => (p.SeasonStats?.Tackles ?? 0) + (p.SeasonStats?.Sacks ?? 0) * 20) }; foreach (var award in awards) { var panel = CreateHomeTile(award.Item1, () => { }); var body = AddTileBody(panel); foreach (var x in league.Teams.SelectMany(t => t.Roster.Select(p => (t, p))).OrderByDescending(x => award.Item2(x.p)).Take(3)) body.AddChild(HomeLabel($"{x.p.Name} · {x.t.Name} · {award.Item2(x.p):N0}", 11)); body.AddChild(HomeLabel("Projection · current season", 10, new Color("9cadb8"))); grid.AddChild(panel); } }

    private async Task ShowLeagueNewsWorkspaceAsync()
    {
        await SelectMainTab(LEAGUE_TAB_INDEX); SetLeagueStatsWorkspaceVisible(false); SetLeagueScheduleWorkspaceVisible(false); SetLeagueNewsWorkspaceVisible(true); BuildLeagueNewsStories(); RenderLeagueNewsStories();
    }

    private void SetLeagueNewsWorkspaceVisible(bool visible)
    {
        if (_leagueNewsWorkspace != null) _leagueNewsWorkspace.Visible = visible; var hub = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/MainTabs/LeagueTab/LeagueHubPanel"); if (hub != null) hub.Visible = !visible;
    }

    private void BuildLeagueNewsStories()
    {
        _leagueNewsStories.Clear(); var league = _nativeGameCoreContext?.ActiveLeague; if (league == null) return;
        foreach (var transaction in (league.Transactions ?? new List<TransactionRecord>()).OrderByDescending(item => item.TransactionId).Take(12)) _leagueNewsStories.Add(new LeagueNewsStory($"{transaction.Type}: {transaction.PlayerName}", string.IsNullOrWhiteSpace(transaction.Details) ? "Transaction detail unavailable." : transaction.Details, transaction.DateLabel, transaction.TeamId, transaction.PlayerId, "transaction"));
        foreach (var result in (league.Results ?? new List<GameResult>()).OrderByDescending(item => item.AbsoluteWeek).ThenByDescending(item => item.GameId).Take(12)) _leagueNewsStories.Add(new LeagueNewsStory($"Final: {result.AwayTeam} {result.AwayScore}, {result.HomeTeam} {result.HomeScore}", string.IsNullOrWhiteSpace(result.Summary) ? "Completed league game." : result.Summary, result.WeekLabel, result.HomeTeamId, "", "game"));
        foreach (var team in league.Teams) foreach (var player in team.Roster.Where(player => player.CurrentInjury?.IsActive == true).Take(2)) _leagueNewsStories.Add(new LeagueNewsStory($"Injury update: {player.Name}", $"{team.Name} lists {player.Name} ({player.Position}) with {player.CurrentInjury.Name}. Recovery: {player.CurrentInjury.DaysRemaining} day(s) remaining.", league.Calendar?.CurrentDate ?? "Current season", team.TeamId, player.PlayerId, "injury"));
        _leagueNewsStories.Sort((left, right) => string.Compare(right.Context, left.Context, StringComparison.OrdinalIgnoreCase));
    }

    private void RenderLeagueNewsStories()
    {
        if (_leagueNewsGrid == null) return; foreach (var child in _leagueNewsGrid.GetChildren()) child.QueueFree(); var header = new Label { Text = "LEAGUE > NEWS" }; header.AddThemeFontSizeOverride("font_size", 20); header.AddThemeColorOverride("font_color", new Color("f4eddf")); _leagueNewsGrid.AddChild(header); _leagueNewsGrid.AddChild(HomeLabel("Current league events · neutral imagery is used because no article-image assets are persisted.", 11, new Color("9cadb8")));
        if (_leagueNewsStories.Count == 0) { _leagueNewsGrid.AddChild(HomeLabel("No recorded league news is available. Transactions, results, injuries, and draft events will appear here when they are saved.", 13)); return; }
        var lead = CreateLeagueNewsTile(_leagueNewsStories[0], true); _leagueNewsGrid.AddChild(lead); var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 8); _leagueNewsGrid.AddChild(row); foreach (var story in _leagueNewsStories.Skip(1).Take(8)) { var tile = CreateLeagueNewsTile(story, false); tile.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(tile); if (row.GetChildCount() == 4) { row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 8); _leagueNewsGrid.AddChild(row); } }
    }

    private PanelContainer CreateLeagueNewsTile(LeagueNewsStory story, bool lead)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(lead ? 0 : 220, lead ? 160 : 120) }; panel.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(lead ? new Color("123044") : new Color("0d2031"), new Color("254258"), 0, 1)); var content = new VBoxContainer(); content.AddThemeConstantOverride("separation", 4); panel.AddChild(content); var label = HomeLabel(story.Headline, lead ? 18 : 13, new Color("f4eddf")); label.AutowrapMode = TextServer.AutowrapMode.WordSmart; content.AddChild(label); content.AddChild(HomeLabel(story.Summary, lead ? 13 : 11)); content.AddChild(HomeLabel($"{story.Context} · {story.Kind.ToUpperInvariant()} · Neutral visual", 10, new Color("9cadb8"))); var open = new Button { Text = "OPEN STORY" }; open.Pressed += async () => await OpenLeagueNewsStory(story); content.AddChild(open); return panel;
    }

    private async Task OpenLeagueNewsStory(LeagueNewsStory story)
    {
        if (!string.IsNullOrWhiteSpace(story.TeamId)) { await SelectMainTab(ROSTER_TAB_INDEX); await TrySelectTeamInRoster(story.TeamId); if (!string.IsNullOrWhiteSpace(story.PlayerId)) TrySelectRosterPlayer(story.PlayerId); return; }
        SetPrimaryStatus(story.Summary);
    }

    private void CreateTeamStatsWorkspace()
    {
        if (_rosterTabPanel == null || _teamStatsWorkspace != null) return;
        _teamStatsWorkspace = new VBoxContainer { Name = "TeamStatsWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _teamStatsWorkspace.AddThemeConstantOverride("separation", 7);
        var header = new HBoxContainer(); _teamStatsWorkspace.AddChild(header); var title = new Label { Text = "TEAM > STATS", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); header.AddChild(title);
        var edit = new Button { Text = "EDIT ANALYTICS", CustomMinimumSize = new Vector2(135, 30) }; edit.Pressed += ToggleTeamStatsEdit; header.AddChild(edit);
        var filters = new HBoxContainer(); _teamStatsWorkspace.AddChild(filters); filters.AddChild(HomeLabel("TIMEFRAME", 11, new Color("9cadb8"))); _teamStatsSeasonFilter = new OptionButton(); _teamStatsSeasonFilter.AddItem("Current season"); _teamStatsSeasonFilter.ItemSelected += _ => BuildTeamStatsGrid(); filters.AddChild(_teamStatsSeasonFilter); filters.AddChild(HomeLabel("COMPARISON", 11, new Color("9cadb8"))); _teamStatsComparisonFilter = new OptionButton(); _teamStatsComparisonFilter.AddItem("League average"); _teamStatsComparisonFilter.AddItem("Division rivals"); _teamStatsComparisonFilter.ItemSelected += _ => BuildTeamStatsGrid(); filters.AddChild(_teamStatsComparisonFilter);
        _teamStatsEditorBar = new HBoxContainer { Visible = false }; _teamStatsEditorBar.AddThemeConstantOverride("separation", 5); _teamStatsWorkspace.AddChild(_teamStatsEditorBar); _teamStatsTilePicker = new OptionButton(); _teamStatsEditorBar.AddChild(_teamStatsTilePicker); AddTeamStatsEditorButton("MOVE ◀", () => MoveTeamStatsTile(-1)); AddTeamStatsEditorButton("MOVE ▶", () => MoveTeamStatsTile(1)); AddTeamStatsEditorButton("RESIZE", () => { _teamStatsFeaturedWide = !_teamStatsFeaturedWide; BuildTeamStatsGrid(); }); AddTeamStatsEditorButton("REMOVE", () => { _teamStatsHiddenTiles.Add(SelectedTeamStatsTile()); BuildTeamStatsGrid(); }); AddTeamStatsEditorButton("ADD TILE", () => { var hidden = _teamStatsTileOrder.FirstOrDefault(_teamStatsHiddenTiles.Contains); if (!string.IsNullOrWhiteSpace(hidden)) _teamStatsHiddenTiles.Remove(hidden); BuildTeamStatsGrid(); }); AddTeamStatsEditorButton("RESET", ResetTeamStatsLayout); AddTeamStatsEditorButton("SAVE", SaveTeamStatsLayout);
        _teamStatsEditHint = HomeLabel("Edit mode: choose a tile, then move, resize, remove, restore, reset, or save the analytics grid.", 11, new Color("9cadb8")); _teamStatsEditHint.Visible = false; _teamStatsWorkspace.AddChild(_teamStatsEditHint);
        _teamStatsGrid = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _teamStatsGrid.AddThemeConstantOverride("separation", 8); _teamStatsWorkspace.AddChild(_teamStatsGrid); _rosterTabPanel.AddChild(_teamStatsWorkspace);
        var config = new ConfigFile(); if (config.Load("user://team_stats_layout.cfg") == Error.Ok) { var order = ((string)config.GetValue("stats", "order", string.Join(",", _teamStatsTileOrder))).Split(',', StringSplitOptions.RemoveEmptyEntries).Where(new[] { "totals", "league_rank", "leaders", "trend" }.Contains).Distinct().ToList(); if (order.Count == 4) { _teamStatsTileOrder.Clear(); _teamStatsTileOrder.AddRange(order); } foreach (var tile in ((string)config.GetValue("stats", "hidden", "")).Split(',', StringSplitOptions.RemoveEmptyEntries)) _teamStatsHiddenTiles.Add(tile); _teamStatsFeaturedWide = (bool)config.GetValue("stats", "wide", true); }
    }

    private void AddTeamStatsEditorButton(string text, Action action) { var button = CreateShellButton(text, new Color("c5d1d8"), new Color("254258")); button.AddThemeFontSizeOverride("font_size", 11); button.Pressed += action; _teamStatsEditorBar.AddChild(button); }
    private async Task ShowTeamStatsWorkspaceAsync() { _practiceSquadViewActive = false; _teamStatsViewActive = true; _teamFinancesViewActive = false; _teamStandingsViewActive = _teamHistoryViewActive = _staffViewActive = _injuriesViewActive = _developmentViewActive = _depthChartViewActive = false; UpdateRosterViewModeUi(); foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" }) { var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = false; } BuildTeamStatsGrid(); await Task.CompletedTask; }
    private void ToggleTeamStatsEdit() { _teamStatsEditMode = !_teamStatsEditMode; _teamStatsEditorBar.Visible = _teamStatsEditMode; _teamStatsEditHint.Visible = _teamStatsEditMode; UpdateTeamStatsEditor(); }
    private string SelectedTeamStatsTile() => _teamStatsTilePicker?.Selected >= 0 ? _teamStatsTileOrder[_teamStatsTilePicker.Selected] : "totals";
    private void UpdateTeamStatsEditor() { if (_teamStatsTilePicker == null) return; _teamStatsTilePicker.Clear(); foreach (var tile in _teamStatsTileOrder) _teamStatsTilePicker.AddItem($"{TeamStatsTileName(tile)}{(_teamStatsHiddenTiles.Contains(tile) ? " (hidden)" : "")}"); _teamStatsTilePicker.Select(0); }
    private static string TeamStatsTileName(string tile) => tile switch { "totals" => "Team Totals", "league_rank" => "League Comparison", "leaders" => "Player Contributions", _ => "Game Trend" };
    private void MoveTeamStatsTile(int direction) { var tile = SelectedTeamStatsTile(); var index = _teamStatsTileOrder.IndexOf(tile); var target = Math.Clamp(index + direction, 0, _teamStatsTileOrder.Count - 1); if (index == target) return; _teamStatsTileOrder.RemoveAt(index); _teamStatsTileOrder.Insert(target, tile); BuildTeamStatsGrid(); }
    private void ResetTeamStatsLayout() { _teamStatsTileOrder.Clear(); _teamStatsTileOrder.AddRange(new[] { "totals", "league_rank", "leaders", "trend" }); _teamStatsHiddenTiles.Clear(); _teamStatsFeaturedWide = true; BuildTeamStatsGrid(); }
    private void SaveTeamStatsLayout() { var config = new ConfigFile(); config.SetValue("stats", "order", string.Join(",", _teamStatsTileOrder)); config.SetValue("stats", "hidden", string.Join(",", _teamStatsHiddenTiles)); config.SetValue("stats", "wide", _teamStatsFeaturedWide); config.Save("user://team_stats_layout.cfg"); }
    private void BuildTeamStatsGrid()
    {
        if (_teamStatsGrid == null) return; foreach (var child in _teamStatsGrid.GetChildren()) child.QueueFree(); UpdateTeamStatsEditor(); var visible = _teamStatsTileOrder.Where(tile => !_teamStatsHiddenTiles.Contains(tile)).ToList(); var top = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; var bottom = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; top.AddThemeConstantOverride("separation", 8); bottom.AddThemeConstantOverride("separation", 8);
        foreach (var tile in visible.Take(2)) { var panel = CreateTeamStatsTile(tile); panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; panel.SizeFlagsStretchRatio = _teamStatsFeaturedWide && tile == visible.FirstOrDefault() ? 2f : 1f; top.AddChild(panel); }
        foreach (var tile in visible.Skip(2)) { var panel = CreateTeamStatsTile(tile); panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; bottom.AddChild(panel); }
        if (top.GetChildCount() > 0) _teamStatsGrid.AddChild(top); if (bottom.GetChildCount() > 0) _teamStatsGrid.AddChild(bottom);
    }
    private PanelContainer CreateTeamStatsTile(string tile)
    {
        var action = tile == "leaders" ? (Action)(async () => { await SetRosterViewMode(false); }) : (Action)(async () => await OpenFullLeagueStandingsAsync()); var panel = CreateHomeTile(TeamStatsTileName(tile).ToUpperInvariant(), action, tile == "leaders" ? "OPEN ROSTER" : "OPEN DETAIL"); var body = AddTileBody(panel); var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase)); var games = (league?.Results ?? new List<GameResult>()).Where(ScheduleService.CountsTowardRegularSeasonStandings).Where(game => string.Equals(game.HomeTeamId, team?.TeamId, StringComparison.OrdinalIgnoreCase) || string.Equals(game.AwayTeamId, team?.TeamId, StringComparison.OrdinalIgnoreCase)).OrderBy(game => game.AbsoluteWeek).ToList();
        body.AddChild(HomeLabel($"Scope: {(_teamStatsComparisonFilter?.Selected == 1 ? "division rivals" : "league average")} · Timeframe: current season · Units: totals / per game", 10, new Color("9cadb8")));
        if (team == null || games.Count == 0) { body.AddChild(HomeLabel("Unavailable: no completed regular-season games for the selected timeframe.", 12, new Color("9cadb8"))); return panel; }
        if (tile == "leaders") RenderTeamStatsLeaders(body, team); else if (tile == "trend") RenderTeamStatsTrend(body, games, team.TeamId); else if (tile == "league_rank") RenderTeamStatsComparison(body, league, team, games); else RenderTeamStatsTotals(body, games, team.TeamId); return panel;
    }
    private void RenderTeamStatsTotals(VBoxContainer body, List<GameResult> games, string teamId)
    {
        var pointsFor = games.Sum(game => game.HomeTeamId == teamId ? game.HomeScore : game.AwayScore); var pointsAgainst = games.Sum(game => game.HomeTeamId == teamId ? game.AwayScore : game.HomeScore); var yards = games.Sum(game => GetGameStat(game, teamId, "total_yards")); var turnovers = games.Sum(game => GetGameStat(game, teamId, "turnovers")); body.AddChild(HomeLabel($"OFFENSE  {pointsFor} pts · {yards:N0} total yards", 13, new Color("f4eddf"))); body.AddChild(HomeLabel($"DEFENSE  {pointsAgainst} pts allowed · {turnovers} takeaways", 12)); body.AddChild(HomeLabel($"RATES    {pointsFor / (double)games.Count:0.0} pts/game · {yards / (double)games.Count:0.0} yards/game", 12)); body.AddChild(HomeLabel("Special teams: unavailable; no special-teams aggregate is recorded.", 11, new Color("9cadb8")));
    }
    private static int GetGameStat(GameResult game, string teamId, string baseKey) { var home = string.Equals(game.HomeTeamId, teamId, StringComparison.OrdinalIgnoreCase); return game.BoxScore?.TeamStats?.TryGetValue($"{baseKey}_{(home ? "home" : "away")}", out var value) == true ? value : 0; }
    private void RenderTeamStatsTrend(VBoxContainer body, List<GameResult> games, string teamId) { body.AddChild(HomeLabel("GAME-BY-GAME POINTS · current season", 12, new Color("f4eddf"))); foreach (var game in games.TakeLast(6)) { var scored = game.HomeTeamId == teamId ? game.HomeScore : game.AwayScore; var allowed = game.HomeTeamId == teamId ? game.AwayScore : game.HomeScore; body.AddChild(HomeLabel($"{game.WeekLabel}: {scored} for / {allowed} against", 11)); } }
    private void RenderTeamStatsLeaders(VBoxContainer body, TeamState team)
    {
        void Add(string label, PlayerState player, int value) => body.AddChild(HomeLabel($"{label,-4} {player?.Name ?? "Unavailable"} · {(player == null ? "—" : value.ToString())}", 12));
        var roster = team.Roster ?? new List<PlayerState>(); Add("PASS", roster.OrderByDescending(player => player.SeasonStats?.PassingYards ?? 0).FirstOrDefault(), roster.Max(player => player.SeasonStats?.PassingYards ?? 0)); Add("RUSH", roster.OrderByDescending(player => player.SeasonStats?.RushingYards ?? 0).FirstOrDefault(), roster.Max(player => player.SeasonStats?.RushingYards ?? 0)); Add("REC", roster.OrderByDescending(player => player.SeasonStats?.ReceivingYards ?? 0).FirstOrDefault(), roster.Max(player => player.SeasonStats?.ReceivingYards ?? 0)); Add("TACK", roster.OrderByDescending(player => player.SeasonStats?.Tackles ?? 0).FirstOrDefault(), roster.Max(player => player.SeasonStats?.Tackles ?? 0));
    }
    private void RenderTeamStatsComparison(VBoxContainer body, LeagueState league, TeamState team, List<GameResult> games)
    {
        var userPoints = games.Sum(game => game.HomeTeamId == team.TeamId ? game.HomeScore : game.AwayScore);
        var allGames = (league.Results ?? new List<GameResult>()).Where(ScheduleService.CountsTowardRegularSeasonStandings).ToList();
        var useDivision = _teamStatsComparisonFilter?.Selected == 1;
        var comparisonTeams = useDivision
            ? league.Teams.Where(candidate => string.Equals(candidate.Division, team.Division, StringComparison.OrdinalIgnoreCase) && !string.Equals(candidate.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase)).ToList()
            : league.Teams.ToList();
        var comparisonIds = comparisonTeams.Select(candidate => candidate.TeamId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var comparisonScores = allGames.SelectMany(game => new[]
        {
            comparisonIds.Contains(game.HomeTeamId) ? game.HomeScore : (int?)null,
            comparisonIds.Contains(game.AwayTeamId) ? game.AwayScore : (int?)null,
        }).Where(score => score.HasValue).Select(score => score.Value).ToList();
        var comparisonAverage = comparisonScores.Count == 0
            ? 0
            : comparisonScores.Average();
        var comparisonLabel = useDivision ? "Division-rival average" : "League average";

        body.AddChild(HomeLabel("POINTS FOR · league comparison", 12, new Color("f4eddf")));
        body.AddChild(HomeLabel($"Your team: {userPoints / (double)games.Count:0.0} per game", 13));
        body.AddChild(HomeLabel($"{comparisonLabel}: {comparisonAverage:0.0} per game", 13));
        body.AddChild(HomeLabel("Team ranks and opponent-adjusted rates: unavailable; those metrics are not persisted.", 11, new Color("9cadb8")));
    }

    private void CreateTeamFinancesWorkspace()
    {
        if (_rosterTabPanel == null || _teamFinancesWorkspace != null) return;
        _teamFinancesWorkspace = new VBoxContainer { Name = "TeamFinancesWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _teamFinancesWorkspace.AddThemeConstantOverride("separation", 8); _rosterTabPanel.AddChild(_teamFinancesWorkspace);
    }

    private async Task ShowTeamFinancesWorkspaceAsync()
    {
        _practiceSquadViewActive = false; _teamFinancesViewActive = true; _contractsViewActive = false; _accountingViewActive = false; _teamStatsViewActive = _teamStandingsViewActive = _teamHistoryViewActive = _staffViewActive = _injuriesViewActive = _developmentViewActive = _depthChartViewActive = false; UpdateRosterViewModeUi();
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" }) { var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = false; }
        RenderTeamFinancesWorkspace(); await Task.CompletedTask;
    }

    private void RenderTeamFinancesWorkspace()
    {
        if (_teamFinancesWorkspace == null) return; foreach (var child in _teamFinancesWorkspace.GetChildren()) child.QueueFree(); var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var header = new HBoxContainer(); _teamFinancesWorkspace.AddChild(header); var title = new Label { Text = "FINANCES > TEAM FINANCES", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); header.AddChild(title); var contracts = new Button { Text = "CONTRACTS", TooltipText = "Open multi-year cap planning." }; contracts.Pressed += async () => await ShowContractsWorkspaceAsync(); header.AddChild(contracts); var accounting = new Button { Text = "ACCOUNTING", TooltipText = "Open the current-year financial record." }; accounting.Pressed += async () => await ShowAccountingWorkspaceAsync(); header.AddChild(accounting);
        var summary = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; summary.AddThemeConstantOverride("separation", 8); _teamFinancesWorkspace.AddChild(summary); var funds = CreateFinancePanel("AVAILABLE FUNDS & CAP"); var attendance = CreateFinancePanel("ATTENDANCE & TICKETING"); summary.AddChild(funds); summary.AddChild(attendance);
        if (league == null || team == null) { funds.AddChild(HomeLabel("No active franchise finance data is available.", 12)); attendance.AddChild(HomeLabel("Attendance and ticketing are unavailable without an active franchise.", 12)); return; }
        var contractService = new ContractService(_nativeGameCoreContext); var payroll = contractService.GetCommittedSalary(team); var cap = league.SalaryCap; var capRoom = contractService.GetCapRoom(team); var payrollRank = league.Teams.OrderByDescending(contractService.GetCommittedSalary).ToList().FindIndex(item => item.TeamId == team.TeamId) + 1;
        funds.AddChild(FinanceLine("Salary cap", GameCoreStateHelper.FormatCapRoom(cap))); funds.AddChild(FinanceLine("Committed payroll", GameCoreStateHelper.FormatCapRoom(payroll))); funds.AddChild(FinanceLine("Available cap room", GameCoreStateHelper.FormatCapRoom(capRoom), capRoom > cap * .1m ? new Color("8fcf98") : new Color("f0c96a"))); funds.AddChild(FinanceLine("Payroll rank", $"{payrollRank} of {league.Teams.Count} · current commitments")); funds.AddChild(HomeLabel("Cash balance, revenue, operating costs, and net outcome are not recorded by the current financial system.", 11, new Color("9cadb8")));
        attendance.AddChild(HomeLabel("Attendance: unavailable — no attendance system is persisted.", 12)); attendance.AddChild(HomeLabel("Ticket revenue: unavailable — ticket sales are not modeled.", 12)); var ticketTable = new GridContainer { Columns = 3 }; ticketTable.AddChild(HomeLabel("Tier", 11, new Color("f4eddf"))); ticketTable.AddChild(HomeLabel("Price", 11, new Color("f4eddf"))); ticketTable.AddChild(HomeLabel("Control", 11, new Color("f4eddf"))); foreach (var tier in new[] { "Standard", "Premium", "Club" }) { ticketTable.AddChild(HomeLabel(tier, 11)); ticketTable.AddChild(HomeLabel("Unavailable", 11, new Color("9cadb8"))); ticketTable.AddChild(new Button { Text = "NOT MODELED", Disabled = true }); }
        attendance.AddChild(ticketTable); attendance.AddChild(HomeLabel("No ticket-price setting exists in the current persisted franchise model, so price controls cannot be applied safely.", 11, new Color("9cadb8")));
        var context = CreateFinancePanel("BUSINESS CONTEXT · CURRENT SEASON"); _teamFinancesWorkspace.AddChild(context); context.AddChild(FinanceLine("Timeframe", $"{league.SeasonYear} · current franchise state")); context.AddChild(FinanceLine("Operating outcome", "Unavailable — revenue and expense ledger not tracked")); context.AddChild(FinanceLine("League attendance rank", "Unavailable — attendance not tracked")); context.AddChild(HomeLabel("Use Contracts for player-cap actions. Accounting remains the future authority for year-to-date revenue and expenses.", 11, new Color("9cadb8")));
    }

    private VBoxContainer CreateFinancePanel(string heading) { var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 5); body.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("0d2031"), new Color("254258"), 0, 1)); body.AddChild(HomeLabel(heading, 13, new Color("f4eddf"))); return body; }
    private Control FinanceLine(string label, string value, Color? valueColor = null) { var row = new HBoxContainer(); var left = HomeLabel(label, 12); left.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(left); row.AddChild(HomeLabel(value, 12, valueColor ?? new Color("c5d1d8"))); return row; }

    private void CreatePracticeSquadWorkspace()
    {
        if (_rosterTabPanel == null || _practiceSquadWorkspace != null) return;
        _practiceSquadWorkspace = new VBoxContainer { Name = "PracticeSquadWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _practiceSquadWorkspace.AddThemeConstantOverride("separation", 7);
        _rosterTabPanel.AddChild(_practiceSquadWorkspace);
    }

    private async Task ShowPracticeSquadWorkspaceAsync()
    {
        _practiceSquadViewActive = true;
        _developmentViewActive = _injuriesViewActive = _staffViewActive = _teamHistoryViewActive = _teamStandingsViewActive = _teamStatsViewActive = _teamFinancesViewActive = _contractsViewActive = _accountingViewActive = _depthChartViewActive = false;
        UpdateRosterViewModeUi();
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" })
        {
            var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}");
            if (control != null) control.Visible = false;
        }
        RenderPracticeSquadWorkspace();
        await Task.CompletedTask;
    }

    private void RenderPracticeSquadWorkspace()
    {
        if (_practiceSquadWorkspace == null) return;
        foreach (var child in _practiceSquadWorkspace.GetChildren()) child.QueueFree();
        _practiceSquadWorkspacePlayerId = "";
        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var header = new HBoxContainer(); _practiceSquadWorkspace.AddChild(header);
        var title = new Label { Text = "TEAM > PRACTICE SQUAD", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); header.AddChild(title);
        var roster = new Button { Text = "ACTIVE ROSTER" }; roster.Pressed += async () => await SetRosterViewMode(false); header.AddChild(roster);
        var manage = new Button { Text = "SIGN PLAYERS", TooltipText = "Open the existing validated practice-squad signing market." }; manage.Pressed += ShowRosterManagement; header.AddChild(manage);

        var capRoom = team == null ? 0m : new ContractService(_nativeGameCoreContext).GetCapRoom(team);
        var activeOpening = team != null && team.Roster.Count < RosterService.RosterLimit;
        _practiceSquadWorkspace.AddChild(HomeLabel(team == null ? "No active franchise practice squad is available." : $"{team.Name} · Practice squad {team.PracticeSquad.Count}/16 · Active roster {team.Roster.Count}/{RosterService.RosterLimit} · Cap room {GameCoreStateHelper.FormatCapRoom(capRoom)} · {(activeOpening ? "active-roster opening available" : "active roster full")}", 12, new Color("9cadb8")));

        var tableWrap = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto }; _practiceSquadWorkspace.AddChild(tableWrap);
        _practiceSquadRosterTree = new Tree { Columns = 9, HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(1050, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var headers = new[] { "PLAYER", "POS", "AGE", "OVR", "POT", "CONTRACT", "HEALTH", "ELIGIBILITY", "ACTIVE SLOT" };
        var widths = new[] { 190, 50, 48, 48, 48, 130, 120, 150, 110 };
        for (var column = 0; column < headers.Length; column++) { _practiceSquadRosterTree.SetColumnTitle(column, headers[column]); _practiceSquadRosterTree.SetColumnCustomMinimumWidth(column, widths[column]); _practiceSquadRosterTree.SetColumnExpand(column, column == 0); }
        _practiceSquadRosterTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1));
        _practiceSquadRosterTree.ItemSelected += OnPracticeSquadWorkspaceSelected;
        _practiceSquadRosterTree.ItemActivated += () => _ = ElevatePracticeSquadWorkspacePlayer();
        tableWrap.AddChild(_practiceSquadRosterTree);
        var root = _practiceSquadRosterTree.CreateItem();
        var index = 0;
        foreach (var player in (team?.PracticeSquad ?? new List<PlayerState>()).OrderByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase))
        {
            var row = _practiceSquadRosterTree.CreateItem(root); row.SetMetadata(0, player.PlayerId); row.SetText(0, player.Name); row.SetText(1, player.Position); row.SetText(2, player.Age.ToString()); row.SetText(3, player.Overall.ToString()); row.SetText(4, player.Potential.ToString()); row.SetText(5, $"{GameCoreStateHelper.FormatCapRoom(player.Contract?.AnnualSalary ?? 0m)} · {player.Contract?.YearsRemaining ?? 0} yr"); row.SetText(6, player.CurrentInjury?.IsActive == true ? $"{player.CurrentInjury.Name} · {player.CurrentInjury.DaysRemaining}d" : "Available"); row.SetText(7, player.Age <= 25 ? "Eligible · signed" : "Grandfathered"); row.SetText(8, activeOpening ? "Available" : "Full");
            for (var column = 0; column < headers.Length; column++) row.SetCustomBgColor(column, index % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
            index++;
        }
        if (index == 0) AddHistoryEmptyRow(_practiceSquadRosterTree, root, "No players are currently assigned to the practice squad.");
        var actions = new HBoxContainer(); _practiceSquadWorkspace.AddChild(actions);
        var elevate = new Button { Text = "SIGN PERMANENTLY TO ACTIVE ROSTER", TooltipText = "Review the permanent contract and roster consequences before confirmation." }; elevate.Pressed += async () => await ElevatePracticeSquadWorkspacePlayer(); actions.AddChild(elevate);
        _practiceSquadWorkspaceStatus = new Label { Text = "Select a player to review a permanent active-roster signing. Temporary game-day elevations are not available until their rules are finalized.", AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; actions.AddChild(_practiceSquadWorkspaceStatus);
    }

    private void OnPracticeSquadWorkspaceSelected()
    {
        var selected = _practiceSquadRosterTree?.GetSelected();
        _practiceSquadWorkspacePlayerId = selected == null || IsNil(selected.GetMetadata(0)) ? "" : selected.GetMetadata(0).AsString();
        if (string.IsNullOrWhiteSpace(_practiceSquadWorkspacePlayerId) || _practiceSquadWorkspaceStatus == null) return;
        var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(candidate => candidate.TeamId == league.UserTeamId); var player = team?.PracticeSquad?.FirstOrDefault(candidate => candidate.PlayerId == _practiceSquadWorkspacePlayerId);
        if (player == null) { _practiceSquadWorkspaceStatus.Text = "Selected player is no longer on the practice squad."; return; }
        var preview = new ContractService(_nativeGameCoreContext).PreviewPracticeSquadActiveSigning(player.PlayerId);
        _practiceSquadWorkspaceStatus.Text = preview.Ok ? $"{player.Name}: permanent signing would replace the practice-squad contract with {GameCoreStateHelper.FormatCapRoom(preview.NewAnnualSalary)} annually and use the open active-roster slot." : preview.Error;
    }

    private async Task ElevatePracticeSquadWorkspacePlayer()
    {
        if (string.IsNullOrWhiteSpace(_practiceSquadWorkspacePlayerId)) { if (_practiceSquadWorkspaceStatus != null) _practiceSquadWorkspaceStatus.Text = "Select a practice-squad player first."; return; }
        ShowPracticeSquadActiveSigningConfirmation(_practiceSquadWorkspacePlayerId);
        await Task.CompletedTask;
    }

    private void CreateContractsWorkspace()
    {
        if (_rosterTabPanel == null || _contractsWorkspace != null) return;
        _contractsWorkspace = new VBoxContainer { Name = "ContractsWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _contractsWorkspace.AddThemeConstantOverride("separation", 7); _rosterTabPanel.AddChild(_contractsWorkspace);
    }

    private async Task ShowContractsWorkspaceAsync()
    {
        _practiceSquadViewActive = false; _contractsViewActive = true; _accountingViewActive = false; _teamFinancesViewActive = _teamStatsViewActive = _teamStandingsViewActive = _teamHistoryViewActive = _staffViewActive = _injuriesViewActive = _developmentViewActive = _depthChartViewActive = false; UpdateRosterViewModeUi();
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" }) { var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = false; }
        RenderContractsWorkspace(); await Task.CompletedTask;
    }

    private void RenderContractsWorkspace()
    {
        if (_contractsWorkspace == null) return; foreach (var child in _contractsWorkspace.GetChildren()) child.QueueFree(); var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var header = new HBoxContainer(); _contractsWorkspace.AddChild(header); var title = new Label { Text = "FINANCES > CONTRACTS", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); header.AddChild(title); var finances = new Button { Text = "TEAM FINANCES" }; finances.Pressed += async () => await ShowTeamFinancesWorkspaceAsync(); header.AddChild(finances); var accounting = new Button { Text = "ACCOUNTING" }; accounting.Pressed += async () => await ShowAccountingWorkspaceAsync(); header.AddChild(accounting);
        _contractsWorkspace.AddChild(HomeLabel(BuildContractRiskSummary(team), 11, new Color("9cadb8")));
        var tableWrap = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto }; _contractsWorkspace.AddChild(tableWrap); _contractsTree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, CustomMinimumSize = new Vector2(1040, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _contractsTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); _contractsTree.ColumnTitleClicked += OnContractsColumnClicked; _contractsTree.ItemActivated += () => _ = OpenSelectedContractPlayerProfile(); tableWrap.AddChild(_contractsTree);
        if (league == null || team == null) { ConfigureContractsHeaders(0); var root = _contractsTree.CreateItem(); AddHistoryEmptyRow(_contractsTree, root, "No active franchise contract data is available."); return; }
        ConfigureContractsHeaders(league.SeasonYear); var rows = team.Roster.Where(player => player?.Contract != null && player.Contract.AnnualSalary > 0m); rows = _contractsSortColumn == "player" ? (_contractsSortAscending ? rows.OrderBy(player => player.Name) : rows.OrderByDescending(player => player.Name)) : _contractsSortColumn == "years" ? (_contractsSortAscending ? rows.OrderBy(player => player.Contract.YearsRemaining) : rows.OrderByDescending(player => player.Contract.YearsRemaining)) : (_contractsSortAscending ? rows.OrderBy(player => player.Contract.AnnualSalary) : rows.OrderByDescending(player => player.Contract.AnnualSalary)); var rootItem = _contractsTree.CreateItem(); var index = 0;
        foreach (var player in rows)
        {
            var contract = player.Contract; var item = _contractsTree.CreateItem(rootItem); item.SetMetadata(0, player.PlayerId); item.SetText(0, player.Name); item.SetText(1, player.Position); item.SetText(2, GameCoreStateHelper.FormatCapRoom(contract.AnnualSalary)); item.SetText(3, contract.YearsRemaining.ToString()); item.SetText(4, contract.ContractType); item.SetText(5, GameCoreStateHelper.FormatCapRoom(contract.GuaranteedSalary));
            for (var year = 0; year < 3; year++) item.SetText(6 + year, year < contract.YearsRemaining ? GameCoreStateHelper.FormatCapRoom(contract.AnnualSalary) : "—");
            for (var column = 0; column < 9; column++) item.SetCustomBgColor(column, index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); item.SetTextAlignment(2, HorizontalAlignment.Right); item.SetTextAlignment(3, HorizontalAlignment.Right); item.SetTextAlignment(5, HorizontalAlignment.Right); for (var column = 6; column < 9; column++) item.SetTextAlignment(column, HorizontalAlignment.Right);
        }
        var services = new ContractService(_nativeGameCoreContext); var summary = _contractsTree.CreateItem(rootItem); summary.SetText(0, "AVAILABLE CAP SPACE"); for (var year = 0; year < 3; year++) summary.SetText(6 + year, year == 0 ? GameCoreStateHelper.FormatCapRoom(services.GetCapRoom(team)) : "Unavailable"); var dead = _contractsTree.CreateItem(rootItem); dead.SetText(0, "DEAD CAP"); for (var year = 0; year < 3; year++) dead.SetText(6 + year, "Unavailable");
    }

    private void ConfigureContractsHeaders(int season) { var headers = new[] { "Player", "Pos", "Current Cap Hit", "Years", "Type", "Guaranteed", (season + 1).ToString(), (season + 2).ToString(), (season + 3).ToString() }; _contractsTree.Clear(); _contractsTree.Columns = headers.Length; var widths = new[] { 190, 55, 115, 55, 120, 110, 110, 110, 110 }; for (var column = 0; column < headers.Length; column++) { _contractsTree.SetColumnTitle(column, headers[column]); _contractsTree.SetColumnCustomMinimumWidth(column, widths[column]); _contractsTree.SetColumnExpand(column, column == 0); } }
    private static string BuildContractRiskSummary(TeamState team)
    {
        var contracts = team?.Roster?.Where(player => player?.Contract?.AnnualSalary > 0m).ToList() ?? new List<PlayerState>();
        if (contracts.Count == 0) return "Contract risk summary: unavailable; no active contract commitments are recorded.";
        var expiring = contracts.Count(player => player.Contract.YearsRemaining <= 1);
        var veteranCost = contracts.Where(player => player.Age >= 32 && player.Contract.AnnualSalary >= 10_000_000m).Sum(player => player.Contract.AnnualSalary);
        return $"Contract risk summary (informational): {expiring} expiring commitment(s) within one year · {GameCoreStateHelper.FormatCapRoom(veteranCost)} in age-32+ commitments at $10M+ annually. Uses current salary, years, and age only; no future-cap or dead-cap estimate is stored.";
    }
    private void OnContractsColumnClicked(long column, long mouse) { var key = column switch { 0 => "player", 3 => "years", _ => "current" }; _contractsSortAscending = _contractsSortColumn == key ? !_contractsSortAscending : false; _contractsSortColumn = key; RenderContractsWorkspace(); }
    private async Task OpenSelectedContractPlayerProfile() { var selected = _contractsTree?.GetSelected(); if (selected == null || IsNil(selected.GetMetadata(0))) return; var playerId = selected.GetMetadata(0).AsString(); await SetRosterViewMode(false); await RefreshRosterTab(); TrySelectRosterPlayer(playerId); }

    private void CreateAccountingWorkspace()
    {
        if (_rosterTabPanel == null || _accountingWorkspace != null) return;
        _accountingWorkspace = new VBoxContainer { Name = "AccountingWorkspace", Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _accountingWorkspace.AddThemeConstantOverride("separation", 7); _rosterTabPanel.AddChild(_accountingWorkspace);
    }

    private async Task ShowAccountingWorkspaceAsync()
    {
        _practiceSquadViewActive = false; _accountingViewActive = true; _contractsViewActive = _teamFinancesViewActive = _teamStatsViewActive = _teamStandingsViewActive = _teamHistoryViewActive = _staffViewActive = _injuriesViewActive = _developmentViewActive = _depthChartViewActive = false; UpdateRosterViewModeUi();
        foreach (var path in new[] { "SquadWorkspaceHeader", "SquadWorkspaceHint", "RosterSummary", "RosterModeRow" }) { var control = GetNodeOrNull<Control>($"AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/{path}"); if (control != null) control.Visible = false; }
        RenderAccountingWorkspace(); await Task.CompletedTask;
    }

    private void RenderAccountingWorkspace()
    {
        if (_accountingWorkspace == null) return; foreach (var child in _accountingWorkspace.GetChildren()) child.QueueFree(); var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var header = new HBoxContainer(); _accountingWorkspace.AddChild(header); var title = new Label { Text = "FINANCES > ACCOUNTING", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color("f4eddf")); header.AddChild(title); var finances = new Button { Text = "TEAM FINANCES" }; finances.Pressed += async () => await ShowTeamFinancesWorkspaceAsync(); header.AddChild(finances); var contracts = new Button { Text = "CONTRACTS" }; contracts.Pressed += async () => await ShowContractsWorkspaceAsync(); header.AddChild(contracts);
        var summary = new HBoxContainer(); summary.AddThemeConstantOverride("separation", 12); _accountingWorkspace.AddChild(summary); var income = HomeLabel($"CURRENT FINANCIAL YEAR: {league?.SeasonYear.ToString() ?? "Unavailable"}\nRecorded revenue: unavailable\nRecorded expenses: unavailable\nNet result: unavailable", 12, new Color("c5d1d8")); income.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; summary.AddChild(income); var detailHint = HomeLabel("Only current player contract commitments are persisted as a directly supported finance-related value. Category drill-down is available for that recorded aggregate.", 11, new Color("9cadb8")); detailHint.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; summary.AddChild(detailHint);
        var split = new HSplitContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, SplitOffset = 700 }; _accountingWorkspace.AddChild(split); _accountingLedgerTree = new Tree { HideRoot = true, ColumnTitlesVisible = true, SelectMode = Tree.SelectModeEnum.Row, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _accountingLedgerTree.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(new Color("091927"), new Color("254258"), 0, 1)); _accountingLedgerTree.ItemSelected += RenderSelectedAccountingDetail; split.AddChild(_accountingLedgerTree); _accountingDetail = new RichTextLabel { BbcodeEnabled = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; split.AddChild(_accountingDetail);
        ConfigureHistoryTree(_accountingLedgerTree, new[] { "Category", "Type", "Period", "Amount", "Detail" }, new[] { 180, 85, 110, 140, 280 }); var root = _accountingLedgerTree.CreateItem();
        if (league == null || team == null) { AddHistoryEmptyRow(_accountingLedgerTree, root, "No active franchise accounting data is available."); _accountingDetail.Text = "No active franchise accounting context is available."; return; }
        var contractsService = new ContractService(_nativeGameCoreContext); var payroll = contractsService.GetCommittedSalary(team); var payrollItem = _accountingLedgerTree.CreateItem(root); payrollItem.SetMetadata(0, "player_payroll"); payrollItem.SetText(0, "Player contract commitments"); payrollItem.SetText(1, "Expense context"); payrollItem.SetText(2, $"{league.SeasonYear} current commitments"); payrollItem.SetText(3, GameCoreStateHelper.FormatCapRoom(payroll)); payrollItem.SetText(4, "Active roster contracts"); payrollItem.SetCustomColor(3, new Color("f0c96a"));
        foreach (var category in new[] { "Attendance and ticket revenue", "Scouting expenses", "Staff contracts", "Travel", "Stadium operations", "Equipment", "Merchandise" }) { var item = _accountingLedgerTree.CreateItem(root); item.SetText(0, category); item.SetText(1, category.Contains("revenue", StringComparison.OrdinalIgnoreCase) ? "Revenue" : "Expense"); item.SetText(2, $"{league.SeasonYear} current year"); item.SetText(3, "Unavailable"); item.SetText(4, "Not tracked by the saved financial model"); item.SetCustomColor(3, new Color("9cadb8")); }
        _accountingDetail.Text = "Select a ledger category to see saved detail. Revenue, operational costs, staff contracts, attendance, and scouting spend have no persisted financial entries in this build.";
    }

    private void RenderSelectedAccountingDetail()
    {
        var selected = _accountingLedgerTree?.GetSelected(); if (selected == null || IsNil(selected.GetMetadata(0))) { if (_accountingDetail != null) _accountingDetail.Text = "No recorded detail is available for this category."; return; }
        if (!string.Equals(selected.GetMetadata(0).AsString(), "player_payroll", StringComparison.OrdinalIgnoreCase)) { _accountingDetail.Text = "No recorded underlying entries are available for this category."; return; }
        var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase)); if (team == null) return; var lines = new List<string> { "Player contract commitments", "Current active-contract annual salaries:" }; lines.AddRange(team.Roster.Where(player => player.Contract != null).OrderByDescending(player => player.Contract.AnnualSalary).Take(15).Select(player => $"{player.Name} · {GameCoreStateHelper.FormatCapRoom(player.Contract.AnnualSalary)} · {player.Contract.YearsRemaining} year(s) remaining")); _accountingDetail.Text = string.Join("\n", lines);
    }

    private void BuildDevelopmentRows()
    {
        _developmentRows.Clear(); var league = _nativeGameCoreContext?.ActiveLeague; var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var date = DateTime.TryParse(league?.Calendar?.CurrentDate, out var current) ? current : DateTime.Today;
        if (_developmentWindowLabel != null) _developmentWindowLabel.Text = $"Trailing 12 months: {date.AddMonths(-12):MMM d, yyyy} – {date:MMM d, yyyy}";
        foreach (var player in team?.Roster ?? Enumerable.Empty<PlayerState>())
        {
            var record = (player.DevelopmentHistory ?? new List<PlayerDevelopmentRecord>()).Where(item => item.SeasonYear >= league.SeasonYear - 1 && item.SeasonYear <= league.SeasonYear).OrderByDescending(item => item.SeasonYear).FirstOrDefault();
            var movement = record == null ? 0 : record.OverallAfter - record.OverallBefore;
            _developmentRows.Add(new DevelopmentRow(player.PlayerId, player.Name, player.Position, player.Age, player.Overall, record == null ? "No 12-month rating history" : movement > 0 ? "Improving" : movement < 0 ? "Regressing" : "Stable", record == null ? "Unavailable" : $"OVR {movement:+#;-#;0}", record?.Note ?? "No development notes recorded."));
        }
    }

    private void RenderDevelopmentRows()
    {
        if (_developmentTree == null) return;
        var headers = new[] { "Player", "Age", "Position", "Current OVR", "Overall Trend", "Rating Movement", "Coach / Scout Notes" };
        _developmentTree.Clear(); _developmentTree.Columns = headers.Length;
        for (var column = 0; column < headers.Length; column++) _developmentTree.SetColumnTitle(column, headers[column]);
        _developmentTree.SetColumnCustomMinimumWidth(0, 190); _developmentTree.SetColumnExpand(0, true); _developmentTree.SetColumnCustomMinimumWidth(1, 48); _developmentTree.SetColumnCustomMinimumWidth(2, 66); _developmentTree.SetColumnCustomMinimumWidth(3, 82); _developmentTree.SetColumnCustomMinimumWidth(4, 156); _developmentTree.SetColumnCustomMinimumWidth(5, 136); _developmentTree.SetColumnCustomMinimumWidth(6, 220); _developmentTree.SetColumnExpand(6, true);
        IEnumerable<DevelopmentRow> rows = _developmentRows;
        var search = _developmentSearch?.Text?.Trim() ?? ""; if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(row => row.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        var position = _developmentPositionFilter?.GetItemText(_developmentPositionFilter.Selected) ?? "All"; if (!string.Equals(position, "All", StringComparison.OrdinalIgnoreCase)) rows = rows.Where(row => MatchesPosFilter(row.Position, position));
        var trend = _developmentTrendFilter?.GetItemText(_developmentTrendFilter.Selected) ?? "All trends"; if (string.Equals(trend, "No rating history", StringComparison.OrdinalIgnoreCase)) rows = rows.Where(row => row.Trend.StartsWith("No", StringComparison.OrdinalIgnoreCase)); else if (!string.Equals(trend, "All trends", StringComparison.OrdinalIgnoreCase)) rows = Enumerable.Empty<DevelopmentRow>();
        var change = _developmentChangeFilter?.GetItemText(_developmentChangeFilter.Selected) ?? "All changes"; if (string.Equals(change, "Material change", StringComparison.OrdinalIgnoreCase)) rows = Enumerable.Empty<DevelopmentRow>();
        rows = SortDevelopmentRows(rows).ToList(); var root = _developmentTree.CreateItem(); var index = 0;
        foreach (var row in rows)
        {
            var item = _developmentTree.CreateItem(root); item.SetMetadata(0, row.PlayerId); item.SetText(0, row.Name); item.SetText(1, row.Age.ToString()); item.SetText(2, row.Position); item.SetText(3, row.Overall.ToString()); item.SetText(4, row.Trend); item.SetText(5, row.Movement); item.SetText(6, row.Notes);
            for (var column = 0; column < headers.Length; column++) item.SetCustomBgColor(column, index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
            item.SetTextAlignment(1, HorizontalAlignment.Right); item.SetTextAlignment(3, HorizontalAlignment.Right); item.SetCustomColor(4, new Color("9cadb8")); item.SetCustomColor(5, new Color("9cadb8"));
        }
        if (_developmentTree.GetRoot().GetFirstChild() == null) { var empty = _developmentTree.CreateItem(root); empty.SetText(0, "No players match these development filters."); }
    }

    private IEnumerable<DevelopmentRow> SortDevelopmentRows(IEnumerable<DevelopmentRow> rows) => _developmentSortColumn switch { "age" => _developmentSortAscending ? rows.OrderBy(row => row.Age) : rows.OrderByDescending(row => row.Age), "overall" => _developmentSortAscending ? rows.OrderBy(row => row.Overall) : rows.OrderByDescending(row => row.Overall), "position" => _developmentSortAscending ? rows.OrderBy(row => row.Position) : rows.OrderByDescending(row => row.Position), _ => _developmentSortAscending ? rows.OrderBy(row => row.Name) : rows.OrderByDescending(row => row.Name) };
    private void OnDevelopmentColumnTitleClicked(long column, long mouseButton) { var ids = new[] { "player", "age", "position", "overall", "trend", "movement", "notes" }; if (column < 0 || column >= ids.Length) return; _developmentSortAscending = _developmentSortColumn == ids[column] ? !_developmentSortAscending : true; _developmentSortColumn = ids[column]; RenderDevelopmentRows(); }
    private async Task OpenSelectedDevelopmentPlayerProfile() { var selected = _developmentTree?.GetSelected(); if (selected == null || IsNil(selected.GetMetadata(0))) return; var playerId = selected.GetMetadata(0).AsString(); await SetRosterViewMode(false); await RefreshRosterTab(); TrySelectRosterPlayer(playerId); }

    private void OnDepthFieldPlayerPressed(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || _depthChartTree == null) return;
        var root = _depthChartTree.GetRoot();
        var item = root?.GetFirstChild();
        while (item != null)
        {
            var child = item.GetFirstChild();
            while (child != null)
            {
                if (TryGetDictionary(child.GetMetadata(0), out var data) && string.Equals(SafeString(data, "player_id", ""), playerId, StringComparison.OrdinalIgnoreCase))
                { child.Select(0); OnDepthChartItemSelected(child); _depthChartTree.ScrollToItem(child); return; }
                child = child.GetNext();
            }
            item = item.GetNext();
        }
    }

    private async Task RefreshRosterTab()
    {
        UpdateRosterViewModeUi();
        if (_depthChartViewActive)
        {
            await RefreshDepthChartView();
            return;
        }

        RefreshNativeRosterTab();
        await Task.CompletedTask;
    }

    private async Task RefreshDepthChartView()
    {
        RefreshNativeDepthChartView();
        await Task.CompletedTask;
    }

    private async Task AutoFillDepthChart()
    {
        if (_btnAutoFillDepthChart == null || _depthChartRequestBusy) return;
        SetDepthChartRequestBusy(true, "Auto-filling..."); SetDepthChartActionStatus("Auto-filling...");
        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeDepthChartService.AutoFillDepthChart(ResolveNativeRosterDepthChartTeamId());
            if (response?.Ok != true)
            {
                var error = string.IsNullOrWhiteSpace(response?.Error) ? "Unable to auto-fill depth chart." : response.Error;
                SetDepthChartActionStatus(error); SetPrimaryStatus(error); return;
            }
            RenderDepthChartSnapshot(ConvertDepthChartResponseToPayload(response));
            await SaveNativeAutosave("Native autosave updated."); await RefreshDashboardState(); await RefreshInbox(); await RefreshLeagueHub();
            _dashboardRefreshPendingFromDepthChartEdit = false;
            SetDepthChartActionStatus("Depth chart auto-filled."); SetPrimaryStatus("Depth chart auto-filled.");
        }
        catch (Exception ex) { SetDepthChartActionStatus("Unable to auto-fill depth chart."); SetPrimaryStatus($"Native C# auto-fill failed: {InlineMessage(ex.Message)}"); }
        finally { SetDepthChartRequestBusy(false); }
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
        _nativeLiveGameSessionService ??= new LiveGameSessionService(context);
    }

    private async Task<GameCoreSaveResult> SaveCurrentNativeGame(string saveName, string successMessage, bool autosaveToo)
    {
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
        UpdateRosterContractActionAvailability();
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

    private void UpdateRosterContractActionAvailability()
    {
        if (_btnApplyFranchiseTag == null)
            return;

        var status = ContractPhaseRules.GetStatus(_nativeGameCoreContext?.ActiveLeague);
        _btnApplyFranchiseTag.Disabled = !status.CanApplyFranchiseTag;
        _btnApplyFranchiseTag.TooltipText = status.CanApplyFranchiseTag
            ? "Apply one fully guaranteed one-year tag to an eligible final-year player."
            : status.Explanation;
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
                ["injured_reserve_count"] = response?.RosterStatus?.InjuredReserveCount ?? 0,
                ["practice_squad_count"] = response?.RosterStatus?.PracticeSquadCount ?? 0,
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
                    ["overall"] = player?.EstimatedOverall ?? 0,
                    ["potential"] = player?.EstimatedPotential ?? 0,
                    ["estimated_overall_range"] = player?.EstimatedOverallRange ?? "Unavailable",
                    ["estimated_potential_range"] = player?.EstimatedPotentialRange ?? "Unavailable",
                    ["scouting_confidence"] = player?.ScoutingConfidence ?? "Low",
                    ["age"] = player?.Age ?? 0,
                    ["fatigue"] = player?.Fatigue ?? 0,
                    ["status"] = player?.Status ?? "",
                    ["injury"] = player?.Injury ?? "",
                    ["injury_days_remaining"] = player?.InjuryDaysRemaining ?? 0,
                    ["is_available"] = player?.IsAvailable ?? false,
                    ["depth_role"] = player?.DepthRole ?? "",
                    ["contract_summary"] = player?.ContractSummary ?? "Unavailable",
                    ["annual_salary"] = (double)(player?.AnnualSalary ?? 0m),
                    ["contract_years"] = player?.ContractYearsRemaining ?? 0,
                    ["morale"] = player?.Morale ?? 0,
                    ["morale_trend"] = player?.MoraleTrend ?? "Unavailable",
                    ["games_played"] = player?.GamesPlayed ?? 0,
                    ["passing_yards"] = player?.PassingYards ?? 0,
                    ["rushing_yards"] = player?.RushingYards ?? 0,
                    ["receiving_yards"] = player?.ReceivingYards ?? 0,
                    ["tackles"] = player?.Tackles ?? 0,
                    ["sacks"] = player?.Sacks ?? 0,
                    ["interceptions"] = player?.Interceptions ?? 0,
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
                    ["is_locked"] = position?.IsLocked ?? false,
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
                            ["estimated_overall"] = player?.EstimatedOverall ?? 0,
                            ["estimated_overall_range"] = player?.EstimatedOverallRange ?? "Unavailable",
                            ["scouting_confidence"] = player?.ScoutingConfidence ?? "Low",
                            ["role"] = player?.Role ?? "",
                            ["status"] = player?.Status ?? "",
                            ["injury"] = player?.Injury ?? "",
                            ["injury_days_remaining"] = player?.InjuryDaysRemaining ?? 0,
                            ["is_available"] = player?.IsAvailable ?? false,
                            ["contract_summary"] = player?.ContractSummary ?? "Unavailable",
                            ["morale"] = player?.Morale ?? 0,
                            ["morale_trend"] = player?.MoraleTrend ?? "Unavailable",
                            ["potential"] = player?.Potential ?? 0,
                            ["passing_yards"] = player?.PassingYards ?? 0,
                            ["rushing_yards"] = player?.RushingYards ?? 0,
                            ["receiving_yards"] = player?.ReceivingYards ?? 0,
                            ["tackles"] = player?.Tackles ?? 0,
                            ["sacks"] = player?.Sacks ?? 0,
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

            PopulateLeagueStandingsReference(response);
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
        };

        var teamStats = new Godot.Collections.Dictionary();
        var playByPlay = new Godot.Collections.Array();
        var playerStats = new Godot.Collections.Array();
        var typedPlays = new List<GamePlayEventState>();
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
                else if (pair.Value is IEnumerable<GamePlayEventState> plays && string.Equals(pair.Key, "play_by_play", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var play in plays)
                    {
                        typedPlays.Add(play);
                        playByPlay.Add(new Godot.Collections.Dictionary
                        {
                            ["sequence"] = play.Sequence,
                            ["quarter"] = play.Quarter,
                            ["clock_seconds"] = play.ClockSeconds,
                            ["possession_team_id"] = play.PossessionTeamId ?? "",
                            ["down"] = play.Down,
                            ["distance"] = play.Distance,
                            ["yard_line"] = play.YardLine,
                            ["yards_gained"] = play.YardsGained,
                            ["description"] = play.Description ?? "",
                            ["home_score"] = play.HomeScore,
                            ["away_score"] = play.AwayScore,
                            ["is_scoring_play"] = play.IsScoringPlay,
                            ["is_turnover"] = play.IsTurnover,
                            ["is_injury"] = play.IsInjury,
                        });
                    }
                }
                else if (pair.Value is IEnumerable<PlayerGameStats> playerLines && string.Equals(pair.Key, "player_stats", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var line in playerLines)
                    {
                        playerStats.Add(new Godot.Collections.Dictionary
                        {
                            ["player_id"] = line.PlayerId ?? "",
                            ["player_name"] = line.PlayerName ?? "",
                            ["team_id"] = line.TeamId ?? "",
                            ["position"] = line.Position ?? "",
                            ["passing_yards"] = line.PassingYards,
                            ["passing_touchdowns"] = line.PassingTouchdowns,
                            ["rushing_yards"] = line.RushingYards,
                            ["rushing_touchdowns"] = line.RushingTouchdowns,
                            ["receiving_yards"] = line.ReceivingYards,
                            ["receiving_touchdowns"] = line.ReceivingTouchdowns,
                            ["tackles"] = line.Tackles,
                            ["sacks"] = line.Sacks,
                            ["interceptions"] = line.Interceptions,
                        });
                    }
                }
            }
        }
        var homeQuarterScores = new Godot.Collections.Array();
        var awayQuarterScores = new Godot.Collections.Array();
        var priorHome = 0;
        var priorAway = 0;
        for (var quarter = 1; quarter <= 4; quarter++)
        {
            var quarterEnd = typedPlays.Where(play => play.Quarter == quarter).OrderBy(play => play.Sequence).LastOrDefault();
            var homeAtEnd = quarterEnd?.HomeScore ?? priorHome;
            var awayAtEnd = quarterEnd?.AwayScore ?? priorAway;
            homeQuarterScores.Add(Math.Max(0, homeAtEnd - priorHome));
            awayQuarterScores.Add(Math.Max(0, awayAtEnd - priorAway));
            priorHome = homeAtEnd;
            priorAway = awayAtEnd;
        }
        if (homeQuarterScores.Count == 4 && priorHome != (result?.HomeScore ?? 0))
            homeQuarterScores[3] = GetIntValue(homeQuarterScores[3], 0) + Math.Max(0, (result?.HomeScore ?? 0) - priorHome);
        if (awayQuarterScores.Count == 4 && priorAway != (result?.AwayScore ?? 0))
            awayQuarterScores[3] = GetIntValue(awayQuarterScores[3], 0) + Math.Max(0, (result?.AwayScore ?? 0) - priorAway);
        boxScore["quarter_scores"] = new Godot.Collections.Dictionary { ["away"] = awayQuarterScores, ["home"] = homeQuarterScores };
        boxScore["team_stats"] = teamStats;
        boxScore["play_by_play"] = playByPlay;
        boxScore["player_stats"] = playerStats;
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
        RefreshNativeHistoryView();
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

            var recordBook = _nativeDashboardService.GetRecordBook();
            _recordBookSummary = recordBook?.Ok == true ? BuildRecordBookSummary(recordBook) : "";
            _historicalArchive = _nativeDashboardService.GetHistoricalArchive() ?? new HistoricalArchiveResponse();
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
        var nativeTeam = _nativeGameCoreContext?.ActiveLeague?.Teams.FirstOrDefault(candidate => string.Equals(candidate.TeamId, _currentTeamId, StringComparison.OrdinalIgnoreCase));

        if (_rosterSummary != null)
        {
            var capRoom = nativeTeam == null ? "Unavailable" : GameCoreStateHelper.FormatCapRoom(nativeTeam.CapRoom);
            _rosterSummary.Text =
                $"{teamLabel}   •   ACTIVE {SafeIntDisplay(rosterStatus, "roster_size")}/{SafeIntDisplay(rosterStatus, "roster_limit")}   •   PRACTICE SQUAD {SafeIntDisplay(rosterStatus, "practice_squad_count")}   •   IR {SafeIntDisplay(rosterStatus, "injured_reserve_count")}   •   CAP SPACE {capRoom}";
            if (!string.IsNullOrWhiteSpace(issueText))
                _rosterSummary.Text += $"   •   {rosterStatusLabel.ToUpperInvariant()}: {issueText}";
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

    private static string BuildRosterChemistrySummary(Godot.Collections.Array players)
    {
        var traitCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var moraleTotal = 0;
        var moraleCount = 0;
        foreach (var entry in players)
        {
            if (entry.VariantType != Variant.Type.Dictionary)
                continue;

            var player = entry.AsGodotDictionary();
            var trait = SafeString(player, "trait", "");
            if (!string.IsNullOrWhiteSpace(trait))
                traitCounts[trait] = traitCounts.TryGetValue(trait, out var count) ? count + 1 : 1;
            moraleTotal += GetIntValue(GetFirstNonNil(player, "morale"), 50);
            moraleCount++;
        }

        if (moraleCount == 0)
            return "Chemistry context: unavailable.";

        var leadingTrait = traitCounts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        var traitContext = string.IsNullOrWhiteSpace(leadingTrait.Key)
            ? "traits unavailable"
            : $"leading trait {leadingTrait.Key} ({leadingTrait.Value})";
        return $"Chemistry context (informational): {traitContext} · average morale {moraleTotal / (double)moraleCount:0}/100 · no gameplay effect.";
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
        _depthChartPayload = payload;
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
        _depthChartTree.Columns = 10;
        _depthChartTree.SelectMode = Tree.SelectModeEnum.Row;
        var headers = new[] { "Pos", "#", "Player", "Role", "Health", "Morale", "Staff OVR", "Confidence", "Season Production", "Contract" };
        for (var headerIndex = 0; headerIndex < headers.Length; headerIndex++) _depthChartTree.SetColumnTitle(headerIndex, headers[headerIndex]);
        _depthChartTree.SetColumnCustomMinimumWidth(0, 48); _depthChartTree.SetColumnCustomMinimumWidth(1, 34);
        _depthChartTree.SetColumnCustomMinimumWidth(2, 158); _depthChartTree.SetColumnExpand(2, true);
        _depthChartTree.SetColumnCustomMinimumWidth(3, 104); _depthChartTree.SetColumnCustomMinimumWidth(4, 108);
        _depthChartTree.SetColumnCustomMinimumWidth(5, 84); _depthChartTree.SetColumnCustomMinimumWidth(6, 86); _depthChartTree.SetColumnCustomMinimumWidth(7, 82);
        _depthChartTree.SetColumnCustomMinimumWidth(8, 175); _depthChartTree.SetColumnExpand(8, true); _depthChartTree.SetColumnCustomMinimumWidth(9, 110);

        var root = _depthChartTree.CreateItem();
        var selectedStillExists = false;
        for (var i = 0; i < positions.Count; i++)
        {
            if (!TryGetDictionary((Variant)positions[i], out var positionRow))
                continue;

            var position = SafeString(positionRow, "position", "UNK");
            if (!DepthChartPositionMatchesUnit(position, _depthChartUnitFilter))
                continue;
            var positionLocked = GetBoolValue(GetFirstNonNil(positionRow, "is_locked"), false);
            var requiredStarters = SafeIntDisplay(positionRow, "required_starters", "requiredStarters", fallback: "0");
            var players = TryExtractArray(positionRow, "players") ?? new Godot.Collections.Array();
            var search = _depthChartSearch?.Text?.Trim() ?? "";
            var filteredPlayers = new List<Godot.Collections.Dictionary>();
            for (var playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                if (!TryGetDictionary((Variant)players[playerIndex], out var candidate)) continue;
                if (!string.IsNullOrWhiteSpace(search) && !SafeString(candidate, "name", "").Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                filteredPlayers.Add(candidate);
            }
            if (!string.IsNullOrWhiteSpace(search) && filteredPlayers.Count == 0)
                continue;
            var header = _depthChartTree.CreateItem(root);
            header.SetText(0, position);
            header.SetText(2, $"{position} GROUP  ·  {requiredStarters} starter(s){(positionLocked ? "  ·  LOCKED" : "")}");
            header.SetCustomBgColor(0, new Color("142f3a"));
            for (var column = 0; column < headers.Length; column++)
                header.SetSelectable(column, false);

            if (filteredPlayers.Count == 0)
            {
                var emptyItem = _depthChartTree.CreateItem(header);
                emptyItem.SetText(1, "No players available");
                continue;
            }

            for (var playerIndex = 0; playerIndex < filteredPlayers.Count; playerIndex++)
            {
                var player = filteredPlayers[playerIndex];

                var row = _depthChartTree.CreateItem(header);
                var playerName = SafeString(player, "name", "Unknown Player");
                var playerId = SafeString(player, "player_id", "");
                var available = GetBoolValue(GetFirstNonNil(player, "is_available"), true);
                var injury = SafeString(player, "injury", "");
                var days = GetIntValue(GetFirstNonNil(player, "injury_days_remaining"), 0);
                row.SetText(0, position);
                row.SetText(1, (playerIndex + 1).ToString(CultureInfo.InvariantCulture));
                row.SetText(2, playerName);
                row.SetText(3, SafeString(player, "role", "Backup"));
                row.SetText(4, available ? "Available" : string.IsNullOrWhiteSpace(injury) ? "Unavailable" : days > 0 ? $"{injury} · {days}d" : injury);
                row.SetText(5, GetIntValue(GetFirstNonNil(player, "morale"), -1) < 0 ? "Unavailable" : $"{GetIntValue(GetFirstNonNil(player, "morale"), 0)} · {SafeString(player, "morale_trend", "")}");
                row.SetText(6, SafeString(player, "estimated_overall_range", "Unavailable"));
                row.SetText(7, SafeString(player, "scouting_confidence", "Low"));
                row.SetText(8, FormatDepthChartProduction(position, player));
                row.SetText(9, SafeString(player, "contract_summary", "Unavailable"));
                for (var column = 0; column < headers.Length; column++) row.SetCustomBgColor(column, playerIndex % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
                row.SetTextAlignment(1, HorizontalAlignment.Right);
                row.SetCustomColor(4, available ? new Color("8fcf98") : new Color("f0c96a"));
                row.SetCustomColor(7, string.Equals(SafeString(player, "scouting_confidence", "Low"), "High", StringComparison.OrdinalIgnoreCase) ? new Color("8fcf98") : string.Equals(SafeString(player, "scouting_confidence", "Low"), "Low", StringComparison.OrdinalIgnoreCase) ? new Color("e58b7a") : new Color("f0c96a"));
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

        if (root.GetFirstChild() == null)
        {
            var empty = _depthChartTree.CreateItem(root);
            empty.SetText(2, string.IsNullOrWhiteSpace(_depthChartSearch?.Text) ? "No positions are available for this unit." : "No depth-chart players match this search.");
            for (var column = 0; column < headers.Length; column++) empty.SetSelectable(column, false);
        }
        if (!selectedStillExists)
            ClearDepthChartSelection();
        else
            UpdateDepthChartSelectionLabel();
        _depthChartPositions = positions;
    }

    private static bool DepthChartPositionMatchesUnit(string position, int unit)
    {
        var normalized = (position ?? "").Trim().ToUpperInvariant();
        var offense = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "QB", "RB", "FB", "WR", "TE", "OT", "OG", "C", "LT", "LG", "RG", "RT", "OL" };
        var specialTeams = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "K", "P", "LS", "KR", "PR" };
        if (unit == 0) return offense.Contains(normalized);
        if (unit == 2) return specialTeams.Contains(normalized);
        return !offense.Contains(normalized) && !specialTeams.Contains(normalized);
    }

    private static string FormatDepthChartProduction(string position, Godot.Collections.Dictionary player)
    {
        var pass = GetIntValue(GetFirstNonNil(player, "passing_yards"), 0);
        var rush = GetIntValue(GetFirstNonNil(player, "rushing_yards"), 0);
        var receiving = GetIntValue(GetFirstNonNil(player, "receiving_yards"), 0);
        var tackles = GetIntValue(GetFirstNonNil(player, "tackles"), 0);
        var sacks = GetIntValue(GetFirstNonNil(player, "sacks"), 0);
        if (string.Equals(position, "QB", StringComparison.OrdinalIgnoreCase)) return $"{pass:N0} pass yd · {rush:N0} rush yd";
        if (new[] { "RB", "FB" }.Contains(position, StringComparer.OrdinalIgnoreCase)) return $"{rush:N0} rush yd · {receiving:N0} rec yd";
        if (new[] { "WR", "TE" }.Contains(position, StringComparer.OrdinalIgnoreCase)) return $"{receiving:N0} rec yd";
        if (tackles > 0 || sacks > 0) return $"{tackles} TKL · {sacks} SK";
        return "No recorded production";
    }

    private void SetDepthChartRequestBusy(bool isBusy, string autoFillButtonText = null)
    {
        _depthChartRequestBusy = isBusy;
        UpdateDepthChartEditButtons();

        if (_btnAutoFillDepthChart == null)
            return;

        _btnAutoFillDepthChart.Disabled = isBusy || _liveGameAdjustmentMode;
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
        if (_depthFieldDiagram != null)
        {
            _depthFieldDiagram.SelectedPlayerId = _selectedDepthChartPlayerId;
            _depthFieldDiagram.QueueRedraw();
        }
    }

    private void UpdateDepthChartEditButtons()
    {
        var canEdit =
            !_depthChartRequestBusy
            && !string.IsNullOrWhiteSpace(_selectedDepthChartPosition)
            && !string.IsNullOrWhiteSpace(_selectedDepthChartPlayerId);

        if (_btnDepthChartSetStarter != null)
            _btnDepthChartSetStarter.Disabled = !canEdit;
        if (_btnDepthChartToggleLock != null)
        {
            var canToggleLock = !_depthChartRequestBusy && !_liveGameAdjustmentMode && !string.IsNullOrWhiteSpace(_selectedDepthChartPosition);
            _btnDepthChartToggleLock.Disabled = !canToggleLock;
            _btnDepthChartToggleLock.Text = IsDepthChartPositionLocked(_selectedDepthChartPosition)
                ? "UNLOCK POSITION"
                : "LOCK POSITION";
        }
    }

    private bool IsDepthChartPositionLocked(string position)
    {
        if (string.IsNullOrWhiteSpace(position) || _depthChartPositions == null)
            return false;

        for (var index = 0; index < _depthChartPositions.Count; index++)
        {
            if (!TryGetDictionary((Variant)_depthChartPositions[index], out var group))
                continue;
            if (string.Equals(SafeString(group, "position", ""), position, StringComparison.OrdinalIgnoreCase))
                return GetBoolValue(GetFirstNonNil(group, "is_locked"), false);
        }

        return false;
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

    private void RenderDepthFieldDiagram()
    {
        if (_depthFieldDiagram == null) return;
        var slots = new List<DepthFieldSlot>();
        if (_depthChartPositions != null)
        {
            for (var i = 0; i < _depthChartPositions.Count; i++)
            {
                if (!TryGetDictionary((Variant)_depthChartPositions[i], out var group)) continue;
                var position = SafeString(group, "position", "");
                var players = TryExtractArray(group, "players") ?? new Godot.Collections.Array();
                for (var p = 0; p < players.Count; p++)
                {
                    if (!TryGetDictionary((Variant)players[p], out var player) || !string.Equals(SafeString(player, "role", ""), "Starter", StringComparison.OrdinalIgnoreCase)) continue;
                    slots.Add(new DepthFieldSlot(SafeString(player, "player_id", ""), SafeString(player, "name", "Unknown"), position, GetBoolValue(GetFirstNonNil(player, "is_available"), true)));
                }
            }
        }
        _depthFieldDiagram.SetSlots(slots);
    }

    private async Task ReorderDepthChartByDrop(string position, string playerId, string targetPlayerId, bool insertAfter)
    {
        if (_depthChartRequestBusy || string.IsNullOrWhiteSpace(position) || string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(targetPlayerId))
            return;
        if (_liveGameAdjustmentMode)
        {
            await ApplyLiveDepthAdjustment(insertAfter ? "move_after" : "move_before", position, playerId, targetPlayerId);
            return;
        }
        SetDepthChartRequestBusy(true);
        SetDepthChartActionStatus("Saving depth chart order…");
        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeDepthChartService.UpdateDepthChart(insertAfter ? "move_after" : "move_before", position, playerId, ResolveNativeRosterDepthChartTeamId(), targetPlayerId);
            if (response == null || !response.Ok)
            {
                var error = string.IsNullOrWhiteSpace(response?.Error) ? "Unable to reorder the depth chart." : response.Error;
                SetDepthChartActionStatus(error);
                SetPrimaryStatus(error);
                return;
            }

            _selectedDepthChartPosition = position;
            _selectedDepthChartPlayerId = playerId;
            RenderDepthChartSnapshot(ConvertDepthChartResponseToPayload(response));
            await SaveNativeAutosave("Native autosave updated.");
            await RefreshDashboardState();
            await RefreshInbox();
            await RefreshLeagueHub();
            _dashboardRefreshPendingFromDepthChartEdit = false;
            SetDepthChartActionStatus("Depth chart order saved.");
            SetPrimaryStatus("Depth chart order saved.");
        }
        catch (Exception ex)
        {
            SetDepthChartActionStatus("Unable to reorder the depth chart.");
            SetPrimaryStatus($"Depth chart reorder failed: {InlineMessage(ex.Message)}");
        }
        finally
        {
            SetDepthChartRequestBusy(false);
        }
    }

    private async Task ToggleSelectedDepthChartLock()
    {
        if (_depthChartRequestBusy || string.IsNullOrWhiteSpace(_selectedDepthChartPosition))
            return;
        var wasLocked = IsDepthChartPositionLocked(_selectedDepthChartPosition);
        SetDepthChartRequestBusy(true);
        SetDepthChartActionStatus(wasLocked ? "Unlocking position…" : "Locking position…");
        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeDepthChartService.TogglePositionLock(_selectedDepthChartPosition, ResolveNativeRosterDepthChartTeamId());
            if (response == null || !response.Ok)
            {
                var error = string.IsNullOrWhiteSpace(response?.Error) ? "Unable to update the position lock." : response.Error;
                SetDepthChartActionStatus(error);
                SetPrimaryStatus(error);
                return;
            }

            RenderDepthChartSnapshot(ConvertDepthChartResponseToPayload(response));
            await SaveNativeAutosave("Native autosave updated.");
            var message = wasLocked
                ? $"{_selectedDepthChartPosition} is unlocked for Auto-Fill."
                : $"{_selectedDepthChartPosition} order is locked against Auto-Fill.";
            SetDepthChartActionStatus(message);
            SetPrimaryStatus(message);
        }
        catch (Exception ex)
        {
            SetDepthChartActionStatus("Unable to update the position lock.");
            SetPrimaryStatus($"Depth chart lock failed: {InlineMessage(ex.Message)}");
        }
        finally
        {
            SetDepthChartRequestBusy(false);
        }
    }

    private async Task UpdateDepthChart(string action)
    {
        if (_depthChartRequestBusy) return;
        if (string.IsNullOrWhiteSpace(_selectedDepthChartPosition) || string.IsNullOrWhiteSpace(_selectedDepthChartPlayerId))
        { const string message = "Select a depth chart player first."; SetDepthChartActionStatus(message); SetPrimaryStatus(message); UpdateDepthChartEditButtons(); return; }
        if (_liveGameAdjustmentMode) { await ApplyLiveDepthAdjustment(action, _selectedDepthChartPosition, _selectedDepthChartPlayerId, null); return; }
        SetDepthChartRequestBusy(true); SetDepthChartActionStatus("Updating depth chart...");
        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeDepthChartService.UpdateDepthChart(action, _selectedDepthChartPosition, _selectedDepthChartPlayerId, ResolveNativeRosterDepthChartTeamId());
            if (response?.Ok != true)
            { var error = string.IsNullOrWhiteSpace(response?.Error) ? "Unable to update depth chart." : response.Error; SetDepthChartActionStatus(error); SetPrimaryStatus(error); return; }
            RenderDepthChartSnapshot(ConvertDepthChartResponseToPayload(response));
            await SaveNativeAutosave("Native autosave updated."); await RefreshDashboardState(); await RefreshInbox(); await RefreshLeagueHub();
            _dashboardRefreshPendingFromDepthChartEdit = false;
            SetDepthChartActionStatus("Depth chart updated."); SetPrimaryStatus("Depth chart updated.");
        }
        catch (Exception ex) { SetDepthChartActionStatus("Unable to update depth chart."); SetPrimaryStatus($"Native C# depth chart update failed: {InlineMessage(ex.Message)}"); }
        finally { SetDepthChartRequestBusy(false); }
    }

    private async Task ApplyLiveDepthAdjustment(string action, string position, string playerId, string targetPlayerId)
    {
        SetDepthChartRequestBusy(true);
        SetDepthChartActionStatus("Applying adjustment to unplayed events…");
        try
        {
            EnsureNativeGameCoreServices();
            var response = _nativeLiveGameSessionService.ApplyDepthAdjustment(action, position, playerId, targetPlayerId);
            if (!response.Ok)
            {
                SetDepthChartActionStatus(response.Error);
                SetPrimaryStatus(response.Error);
                return;
            }
            var chart = _nativeDepthChartService.GetTeamDepthChart(ResolveNativeRosterDepthChartTeamId());
            if (chart.Ok)
                RenderDepthChartSnapshot(ConvertDepthChartResponseToPayload(chart));
            await SaveNativeAutosave("Native autosave updated.");
            _selectedDepthChartPosition = position;
            _selectedDepthChartPlayerId = playerId;
            SetDepthChartActionStatus("Adjustment queued for the next unplayed event. Return to Live Game when ready.");
            SetPrimaryStatus("Live depth-chart adjustment saved.");
        }
        catch (Exception ex)
        {
            SetDepthChartActionStatus("Unable to apply live adjustment.");
            SetPrimaryStatus($"Live depth adjustment failed: {InlineMessage(ex.Message)}");
        }
        finally
        {
            SetDepthChartRequestBusy(false);
        }
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
        SetContinueButtonBusy(true); SetPrimaryStatus("Simulating season...");
        try
        {
            EnsureNativeGameCoreServices(); var response = _nativeContinueService.Continue(CONTINUE_MAX_DAYS);
            if (response?.Ok != true) { SetPrimaryStatus(string.IsNullOrWhiteSpace(response?.Error) ? "Continue failed." : response.Error); return; }
            ApplyNativeContinueStatus(response.Result);
            if (response.Result?.Advanced == true) await SaveNativeAutosave("Native autosave updated.");
            await RefreshDashboardState(); await RefreshStateSummary(); await RefreshInbox(); await RefreshLeagueHub();
        }
        catch (Exception ex) { SetPrimaryStatus($"Native continue failed: {InlineMessage(ex.Message)}"); }
        finally { SetContinueButtonBusy(false); }
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



    private async Task SimUntilSelectedMilestone()
    {
        if (_btnSimUntil != null) _btnSimUntil.Disabled = true;
        SetPrimaryStatus("Simulating to selected milestone...");
        try { await RunNativeSimUntilSelectedMilestone(); }
        finally { if (_btnSimUntil != null) _btnSimUntil.Disabled = false; }
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



    private async Task RefreshInbox()
    {
        UpdateInboxList();
        await Task.CompletedTask;
    }

    private async Task RefreshLeagueHub()
    {
        RefreshLeagueWorkspaceContext();
        await RefreshStandingsAsync();
        await RefreshResultsAsync(GetSelectedResultsWeekKey());
        await RefreshScheduleAsync(_currentTeamId);
        await RefreshInjuryReportAsync(_currentTeamId);
        await RefreshHistoryAsync();
    }

    private void RefreshLeagueWorkspaceContext()
    {
        if (_leagueContextSummary == null)
            return;

        var league = _nativeGameCoreContext?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || team == null)
        {
            _leagueContextSummary.Text = "Start or load a franchise to browse the active league, schedule, injuries, and season archive.";
            return;
        }

        var calendar = league.Calendar;
        _leagueContextSummary.Text = $"{team.Name} | {league.SeasonYear} Season | {calendar?.WeekLabel ?? "Week unavailable"} | {calendar?.Phase ?? "Phase unavailable"} | Active franchise schedule and injury report remain selected while standings, results, and history are browsed.";
    }

    private async Task RefreshStandingsAsync()
    {
        RefreshNativeStandingsView();
        await Task.CompletedTask;
    }

    private async Task RefreshResultsAsync(string weekKey)
    {
        RefreshNativeResultsView(weekKey);
        await Task.CompletedTask;
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

        RefreshNativeScheduleView(teamId, selectionVersion);
        await Task.CompletedTask;
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

        RefreshNativeInjuryReport(teamId, selectionVersion);
        await Task.CompletedTask;
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

        if (IsOpeningWeekReadinessMessage(selectedMessage))
        {
            _depthChartViewActive = false;
            await SelectMainTab(ROSTER_TAB_INDEX);
            SetPrimaryStatus("Review the active roster and player availability, then open Depth Chart to confirm the saved order before Week 1.");
            return;
        }

        if (IsDepthChartInvalidMessage(selectedMessage))
        {
            _depthChartViewActive = true;
            await SelectMainTab(ROSTER_TAB_INDEX);
            return;
        }

        if (IsInjuryDepthAdvisoryMessage(selectedMessage))
        {
            _depthChartViewActive = true;
            await SelectMainTab(ROSTER_TAB_INDEX);
            SetPrimaryStatus("This unit is legal but has no available reserve. Review the saved emergency order; no personnel or depth move was made automatically.");
            return;
        }

        if (IsWaiverClaimConfirmationMessage(selectedMessage))
        {
            ShowRosterManagement();
            SetPrimaryStatus("Time is paused for the winning waiver opportunity. Review the inherited contract and finalize or cancel the selected claim.");
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
            if (string.Equals(type, ScheduleService.DraftPrepPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
            {
                ShowDraftBoard();
                SetPrimaryStatus("Draft preparation remains active. Review the private Team Draft Board; no approval or change is required before starting the draft.");
                return;
            }
            if (string.Equals(type, ScheduleService.TrainingCampPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
            {
                ShowTrainingCamp();
                return;
            }

            await ContinueUntilPause();
            return;
        }

        SetPrimaryStatus("This action is not available yet.");
        await Task.CompletedTask;
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
            _lblGameDayStatus.Text = "Choose full-game simulation or open the incremental live observer.";
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

    private void ConfigureLiveGameObserver()
    {
        if (_liveGameObserver != null)
            return;
        _liveGameObserver = new LiveGameObserver { Name = "LiveGameObserver" };
        _liveGameObserver.ExitRequested += OnLiveGameObserverExit;
        _liveGameObserver.BoxScoreRequested += OnLiveGameObserverBoxScore;
        _liveGameObserver.AdvanceRequested += OnLiveGameAdvanceRequested;
        _liveGameObserver.PauseChanged += OnLiveGamePauseChanged;
        _liveGameObserver.AdjustmentsRequested += () => _ = OpenLiveGameAdjustments();
        AddChild(_liveGameObserver);
    }

    private void ConfigurePostGameHub()
    {
        if (_postGameHub != null)
            return;
        _postGameHub = new PostGameHub { Name = "PostGameHub" };
        _postGameHub.ReturnRequested += () => _ = ClosePostGameRecapPopupAsync();
        AddChild(_postGameHub);
    }

    private async Task OnWatchGamePressed()
    {
        var gameId = FmtString(GetFirstNonNil(_activeGameDayGame, "game_id"), "");
        if (string.IsNullOrWhiteSpace(gameId))
            gameId = FmtString(GetFirstNonNil(_dashboardNextGame, "game_id"), "");
        if (string.IsNullOrWhiteSpace(gameId))
        {
            SetPrimaryStatus("No current game is available to watch.");
            return;
        }

        _btnGameDayWatch.Disabled = true;
        if (_btnGameDaySim != null) _btnGameDaySim.Disabled = true;
        if (_lblGameDayStatus != null) _lblGameDayStatus.Text = "Starting incremental game session…";
        SetPrimaryStatus("Preparing incremental game session...");
        try
        {
            EnsureNativeGameCoreServices();
            var scheduledGame = _nativeGameDayService.GetCurrentUserGame();
            var response = _nativeLiveGameSessionService.Start(gameId);
            if (response?.Ok != true || response.Session?.Result == null)
            {
                var error = string.IsNullOrWhiteSpace(response?.Error) ? "Unable to prepare game playback." : response.Error;
                if (_lblGameDayStatus != null) _lblGameDayStatus.Text = error;
                SetPrimaryStatus(error);
                return;
            }

            _observedGameResult = null;
            CloseGameDayPopup();
            await SaveNativeAutosave("Native autosave updated.");
            var preview = response.Session.Result;
            _liveGameObserver.StartSession(preview, scheduledGame?.HomeTeamId ?? "", response.Session.PlayedEvents, response.Session.IsPaused, LoadTeamLogo(preview.AwayTeam), LoadTeamLogo(preview.HomeTeam));
            SetPrimaryStatus("Live game paused and ready.");
        }
        catch (Exception ex)
        {
            if (_lblGameDayStatus != null) _lblGameDayStatus.Text = "Unable to prepare game playback.";
            SetPrimaryStatus($"Watch Game failed: {InlineMessage(ex.Message)}");
        }
        finally
        {
            _btnGameDayWatch.Disabled = false;
            if (_btnGameDaySim != null) _btnGameDaySim.Disabled = false;
        }
    }

    private void OnLiveGameObserverExit()
    {
        if (_observedGameResult != null)
        {
            ShowPostGameRecapFromResult(_observedGameResult);
            _observedGameResult = null;
            SetPrimaryStatus("Game playback complete.");
        }
        else
            SetPrimaryStatus("Live game saved. Reopen Game Day to continue.");
    }

    private void OnLiveGamePauseChanged(bool paused)
    {
        EnsureNativeGameCoreServices();
        var response = _nativeLiveGameSessionService.SetPaused(paused);
        if (!response.Ok)
            _liveGameObserver.SetSessionError(response.Error);
        else
            _ = SaveNativeAutosave("Native autosave updated.");
    }

    private void OnLiveGameAdvanceRequested()
    {
        EnsureNativeGameCoreServices();
        var response = _nativeLiveGameSessionService.Advance();
        if (!response.Ok)
        {
            _liveGameObserver.SetSessionError(response.Error);
            return;
        }
        _liveGameObserver.ApplySessionEvent(response.Session.CurrentEvent, response.Session.Completed);
        if (response.Session.Completed && response.Session.Result != null)
            _ = FinalizeLiveGameSession(response.Session.Result);
    }

    private async Task FinalizeLiveGameSession(GameResultDto result)
    {
        _observedGameResult = BuildNativeGameResultDictionary(result);
        await SaveNativeAutosave("Native autosave updated.");
        await RefreshDashboardState();
        await RefreshStateSummary();
        await RefreshInbox();
        await RefreshLeagueHub();
        SetPrimaryStatus("Live game final. Review the box score or continue to postgame.");
    }

    private async Task OpenLiveGameAdjustments()
    {
        EnsureNativeGameCoreServices();
        var paused = _nativeLiveGameSessionService.SetPaused(true);
        if (!paused.Ok)
        {
            _liveGameObserver.SetSessionError(paused.Error);
            return;
        }
        _liveGameAdjustmentMode = true;
        _liveGameObserver.PauseForAdjustments();
        _liveGameObserver.Visible = false;
        if (_btnReturnToLiveGame != null) _btnReturnToLiveGame.Visible = true;
        await SelectMainTab(ROSTER_TAB_INDEX);
        await SetRosterViewMode(true);
        SetDepthChartActionStatus("LIVE GAME PAUSED · Drag within a position group or set a starter. Changes apply only to unplayed events.");
        SetPrimaryStatus("Live game paused for depth-chart adjustments.");
    }

    private void ReturnToLiveGameObserver()
    {
        _liveGameAdjustmentMode = false;
        if (_btnReturnToLiveGame != null) _btnReturnToLiveGame.Visible = false;
        if (_btnAutoFillDepthChart != null) _btnAutoFillDepthChart.Disabled = false;
        UpdateDepthChartEditButtons();
        if (_liveGameObserver != null) _liveGameObserver.Visible = true;
        SetPrimaryStatus("Returned to paused live game.");
    }

    private void OnLiveGameObserverBoxScore()
    {
        if (_observedGameResult == null)
            return;
        _restoreLiveGameObserverAfterBoxScore = _liveGameObserver?.Visible == true;
        if (_liveGameObserver != null) _liveGameObserver.Visible = false;
        ShowBoxScoreFromResult(_observedGameResult);
    }





    private void ShowPostGameRecapFromResult(Godot.Collections.Dictionary result, string statusText = "")
    {
        _latestGameResult = result?.Duplicate(true);
        if (_postGameHub != null)
        {
            HidePostGameRecapPopup();
            _postGameHub.ShowResult(result, LoadTeamLogo(FmtString(GetFirstNonNil(result, "away_team"), "")), LoadTeamLogo(FmtString(GetFirstNonNil(result, "home_team"), "")));
            return;
        }
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
        if (_postGameHub != null && _latestGameResult != null)
        {
            _postGameHub.ShowResult(_latestGameResult, LoadTeamLogo(FmtString(GetFirstNonNil(_latestGameResult, "away_team"), "")), LoadTeamLogo(FmtString(GetFirstNonNil(_latestGameResult, "home_team"), "")));
            return;
        }
        if (_postGameRecapPopup != null)
            _postGameRecapPopup.Visible = true;
    }

    private void HidePostGameRecapPopup()
    {
        if (_postGameHub != null)
            _postGameHub.Visible = false;
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
        if (_restoreLiveGameObserverAfterBoxScore && _liveGameObserver != null)
            _liveGameObserver.Visible = true;
        if (_restorePostGameRecapAfterBoxScore)
            ShowPostGameRecapPopup();
        _restoreLiveGameObserverAfterBoxScore = false;
        _restorePostGameRecapAfterBoxScore = false;
        SetPrimaryStatus("Closed box score.");
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

        if (!string.IsNullOrWhiteSpace(_recordBookSummary))
        {
            lines.Add("");
            lines.Add(_recordBookSummary);
        }

        var archiveSummary = BuildArchiveBrowseSummary(season.SeasonYear);
        if (!string.IsNullOrWhiteSpace(archiveSummary))
        {
            lines.Add("");
            lines.Add(archiveSummary);
        }

        if (!string.IsNullOrWhiteSpace(season.GeneratedAtLabel))
            lines.Add($"Archived: {season.GeneratedAtLabel}");

        lines.Add("");
        lines.Add("Champion Summary");
        lines.Add($"Winner: {FallbackText(season.ChampionTeamName, "TBD")}");
        lines.Add($"Runner-Up: {FallbackText(season.RunnerUpTeamName, "TBD")}");

        lines.Add("");
        lines.Add("Season Awards");
        if (season.Awards == null || season.Awards.Count == 0)
            lines.Add("No award archive is available.");
        else
            foreach (var award in season.Awards)
                lines.Add($"{FallbackText(award.AwardName, "Award")}: {FallbackText(award.PlayerName, "Unknown")} ({FallbackText(award.Position, "?")}, {FallbackText(award.TeamName, "Team")}) — {FallbackText(award.Summary, "No summary")}");

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
        lines.Add("Draft Class Recap");
        if (season.DraftClass == null || season.DraftClass.Count == 0)
        {
            lines.Add("No immutable draft recap is available for this season.");
        }
        else
        {
            foreach (var pick in season.DraftClass.OrderBy(entry => entry.OverallPick))
            {
                lines.Add($"#{pick.OverallPick} R{pick.Round}.{pick.PickInRound} - {FallbackText(pick.TeamName, "TBD")}: {FallbackText(pick.Name, "Unknown")} ({FallbackText(pick.Position, "?")}, {FallbackText(pick.College, "College")}, age {pick.Age})");
                lines.Add($"Pre-draft estimate: OVR {pick.EstimatedOverall}, POT {pick.EstimatedPotential} ({pick.Confidence} confidence) | Combine {pick.CombineScore}/100 | Pro day {pick.ProDayScore}/100");
                lines.Add($"Trait: {FallbackText(pick.Trait, "None")} | Interview: {FallbackText(pick.Interview, "No interview note.")}");
                lines.Add($"Placement: {FallbackText(pick.RookiePlacement, "Unavailable")} | {FallbackText(pick.ContractSummary, "Contract unavailable")}");
                if (!string.IsNullOrWhiteSpace(pick.Report))
                    lines.Add($"Report: {pick.Report}");
            }
        }
        lines.Add("");
        lines.Add("Playoff Results");
        AppendHistoryRound(lines, season, "Wild Card");
        AppendHistoryRound(lines, season, "Divisional");
        AppendHistoryRound(lines, season, "Conference Championship");
        AppendHistoryRound(lines, season, "League Championship");

        return string.Join("\n", lines);
    }

    private static string BuildRecordBookSummary(RecordBookResponse recordBook)
    {
        var lines = new List<string> { "Record Book (read-only)" };
        void Add(string heading, IEnumerable<RecordBookEntryDto> records)
        {
            var entry = records?.FirstOrDefault();
            if (entry != null)
                lines.Add($"{heading}: {entry.Label} — {entry.SubjectName} ({entry.Value:N0}{(entry.SeasonYear > 0 ? $", {entry.SeasonYear}" : "")})");
        }
        Add("Season", recordBook.SeasonRecords);
        Add("Career", recordBook.CareerRecords);
        Add("Franchise", recordBook.FranchiseRecords);
        return lines.Count > 1 ? string.Join("\n", lines) : "";
    }

    private string BuildArchiveBrowseSummary(int selectedSeasonYear)
    {
        if (_historicalArchive?.Ok != true) return "";
        var lines = new List<string> { "Archive Browser (read-only)", "Compare completed seasons:" };
        foreach (var championship in (_historicalArchive.Championships ?? new List<HistoricalChampionshipDto>()).Take(8))
        {
            var marker = championship.SeasonYear == selectedSeasonYear ? " >" : "";
            lines.Add($"{marker} {championship.SeasonYear}: {FallbackText(championship.ChampionTeamName, "Champion TBD")} def. {FallbackText(championship.RunnerUpTeamName, "Runner-Up TBD")} {championship.ChampionScore}-{championship.RunnerUpScore}");
        }
        lines.Add("Championship archive is listed above; record-book leaders are shown in the Record Book section.");
        var retirements = _historicalArchive.Retirements ?? new List<HistoricalRetirementDto>();
        lines.Add(retirements.Count == 0 ? "Retirements: no archived retirements." : "Recent retirements:");
        foreach (var retirement in retirements.Take(8))
            lines.Add($"{retirement.SeasonYear}: {FallbackText(retirement.PlayerName, "Unknown")} ({FallbackText(retirement.Position, "?")}, {retirement.Age}) — {FallbackText(retirement.TeamName, "Team")}");
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

    private void PopulateLeagueStandingsReference(StandingsResponse response)
    {
        if (_standingsTree == null) return;
        _standingsTree.Clear(); _standingsTree.Columns = 6;
        var headers = new[] { "Team", "Record", "GB", "PF", "PA", "Playoff Context" };
        var widths = new[] { 210, 86, 58, 62, 62, 180 };
        for (var column = 0; column < headers.Length; column++) { _standingsTree.SetColumnTitle(column, headers[column]); _standingsTree.SetColumnCustomMinimumWidth(column, widths[column]); _standingsTree.SetColumnExpand(column, column is 0 or 5); }
        var root = _standingsTree.CreateItem(); var rows = response?.Standings ?? new List<StandingRowDto>();
        if (rows.Count == 0) { AddHistoryEmptyRow(_standingsTree, root, "Preseason / no regular-season standings are available."); return; }
        var userTeamId = _nativeGameCoreContext?.ActiveLeague?.UserTeamId ?? ""; var seedByTeam = (response.PlayoffBracket?.ConferenceBrackets ?? new List<PlayoffConferenceBracketDto>()).SelectMany(bracket => bracket.Seeds ?? new List<PlayoffSeedDto>()).ToDictionary(seed => seed.TeamId, seed => seed, StringComparer.OrdinalIgnoreCase);
        foreach (var conference in rows.GroupBy(row => row.Conference ?? "Conference", StringComparer.OrdinalIgnoreCase).OrderBy(group => group.Key))
        {
            var conferenceHeader = _standingsTree.CreateItem(root); conferenceHeader.SetText(0, conference.Key.ToUpperInvariant()); for (var column = 0; column < headers.Length; column++) { conferenceHeader.SetCustomBgColor(column, new Color("163242")); conferenceHeader.SetCustomColor(column, new Color("8fcf98")); }
            foreach (var division in conference.GroupBy(row => row.Division ?? "Division", StringComparer.OrdinalIgnoreCase).OrderBy(group => group.Key))
            {
                var divisionHeader = _standingsTree.CreateItem(root); divisionHeader.SetText(0, "  " + division.Key); for (var column = 0; column < headers.Length; column++) { divisionHeader.SetCustomBgColor(column, new Color("102737")); divisionHeader.SetCustomColor(column, new Color("c5d1d8")); }
                var ordered = division.OrderByDescending(row => row.WinPct).ThenByDescending(row => row.PointsFor - row.PointsAgainst).ThenBy(row => row.TeamName).ToList(); var leader = ordered.FirstOrDefault(); var index = 0;
                foreach (var row in ordered)
                {
                    var item = _standingsTree.CreateItem(root); item.SetMetadata(0, row.TeamId); var isUser = string.Equals(row.TeamId, userTeamId, StringComparison.OrdinalIgnoreCase); var gap = ((leader?.Wins ?? 0) - row.Wins + row.Losses - (leader?.Losses ?? 0)) / 2d; seedByTeam.TryGetValue(row.TeamId, out var seed); var playoff = seed == null ? "Seed unavailable" : $"Seed {seed.Seed} · {(seed.IsDivisionWinner ? "division winner" : "wild card")}";
                    item.SetText(0, (isUser ? "◆ " : "    ") + row.TeamName); item.SetText(1, $"{row.Wins}-{row.Losses}" + (row.Ties > 0 ? $"-{row.Ties}" : "")); item.SetText(2, row.TeamId == leader?.TeamId ? "—" : gap.ToString("0.0")); item.SetText(3, row.PointsFor.ToString()); item.SetText(4, row.PointsAgainst.ToString()); item.SetText(5, playoff);
                    for (var column = 0; column < headers.Length; column++) item.SetCustomBgColor(column, isUser ? new Color("193d37") : index++ % 2 == 0 ? new Color("0b1a28") : new Color("0d2031")); for (var column = 1; column < 5; column++) item.SetTextAlignment(column, HorizontalAlignment.Right); if (isUser) item.SetCustomColor(5, new Color("f0c96a"));
                }
            }
        }
        var playoffHeader = _standingsTree.CreateItem(root); playoffHeader.SetText(0, "PLAYOFF PICTURE · saved seeds only"); for (var column = 0; column < headers.Length; column++) { playoffHeader.SetCustomBgColor(column, new Color("3a321a")); playoffHeader.SetCustomColor(column, new Color("f0c96a")); }
        if (seedByTeam.Count == 0) { var unavailable = _standingsTree.CreateItem(root); unavailable.SetText(0, "No current playoff qualification or cut-line state is saved."); unavailable.SetCustomColor(0, new Color("9cadb8")); }
        else foreach (var seed in seedByTeam.Values.OrderBy(seed => seed.Conference).ThenBy(seed => seed.Seed)) { var item = _standingsTree.CreateItem(root); item.SetText(0, $"{seed.Conference} · #{seed.Seed} {seed.TeamName}"); item.SetText(1, $"{seed.Wins}-{seed.Losses}" + (seed.Ties > 0 ? $"-{seed.Ties}" : "")); item.SetText(5, seed.IsDivisionWinner ? "Division winner" : "Wild card"); item.SetTextAlignment(1, HorizontalAlignment.Right); }
    }

    private async Task OpenSelectedLeagueStandingsTeam()
    {
        var selected = _standingsTree?.GetSelected(); if (selected == null || IsNil(selected.GetMetadata(0))) return; var teamId = selected.GetMetadata(0).AsString(); if (string.IsNullOrWhiteSpace(teamId)) return;
        await SelectMainTab(ROSTER_TAB_INDEX); await TrySelectTeamInRoster(teamId);
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

        if (_scheduleInspector != null)
        {
            _scheduleInspector.Text = game == null
                ? "Select a game to review matchup context, result status, and available detail actions."
                : $"MATCHUP DETAIL\n{BuildScheduleMatchupText(game, ResolveNativeScheduleTeamId(_currentTeamId))}\n{GetScheduleWeekText(game)} | {GetScheduleResultText(game)} | Status: {HumanizeStatus(status)}";
        }

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
            id: "player",
            title: "Player",
            defaultVisible: true,
            width: 220,
            expand: true,
            getter: row => row.Name,
            sortGetter: row => row.Name,
            sortable: true));
        _columns.Add(new RosterColumn(
            id: "ovr",
            title: "Staff OVR",
            defaultVisible: true,
            width: 82,
            expand: false,
            getter: row => SafeString(row.Source, "estimated_overall_range", row.Overall > 0 ? row.Overall.ToString() : "-"),
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
            id: "position_role", title: "Position / Role", defaultVisible: false, width: 118, expand: false,
            getter: row => $"{row.PositionDisplay} · {SafeString(row.Source, "depth_role", "Unassigned")}",
            sortGetter: row => $"{row.Position}|{SafeString(row.Source, "depth_role", "")}", sortable: true));
        _columns.Add(new RosterColumn(
            id: "contract", title: "Contract", defaultVisible: true, width: 120, expand: false,
            getter: row => SafeString(row.Source, "contract_summary", "Unavailable"),
            sortGetter: row => GetIntValue(GetFirstNonNil(row.Source, "annual_salary"), 0), sortable: true));
        _columns.Add(new RosterColumn(
            id: "health", title: "Health", defaultVisible: true, width: 128, expand: false,
            getter: row => GetRosterHealthText(row),
            sortGetter: row => GetBoolValue(GetFirstNonNil(row.Source, "is_available"), true) ? 1 : 0, sortable: true));
        _columns.Add(new RosterColumn(
            id: "morale", title: "Morale", defaultVisible: true, width: 105, expand: false,
            getter: row => GetRosterMoraleText(row),
            sortGetter: row => GetIntValue(GetFirstNonNil(row.Source, "morale"), -1), sortable: true));
        _columns.Add(new RosterColumn(
            id: "trait", title: "Trait", defaultVisible: false, width: 145, expand: false,
            getter: row => SafeString(row.Source, "trait", "Unavailable"),
            sortGetter: row => SafeString(row.Source, "trait", ""), sortable: true));
        _columns.Add(new RosterColumn(
            id: "scout_overall", title: "Scout OVR", defaultVisible: false, width: 92, expand: false,
            getter: row => "Unavailable",
            sortGetter: row => 0, sortable: true));
        _columns.Add(new RosterColumn(
            id: "status",
            title: "Status",
            defaultVisible: false,
            width: 120,
            expand: false,
            getter: row => row.Status,
            sortGetter: row => row.Status,
            sortable: true));
        _columns.Add(new RosterColumn(
            id: "injury",
            title: "Injury",
            defaultVisible: false,
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
        _columns.Add(new RosterColumn("potential", "Potential", false, 82, false,
            row => SafeString(row.Source, "estimated_potential_range", "Unavailable"),
            row => GetIntValue(GetFirstNonNil(row.Source, "potential"), 0), true));
        _columns.Add(new RosterColumn("salary", "Salary", false, 92, false,
            row => GetFloatValue(GetFirstNonNil(row.Source, "annual_salary"), 0f) > 0f ? $"${GetFloatValue(GetFirstNonNil(row.Source, "annual_salary"), 0f) / 1_000_000f:0.00}M" : "Unavailable",
            row => GetFloatValue(GetFirstNonNil(row.Source, "annual_salary"), 0f), true));
        _columns.Add(new RosterColumn("fatigue", "Fatigue", false, 72, false,
            row => $"{GetIntValue(GetFirstNonNil(row.Source, "fatigue"), 0)}/100",
            row => GetIntValue(GetFirstNonNil(row.Source, "fatigue"), 0), true));
        _columns.Add(new RosterColumn("pass_yd", "Pass Yds", false, 80, false,
            row => GetIntValue(GetFirstNonNil(row.Source, "passing_yards"), 0).ToString(), row => GetIntValue(GetFirstNonNil(row.Source, "passing_yards"), 0), true));
        _columns.Add(new RosterColumn("rush_yd", "Rush Yds", false, 80, false,
            row => GetIntValue(GetFirstNonNil(row.Source, "rushing_yards"), 0).ToString(), row => GetIntValue(GetFirstNonNil(row.Source, "rushing_yards"), 0), true));
        _columns.Add(new RosterColumn("rec_yd", "Rec Yds", false, 80, false,
            row => GetIntValue(GetFirstNonNil(row.Source, "receiving_yards"), 0).ToString(), row => GetIntValue(GetFirstNonNil(row.Source, "receiving_yards"), 0), true));
        _columns.Add(new RosterColumn("tackles", "Tackles", false, 72, false,
            row => GetIntValue(GetFirstNonNil(row.Source, "tackles"), 0).ToString(), row => GetIntValue(GetFirstNonNil(row.Source, "tackles"), 0), true));
        _columns.Add(new RosterColumn("sacks", "Sacks", false, 64, false,
            row => GetIntValue(GetFirstNonNil(row.Source, "sacks"), 0).ToString(), row => GetIntValue(GetFirstNonNil(row.Source, "sacks"), 0), true));

        InitRosterTree();
        LoadColumnVisibility();
        ApplyColumnVisibility();
        PopulateColumnsMenu();
    }

    private void ConfigureRosterWorkspacePresentation()
    {
        var header = GetNodeOrNull<Label>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/SquadWorkspaceHeader");
        if (header != null)
        {
            header.Text = "TEAM > ROSTER";
            header.AddThemeFontSizeOverride("font_size", 18);
            header.AddThemeColorOverride("font_color", new Color("f4eddf"));
        }

        var hint = GetNodeOrNull<Control>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/SquadWorkspaceHint");
        if (hint != null) hint.Visible = false;
        if (_rosterSummary != null)
        {
            _rosterSummary.AddThemeFontSizeOverride("font_size", 12);
            _rosterSummary.AutowrapMode = TextServer.AutowrapMode.Off;
            _rosterSummary.EllipsisChar = "…";
        }

        var filterRow = GetNodeOrNull<Container>("AppMargin/MainPadding/MainLayout/MainTabs/RosterTab/RosterSplit/RosterPane/FilterRow");
        if (filterRow == null) return;
        if (_rosterSearch != null)
        {
            _rosterSearch.PlaceholderText = "Search player";
            _rosterSearch.CustomMinimumSize = new Vector2(190, 28);
            _rosterSearch.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        }
        if (_posFilter != null) _posFilter.CustomMinimumSize = new Vector2(88, 28);
        if (_rosterStatusFilter == null)
        {
            _rosterStatusFilter = new OptionButton { Name = "RosterStatusFilter", CustomMinimumSize = new Vector2(108, 28), TooltipText = "Filter by availability or roster status" };
            foreach (var item in new[] { "All statuses", "Available", "Injured", "Unavailable", "Starter", "Depth" }) _rosterStatusFilter.AddItem(item);
            filterRow.AddChild(_rosterStatusFilter);
        }
        if (_btnColumns != null)
        {
            var oldParent = _btnColumns.GetParent();
            if (oldParent != filterRow)
            {
                oldParent?.RemoveChild(_btnColumns);
                filterRow.AddChild(_btnColumns);
            }
            _btnColumns.Text = "COLUMNS / VIEW";
            _btnColumns.TooltipText = "Show, hide, reorder, and resize roster columns";
            _btnColumns.CustomMinimumSize = new Vector2(126, 28);
            _btnColumns.Visible = true;
        }
        if (_btnClearFilters != null) { _btnClearFilters.Text = "RESET"; _btnClearFilters.CustomMinimumSize = new Vector2(62, 28); }
        if (_rosterTree != null)
        {
            _rosterTree.CustomMinimumSize = Vector2.Zero;
            _rosterTree.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _rosterTree.AddThemeConstantOverride("item_margin", 2);
            _rosterTree.TooltipText = "Select a player to open their player profile. Column headers sort the roster.";
        }
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
        const int rosterColumnSchemaVersion = 3;
        _columnVisibility.Clear();
        _rosterColumnOrder.Clear();
        _rosterColumnWidths.Clear();
        foreach (var column in _columns)
        {
            _columnVisibility[column.Id] = column.DefaultVisible;
            _rosterColumnOrder.Add(column.Id);
            _rosterColumnWidths[column.Id] = column.Width;
        }
        _rosterColumnOrder.Remove("player");
        _rosterColumnOrder.Insert(0, "player");

        var config = new ConfigFile();
        if (config.Load("user://ui.cfg") != Error.Ok)
            return;

        var savedVersion = GetIntValue(config.GetValue("ui", "dashboard_roster_columns_version", 0), 0);
        if (savedVersion != rosterColumnSchemaVersion)
            return;

        var raw = config.GetValue("ui", "dashboard_roster_columns", "").AsString();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var visibleIds = raw.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var visibleSet = new HashSet<string>(visibleIds, StringComparer.OrdinalIgnoreCase);
            foreach (var column in _columns)
                _columnVisibility[column.Id] = visibleSet.Contains(column.Id);
        }
        _columnVisibility["player"] = true;

        var orderRaw = config.GetValue("ui", "dashboard_roster_column_order", "").AsString();
        if (!string.IsNullOrWhiteSpace(orderRaw))
        {
            var valid = new HashSet<string>(_columns.Select(column => column.Id), StringComparer.OrdinalIgnoreCase);
            var saved = orderRaw.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(valid.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            saved.RemoveAll(id => string.Equals(id, "player", StringComparison.OrdinalIgnoreCase));
            _rosterColumnOrder.Clear(); _rosterColumnOrder.Add("player"); _rosterColumnOrder.AddRange(saved);
            _rosterColumnOrder.AddRange(_columns.Select(column => column.Id).Where(id => !_rosterColumnOrder.Contains(id, StringComparer.OrdinalIgnoreCase)));
        }
        foreach (var column in _columns)
        {
            var width = GetIntValue(config.GetValue("ui", $"dashboard_roster_column_width_{column.Id}", column.Width), column.Width);
            _rosterColumnWidths[column.Id] = Math.Clamp(width, 48, 360);
        }
    }

    private void SaveColumnVisibility()
    {
        const int rosterColumnSchemaVersion = 3;
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
        config.SetValue("ui", "dashboard_roster_column_order", string.Join(",", _rosterColumnOrder));
        foreach (var pair in _rosterColumnWidths)
            config.SetValue("ui", $"dashboard_roster_column_width_{pair.Key}", pair.Value);
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
        _rosterStatusFilterValue = "All statuses";

        try
        {
            var config = new ConfigFile();
            if (config.Load("user://ui.cfg") == Error.Ok)
            {
                _rosterSearchText = config.GetValue("ui", "dashboard_roster_search", "").AsString();
                _posFilterValue = config.GetValue("ui", "dashboard_roster_pos_filter", PosFilterOptions[0]).AsString();
                _rosterStatusFilterValue = config.GetValue("ui", "dashboard_roster_status_filter", "All statuses").AsString();
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
        if (_rosterStatusFilter != null)
        {
            var selected = 0;
            for (var i = 0; i < _rosterStatusFilter.ItemCount; i++)
                if (string.Equals(_rosterStatusFilter.GetItemText(i), _rosterStatusFilterValue, StringComparison.OrdinalIgnoreCase)) { selected = i; break; }
            _rosterStatusFilter.Select(selected);
            _rosterStatusFilterValue = _rosterStatusFilter.GetItemText(selected);
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
            config.SetValue("ui", "dashboard_roster_status_filter", _rosterStatusFilterValue ?? "All statuses");
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
                || IsInjuryDepthAdvisoryMessage(message)
                || IsOpeningWeekReadinessMessage(message)
                || IsWaiverClaimConfirmationMessage(message)
                || IsPostseasonPendingMessage(message)
                || IsSeasonCompleteMessage(message)
                || IsOffseasonPendingMessage(message);
            var primaryActionLabel = ResolveInboxPrimaryActionLabel(message);
            _overviewActionButton.Visible = canUsePrimaryAction;
            _overviewActionButton.Disabled = !canUsePrimaryAction;
            _overviewActionButton.Text = primaryActionLabel;
            _overviewActionButton.TooltipText = "";
        }
        RefreshInboxDesk();
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

    private static bool IsOpeningWeekReadinessMessage(Godot.Collections.Dictionary message)
    {
        if (message == null)
            return false;

        var type = FmtString(GetFirstNonNil(message, "type"), "");
        return string.Equals(type, "opening_week_readiness", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInjuryDepthAdvisoryMessage(Godot.Collections.Dictionary message)
    {
        if (message == null)
            return false;

        var type = FmtString(GetFirstNonNil(message, "type"), "");
        return string.Equals(type, "injury_depth_advisory", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWaiverClaimConfirmationMessage(Godot.Collections.Dictionary message)
    {
        if (message == null)
            return false;

        var type = FmtString(GetFirstNonNil(message, "type"), "");
        return string.Equals(type, "waiver_claim_confirmation", StringComparison.OrdinalIgnoreCase);
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
        RefreshInboxDesk();
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

        var status = _rosterStatusFilterValue ?? "All statuses";
        if (!string.Equals(status, "All statuses", StringComparison.OrdinalIgnoreCase))
        {
            var available = GetBoolValue(GetFirstNonNil(row.Source, "is_available"), true);
            var injury = SafeString(row.Source, "injury", "");
            var role = SafeString(row.Source, "depth_role", "");
            if (string.Equals(status, "Available", StringComparison.OrdinalIgnoreCase) && !available) return false;
            if (string.Equals(status, "Injured", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(injury)) return false;
            if (string.Equals(status, "Unavailable", StringComparison.OrdinalIgnoreCase) && available) return false;
            if (string.Equals(status, "Starter", StringComparison.OrdinalIgnoreCase) && !role.Contains("starter", StringComparison.OrdinalIgnoreCase)) return false;
            if (string.Equals(status, "Depth", StringComparison.OrdinalIgnoreCase) && role.Contains("starter", StringComparison.OrdinalIgnoreCase)) return false;
        }

        return true;
    }

    private void OnRosterStatusFilterItemSelected(long index)
    {
        if (_suppressRosterFilterEvents || _rosterStatusFilter == null) return;
        _rosterStatusFilterValue = index >= 0 && index < _rosterStatusFilter.ItemCount ? _rosterStatusFilter.GetItemText((int)index) : "All statuses";
        SaveRosterFilters(); ApplyRosterFilters();
    }

    private static string GetRosterHealthText(PlayerRow row)
    {
        var available = GetBoolValue(GetFirstNonNil(row.Source, "is_available"), true);
        var injury = SafeString(row.Source, "injury", "");
        var days = GetIntValue(GetFirstNonNil(row.Source, "injury_days_remaining"), 0);
        if (!available) return string.IsNullOrWhiteSpace(injury) ? "Unavailable" : days > 0 ? $"{injury} · {days}d" : injury;
        return "Available";
    }

    private static string GetRosterMoraleText(PlayerRow row)
    {
        var morale = GetIntValue(GetFirstNonNil(row.Source, "morale"), -1);
        var trend = SafeString(row.Source, "morale_trend", "Unavailable");
        return morale < 0 ? "Unavailable" : $"{morale} · {trend}";
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
        foreach (var id in _rosterColumnOrder)
        {
            var column = GetColumnById(id);
            if (column == null) continue;
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
            var width = _rosterColumnWidths.TryGetValue(column.Id, out var savedWidth) ? savedWidth : column.Width;
            if (width > 0) _rosterTree.SetColumnCustomMinimumWidth(i, width);
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
            if (string.Equals(column.Id, "player", StringComparison.OrdinalIgnoreCase))
                _popupColumns.SetItemDisabled(i, true);
        }
        _popupColumns.AddSeparator();
        _popupColumns.AddItem("Move selected column left", 9001);
        _popupColumns.AddItem("Move selected column right", 9002);
        _popupColumns.AddItem("Reset roster view", 9003);
    }

    private void OnColumnsPressed()
    {
        if (_btnColumns == null || _popupColumns == null)
            return;

        CaptureRosterColumnWidths();
        SaveColumnVisibility();
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
        if (_rosterStatusFilter != null)
            _rosterStatusFilter.Select(0);
        _suppressRosterFilterEvents = false;

        _rosterSearchText = "";
        _posFilterValue = PosFilterOptions[0];
        _rosterStatusFilterValue = "All statuses";
        SaveRosterFilters();
        ApplyRosterFilters();
    }

    private void OnColumnMenuIdPressed(long id)
    {
        if (id >= 9001)
        {
            if (id == 9003)
            {
                _rosterColumnOrder.Clear();
                _rosterColumnOrder.AddRange(new[] { "player", "age", "position_role", "contract", "health", "morale", "scout_overall" });
                _rosterColumnOrder.AddRange(_columns.Select(column => column.Id).Where(columnId => !_rosterColumnOrder.Contains(columnId, StringComparer.OrdinalIgnoreCase)));
                foreach (var column in _columns) _columnVisibility[column.Id] = column.DefaultVisible;
                _columnVisibility["player"] = true;
            }
            else if (!string.IsNullOrWhiteSpace(_lastRosterColumnId) && !string.Equals(_lastRosterColumnId, "player", StringComparison.OrdinalIgnoreCase))
            {
                var oldIndex = _rosterColumnOrder.FindIndex(columnId => string.Equals(columnId, _lastRosterColumnId, StringComparison.OrdinalIgnoreCase));
                var nextIndex = id == 9001 ? oldIndex - 1 : oldIndex + 1;
                if (oldIndex > 0 && nextIndex > 0 && nextIndex < _rosterColumnOrder.Count)
                {
                    (_rosterColumnOrder[oldIndex], _rosterColumnOrder[nextIndex]) = (_rosterColumnOrder[nextIndex], _rosterColumnOrder[oldIndex]);
                }
            }
            ApplyColumnVisibility(); SaveColumnVisibility(); return;
        }
        var columnIndex = (int)id;
        if (columnIndex < 0 || columnIndex >= _columns.Count)
            return;

        var selectedColumn = _columns[columnIndex];
        var current = _columnVisibility.TryGetValue(selectedColumn.Id, out var isVisible)
            ? isVisible
            : selectedColumn.DefaultVisible;
        _columnVisibility[selectedColumn.Id] = !current;

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
        _lastRosterColumnId = columnDef.Id;
        CaptureRosterColumnWidths();
        SaveColumnVisibility();
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

    private void CaptureRosterColumnWidths()
    {
        if (_rosterTree == null) return;
        var visible = GetVisibleColumns();
        for (var index = 0; index < visible.Count; index++)
        {
            var width = _rosterTree.GetColumnWidth(index);
            if (width > 0) _rosterColumnWidths[visible[index].Id] = Math.Clamp(width, 48, 360);
        }
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
                item.SetCustomBgColor(colIndex, i % 2 == 0 ? new Color("0b1a28") : new Color("0d2031"));
                if (IsNumericRosterColumn(column.Id))
                    item.SetTextAlignment(colIndex, HorizontalAlignment.Right);
                if (string.Equals(column.Id, "health", StringComparison.OrdinalIgnoreCase))
                {
                    var health = column.Getter(player);
                    item.SetCustomColor(colIndex, health.StartsWith("Available", StringComparison.OrdinalIgnoreCase) ? new Color("8fcf98") : new Color("f0c96a"));
                }
                else if (string.Equals(column.Id, "morale", StringComparison.OrdinalIgnoreCase))
                {
                    var morale = GetIntValue(GetFirstNonNil(player.Source, "morale"), -1);
                    item.SetCustomColor(colIndex, morale < 0 ? new Color("9cadb8") : morale < 40 ? new Color("e58b7a") : new Color("c5d1d8"));
                }
            }
        }
    }

    private static bool IsNumericRosterColumn(string id)
        => id is "age" or "ovr" or "potential" or "salary" or "fatigue" or "pass_yd" or "rush_yd" or "rec_yd" or "tackles" or "sacks";

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
        if (_rosterPane != null)
            _rosterPane.Visible = false;
        if (_playerReportPanel != null)
        {
            _playerReportPanel.Visible = true;
            _playerReportPanel.CustomMinimumSize = Vector2.Zero;
        }

        var pos = GetString(player, "position");
        var name = GetString(player, "name");
        var age = GetAgeValue(player);

        var displayName = string.IsNullOrWhiteSpace(name) ? "Player" : name;
        if (_squadWorkspaceHeader != null)
            _squadWorkspaceHeader.Text = $"TEAM  >  ROSTER  >  {displayName.ToUpperInvariant()}";
        var ageText = age > 0 ? age.ToString() : "?";
        var confidenceValue = GetFirstNonNil(player, "confidence", "scout_confidence", "scouting_confidence");
        var confidence = FmtString(confidenceValue, "");

        var header = string.IsNullOrWhiteSpace(pos)
            ? $"{displayName}  ·  AGE {ageText}"
            : $"{displayName}  ·  {pos}  ·  AGE {ageText}";
        if (!string.IsNullOrWhiteSpace(confidence))
            header += $"  ·  SCOUT CONFIDENCE {confidence.ToUpperInvariant()}";

        var nativePlayer = _nativeGameCoreContext?.ActiveLeague?.Teams.SelectMany(team => team?.Roster ?? Enumerable.Empty<PlayerState>()).FirstOrDefault(candidate => string.Equals(candidate?.PlayerId, GetPlayerId(player), StringComparison.OrdinalIgnoreCase));
        var playerTeam = _nativeGameCoreContext?.ActiveLeague?.Teams.FirstOrDefault(team => team.Roster.Any(candidate => candidate.PlayerId == nativePlayer?.PlayerId));
        if (nativePlayer != null)
            header += $"\n{playerTeam?.Name ?? "Team unavailable"}  ·  {nativePlayer.Status}  ·  Morale {nativePlayer.Morale}/100 ({nativePlayer.MoraleTrend})";

        if (_lblPlayerHeader != null)
            _lblPlayerHeader.Text = header;

        var summary = player.ContainsKey("scout_summary")
            ? FmtString((Variant)player["scout_summary"], "")
            : "";
        if (_rtlScoutSummary != null)
            _rtlScoutSummary.Text = "STAFF EVALUATION\n" + $"Overall: {SafeString(player, "estimated_overall_range", "Unavailable")} | Potential: {SafeString(player, "estimated_potential_range", "Unavailable")} | Confidence: {SafeString(player, "scouting_confidence", "Low")} | Fatigue: {nativePlayer?.Fatigue.ToString() ?? "Unavailable"}\n\nSCOUT & COACH ASSESSMENTS\n" + BuildPlayerInspectorSummary(player, summary);

        var report = player.ContainsKey("scout_report")
            ? FmtString((Variant)player["scout_report"], "")
            : "";
        if (_rtlScoutReport != null)
        {
            var trait = string.IsNullOrWhiteSpace(nativePlayer?.Trait) ? "No recorded trait." : nativePlayer.Trait;
            _rtlScoutReport.Text = $"TRAITS & PERSONALITY\n{trait}\n\nDETAILED SCOUT REPORT\n"
                + (string.IsNullOrWhiteSpace(report) ? "No staff scouting narrative has been recorded for this player." : report)
                + $"\n\n{BuildPlayerRecentHistory(nativePlayer)}";
        }

        UpdateRosterEvaluationFeedback(GetPlayerId(player));
        UpdatePlayerStatisticsHistory(GetPlayerId(player));
        UpdateTags(player);
    }

    private string BuildPlayerRecentHistory(PlayerState player)
    {
        if (player == null)
            return "RECENT HISTORY\nPlayer history is unavailable.";

        var lines = new List<string> { "RECENT HISTORY" };
        var activeInjury = player.CurrentInjury?.IsActive == true
            ? $"Current injury: {player.CurrentInjury.Name} · {player.CurrentInjury.DaysRemaining} day(s) remaining"
            : "Health: no active injury";
        lines.Add(activeInjury);

        foreach (var injury in (player.InjuryHistory ?? new List<PlayerInjuryRecord>())
                     .OrderByDescending(record => record.SeasonYear)
                     .ThenByDescending(record => record.OccurredOn)
                     .Take(3))
        {
            var recovery = string.IsNullOrWhiteSpace(injury.RecoveredOn) ? "recovery pending" : $"recovered {injury.RecoveredOn}";
            lines.Add($"{injury.SeasonYear}: {injury.Name} · {injury.DaysOut} day(s) · {recovery}");
        }

        foreach (var development in (player.DevelopmentHistory ?? new List<PlayerDevelopmentRecord>())
                     .OrderByDescending(record => record.SeasonYear)
                     .Take(3))
        {
            var direction = development.OverallAfter > development.OverallBefore ? "Improved"
                : development.OverallAfter < development.OverallBefore ? "Regressed"
                : "Stable";
            lines.Add($"{development.SeasonYear}: {direction} · {development.Note}");
        }

        var transactions = _nativeGameCoreContext?.ActiveLeague?.Transactions?
            .Where(record => string.Equals(record.PlayerId, player.PlayerId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.SeasonYear)
            .ThenByDescending(record => record.DateLabel)
            .Take(3)
            .ToList() ?? new List<TransactionRecord>();
        foreach (var transaction in transactions)
            lines.Add($"{transaction.DateLabel}: {transaction.Type} · {(string.IsNullOrWhiteSpace(transaction.Details) ? transaction.TeamName : transaction.Details)}");

        if (lines.Count == 2 && player.CurrentInjury?.IsActive != true)
            lines.Add("No archived development, injury, or transaction events are recorded yet.");
        return string.Join("\n", lines);
    }

    private string BuildPlayerInspectorSummary(Godot.Collections.Dictionary player, string scoutSummary)
    {
        var playerId = GetPlayerId(player);
        var depthRole = SafeString(player, "depth_role", "Unassigned");
        var fatigue = SafeIntDisplay(player, "fatigue", fallback: "0");
        var available = GetBoolValue(GetFirstNonNil(player, "is_available"), true);
        var injury = SafeString(player, "injury", "");
        var injuryDays = SafeIntDisplay(player, "injury_days_remaining", fallback: "0");
        var availability = available ? "Available" : string.IsNullOrWhiteSpace(injury) ? "Unavailable" : $"{injury} ({injuryDays}d)";
        var nativePlayer = _nativeGameCoreContext?.ActiveLeague?.Teams
            .SelectMany(team => team?.Roster ?? Enumerable.Empty<PlayerState>())
            .FirstOrDefault(candidate => string.Equals(candidate?.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        var contract = nativePlayer?.Contract;
        var contractText = contract == null || contract.AnnualSalary <= 0m
            ? "Contract: unavailable"
            : $"Contract: {contract.ContractType} | ${contract.AnnualSalary / 1_000_000m:0.00}M | {contract.YearsRemaining} yr";
        var overview = $"Depth: {depthRole} | {availability} | Fatigue: {fatigue}/100\n{contractText}";
        return string.IsNullOrWhiteSpace(scoutSummary) ? overview : $"{overview}\n{scoutSummary}";
    }

    private void UpdatePlayerStatisticsHistory(string playerId)
    {
        if (_rtlPlayerStats == null)
            return;

        if (_nativeGameCoreContext?.ActiveLeague == null)
        {
            _rtlPlayerStats.Text = "Season statistics are available in the Native C# GameCore.";
            return;
        }

        var response = new PlayerHistoryService(_nativeGameCoreContext).GetPlayerHistory(playerId, _currentTeamId);
        if (!response.Ok)
        {
            _rtlPlayerStats.Text = response.Error;
            return;
        }

        var lines = new List<string>
        {
            $"Season Statistics ({response.CurrentSeason.SeasonYear} live): {FormatPlayerStatistics(response.CurrentSeason)}",
            "Career History (archived):",
        };
        if (response.CareerSeasons.Count == 0)
            lines.Add("No archived seasons yet.");
        else
            lines.AddRange(response.CareerSeasons.Select(stats => $"{stats.SeasonYear}: {FormatPlayerStatistics(stats)}"));

        if (!string.IsNullOrWhiteSpace(response.College) || response.CollegeSeasons.Count > 0)
        {
            lines.Add("");
            lines.Add($"COLLEGE CAREER{(string.IsNullOrWhiteSpace(response.College) ? "" : $" · {response.College}")}");
            if (response.CollegeSeasons.Count == 0)
            {
                lines.Add("No college season statistics available.");
            }
            else
            {
                lines.AddRange(response.CollegeSeasons.Select(stats => $"{stats.SeasonYear}: {FormatCollegeStatistics(stats)}"));
                lines.Add($"Career: {FormatCollegeStatistics(new CollegeSeasonHistoryDto
                {
                    GamesPlayed = response.CollegeSeasons.Sum(stats => stats.GamesPlayed),
                    PassingYards = response.CollegeSeasons.Sum(stats => stats.PassingYards),
                    RushingYards = response.CollegeSeasons.Sum(stats => stats.RushingYards),
                    ReceivingYards = response.CollegeSeasons.Sum(stats => stats.ReceivingYards),
                    Touchdowns = response.CollegeSeasons.Sum(stats => stats.Touchdowns),
                })}");
            }
        }
        _rtlPlayerStats.Text = "CURRENT-YEAR STATS\n" + string.Join("\n", lines);
    }

    private static string FormatPlayerStatistics(PlayerSeasonHistoryDto stats)
    {
        var lines = new List<string> { $"GP {stats.GamesPlayed}" };
        if (stats.PassingYards > 0 || stats.PassingTouchdowns > 0)
            lines.Add($"Pass {stats.PassingYards} yd, {stats.PassingTouchdowns} TD");
        if (stats.RushingYards > 0 || stats.RushingTouchdowns > 0)
            lines.Add($"Rush {stats.RushingYards} yd, {stats.RushingTouchdowns} TD");
        if (stats.ReceivingYards > 0 || stats.ReceivingTouchdowns > 0)
            lines.Add($"Rec {stats.ReceivingYards} yd, {stats.ReceivingTouchdowns} TD");
        if (stats.Tackles > 0 || stats.Sacks > 0 || stats.Interceptions > 0)
            lines.Add($"Def {stats.Tackles} TKL, {stats.Sacks} SK, {stats.Interceptions} INT");
        return string.Join(" | ", lines);
    }

    private static string FormatCollegeStatistics(CollegeSeasonHistoryDto stats)
    {
        var lines = new List<string> { $"GP {stats.GamesPlayed}" };
        if (stats.PassingYards > 0)
            lines.Add($"Pass {stats.PassingYards} yd");
        if (stats.RushingYards > 0)
            lines.Add($"Rush {stats.RushingYards} yd");
        if (stats.ReceivingYards > 0)
            lines.Add($"Rec {stats.ReceivingYards} yd");
        if (stats.Touchdowns > 0)
            lines.Add($"{stats.Touchdowns} TD");
        return string.Join(" | ", lines);
    }

    private void UpdateRosterEvaluationFeedback(string playerId)
    {
        if (_lblRosterEvaluation == null)
            return;

        if (_nativeGameCoreContext?.ActiveLeague == null || string.IsNullOrWhiteSpace(playerId))
        {
            _lblRosterEvaluation.Text = "Current role: unavailable.";
            return;
        }

        var response = new RosterEvaluationService(_nativeGameCoreContext).GetPlayerRoles(_currentTeamId);
        var feedback = response.Ok
            ? response.Players.FirstOrDefault(item => string.Equals(item.PlayerId, playerId, StringComparison.OrdinalIgnoreCase))
            : null;
        _lblRosterEvaluation.Text = feedback == null
            ? "Current role: unavailable."
            : $"Current role: {feedback.Role} | Readiness: {feedback.Readiness}\n{feedback.Explanation}";
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
        if (_rosterPane != null)
            _rosterPane.Visible = true;
        if (_playerReportPanel != null)
            _playerReportPanel.Visible = false;
        if (_squadWorkspaceHeader != null)
            _squadWorkspaceHeader.Text = "TEAM  >  ROSTER";
        if (_lblPlayerHeader != null)
            _lblPlayerHeader.Text = "Player Profile";
        if (_rtlScoutSummary != null)
            _rtlScoutSummary.Text = message;
        if (_rtlScoutReport != null)
            _rtlScoutReport.Text = "";
        if (_lblRosterEvaluation != null)
            _lblRosterEvaluation.Text = "Current role: select a player to evaluate readiness and depth standing.";
        if (_rtlPlayerStats != null)
            _rtlPlayerStats.Text = "Season statistics: select a player to review live totals and archived career seasons.";
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

    private readonly record struct DepthFieldSlot(string PlayerId, string Name, string Position, bool Available);
    private readonly record struct DevelopmentRow(string PlayerId, string Name, string Position, int Age, int Overall, string Trend, string Movement, string Notes);
    private readonly record struct TeamHistorySeasonRow(int Season, string Record, string DivisionFinish, string PlayoffResult, string ChampionshipResult, SeasonHistoryRecord Source);
    private readonly record struct LeagueNewsStory(string Headline, string Summary, string Context, string TeamId, string PlayerId, string Kind);
    private readonly record struct StaffRow(string Department, string Role, string CoachId, string Member, string Tendency, string Aptitude, string Age, bool IsVacant)
    {
        public static StaffRow FromCoach(string department, string role, CoachState coach)
        {
            if (coach == null)
                return new StaffRow(department, role, string.Empty, "VACANT", "Tendency unavailable", "Aptitude unavailable", "Unavailable", true);
            return new StaffRow(
                department,
                role,
                coach.CoachId,
                string.IsNullOrWhiteSpace(coach.Name) ? "Assigned staff member unavailable" : coach.Name,
                "Tendency unavailable",
                coach.Overall > 0 ? $"Overall {coach.Overall} · role aptitude unavailable" : "Aptitude unavailable",
                coach.Age > 0 ? coach.Age.ToString() : "Unavailable",
                false);
        }
    }
    private readonly record struct InjuryRow(string PlayerId, string Name, string Position, string Availability, string InjuryStatus, string Recovery)
    {
        public static InjuryRow FromPlayer(PlayerState player, bool isInjuredReserve)
        {
            var injury = !string.IsNullOrWhiteSpace(player?.CurrentInjury?.Name)
                ? player.CurrentInjury.Name
                : !string.IsNullOrWhiteSpace(player?.Injury)
                    ? player.Injury
                    : "Injury detail unavailable";
            var daysRemaining = player?.CurrentInjury?.DaysRemaining ?? 0;
            var recovery = daysRemaining > 0
                ? $"{daysRemaining} day{(daysRemaining == 1 ? string.Empty : "s")} remaining"
                : "Recovery estimate unavailable";
            var guidance = isInjuredReserve
                ? "IR: plan without this player; review activation when healthy."
                : daysRemaining <= 0
                    ? "Review availability before changing depth."
                    : daysRemaining <= 3
                        ? "Near return: keep the current backup ready."
                        : daysRemaining <= 7
                            ? "Short absence: review the next depth option."
                            : "Extended absence: consider a depth-chart or roster response.";
            var availability = isInjuredReserve
                ? "Injured Reserve"
                : PlayerInjuryService.IsAvailableForGame(player)
                    ? "Available"
                    : string.IsNullOrWhiteSpace(player?.Status)
                        ? "Availability unavailable"
                        : player.Status;
            var rosterStatus = string.IsNullOrWhiteSpace(player?.Status) ? "Status unavailable" : player.Status;
            var injuryStatus = isInjuredReserve ? $"{injury} · IR" : $"{injury} · {rosterStatus}";
            return new InjuryRow(player?.PlayerId ?? string.Empty, player?.Name ?? "Unknown player", player?.Position ?? "—", availability, injuryStatus, $"{recovery} · {guidance}");
        }
    }

    private sealed partial class DepthFieldDiagram : Control
    {
        private readonly List<DepthFieldSlot> _slots = new();
        private readonly List<(Rect2 rect, string playerId)> _hitAreas = new();
        public string SelectedPlayerId { get; set; } = "";
        public Action<string> PlayerPressed { get; set; }

        public DepthFieldDiagram()
        {
            TooltipText = "Starting offense and defense. Select a player to reveal their depth assignment.";
            MouseFilter = MouseFilterEnum.Stop;
        }

        public void SetSlots(IEnumerable<DepthFieldSlot> slots)
        {
            _slots.Clear(); if (slots != null) _slots.AddRange(slots); QueueRedraw();
        }

        public override void _Draw()
        {
            var area = GetRect(); var size = Size;
            DrawRect(new Rect2(Vector2.Zero, size), new Color("08252c"));
            for (var line = 1; line < 10; line++)
            {
                var y = size.Y * line / 10f;
                DrawLine(new Vector2(12, y), new Vector2(size.X - 12, y), new Color("4d8585", 0.33f), 1f);
            }
            DrawString(ThemeDB.FallbackFont, new Vector2(14, 22), "STARTING LINEUP", HorizontalAlignment.Left, -1, 13, new Color("d7e0e4"));
            DrawString(ThemeDB.FallbackFont, new Vector2(14, size.Y * .5f), "OFFENSE", HorizontalAlignment.Left, -1, 11, new Color("8fcf98"));
            DrawString(ThemeDB.FallbackFont, new Vector2(14, size.Y * .78f), "DEFENSE", HorizontalAlignment.Left, -1, 11, new Color("8fcf98"));
            _hitAreas.Clear();
            var offense = _slots.Where(slot => IsOffense(slot.Position)).ToList();
            var defense = _slots.Where(slot => !IsOffense(slot.Position)).ToList();
            DrawSlots(offense, size.Y * .28f, size, true); DrawSlots(defense, size.Y * .68f, size, false);
        }

        private void DrawSlots(List<DepthFieldSlot> slots, float centerY, Vector2 size, bool offense)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var x = 42 + (size.X - 84) * (i + 0.5f) / Math.Max(1, slots.Count);
                var y = centerY + ((i % 3) - 1) * 38;
                var rect = new Rect2(x - 40, y - 17, 80, 34);
                var selected = string.Equals(slots[i].PlayerId, SelectedPlayerId, StringComparison.OrdinalIgnoreCase);
                var color = selected ? new Color("d2a74b") : slots[i].Available ? new Color("1c5960") : new Color("6d423c");
                DrawStyleBox(CreateSlotStyle(color, selected), rect);
                DrawString(ThemeDB.FallbackFont, new Vector2(rect.Position.X + 5, rect.Position.Y + 13), slots[i].Position, HorizontalAlignment.Left, 70, 10, new Color("f4eddf"));
                var shortName = slots[i].Name.Length > 11 ? slots[i].Name[..11] : slots[i].Name;
                DrawString(ThemeDB.FallbackFont, new Vector2(rect.Position.X + 5, rect.Position.Y + 27), shortName, HorizontalAlignment.Left, 72, 10, new Color("d7e0e4"));
                _hitAreas.Add((rect, slots[i].PlayerId));
            }
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
                foreach (var area in _hitAreas)
                    if (area.rect.HasPoint(mouse.Position)) { PlayerPressed?.Invoke(area.playerId); AcceptEvent(); return; }
        }

        private static bool IsOffense(string position) => position is "QB" or "RB" or "FB" or "WR" or "TE" or "LT" or "LG" or "C" or "RG" or "RT";
        private static StyleBoxFlat CreateSlotStyle(Color fill, bool selected)
        {
            var style = new StyleBoxFlat { BgColor = fill, BorderColor = selected ? new Color("ffe3a1") : new Color("70a7a7") };
            style.BorderWidthLeft = style.BorderWidthRight = style.BorderWidthTop = style.BorderWidthBottom = selected ? 2 : 1; return style;
        }
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
