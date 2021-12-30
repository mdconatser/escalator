using System.Collections.Generic;
using System.Linq;

namespace Escalator.Managers
{
    public static class LogManager
    {
        private static List<string> _logs = new();

        public static void Log(string message)
        {
            _logs.Add(message);
        }

        public static List<string> GetLogs()
        {
            return _logs.ToList();
        }
    }
}
