using NeoModLoader.api;
using NeoModLoader.General;
using NeoModLoader.General.UI.Prefabs;
using NeoModLoader.services;
using NeoModLoader.utils;
using UnityEngine;
using UnityEngine.UI;

namespace NeoModLoader.ui;

internal class ExternalModHotLoadWindow : AbstractWideWindow<ExternalModHotLoadWindow>
{
    private ExternalModHotLoadService.ExternalModCandidate _selectedCandidate;
    private List<ExternalModHotLoadService.ExternalModCandidate> _candidates = new();
    private ObjectPoolGenericMono<SimpleButton> _listItemPool;
    private RectTransform _listPart;
    private Text _detailsText;
    private Text _statusText;
    private TextInput _pathInput;
    private string _currentRootPath;

    protected override void Init()
    {
        SetSize(new Vector2(650, 320));

        CreateToolbar();
        CreateList();
        CreateInfoPanel();
        CreateControlBar();

        _currentRootPath = GetInitialRootPath();
        RefreshPathInput();
        RefreshCandidates();
    }

    public static void ShowWindow()
    {
        if (Instance == null)
        {
            CreateAndInit("ExternalMods", new Vector2(650, 320));
        }

        Instance.RefreshPathInput();
        Instance.RefreshCandidates();

        ScrollWindow window = ScrollWindow.get(WindowId);
        if (window != null)
        {
            window.gameObject.SetActive(true);
            window.transform.SetAsLastSibling();
            window.clickShow();
            return;
        }

        Instance.gameObject.SetActive(true);
        Instance.transform.SetAsLastSibling();
        ScrollWindow.showWindow(WindowId);
    }

    private void CreateToolbar()
    {
        var toolbar = new GameObject("Toolbar", typeof(Image), typeof(HorizontalLayoutGroup));
        toolbar.transform.SetParent(BackgroundTransform);
        toolbar.transform.localPosition = new Vector3(0, 116, 0);
        toolbar.transform.localScale = Vector3.one;
        toolbar.GetComponent<Image>().sprite = InternalResourcesGetter.GetWindowEmptyFrame();
        toolbar.GetComponent<Image>().type = Image.Type.Sliced;

        RectTransform toolbarRect = toolbar.GetComponent<RectTransform>();
        toolbarRect.sizeDelta = new Vector2(600, 36);

        HorizontalLayoutGroup toolbarLayout = toolbar.GetComponent<HorizontalLayoutGroup>();
        toolbarLayout.childAlignment = TextAnchor.MiddleLeft;
        toolbarLayout.childControlHeight = false;
        toolbarLayout.childControlWidth = false;
        toolbarLayout.childForceExpandHeight = false;
        toolbarLayout.childForceExpandWidth = false;
        toolbarLayout.spacing = 4;
        toolbarLayout.padding = new RectOffset(8, 8, 2, 2);

        _pathInput = Instantiate(TextInput.Prefab, toolbar.transform);
        _pathInput.SetSize(new Vector2(120, 28));
        _pathInput.Setup(_currentRootPath ?? string.Empty, value => { _currentRootPath = value; });

        AddToolbarButton(toolbar.transform, "Use Path", () =>
        {
            _currentRootPath = _pathInput.input.text;
            RefreshCandidates();
        }, 72);

        AddToolbarButton(toolbar.transform, "Browse", () =>
        {
            bool success = ExternalModHotLoadService.TryBrowseForFolder(_currentRootPath, out string selectedPath,
                out string message);
            SetStatus(message);
            if (success)
            {
                _currentRootPath = selectedPath;
                RefreshPathInput();
                RefreshCandidates();
            }
            else
            {
                WorldTip.showNow(message, true, "top", 4f);
            }
        }, 66);

        AddToolbarButton(toolbar.transform, "Documents", () =>
        {
            _currentRootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            RefreshPathInput();
            RefreshCandidates();
        }, 62);

        AddToolbarButton(toolbar.transform, "Downloads", () =>
        {
            _currentRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            RefreshPathInput();
            RefreshCandidates();
        }, 82);

        AddToolbarButton(toolbar.transform, "Desktop", () =>
        {
            _currentRootPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            RefreshPathInput();
            RefreshCandidates();
        }, 76);

        AddToolbarButton(toolbar.transform, "Explorer", () =>
        {
            bool success = ExternalModHotLoadService.TryOpenInFileManager(_currentRootPath, out string message);
            SetStatus(message);
            WorldTip.showNow(message, !success, "top", success ? 3f : 4f);
        }, 78);
    }

    private void CreateList()
    {
        GameObject listPart = BackgroundTransform.Find("Scroll View").gameObject;
        listPart.name = "ExternalMods Scroll View";
        RectTransform rectTransform = listPart.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(180, 214);
        rectTransform.localPosition = new Vector3(-200, -6, 0);
        rectTransform.localScale = Vector3.one;

        ScrollRect scrollRect = listPart.GetComponent<ScrollRect>();
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.verticalScrollbar.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 0);

        Image scrollAreaBackground = listPart.GetComponent<Image>();
        scrollAreaBackground.sprite = SpriteTextureLoader.getSprite("ui/special/windowEmptyFrame");
        scrollAreaBackground.type = Image.Type.Sliced;
        scrollAreaBackground.color = Color.white;

        RectTransform scrollViewPort = listPart.transform.Find("Viewport").GetComponent<RectTransform>();
        scrollViewPort.sizeDelta = new Vector2(0, -20);
        scrollViewPort.localPosition = new Vector3(-90, 97, 0);

        Transform scrollbar = listPart.transform.Find("Scrollbar Vertical Mask");
        scrollbar.localPosition = new Vector3(98.5f, 0, 0);
        scrollbar.gameObject.SetActive(false);

        VerticalLayoutGroup verticalLayout = ContentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        OT.InitializeNoActionVerticalLayoutGroup(verticalLayout);
        verticalLayout.spacing = 4;

        ContentSizeFitter fitter = ContentTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BackgroundTransform.Find("Scrollgradient").GetComponent<Image>().enabled = false;

        _listPart = ContentTransform as RectTransform;
        _listItemPool = new ObjectPoolGenericMono<SimpleButton>(SimpleButton.Prefab, _listPart);
    }

    private void CreateInfoPanel()
    {
        var infoPart = new GameObject("InfoPart", typeof(Image), typeof(VerticalLayoutGroup));
        infoPart.transform.SetParent(BackgroundTransform);
        infoPart.transform.localPosition = new Vector3(100, 12, 0);
        infoPart.transform.localScale = Vector3.one;
        infoPart.GetComponent<Image>().sprite = SpriteTextureLoader.getSprite("ui/special/windowInnerSliced");
        infoPart.GetComponent<Image>().type = Image.Type.Sliced;

        RectTransform rectTransform = infoPart.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(320, 176);

        VerticalLayoutGroup layout = infoPart.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.padding = new RectOffset(10, 10, 10, 10);

        GameObject detailsObject = new GameObject("Details", typeof(Text));
        detailsObject.transform.SetParent(infoPart.transform);
        detailsObject.transform.localScale = Vector3.one;
        _detailsText = detailsObject.GetComponent<Text>();
        OT.InitializeCommonText(_detailsText);
        _detailsText.alignment = TextAnchor.UpperLeft;
        _detailsText.resizeTextForBestFit = true;
        _detailsText.resizeTextMinSize = 8;
        _detailsText.resizeTextMaxSize = 12;
        _detailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailsText.verticalOverflow = VerticalWrapMode.Overflow;
        _detailsText.text = "Pick a root and select a mod to see its details.";

        RectTransform detailsRect = _detailsText.GetComponent<RectTransform>();
        detailsRect.sizeDelta = new Vector2(300, 156);
    }

    private void CreateControlBar()
    {
        var controlPart = new GameObject("ControlPart", typeof(Image), typeof(HorizontalLayoutGroup));
        controlPart.transform.SetParent(BackgroundTransform);
        controlPart.transform.localPosition = new Vector3(100, -94, 0);
        controlPart.transform.localScale = Vector3.one;
        controlPart.GetComponent<Image>().sprite = InternalResourcesGetter.GetWindowEmptyFrame();
        controlPart.GetComponent<Image>().type = Image.Type.Sliced;

        RectTransform rectTransform = controlPart.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(320, 44);

        HorizontalLayoutGroup layout = controlPart.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = 6;
        layout.padding = new RectOffset(8, 8, 4, 4);

        SimpleButton loadButton = Instantiate(SimpleButton.Prefab, controlPart.transform);
        loadButton.Setup(() =>
        {
            if (_selectedCandidate == null)
            {
                SetStatus("Select a mod first.");
                return;
            }

            bool success = ExternalModHotLoadService.TryImportAndLoad(_selectedCandidate, out string message);
            SetStatus(message);
            WorldTip.showNow(message, !success, "top", success ? 3f : 4f);
            RefreshCandidates();
        }, null, "Import & Load", new Vector2(120, 28));

        SimpleButton refreshButton = Instantiate(SimpleButton.Prefab, controlPart.transform);
        refreshButton.Setup(RefreshCandidates, null, "Refresh", new Vector2(80, 28));

        SimpleButton openButton = Instantiate(SimpleButton.Prefab, controlPart.transform);
        openButton.Setup(() =>
        {
            if (_selectedCandidate == null)
            {
                SetStatus("Select a mod first.");
                return;
            }

            bool success = ExternalModHotLoadService.TryOpenInFileManager(_selectedCandidate.SourceFolderPath, out string message);
            SetStatus(message);
            WorldTip.showNow(message, !success, "top", success ? 3f : 4f);
        }, null, "Open Folder", new Vector2(98, 28));

        GameObject statusObject = new GameObject("Status", typeof(Text));
        statusObject.transform.SetParent(BackgroundTransform);
        statusObject.transform.localPosition = new Vector3(100, -130, 0);
        statusObject.transform.localScale = Vector3.one;
        _statusText = statusObject.GetComponent<Text>();
        OT.InitializeCommonText(_statusText);
        _statusText.alignment = TextAnchor.MiddleLeft;
        _statusText.resizeTextForBestFit = true;
        _statusText.resizeTextMinSize = 8;
        _statusText.resizeTextMaxSize = 12;
        _statusText.text = "Choose a folder root to scan for mods.";
        _statusText.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 24);
    }

    private void AddToolbarButton(Transform parent, string text, Action clickAction, float width)
    {
        SimpleButton button = Instantiate(SimpleButton.Prefab, parent);
        button.Setup(() => clickAction?.Invoke(), null, text, new Vector2(width, 28));
    }

    private void RefreshCandidates()
    {
        _listItemPool.clear();
        _selectedCandidate = null;

        string normalizedPath = ExternalModHotLoadService.NormalizePath(_currentRootPath ?? _pathInput.input.text);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            _detailsText.text = "Enter or choose a root folder first.";
            SetStatus("Enter or choose a root folder first.");
            return;
        }

        _currentRootPath = normalizedPath;
        RefreshPathInput();

        _candidates = ExternalModHotLoadService.Scan(normalizedPath);
        foreach (var candidate in _candidates)
        {
            SimpleButton button = _listItemPool.getNext();
            string title = candidate.Declaration.GetDisplayName();
            string author = candidate.Declaration.GetDisplayAuthor();
            if (title.Length > 18) title = title.Substring(0, 18);
            if (author.Length > 18) author = author.Substring(0, 18);
            button.Setup(() =>
            {
                _selectedCandidate = candidate;
                RefreshDetails();
            }, null, $"{title}\n{author}\n{candidate.Status}", new Vector2(160, 48));
        }

        if (_candidates.Count == 0)
        {
            _detailsText.text = $"No mod folders were found under:\n{normalizedPath}";
            SetStatus($"No mod folders were found under {normalizedPath}.");
        }
        else
        {
            _selectedCandidate = _candidates[0];
            RefreshDetails();
            SetStatus($"Found {_candidates.Count} mod(s) under {normalizedPath}.");
        }
    }

    private void RefreshDetails()
    {
        if (_selectedCandidate?.Declaration == null)
        {
            _detailsText.text = "Select a mod to inspect it.";
            return;
        }

        ModDeclare mod = _selectedCandidate.Declaration;
        string installedLine = string.IsNullOrWhiteSpace(_selectedCandidate.InstalledPath)
            ? ""
            : $"\nInstalled Path: {_selectedCandidate.InstalledPath}";

        _detailsText.text =
            $"Name: {mod.GetDisplayName()}\n" +
            $"Author: {mod.GetDisplayAuthor()}\n" +
            $"Version: {mod.Version}\n" +
            $"UID: {mod.UID}\n" +
            $"Status: {_selectedCandidate.Status}\n" +
            $"Source: {_selectedCandidate.SourceFolderPath}{installedLine}\n\n" +
            $"{mod.GetDisplayDesc()}";
    }

    private void RefreshPathInput()
    {
        if (_pathInput != null)
        {
            _pathInput.input.text = _currentRootPath ?? string.Empty;
        }
    }

    private string GetInitialRootPath()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents) && Directory.Exists(documents))
        {
            return documents;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private void SetStatus(string message)
    {
        _statusText.text = message;
        LogService.LogInfo(message);
    }
}
