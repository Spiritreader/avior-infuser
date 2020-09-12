using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recording_Infuser_Windows
{
    class Log
    {
        private readonly string LogName = "Infuser.log";
        private List<string> log;
        private bool dbLogs;

        public Log()
        {
            log = new List<string>();
        }

        public void LogAdd(string message)
        {
            Console.WriteLine(message);
            log.Add(message);
        }

        public void DatabaseLogs(bool enable)
        {
            dbLogs = enable;
        }

        /// <summary>
        /// Writes log to file in UTF-8 file format
        /// </summary>
        /// <param name="success">Whether the logfile should display a success or error message</param>
        public void LogWrite(bool success)
        {
            var culture = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = culture;
            Console.WriteLine(CultureInfo.CurrentCulture);
            using (TextWriter w = File.AppendText(LogName))
            {
                if (success)
                {
                    if (dbLogs)
                    {
                        w.WriteLine("Infuser Parse Success (db enabled): " + DateTime.Now.ToString());
                    }
                    else
                    {
                        w.WriteLine("Infuser Parse Success (db disabled): " + DateTime.Now.ToString());
                    }                    
                }
                else
                {
                    w.WriteLine("Infuser Error: " + DateTime.Now.ToString());
                }
                log.Add("");
                log.ForEach(elem => w.WriteLine(elem));

                log.Clear();
            }
        }
    }
}
