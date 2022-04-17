using System.Timers;

namespace exdrive_web.Models
{
    public class DeleteTemporaryTimer
    {
        public static System.Timers.Timer aTimer;
        static double interval24hours = 60 * 60 * 24 * 1000;

        public static void SetTimer()
        {
            aTimer = new System.Timers.Timer(interval24hours);
            aTimer.Elapsed += new ElapsedEventHandler(checkForTime_Elapsed);
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
        private static void checkForTime_Elapsed(object sender, ElapsedEventArgs e)
        {
              DeleteTemporary.DeleteTemporaryFiles();
        }
    }
}
