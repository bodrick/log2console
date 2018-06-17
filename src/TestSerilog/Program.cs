using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.Network;

namespace SerilogTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //System.Diagnostics.Debug.Listeners.Add(new MineTraceListener());
            //System.Diagnostics.Debug.Listeners.Add(new NLogTraceListener());
            Console.Title = "TestNLog";
            Console.WriteLine("Press x to exit, any other to run tests.");
            var key = Console.ReadKey();
            while (key.Key != ConsoleKey.X)
            {
                DoLog();
                DoWinDebug();


                Console.WriteLine("Press x to exit, any other to run tests.");
                key = Console.ReadKey();
            }
        }

        private static void DoWinDebug()
        {
            Console.WriteLine("Doing WinDebug!");

            Debug.WriteLine("This is a call to System.Diagnostics.Debug");
            Trace.WriteLine("This is a call to System.Diagnostics.Trace");
        }

        private static void DoLog()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.RollingFile(new JsonFormatter(), "log-{Date}.txt", retainedFileCountLimit: 2, buffered: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(5))
                .WriteTo.LiterateConsole()
                //.WriteTo.Udp(IPAddress.Loopback, 7071)
                .WriteTo.UDPSink(IPAddress.Loopback, 1337)
                .CreateLogger();

            var logger = Log.Logger.ForContext<Program>();

            logger.Verbose("Verbose");
            logger.Debug("Debug");
            logger.Information("Information");
            logger.Warning("Warning");
            logger.Error("Error");
            logger.Fatal("Fatal");

            var me = new Person("Duane", "123", "?");
            logger.Information("I just created {@Someone} at {Now}", me, DateTime.Now);
            var count = 1;
            logger.Information("Create {Count} people", count);
            var fruit = new[] {"Apple", "Pear", "Orange"};
            logger.Information("In my bowl I have {Fruit}", fruit);

            //Console.WriteLine("Enter a simple math statement.  Operators supported are (+,-,/,*)");
            //var line = "";
            //while ((line = Console.ReadLine()).ToLower() != "q")
            //{
            //    Domath(line);
            //}
            Domath("10/5");
            Domath("8%2");
            Domath("99/0");

            var person = new Person("Person1", "123", "private info")
            {
                Addresses = new List<Address>
                {
                    new Address
                    {
                        Line1 = "Address1"
                    },
                    new Address
                    {
                        Line1 = "Address2"
                    }
                }
            };
            logger.Information("This person has multiple addresses {@Person}, 1st one is {@Address}", person,
                person.Addresses.FirstOrDefault());
            logger.Information("This person has multiple addresses {Person}, 1st one is {Address}", person,
                person.Addresses.FirstOrDefault());

            logger.Information("This bool is {True}", true);
            logger.Information("The number is {One}", 1);
            logger.Information("The time is {Now}", DateTime.Now);
            logger.Information("Googles uri is {Uri}", new Uri("http://google.com/"));

            logger.Information("Don't try to serialize this {$Person}", person);

            Log.Warning("No context set on this one");
        }

        private static void Domath(string math)
        {
            var match = Regex.Match(math, @"(\d+)\s*(\D)\s*(\d+)");
            if (match.Groups.Count == 4)
            {
                Log.Information("Attempting to process {Operand1}{Operator}{Operand2}", match.Groups[1],
                    match.Groups[2],
                    match.Groups[3]);
                try
                {
                    var operand1 = int.Parse(match.Groups[1].Value);
                    var operand2 = int.Parse(match.Groups[3].Value);
                    double answer;
                    switch (match.Groups[2].Value)
                    {
                        case "+":
                            answer = operand1 + operand2;
                            break;
                        case "-":
                            answer = operand1 - operand2;
                            break;
                        case "/":
                            answer = (double)operand1 / operand2;
                            break;
                        case "*":
                            answer = operand1 * operand2;
                            break;
                        default:
                            throw new InvalidOperationException("Unknown operator");
                    }

                    Log.Information("{Operand1}{Operator}{Operand2}={Answer}", operand1, match.Groups[2], operand2,
                        answer);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error does not compute");
                }
            }
        }
    }
}