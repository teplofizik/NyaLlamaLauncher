using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace QwenLauncher;

public sealed class MainForm : Form
{
    private readonly AppConfig _cfg;
    private readonly LlamaServer _server = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };
    private readonly System.Windows.Forms.Timer _healthTimer = new() { Interval = 3000 };

    private TextBox _txtServerExe = null!;
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
        _cfg = AppConfig.Load();

        Text = "Qwen3.8 Launcher";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 640);
        Size = new Size(1000, 820);
        Font = new Font("Segoe UI", 9f);

        BuildUi();
        LoadConfigIntoUi();

        _server.Output += OnServerOutput;
        _server.Exited += OnServerExited;
        _healthTimer.Tick += (_, _) => CheckHealth();

        Shown += (_, _) =>
        {
            _split.SplitterDistance = 470;
            StartPosToBottom();
        };

        FormClosing += OnFormClosing;
    }

    private SplitContainer _split = null!;

    // ------------------------------------------------------------------ UI

    private void BuildUi()
    {
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(8, 6, 8, 6) };

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };

        _btnStart = MakeButton("▶  Запустить", 120, (_, _) => StartServer(), Color.FromArgb(46, 125, 50));
        _btnStop = MakeButton("■  Остановить", 120, (_, _) => StopServer(), Color.FromArgb(150, 50, 50));
        _btnRestart = MakeButton("⟳  Перезапуск", 120, (_, _) => RestartServer(), Color.FromArgb(60, 70, 110));
        _btnOpenUi = MakeButton("🌐  Web UI", 100, (_, _) => OpenWebUi(), Color.FromArgb(60, 70, 110));
        _btnCopy = MakeButton("⧉  Config opencode", 150, (_, _) => CopyOpencodeConfig(), Color.FromArgb(60, 70, 110));

        _btnStop.Enabled = false;

        buttons.Controls.AddRange(new Control[] { _btnStart, _btnStop, _btnRestart, _btnOpenUi, _btnCopy });

        _lblStatus = new Label
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 12, 6, 0),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Text = "Сервер остановлен"
        };

        toolbar.Controls.Add(buttons);
        toolbar.Controls.Add(_lblStatus);

        _split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 6 };

        var settingsHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        settingsHost.Controls.Add(BuildSettings());

        _split.Panel1.Controls.Add(settingsHost);
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

        _txtServerExe = new TextBox();
        _txtModel = new TextBox();
        _txtMmproj = new TextBox();
        _txtTemplate = new TextBox();

        AddRow(t, "llama-server.exe", FilePicker(_txtServerExe, "Программа сервера (*.exe)|*.exe|Все файлы (*.*)|*.*"));
        AddRow(t, "Модель (.gguf)", FilePicker(_txtModel, "Модель Qwen (*.gguf)|*.gguf|Все файлы (*.*)|*.*"));
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
        var opts = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        opts.Controls.AddRange(new Control[] { _chkFlash, _chkWebUi, _chkReasoning, _chkContextShift });
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
        c.Items.AddRange(new object[] { "bf16", "q8_0", "q4_0" });
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

    // ------------------------------------------------------------- behavior

    private void LoadConfigIntoUi()
    {
        _txtServerExe.Text = _cfg.ServerExe;
        _txtModel.Text = _cfg.ModelPath;
        _txtMmproj.Text = _cfg.MmprojPath;
        _txtTemplate.Text = _cfg.TemplatePath;
        _txtAlias.Text = _cfg.Alias;
        _txtHost.Text = _cfg.Host;
        _txtPort.Text = _cfg.Port.ToString();
        _txtCtx.Text = _cfg.ContextSize.ToString();
        _cmbCacheK.Text = _cfg.CacheTypeK;
        _cmbCacheV.Text = _cfg.CacheTypeV;
        _txtNgl.Text = _cfg.GpuLayers;
        _txtThreads.Text = _cfg.Threads;
        _txtApiKey.Text = _cfg.ApiKey;
        _txtExtra.Text = _cfg.ExtraArgs;
        _chkFlash.Checked = _cfg.FlashAttn;
        _chkWebUi.Checked = _cfg.WebUi;
        _chkReasoning.Checked = _cfg.Reasoning;
        _chkContextShift.Checked = _cfg.ContextShift;
    }

    private void ReadUiIntoConfig()
    {
        _cfg.ServerExe = _txtServerExe.Text.Trim();
        _cfg.ModelPath = _txtModel.Text.Trim();
        _cfg.MmprojPath = _txtMmproj.Text.Trim();
        _cfg.TemplatePath = _txtTemplate.Text.Trim();
        _cfg.Alias = _txtAlias.Text.Trim();
        _cfg.Host = _txtHost.Text.Trim();
        _cfg.Port = int.TryParse(_txtPort.Text.Trim(), out var p) ? p : 8001;
        _cfg.ContextSize = int.TryParse(_txtCtx.Text.Trim(), out var c) ? c : 65536;
        _cfg.CacheTypeK = _cmbCacheK.Text.Trim();
        _cfg.CacheTypeV = _cmbCacheV.Text.Trim();
        _cfg.GpuLayers = _txtNgl.Text.Trim();
        _cfg.Threads = _txtThreads.Text.Trim();
        _cfg.ApiKey = _txtApiKey.Text.Trim();
        _cfg.ExtraArgs = _txtExtra.Text.Trim();
        _cfg.FlashAttn = _chkFlash.Checked;
        _cfg.WebUi = _chkWebUi.Checked;
        _cfg.Reasoning = _chkReasoning.Checked;
        _cfg.ContextShift = _chkContextShift.Checked;
    }

    private void StartServer(bool quiet = false)
    {
        if (_server.IsRunning)
        {
            if (!quiet) MessageBox.Show(this, "Сервер уже запущен.", "Qwen3.8 Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ReadUiIntoConfig();

        if (!File.Exists(_cfg.ServerExe))
        {
            MessageBox.Show(this, "Не найден llama-server.exe:\n" + _cfg.ServerExe, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!File.Exists(_cfg.ModelPath))
        {
            MessageBox.Show(this, "Не найден файл модели:\n" + _cfg.ModelPath, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (IsPortOpen(_cfg.Host, _cfg.Port))
        {
            MessageBox.Show(this,
                $"Порт {_cfg.Port} уже занят.\n\nВозможно, llama-server уже запущен (например, другим окном). " +
                "Закройте его или смените порт.",
                "Порт занят", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _cfg.Save();
        _log.Clear();

        AppendLine("=== Запуск сервера ===");
        AppendLine(LlamaServer.BuildCommandLine(_cfg));
        AppendLine("");

        try
        {
            _server.Start(_cfg);
        }
        catch (Exception ex)
        {
            AppendLine("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateButtons();
            return;
        }

        _btnStart.Enabled = false;
        _btnStop.Enabled = true;
        SetStatus("Загрузка модели…", Color.FromArgb(230, 160, 60));
        _healthTimer.Start();
    }

    private void StopServer()
    {
        _healthTimer.Stop();
        if (_server.IsRunning) AppendLine("=== Остановка сервера ===");
        _server.Stop();
        UpdateButtons();
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
            UpdateButtons();
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
        if (!_server.IsRunning)
        {
            _healthTimer.Stop();
            return;
        }

        var host = _cfg.Host;
        if (host is "0.0.0.0" or "::" or "") host = "127.0.0.1";
        var url = $"http://{host}:{_cfg.Port}/health";

        try
        {
            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            if ((int)resp.StatusCode == 200 && body.Contains("\"ok\""))
            {
                SetStatus($"Готов  →  {BaseUrl()}", Color.FromArgb(90, 200, 90));
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

    private string BaseUrl()
    {
        var host = _cfg.Host;
        if (host is "0.0.0.0" or "::" or "") host = "127.0.0.1";
        return $"http://{host}:{_cfg.Port}";
    }

    private void OpenWebUi()
    {
        try
        {
            Process.Start(new ProcessStartInfo(BaseUrl()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyOpencodeConfig()
    {
        ReadUiIntoConfig();
        var host = _cfg.Host;
        if (host is "0.0.0.0" or "::" or "") host = "127.0.0.1";

        var snippet = new
        {
            provider = new Dictionary<string, object>
            {
                ["llama.cpp"] = new
                {
                    npm = "@ai-sdk/openai-compatible",
                    name = "llama.cpp (local)",
                    options = new { baseURL = $"http://{host}:{_cfg.Port}/v1", apiKey = _cfg.ApiKey },
                    models = new Dictionary<string, object>
                    {
                        [_cfg.Alias] = new { name = _cfg.Alias + " (local)" }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(snippet, new JsonSerializerOptions { WriteIndented = true });
        try
        {
            Clipboard.SetText(json);
            AppendLine("--- Конфиг для opencode.json скопирован в буфер обмена ---");
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

        if (_chkAutoScroll.Checked)
        {
            _log.SelectionStart = _log.TextLength;
            _log.ScrollToCaret();
        }
    }

    private void StartPosToBottom()
    {
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_server.IsRunning)
        {
            var res = MessageBox.Show(this, "Сервер запущен. Остановить его и закрыть?", "Qwen3.8 Launcher",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
        }

        ReadUiIntoConfig();
        _cfg.Save();
        _healthTimer.Stop();
        _server.Dispose();
        _http.Dispose();
    }
}
