namespace ZephyrusKeyboardBattery;

public sealed class NotificationPolicy(AppSettings settings)
{
    private DateTimeOffset? _lastLowNotificationAt;
    private bool _wasLow;

    public bool ShouldNotify(BatteryReadResult result, DateTimeOffset now)
    {
        if (result.IsStale || result.Percent is not int percent)
        {
            return false;
        }

        if (percent > settings.WarningThresholdPercent)
        {
            _wasLow = false;
            _lastLowNotificationAt = null;
            return false;
        }

        if (percent > settings.LowThresholdPercent)
        {
            return false;
        }

        var shouldNotify = !_wasLow ||
                           _lastLowNotificationAt is null ||
                           now - _lastLowNotificationAt.Value >= settings.LowBatteryRenotifyInterval;

        _wasLow = true;
        if (shouldNotify)
        {
            _lastLowNotificationAt = now;
        }

        return shouldNotify;
    }
}
