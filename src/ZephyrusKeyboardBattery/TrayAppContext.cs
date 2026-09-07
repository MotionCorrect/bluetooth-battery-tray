using System.Windows.Forms;

namespace ZephyrusKeyboardBattery;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly IBatteryReader _batteryReader;
    private readonly NotificationPolicy _notificationPolicy = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _checkNowItem;
    private readonly System.Windows.Forms.Timer _timer;
    private bool _refreshInProgress;

    public TrayAppContext(IBatteryReader batteryReader)
    {
        _batteryReader = batteryReader;

        _statusItem = new ToolStripMenuItem("Checking battery…") { Enabled = false };
        _checkNowItem = new ToolStripMenuItem("Check now", null, async (_, _) => await RefreshAsync(showCheckingState: true));
        _startupItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleStartup())
        {
            Checked = StartupRegistration.IsEnabled(),
            CheckOnClick = false
        };
        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitThread());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_checkNowItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Text = "Zephyrus keyboard: checking…",
            Icon = TrayIconFactory.Create(new BatteryReadResult(null, true, "Checking")),
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += async (_, _) => await RefreshAsync(showCheckingState: true);

        _timer = new System.Windows.Forms.Timer { Interval = (int)AppConstants.PollInterval.TotalMilliseconds };
        _timer.Tick += async (_, _) => await RefreshAsync(showCheckingState: false);
        _timer.Start();

        _ = RefreshAsync(showCheckingState: true);
    }

    private async Task RefreshAsync(bool showCheckingState)
    {
        if (_refreshInProgress)
        {
            return;
        }

        _refreshInProgress = true;
        _checkNowItem.Enabled = false;

        try
        {
            if (showCheckingState)
            {
                _statusItem.Text = "Checking battery…";
                _notifyIcon.Text = "Zephyrus keyboard: checking…";
            }

            var result = await _batteryReader.ReadAsync(TimeSpan.FromSeconds(20));
            ApplyStatus(result);
        }
        finally
        {
            _checkNowItem.Enabled = true;
            _refreshInProgress = false;
        }
    }

    private void ApplyStatus(BatteryReadResult result)
    {
        var displayLine = result.ToDisplayLine();
        StatusStore.Write(result);
        _statusItem.Text = displayLine;
        _notifyIcon.Text = TrimTooltip(displayLine);

        var oldIcon = _notifyIcon.Icon;
        _notifyIcon.Icon = TrayIconFactory.Create(result);
        oldIcon?.Dispose();

        if (_notificationPolicy.ShouldNotify(result, DateTimeOffset.Now) && result.Percent is int percent)
        {
            _notifyIcon.ShowBalloonTip(
                10_000,
                "Keyboard battery low",
                $"{AppConstants.KeyboardDisplayName} battery is at {percent}%.",
                ToolTipIcon.Warning);
        }
    }

    private void ToggleStartup()
    {
        if (StartupRegistration.IsEnabled())
        {
            StartupRegistration.Disable();
        }
        else
        {
            StartupRegistration.Enable();
        }

        _startupItem.Checked = StartupRegistration.IsEnabled();
    }

    private static string TrimTooltip(string text)
    {
        const int maxNotifyIconTextLength = 63;
        return text.Length <= maxNotifyIconTextLength ? text : text[..maxNotifyIconTextLength];
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _timer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
