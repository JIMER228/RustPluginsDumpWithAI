using System;
using System.Threading;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("CountdownPlugin", "Zand3rs", "0.0.1")]
    [Description("A countdown plugin for a Rust Server")]

    class CountdownPlugin : RustPlugin
    {
        private Timer countdownTimer;

        void OnServerInitialized()
        {
            StartCountdown();
        }
        private void StartCountdown()
{
    Puts("Countdown started!");

    // Calculate the time until 07:00
    DateTime currentTime = DateTime.Now;
    DateTime targetTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 17, 0, 0);

    // Check if the target time is earlier than the current time
    if (targetTime <= currentTime)
    {
        // If so, set the target time for the next day
        targetTime = targetTime.AddDays(1);
    }

    TimeSpan initialTimeRemaining = targetTime - currentTime;

    // Start a timer to output a message every hour until 1 hour is left
    countdownTimer = timer.Repeat(60 * 60, 0, () => UpdateCountdown(targetTime));

    // If there is less than 1 hour left, adjust the timer
    if (initialTimeRemaining.TotalSeconds <= 60 * 60)
    {
        timer.Once((int)initialTimeRemaining.TotalSeconds, () => AdjustTimer(targetTime));
    }
}

        private void UpdateCountdown(DateTime targetTime)
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan timeRemaining = targetTime - currentTime;

            // Output the countdown message
            OutputCountdownMessage(timeRemaining);

            // If there is less than 1 hour left, adjust the timer
            if (timeRemaining.TotalSeconds <= 60 * 60)
            {
                AdjustTimer(targetTime);
            }
        }
        private void AdjustTimer(DateTime targetTime)
        {
            Puts("Adjusting timer...");

            // Stop the current timer
            countdownTimer.Destroy();

            // If there is less than 30 minutes left, set a new timer for every 30 minutes
            if (targetTime.Subtract(DateTime.Now).TotalMinutes <= 30)
            {
                countdownTimer = timer.Repeat(30 * 60, 0, () => UpdateCountdown(targetTime));
            }
            // If there is less than 10 minutes left, set a new timer for every minute
            else if (targetTime.Subtract(DateTime.Now).TotalMinutes <= 10)
            {
                countdownTimer = timer.Repeat(60, 0, () => UpdateCountdown(targetTime));
            }
            // If there is less than 30 seconds left, set a new timer for every second
            else if (targetTime.Subtract(DateTime.Now).TotalSeconds <= 30)
            {
                countdownTimer = timer.Repeat(1, 0, () => UpdateCountdown(targetTime));
            }
        }

        private void OutputCountdownMessage(TimeSpan timeRemaining)
        {
            // Output the countdown message
            Puts($"Server restart in: {timeRemaining.Hours} hours, {timeRemaining.Minutes} minutes, {timeRemaining.Seconds} seconds");

            // Broadcast the countdown to all players in the in-game chat

           // DISABLED FOR NOW DURING TESTING!!!!
           //rust.BroadcastChat("ADMIN", "Server restart in: {timeRemaining.Hours} hours, {timeRemaining.Minutes} minutes, {timeRemaining.Seconds} seconds");
        }
    }
}