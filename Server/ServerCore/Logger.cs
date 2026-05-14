using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public interface IPacketHandlerLogger
    {
        void Log(string message, [CallerMemberName] string memberName = "");
    }

    public class ConsoleLogger : IPacketHandlerLogger
    {
        public void Log(string message, [CallerMemberName] string memberName = "")
        {
            Console.WriteLine($"[Log] {memberName}: {message}");
        }
    }
}
