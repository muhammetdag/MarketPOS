using System.Media;

namespace MarketPOS.Services;

public static class SoundService
{
    private static bool _soundEnabled = true;

    public static bool SoundEnabled
    {
        get => _soundEnabled;
        set => _soundEnabled = value;
    }

    public static void PlaySuccessBeep()
    {
        if (!_soundEnabled) return;

        Task.Run(() =>
        {
            try
            {
                // High-pitch quick cashier beep (2000Hz, 80ms)
                Console.Beep(2400, 75);
            }
            catch
            {
                try { SystemSounds.Beep.Play(); } catch { }
            }
        });
    }

    public static void PlayErrorBeep()
    {
        if (!_soundEnabled) return;

        Task.Run(() =>
        {
            try
            {
                // Low-pitch double beep (800Hz, 150ms)
                Console.Beep(600, 150);
                Thread.Sleep(50);
                Console.Beep(450, 200);
            }
            catch
            {
                try { SystemSounds.Hand.Play(); } catch { }
            }
        });
    }

    public static void PlayCashDrawerSound()
    {
        if (!_soundEnabled) return;

        Task.Run(() =>
        {
            try
            {
                Console.Beep(1800, 100);
                Thread.Sleep(40);
                Console.Beep(2200, 150);
            }
            catch { }
        });
    }
}
