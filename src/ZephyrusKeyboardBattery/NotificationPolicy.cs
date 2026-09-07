namespace ZephyrusKeyboardBattery;

public sealed class NotificationPolicy
{
    private DateTimeOffset? _lastLowNotificationAt;
    private bool _wasLow;

    public bool ShouldNotify(BatteryReadResult result, DateTimeOffset now)
    {
        if (result.IsStale || result.Percent is not int percent)
        {
            return false;
        }

        if (percent > AppConstants.WarningThresholdPercent)
        {
            _wasLow = false;
            _lastLowNotificationAt = null;
            return false;
        }

        if (percent > AppConstants.LowThresholdPercent)
        {
            return false;
        }

        var shouldNotify = !_wasLow ||
                           _lastLowNotificationAt is null ||
                           now - _lastLowNotificationAt.Value >= AppConstants.LowBatteryRenotifyInterval;

        _wasLow = true;
        if (shouldNotify)
        {
            _lastLowNotificationAt = now;
        }

        return shouldNotify;
    }
}
