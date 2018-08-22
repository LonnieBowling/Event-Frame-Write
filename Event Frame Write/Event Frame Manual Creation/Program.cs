using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.EventFrame;
using OSIsoft.AF.Search;
using OSIsoft.AF.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EventFrames
{
    class Program
    {
        static void Main(string[] args)
        {

            NetworkCredential credential = new NetworkCredential(connectionInfo.user, connectionInfo.password);
            var piSystem = (new PISystems())[connectionInfo.AFServerName];
            piSystem.Connect(credential);
            var afdb = piSystem.Databases[connectionInfo.AFDatabaseName];

            var relayName = "T001";
            var operatorName = "Scotty";
            var reason = "Unplanned";
            //find element
            var relaySearch = new AFElementSearch(afdb, "Relay Search", $"Template: 'Antimatter Relay' Name: {relayName}");
            var relay = relaySearch.FindElements().FirstOrDefault();
            if (relay != null)
            {
                CreateDowntimeEvent(afdb, relay, operatorName, reason);
            }

            //find open eventframes
            var events = FindDowntimeEvents(afdb, relayName, true);

            //close eventframes
            CloseDowntimeEvents(afdb, events);

            Console.ReadKey();
        }

        public static void CloseDowntimeEvents(AFDatabase afdb, IEnumerable<AFEventFrame> events)
        {
            foreach (var item in events)
            {
                item.SetEndTime(DateTime.Now);
                Console.WriteLine($"Event Close {item.Name}, time: {item.StartTime} - {item.EndTime} Duration: {item.Duration}");
            }
            afdb.CheckIn(AFCheckedOutMode.ObjectsCheckedOutThisSession);
        }
        public static IEnumerable<AFEventFrame> FindDowntimeEvents(AFDatabase afdb, string relayName, bool InProgress)
        {
            var eventSearch = new AFEventFrameSearch(afdb, "Template Search", $"Template:'Downtime' Name:{relayName}* InProgress:{InProgress}");
            var events = eventSearch.FindEventFrames();
            foreach (var item in events)
            {
                Console.WriteLine($"Event Found {item.Name}, time: {item.StartTime} - {item.EndTime} Duration: {item.Duration}");
            }
            return events;
        }
        public static void CreateDowntimeEvent(AFDatabase afdb,AFElement relay, string operatorName, string reason )
        {
            
                //Creating Event Frame

                var downtimeReasons = afdb.EnumerationSets["Downtime Reason Codes"];
                var template = afdb.ElementTemplates["Downtime"];

                var eventFrame = new AFEventFrame(afdb, "manual creation", template);
                eventFrame.PrimaryReferencedElement = relay;
                eventFrame.SetStartTime(DateTime.Now);
                eventFrame.Attributes["Operator"].SetValue(new AFValue(operatorName));
                eventFrame.Attributes["Reason Code"].SetValue(new AFValue(downtimeReasons[reason]));
                AFNameSubstitution.ResolveName(eventFrame, template.NamingPattern);

                //commit changes
                afdb.CheckIn(AFCheckedOutMode.ObjectsCheckedOutThisSession);
                Console.WriteLine("Eventframe: " + eventFrame.Name);
        }

    }
}
