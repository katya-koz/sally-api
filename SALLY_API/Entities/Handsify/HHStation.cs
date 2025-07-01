namespace SALLY_API.Entities.Handsify
{
    public class HHStation
    {
        public struct Coords(double x, double y)
        {
            public double X { get; set; } = x ;
            public double Y { get; set; } = y;
        }
        public int StationKey { get; set; } // this is an internal sql id. needed to update
        public string StationName { get; set; } // eg 8.711, 8.110 hallway, etc
        public string Location { get; set; } // inside, outside, extra, workstation, etc
        public string StationType { get; set; } // sanitizer or soap
        public bool OnlineStatus { get; set; } // true - online, false - offline
        public int StationID { get; set; }
        public Coords Coordinates { get; set; }
        public double ModelResult { get; set; }
        public Dictionary<int, Note> Notes { get; set; } = new Dictionary<int, Note>(); // has author, createdate, and string content

        public HHStation()
        {
            //default constructor for newtonsoft json deserializer

        }

        public HHStation(string stationName, string location, string stationType, bool onlineStatus, int stationID, Dictionary<int, Note> notes, Coords coordinates, double modelResult, int stationKey)
        {
            StationKey = stationKey;
            StationName = stationName;
            StationType = stationType;
            Location = location;
            StationID = stationID;
            Notes = notes;
            ModelResult = modelResult;
            Coordinates = coordinates;
            OnlineStatus = onlineStatus;
        }

        public HHStation(string stationName, string location, string stationType, bool onlineStatus, int stationID, Dictionary<int, Note> notes)
        {
            // this is a temporary station constructor to be used in dev, randomizes coordinates and model result
            StationName = stationName;
            OnlineStatus = onlineStatus;
            StationType = stationType;
            Location = location;
            StationID = stationID;
            Notes = notes;

            Random rand = new Random();

            Coordinates = new Coords(rand.Next(0, 1200), rand.Next(0, 800));
            ModelResult = rand.NextDouble();
        }

        public override string ToString()
        {
            return $"StationID: {StationID}, " +
                   $"\nStationKey: {StationKey}, " +
                   $"\nStationName: {StationName}, " +
                   $"\nLocation: {Location}, " +
                   $"\nStationType: {StationType}, " +
                   $"\nOnlineStatus: {(OnlineStatus ? "Online" : "Offline")}, " +
                   $"\nCoordinates: (X: {Coordinates.X}, Y: {Coordinates.Y}), " +
                   $"\nModelResult: {ModelResult}, " +
                   $"\nNotesCount: {Notes.Count}";
        }
    }
}

