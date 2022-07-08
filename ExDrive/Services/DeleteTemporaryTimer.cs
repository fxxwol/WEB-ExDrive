using System.Timers;

namespace ExDrive.Services
{
    public class DeleteTemporaryTimer
    {
        public void SetTimer()
        {
            Timer = new System.Timers.Timer(interval24hours * Days);

            Timer.Elapsed += new ElapsedEventHandler(checkForTimeElapsed);

            Timer.AutoReset = true;

            Timer.Enabled = true;
        }
        private void checkForTimeElapsed(object? sender, ElapsedEventArgs e)
        {
            var deleteTemporary = new DeleteTemporary();
            deleteTemporary.DeleteTemporaryFiles(Days, ContainerName);
        }
        public DeleteTemporaryTimer(int days, string containerName)
        {
            Days = days;
            ContainerName = containerName;
        }
        private int Days { get; set; }
        private string ContainerName { get; set; }
        private System.Timers.Timer? Timer { get; set; }
        private readonly double interval24hours = 60 * 60 * 24 * 1000;
    }
}
