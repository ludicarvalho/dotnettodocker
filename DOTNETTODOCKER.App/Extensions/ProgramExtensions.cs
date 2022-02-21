namespace DOTNETTODOCKER.App.Extensions
{
    public static class ProgramExtensions
    {
        public static int ToInteger(this string n)
        {
            if (string.IsNullOrWhiteSpace(n) || !int.TryParse(n, out int result))
                return default;

            return result;
        }
    }
}
