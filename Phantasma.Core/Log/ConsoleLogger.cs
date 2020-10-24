﻿using System;

namespace Phantasma.Core.Log
{
    public class ConsoleLogger : Logger
    {
        private static object _lock = new object();

        public static bool AppendTimestamp = false;

        public override void Write(LogEntryKind kind, string msg)
        {
            if (AppendTimestamp)
            {
                msg = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + msg;
            }

            lock (_lock)
            {
                var color = Console.ForegroundColor;
                switch (kind)
                {
                    case LogEntryKind.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                    case LogEntryKind.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                    case LogEntryKind.Message: Console.ForegroundColor = ConsoleColor.Gray; break;
                    case LogEntryKind.Success: Console.ForegroundColor = ConsoleColor.Green; break;
                    case LogEntryKind.Debug: Console.ForegroundColor = ConsoleColor.Cyan; break;
                    default: return;
                }
                Console.WriteLine(msg);
                Console.ForegroundColor = color;
            }
        }
    }
}
