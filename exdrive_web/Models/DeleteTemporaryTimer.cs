using System.Timers;

namespace exdrive_web.Models
{
    public class DeleteTemporaryTimer
    {
        int days;
        string containerName;
        public static System.Timers.Timer aTimer;
        static double interval24hours = 60 * 60 * 24 * 1000;

        public DeleteTemporaryTimer(int days, string containerName)
        {
            this.days = days;
            this.containerName = containerName;
        }

        public void SetTimer()
        {
            aTimer = new System.Timers.Timer(interval24hours * days);
            aTimer.Elapsed += new ElapsedEventHandler(checkForTime_Elapsed);
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
        private void checkForTime_Elapsed(object sender, ElapsedEventArgs e)
        {
              DeleteTemporary.DeleteTemporaryFiles(days, containerName);
        }
    }
}
