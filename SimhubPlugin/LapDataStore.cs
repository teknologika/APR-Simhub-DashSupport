using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonFlatFileDataStore;

namespace APR.DashSupport {

    internal class LapDataStore {

        static readonly object threadSafeLock = new object();
        private static string lapDatafileName = "APR-LapDataStore.json";
        private static LapDataStore _instance;
        private static DataStore _store;
        private static IDocumentCollection<Lap> _laps;

        private LapDataStore() {

        }

        public static LapDataStore Instance() {
            lock (threadSafeLock) {
                if (_instance == null) {
                    _instance = new LapDataStore();
                }
                if (_store == null) {
                    _store = new DataStore(lapDatafileName);
                }
                _store.Reload();
                _laps = _store.GetCollection<Lap>();
            }
            return _instance;
        }

        public static void Load() {
            lock (threadSafeLock) {
                if (_store == null) {
                    _store = new DataStore(lapDatafileName);
                }
                _store.Reload();
                _laps = _store.GetCollection<Lap>();
            }
        }

        public IDocumentCollection<Lap> Laps {
            get {
                Load();
                return _laps;

            }
        }

        public static void Add(Lap lap) {
            Load();
            _laps.InsertOne(lap);
        }

        public static void AddLapAsync(Lap lap) {
            Load();
            _laps.InsertOneAsync(lap);
        }

        public static int Count {
            get {
                Load();
                return _laps.Count;
            }

        }

        public static void Save() {
            _store.UpdateAll(lapDatafileName);
        }


        private string secondsToString(double seconds, string format) {
            format.Replace(":", @"\:");
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.ToString(format);
        }

        private string secondsToString(double seconds) {
            return secondsToString(seconds, @"mm:ss.fff");
        }
    }

    public class Lap {
        Lap(int theSessionId, int theLapNumber, int thePosition, string theDriverId, TimeSpan theLapTime) {
            SessionId = theSessionId;
            LapNumber = theLapNumber;
            Position = thePosition;
            DriverId = theDriverId;
            LapTime = theLapTime;
            IsValid = true;
        }


        public int SessionId { get; set; }
        public int LapNumber { get; set; }
        public int Position { get; set; }
        public string DriverId { get; set; }
        public TimeSpan LapTime { get; set; }
        public bool IsValid { get; set; }
    }
}
