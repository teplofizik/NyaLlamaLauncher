using System.Diagnostics;
using System.Net.Http;
using NyaLlamaLauncher.Core;
using NyaLlamaLauncher.Core.Runners;

namespace NyaLlamaLauncher.UI;

public sealed class MainForm : Form
{
    private readonly AppConfig _config;
    private readonly ServerProcess _server = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };
    private readonly System.Windows.Forms.Timer _healthTimer = new() { Interval = 3000 };

    private LaunchProfile _current = null!;
    private LaunchProfile? _running;
    private bool _loading;

    // выбор нейронки
    private ComboBox _cmbProfiles = null!;
    private Button _btnAddProfile = null!;
    private Button _btnDupProfile = null!;
    private Button _btnDelProfile = null!;
    private SplitContainer _split = null!;
    private Panel _settingsHost = null!;

    // поля профиля
    private TextBox _txtName = null!;
    private ComboBox _cmbRunner = null!;
    private TextBox _txtServerExe = null!;
    private TextBox _txtWorkDir = null!;
    private TextBox _txtModel = null!;
    private TextBox _txtMmproj = null!;
    private TextBox _txtTemplate = null!;
    private TextBox _txtAlias = null!;
    private TextBox _txtHost = null!;
    private TextBox _txtPort = null!;
    private TextBox _txtCtx = null!;
    private ComboBox _cmbCacheK = null!;
    private ComboBox _cmbCacheV = null!;
    private TextBox _txtNgl = null!;
    private TextBox _txtThreads = null!;
    private TextBox _txtApiKey = null!;
    private TextBox _txtExtra = null!;
    private CheckBox _chkFlash = null!;
    private CheckBox _chkWebUi = null!;
    private CheckBox _chkReasoning = null!;
    private CheckBox _chkContextShift = null!;
    private CheckBox _chkEmbedding = null!;

    // действия
    private Button _btnStart = null!;
    private Button _btnStop = null!;
    private Button _btnRestart = null!;
    private Button _btnOpenUi = null!;
    private Button _btnCopy = null!;
    private Label _lblStatus = null!;
    private RichTextBox _log = null!;
    private CheckBox _chkAutoScroll = null!;

    public MainForm()
    {
        _config = AppConfig.Load();
        _current = _config.Find(_config.SelectedProfileId) ?? _config.Profiles[0];

        Text = "NyaLlama Launcher";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 680);
        Size = new Size(1060, 860);
        Font = new Font("Segoe UI", 9f);

        BuildUi();
        RefreshProfileCombo(_current.Id);
        LoadProfileIntoUi(_current);

        _server.Output += OnServerOutput;
        _server.Exited += OnServerExited;
        _healthTimer.Tick += (_, _) => CheckHealth();

        Shown += (_, _) =>
        {
            _split.SplitterDistance = 480;
            ScrollLogToBottom();
        };

        FormClosing += OnFormClosing;
        UpdateButtons();
    }

    // ------------------------------------------------------------------ UI

    private void BuildUi()
    {
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 88,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 6, 8, 6)
        };
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        // строка 1 — выбор нейронки
        var row1 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = false };
        _cmbProfiles = new ComboBox
        {
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "Name",
            Margin = new Padding(0, 5, 6, 0)
        };
        _cmbProfiles.SelectedIndexChanged += (_, _) => OnProfileSelected();

        _btnAddProfile = MakeButton("＋ Добавить", 110, (_, _) => AddProfile(), Color.FromArgb(60, 70, 110));
        _btnDupProfile = MakeButton("⧉ Дублировать", 120, (_, _) => DuplicateProfile(), Color.FromArgb(60, 70, 110));
        _btnDelProfile = MakeButton("🗑 Удалить", 100, (_, _) => DeleteProfile(), Color.FromArgb(120, 60, 60));

        row1.Controls.AddRange(new Control[]
        {
            MakeInlineLabel("Нейронка:"), _cmbProfiles, _btnAddProfile, _btnDupProfile, _btnDelProfile
        });

        // строка 2 — действия
        var row2 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = false };
        _btnStart = MakeButton("▶  Запустить", 120, (_, _) => StartServer(), Color.FromArgb(46, 125, 50));
        _btnStop = MakeButton("■  Остановить", 120, (_, _) => StopServer(), Color.FromArgb(150, 50, 50));
        _btnRestart = MakeButton("⟳  Перезапуск", 120, (_, _) => RestartServer(), Color.FromArgb(60, 70, 110));
        _btnOpenUi = MakeButton("🌐  Web UI", 100, (_, _) => OpenWebUi(), Color.FromArgb(60, 70, 110));
        _btnCopy = MakeButton("⧉  Config opencode", 155, (_, _) => CopyOpencodeConfig(), Color.FromArgb(60, 70, 110));

        _lblStatus = new Label
        {
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 10, 6, 0),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Text = "Сервер остановлен"
        };

        row2.Controls.AddRange(new Control[] { _btnStart, _btnStop, _btnRestart, _btnOpenUi, _btnCopy, _lblStatus });

        toolbar.Controls.Add(row1, 0, 0);
        toolbar.Controls.Add(row2, 0, 1);

        _split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 6 };

        _settingsHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _settingsHost.Controls.Add(BuildSettings());

        _split.Panel1.Controls.Add(_settingsHost);
        _split.Panel2.Controls.Add(BuildLogPanel());

        Controls.Add(_split);
        Controls.Add(toolbar);
    }

    private Control BuildSettings()
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Padding = new Padding(10, 8, 10, 10)
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtName = new TextBox();
        _txtName.TextChanged += (_, _) => OnNameChanged();
        AddRow(t, "Имя профиля", _txtName);

        _cmbRunner = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 4, 3, 4) };
        foreach (var r in RunnerRegistry.All) _cmbRunner.Items.Add(r);
        _cmbRunner.DisplayMember = "DisplayName";
        _cmbRunner.SelectedIndexChanged += (_, _) => OnRunnerChanged();
        AddRow(t, "Движок", _cmbRunner);

        _txtServerExe = new TextBox();
        _txtWorkDir = new TextBox();
        _txtModel = new TextBox();
        _txtMmproj = new TextBox();
        _txtTemplate = new TextBox();

        AddRow(t, "Программа / сервер", FilePicker(_txtServerExe, "Программа (*.exe)|*.exe|Все файлы (*.*)|*.*"));
        AddRow(t, "Рабочий каталог", FolderPicker(_txtWorkDir));
        AddRow(t, "Модель (.gguf)", FilePicker(_txtModel, "Модель (*.gguf)|*.gguf|Все файлы (*.*)|*.*"));
        AddRow(t, "mmproj / vision (.gguf)", FilePicker(_txtMmproj, "Проектор (*.gguf)|*.gguf|Все файлы (*.*)|*.*"));
        AddRow(t, "Шаблон чата (.jinja)", FilePicker(_txtTemplate, "Шаблон (*.jinja)|*.jinja|Все файлы (*.*)|*.*"));

        _txtAlias = new TextBox();
        AddRow(t, "Алиас модели", _txtAlias);

        _txtHost = new TextBox();
        _txtPort = new TextBox();
        AddRow2(t, "Host", _txtHost, "Port", _txtPort);

        _txtCtx = new TextBox();
        _txtNgl = new TextBox();
        AddRow2(t, "Контекст (ctx-size)", _txtCtx, "GPU слоёв (-ngl)", _txtNgl);

        _cmbCacheK = CacheCombo();
        _cmbCacheV = CacheCombo();
        AddRow2(t, "KV cache K", _cmbCacheK, "KV cache V", _cmbCacheV);

        _txtThreads = new TextBox();
        _txtApiKey = new TextBox() { UseSystemPasswordChar = true };
        AddRow2(t, "Потоки CPU (-t)", _txtThreads, "API key", _txtApiKey);

        _chkFlash = new CheckBox { Text = "Flash attention", AutoSize = true, Checked = true };
        _chkWebUi = new CheckBox { Text = "Web UI", AutoSize = true, Checked = true };
        _chkReasoning = new CheckBox { Text = "Reasoning (deepseek)", AutoSize = true, Checked = true };
        _chkContextShift = new CheckBox { Text = "Context shift", AutoSize = true, Checked = true };
        _chkEmbedding = new CheckBox { Text = "Эмбеддинги (--embedding)", AutoSize = true };
        var opts = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        opts.Controls.AddRange(new Control[] { _chkFlash, _chkWebUi, _chkReasoning, _chkContextShift, _chkEmbedding });
        AddRow(t, "Опции", opts);

        _txtExtra = new TextBox();
        AddRow(t, "Доп. аргументы", _txtExtra);

        return t;
    }

    private Control BuildLogPanel()
    {
        var host = new Panel { Dock = DockStyle.Fill };

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(6, 3, 6, 3), AutoSize = false };
        var btnClear = new Button { Text = "Очистить лог", Width = 110, Height = 24 };
        btnClear.Click += (_, _) => _log.Clear();
        var btnSave = new Button { Text = "Сохранить лог", Width = 110, Height = 24 };
        btnSave.Click += (_, _) => SaveLog();
        _chkAutoScroll = new CheckBox { Text = "Автопрокрутка", AutoSize = true, Checked = true, Padding = new Padding(10, 4, 0, 0) };
        bar.Controls.AddRange(new Control[] { btnClear, btnSave, _chkAutoScroll });

        _log = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(18, 18, 20),
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 9.5f),
            WordWrap = false,
            HideSelection = false,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Both
        };

        host.Controls.Add(_log);
        host.Controls.Add(bar);
        return host;
    }

    private static Label MakeInlineLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Padding = new Padding(2, 10, 4, 0)
    };

    private static Button MakeButton(string text, int width, EventHandler onClick, Color back)
    {
        var b = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 6, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += onClick;
        return b;
    }

    private static ComboBox CacheCombo()
    {
        var c = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, Margin = new Padding(3, 4, 3, 4) };
        c.Items.AddRange(new object[] { "bf16", "f16", "q8_0", "q4_0" });
        return c;
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 8, 3, 3)
    };

    private void AddRow(TableLayoutPanel t, string label, Control field)
    {
        int row = t.RowCount++;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.Controls.Add(MakeLabel(label), 0, row);
        t.Controls.Add(field, 1, row);
    }

    private void AddRow2(TableLayoutPanel t, string label1, Control c1, string label2, Control c2)
    {
        int row = t.RowCount++;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var inner = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            Margin = new Padding(0)
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        inner.Controls.Add(MakeLabel(label1), 0, 0);
        inner.Controls.Add(c1, 1, 0);
        inner.Controls.Add(MakeLabel(label2), 2, 0);
        inner.Controls.Add(c2, 3, 0);

        t.Controls.Add(inner, 1, row);
    }

    private static Control FilePicker(TextBox box, string filter)
    {
        var inner = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Margin = new Padding(0)
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));

        box.Dock = DockStyle.Fill;
        box.Margin = new Padding(3, 4, 3, 4);

        var btn = new Button { Text = "Обзор…", Dock = DockStyle.Fill, Margin = new Padding(3, 4, 3, 4) };
        btn.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Filter = filter, CheckFileExists = true };
            try
            {
                if (File.Exists(box.Text)) dlg.InitialDirectory = Path.GetDirectoryName(box.Text);
            }
            catch { /* ignore */ }
            if (dlg.ShowDialog() == DialogResult.OK) box.Text = dlg.FileName;
        };

        inner.Controls.Add(box, 0, 0);
        inner.Controls.Add(btn, 1, 0);
        return inner;
    }

    private static Control FolderPicker(TextBox box)
    {
        var inner = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Margin = new Padding(0)
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));

        box.Dock = DockStyle.Fill;
        box.Margin = new Padding(3, 4, 3, 4);

        var btn = new Button { Text = "Обзор…", Dock = DockStyle.Fill, Margin = new Padding(3, 4, 3, 4) };
        btn.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog();
            try
            {
                if (Directory.Exists(box.Text)) dlg.SelectedPath = box.Text;
            }
            catch { /* ignore */ }
            if (dlg.ShowDialog() == DialogResult.OK) box.Text = dlg.SelectedPath;
        };

        inner.Controls.Add(box, 0, 0);
        inner.Controls.Add(btn, 1, 0);
        return inner;
    }

    // --------------------------------------------------------- профили

    private void RefreshProfileCombo(string? selectId = null)
    {
        _loading = true;
        try
        {
            _cmbProfiles.Items.Clear();
            foreach (var p in _config.Profiles) _cmbProfiles.Items.Add(p);
            _cmbProfiles.DisplayMember = "Name";

            var target = _config.Find(selectId ?? _current?.Id ?? _config.SelectedProfileId);
            if (target is not null) _cmbProfiles.SelectedItem = target;
            else if (_cmbProfiles.Items.Count > 0) _cmbProfiles.SelectedIndex = 0;
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnProfileSelected()
    {
        if (_loading) return;
        if (_cmbProfiles.SelectedItem is not LaunchProfile p) return;
        if (ReferenceEquals(p, _current)) return;

        FlushUiIntoProfile();
        _current = p;
        _config.SelectedProfileId = p.Id;
        LoadProfileIntoUi(p);
        _config.Save();
    }

    private void LoadProfileIntoUi(LaunchProfile p)
    {
        _loading = true;
        try
        {
            _txtName.Text = p.Name;
            SelectRunner(p.Runner);
            _txtServerExe.Text = p.ServerExe;
            _txtWorkDir.Text = p.WorkingDir;
            _txtModel.Text = p.ModelPath;
            _txtMmproj.Text = p.MmprojPath;
            _txtTemplate.Text = p.TemplatePath;
            _txtAlias.Text = p.Alias;
            _txtHost.Text = p.Host;
            _txtPort.Text = p.Port.ToString();
            _txtCtx.Text = p.ContextSize.ToString();
            _cmbCacheK.Text = p.CacheTypeK;
            _cmbCacheV.Text = p.CacheTypeV;
            _txtNgl.Text = p.GpuLayers;
            _txtThreads.Text = p.Threads;
            _txtApiKey.Text = p.ApiKey;
            _txtExtra.Text = p.ExtraArgs;
            _chkFlash.Checked = p.FlashAttn;
            _chkWebUi.Checked = p.WebUi;
            _chkReasoning.Checked = p.Reasoning;
            _chkContextShift.Checked = p.ContextShift;
            _chkEmbedding.Checked = p.Embedding;
        }
        finally
        {
            _loading = false;
        }
    }

    private void FlushUiIntoProfile()
    {
        if (_current is null) return;

        _current.Name = string.IsNullOrWhiteSpace(_txtName.Text) ? _current.Name : _txtName.Text.Trim();
        if (_cmbRunner.SelectedItem is IModelRunner r) _current.Runner = r.Id;
        _current.ServerExe = _txtServerExe.Text.Trim();
        _current.WorkingDir = _txtWorkDir.Text.Trim();
        _current.ModelPath = _txtModel.Text.Trim();
        _current.MmprojPath = _txtMmproj.Text.Trim();
        _current.TemplatePath = _txtTemplate.Text.Trim();
        _current.Alias = _txtAlias.Text.Trim();
        _current.Host = _txtHost.Text.Trim();
        _current.Port = int.TryParse(_txtPort.Text.Trim(), out var p) ? p : 8001;
        _current.ContextSize = int.TryParse(_txtCtx.Text.Trim(), out var c) ? c : 65536;
        _current.CacheTypeK = _cmbCacheK.Text.Trim();
        _current.CacheTypeV = _cmbCacheV.Text.Trim();
        _current.GpuLayers = _txtNgl.Text.Trim();
        _current.Threads = _txtThreads.Text.Trim();
        _current.ApiKey = _txtApiKey.Text.Trim();
        _current.ExtraArgs = _txtExtra.Text.Trim();
        _current.FlashAttn = _chkFlash.Checked;
        _current.WebUi = _chkWebUi.Checked;
        _current.Reasoning = _chkReasoning.Checked;
        _current.ContextShift = _chkContextShift.Checked;
        _current.Embedding = _chkEmbedding.Checked;
    }

    private void OnNameChanged()
    {
        if (_loading || _current is null) return;
        _current.Name = string.IsNullOrWhiteSpace(_txtName.Text) ? "Новый профиль" : _txtName.Text.Trim();
        RefreshProfileCombo(_current.Id);
        _config.Save();
    }

    private void OnRunnerChanged()
    {
        if (_loading || _current is null) return;
        if (_cmbRunner.SelectedItem is IModelRunner r)
        {
            _current.Runner = r.Id;
            _config.Save();
        }
    }

    private void SelectRunner(string? runnerId)
    {
        var runner = RunnerRegistry.Get(runnerId);
        var idx = _cmbRunner.Items.IndexOf(runner);
        if (idx >= 0) _cmbRunner.SelectedIndex = idx;
    }

    private void AddProfile()
    {
        FlushUiIntoProfile();

        var p = new LaunchProfile
        {
            Name = UniqueName("Новый профиль"),
            Runner = _current.Runner,
            ServerExe = _current.ServerExe,
            Host = "127.0.0.1",
            Port = NextFreePort(),
            ContextSize = 32768,
            CacheTypeK = "f16",
            CacheTypeV = "f16",
            FlashAttn = true,
            WebUi = true,
            Reasoning = true,
            ContextShift = true,
            ApiKey = ""
        };

        _config.Profiles.Add(p);
        _config.SelectedProfileId = p.Id;
        _current = p;
        RefreshProfileCombo(p.Id);
        LoadProfileIntoUi(p);
        _config.Save();
        UpdateButtons();
    }

    private void DuplicateProfile()
    {
        FlushUiIntoProfile();

        var p = _current.Clone();
        p.Name = UniqueName(_current.Name + " (копия)");
        _config.Profiles.Add(p);
        _config.SelectedProfileId = p.Id;
        _current = p;
        RefreshProfileCombo(p.Id);
        LoadProfileIntoUi(p);
        _config.Save();
        UpdateButtons();
    }

    private void DeleteProfile()
    {
        if (_config.Profiles.Count <= 1)
        {
            MessageBox.Show(this, "Нельзя удалить единственный профиль.", "NyaLlama Launcher",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var res = MessageBox.Show(this, $"Удалить профиль «{_current.Name}»?", "NyaLlama Launcher",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res != DialogResult.Yes) return;

        _config.Profiles.Remove(_current);
        _current = _config.Profiles[0];
        _config.SelectedProfileId = _current.Id;
        RefreshProfileCombo(_current.Id);
        LoadProfileIntoUi(_current);
        _config.Save();
        UpdateButtons();
    }

    private string UniqueName(string baseName)
    {
        var name = baseName;
        int i = 2;
        while (_config.Profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            name = $"{baseName} {i++}";
        return name;
    }

    private int NextFreePort()
    {
        int port = 8001;
        while (_config.Profiles.Any(p => p.Port == port) || IsPortOpen("127.0.0.1", port))
            port++;
        return port;
    }

    // ------------------------------------------------------- запуск

    private void StartServer(bool quiet = false)
    {
        if (_server.IsRunning)
        {
            if (!quiet) MessageBox.Show(this, "Сервер уже запущен.", "NyaLlama Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        FlushUiIntoProfile();
        _config.Save();

        var profile = _current;
        var runner = RunnerRegistry.Get(profile.Runner);

        var errors = runner.Validate(profile);
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join("\n", errors), "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (IsPortOpen(profile.Host, profile.Port))
        {
            MessageBox.Show(this,
                $"Порт {profile.Port} уже занят.\n\nВозможно, сервер уже запущен (например, другим окном). " +
                "Закройте его или смените порт.",
                "Порт занят", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var exe = runner.ResolveExecutable(profile);
        var wd = runner.ResolveWorkingDirectory(profile);
        var args = runner.BuildArguments(profile);

        _log.Clear();
        AppendLine($"=== Запуск: {profile.Name} [{runner.DisplayName}] ===");
        AppendLine(ArgTokenizer.JoinCommandLine(exe, args));
        AppendLine("");

        try
        {
            _server.Start(exe, wd, args);
        }
        catch (Exception ex)
        {
            AppendLine("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateButtons();
            return;
        }

        _running = profile.Clone();
        SetRunningUi(true);
        SetStatus("Загрузка модели…", Color.FromArgb(230, 160, 60));
        _healthTimer.Start();
    }

    private void StopServer()
    {
        _healthTimer.Stop();
        if (_server.IsRunning) AppendLine("=== Остановка сервера ===");
        _server.Stop();
        _running = null;
        SetRunningUi(false);
        SetStatus("Сервер остановлен", Color.FromArgb(150, 150, 150));
    }

    private void RestartServer()
    {
        StopServer();
        System.Windows.Forms.Timer? t = null;
        t = new System.Windows.Forms.Timer { Interval = 800 };
        t.Tick += (_, _) =>
        {
            t!.Stop();
            t.Dispose();
            StartServer(quiet: true);
        };
        t.Start();
    }

    private void OnServerExited()
    {
        if (IsDisposed) return;
        BeginInvoke(() =>
        {
            _healthTimer.Stop();
            _running = null;
            SetRunningUi(false);
            SetStatus("Сервер остановлен", Color.FromArgb(150, 150, 150));
            AppendLine("=== Процесс сервера завершён ===");
        });
    }

    private void OnServerOutput(string line)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(() => AppendLine(line));
        else AppendLine(line);
    }

    private async void CheckHealth()
    {
        if (!_server.IsRunning || _running is null)
        {
            _healthTimer.Stop();
            return;
        }

        var runner = RunnerRegistry.Get(_running.Runner);

        try
        {
            using var resp = await _http.GetAsync(runner.HealthUrl(_running));
            var body = await resp.Content.ReadAsStringAsync();
            if ((int)resp.StatusCode == 200 && body.Contains("\"ok\""))
            {
                SetStatus($"Готов  →  {runner.BaseUrl(_running)}", Color.FromArgb(90, 200, 90));
            }
            else
            {
                SetStatus("Загрузка модели…", Color.FromArgb(230, 160, 60));
            }
        }
        catch
        {
            SetStatus("Загрузка модели…", Color.FromArgb(230, 160, 60));
        }
    }

    private void SetRunningUi(bool running)
    {
        _btnStart.Enabled = !running;
        _btnStop.Enabled = running;
        _btnRestart.Enabled = running;

        _cmbProfiles.Enabled = !running;
        _btnAddProfile.Enabled = !running;
        _btnDupProfile.Enabled = !running;
        _btnDelProfile.Enabled = !running && _config.Profiles.Count > 1;
        _settingsHost.Enabled = !running;
        _btnOpenUi.Enabled = true;
        _btnCopy.Enabled = true;
    }

    private static bool IsPortOpen(string host, int port)
    {
        if (host is "0.0.0.0" or "::" or "") host = "127.0.0.1";
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var task = client.ConnectAsync(host, port);
            return task.Wait(400) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private void OpenWebUi()
    {
        var profile = _running ?? _current;
        var runner = RunnerRegistry.Get(profile.Runner);
        try
        {
            Process.Start(new ProcessStartInfo(runner.BaseUrl(profile)) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyOpencodeConfig()
    {
        FlushUiIntoProfile();
        _config.Save();

        var json = OpencodeProvider.BuildSnippet(_current);
        try
        {
            Clipboard.SetText(json);
            AppendLine("--- Конфиг opencode для профиля «" + _current.Name + "» скопирован в буфер ---");
            AppendLine(json);
        }
        catch (Exception ex)
        {
            AppendLine("ERROR: " + ex.Message);
        }
    }

    private void SaveLog()
    {
        using var dlg = new SaveFileDialog { Filter = "Лог (*.log)|*.log|Текст (*.txt)|*.txt", FileName = "llama-server.log" };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(dlg.FileName, _log.Text);
        }
    }

    private void UpdateButtons()
    {
        bool running = _server.IsRunning;
        _btnStart.Enabled = !running;
        _btnStop.Enabled = running;
        _btnRestart.Enabled = running;
        _btnDelProfile.Enabled = !running && _config.Profiles.Count > 1;
    }

    private void SetStatus(string text, Color color)
    {
        if (IsDisposed) return;
        _lblStatus.Text = text;
        _lblStatus.ForeColor = color;
    }

    private void AppendLine(string line)
    {
        if (_log.IsDisposed) return;

        var color = _log.ForeColor;
        var low = line.ToLowerInvariant();
        if (low.Contains("error") || low.Contains("failed") || low.Contains("out of memory") || low.Contains("abort") || low.Contains("assert"))
            color = Color.FromArgb(255, 120, 120);
        else if (low.Contains("warn"))
            color = Color.FromArgb(230, 200, 90);
        else if (low.Contains("listening") || low.Contains("server is listening") || low.Contains("starting the main loop"))
            color = Color.FromArgb(120, 230, 120);

        _log.SelectionStart = _log.TextLength;
        _log.SelectionLength = 0;
        _log.SelectionColor = color;
        _log.AppendText(line + Environment.NewLine);
        _log.SelectionColor = _log.ForeColor;

        if (_chkAutoScroll.Checked) ScrollLogToBottom();
    }

    private void ScrollLogToBottom()
    {
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_server.IsRunning)
        {
            var res = MessageBox.Show(this, "Сервер запущен. Остановить его и закрыть?", "NyaLlama Launcher",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
        }

        FlushUiIntoProfile();
        _config.Save();
        _healthTimer.Stop();
        _server.Dispose();
        _http.Dispose();
    }
}
