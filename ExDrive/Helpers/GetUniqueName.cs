namespace ExDrive.Helpers
{
    public class GetUniqueName
    {
        public string GetName(string oldName)
        {
            return Guid.NewGuid().ToString() + new FindFileFormat().FindFormat(oldName);
        }
    }
}
